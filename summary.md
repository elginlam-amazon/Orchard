# Orchard 1.x → .NET 10 migration summary

## Result

- All 89 projects under `src/` are SDK-style and build with **0 errors** on .NET 10, both one by one (`dotnet build <project>`) and through `dotnet build src/Orchard.sln`. `Orchard.Messaging.Tests` is not in the solution and is built on its own.
- A full rebuild (`dotnet build src/Orchard.sln --no-incremental`) reports about 100 warnings. The breakdown is under "Remaining warnings".
- Unit tests run with `dotnet test`. Every failure left is environmental or caused by a blocked package (see "Test results"). Tests that need a database cannot run because SQL Server Compact does not exist on .NET or Linux.
- The application itself was not started (`dotnet run`). The specs, however, host the real Orchard pipeline in process through the ASP.NET Core `TestServer`. For the setup spec, the request goes through warmup, host, tenant routing, the tenant pipeline, MVC, the Setup controller, the Orchard view engine and Razor runtime compilation. It then fails while compiling the legacy Razor view (`@helper` is not supported). See "Next steps".

## Target frameworks

| Projects | TFM | Why |
|---|---|---|
| `NHibernate.Linq` | `netstandard2.0` | Library with no web dependency. |
| `Tools/MSBuild.Orchard.Tasks` | `net10.0;netstandard2.0` | Library. `Microsoft.Build.Utilities.Core` 18.0.2 is private and runtime-excluded. |
| `Orchard.Framework`, `Orchard.Core`, all modules and themes, `Orchard.WarmupStarter` | `net10.0` | **Single TFM**: they need the ASP.NET Core shared framework (`FrameworkReference Microsoft.AspNetCore.App`: MVC, routing, Razor, authentication). That framework does not exist for `netstandard2.0` (KB 01 Step 4). |
| `Orchard.Web` (host, `Microsoft.NET.Sdk.Web`), `Tools/Orchard` (command line) | `net10.0` | Executables. |
| Test projects (`*.Tests`, `Orchard.Specs`, `Orchard.Profile`) | `net10.0` | Test projects. NUnit 3.14.0, NUnit3TestAdapter 4.6.0, Microsoft.NET.Test.Sdk 17.14.1, Moq 4.20.72. |

Modules and themes still build to `bin\` (`AppendTargetFrameworkToOutputPath=false`) because Orchard loads them from `~/Modules/*/bin`. `Orchard.Web` also builds to `bin\`, which is where the command line and the specs look for it.

## Main changes

### Host (Orchard.Web)
- `Global.asax` was replaced by `Program.cs`, which contains `OrchardApplication.Create(WebApplicationBuilder)`. It does the following:
  - Registers MVC with `EnableEndpointRouting=false`, `OrchardControllerActivator`, `OrchardFilterProvider`, `OrchardModelBinderProvider`, `LocalizedModelValidatorProvider`, the area and API controller conventions, and `ThemeAwareViewEngineShim` as the only view engine.
  - Turns on Razor runtime compilation, including the in-memory templates of `Orchard.Templates`.
  - Registers the forms cookie authentication scheme `"Orchard"`, session and antiforgery.
  - Sets the 64 MB request limit.
- The request pipeline runs in this order:
  1. Warmup middleware.
  2. `X-Generator` header (this was the OWIN `Startup`).
  3. Static files.
  4. Application path.
  5. Host BeginRequest/EndRequest (this was `Application_BeginRequest`/`Application_EndRequest`).
  6. The tenant URL prefix is moved into `PathBase`.
  7. Session, authentication, then `UseMvc` with the Orchard route collection as the root router.
- Static files are served only from folders whose IIS `Web.config` enables `StaticFileModule` (`Hosting/StaticFileAccessPolicy.cs`). This keeps the old rule from the `Web.config` handlers, where nothing else was ever served.
- `Web.config` is now an ASP.NET Core Module (IIS) configuration. The app settings and connection strings moved to `App.config`, which the build copies to `Orchard.Web.dll.config` and which `ConfigurationManager` and the SMTP settings read.
- `_ViewImports.cshtml` declares the base page (`WebViewPage<dynamic>`) and the namespaces. These used to live in the `system.web.webPages.razor` sections.

### Framework (Orchard.Framework)
- Routing:
  - `Orchard.Mvc.Routes.Route`, `HubRoute`, `ShellRoute` and `OrchardRouteCollection` are now `IRouter` implementations.
  - `RoutePublisher` validates that route names are unique, as the old preloading `RouteCollection` did.
  - `OrchardRouteCollection` has a by-name indexer.
- The tenant pipeline is built from the `OwinMiddlewareRegistration`s, which now configure an `IApplicationBuilder`.
- Controllers are created by `OrchardControllerActivator`, which resolves them as `Keyed<ControllerBase>` from the work context scope. The composition treats any `ControllerBase` that is not an `ApiController` as an MVC controller (this used to be `IController`). Web API is merged into ASP.NET Core MVC.
- `System.Web` replacements:
  - `HostingEnvironment` and `VirtualPathUtility` are static.
  - `HttpContext` comes from the ASP.NET Core accessor.
  - The virtual path provider and monitor work on the physical file system.
  - Roslyn compiles the module projects that are deployed as source.
- Autofac 3 → 9:
  - Registration sources and modules are ported to the new pipeline API.
  - `ResolveComponent(registration)` goes through the registry's service registration, so sharing applies.
  - Logger and localizer property injection is deferred to the end of the resolve operation (`ActivatedHandlers.OnActivatedDeferred`), the same as the old `Activated` event. Without this the host container had a circular dependency: `DefaultHostEnvironment` → logger → log4net factory → `DefaultHostEnvironment`.
  - The legacy Autofac XML configuration files are read by an internal `ConfigurationSettingsReader`. It is internal on purpose: as a public module, the shell composition would pick it up.
  - The decorator registrations keep the order of the decorated components (`ConcurrentQueue` instead of `ConcurrentBag`).
- Security:
  - Forms authentication became cookie authentication.
  - The `System.Web.Helpers` PBKDF2 password hashing was re-implemented in `Crypto`, compatible with existing hashes.
  - Antiforgery uses `IAntiforgery`.
- WCF uses CoreWCF.
- NHibernate.Linq no longer has the WCF Data Services members (`IUpdatable`/`IExpandProvider`).

### Modules (highlights)

| Module | Change |
|---|---|
| OpenId | OWIN providers became ASP.NET Core authentication handlers, registered per tenant at runtime with scheme names prefixed by the tenant (`Security/OpenIdAuthenticationSchemes.cs`). Azure AD uses MSAL instead of ADAL. The retired Azure AD Graph client (`GetActiveDirectoryClient`) was removed; `IAzureActiveDirectoryService` exposes the Graph token. |
| OutputCache | Captures the rendered response by buffering the response body. The `ASP.NET` cache was replaced by a `MemoryCache`. `ResponseCacheAttribute` replaces `OutputCacheAttribute`. Cache key locks no longer rely on thread affinity. Metadata is JSON (this used to be `BinaryFormatter`). The Redis output cache also uses JSON. |
| Glimpse | Glimpse has no .NET successor. The instrumentation (decorators, timings, messages) is kept: messages are stored in the request items and written to the debug log. The Glimpse tabs, the runtime policy and the `SQL` (ADO) feature were removed. |
| Packaging | Moved to NuGet.Packaging/Protocol 7.9.0. |
| Azure | Moved to Azure.Storage.Blobs 12. |
| MediaLibrary, MediaProcessing | Use SkiaSharp. |
| SysCache | Uses `NHibernate.Caches.CoreMemoryCache` 5.9.0. SysCache2 depends on `System.Web`. |
| DynamicForms | Posted values and model errors are stored in TempData as JSON. ASP.NET Core TempData only accepts simple types. |
| Upgrade | The media queue is stored in the session as JSON. |
| Email | SMTP settings are read from the application configuration file. |

Other fixes:
- 27 checks of the form `ValueProviderResult == null` (always false on ASP.NET Core) were corrected to compare with `ValueProviderResult.None`. Examples are the feed queries and the projection forms.
- `FormService` enumerated `IFormCollection` as strings, which would throw at runtime.

### WarmupStarter, command line, specs
- **WarmupStarter**: `WarmupHttpModule` became `WarmupMiddleware`, which holds requests on a `TaskCompletionSource` while the host warms up. `Starter<T>` now drives the middleware.
- **Tools/Orchard**: the command host used to create an ASP.NET AppDomain (`ClientBuildManager`, `MarshalByRefObject`, remoting leases). It now loads `Orchard.Framework` from the installation `bin` folder into the process (KB 28, case B) and calls `CommandHostAgent` by reflection, as before.
- **Orchard.Specs**:
  - The sites run in process on the ASP.NET Core `TestServer` instead of an ASP.NET AppDomain with `SimpleWorkerRequest`.
  - SpecFlow 2.4 (end of life) was replaced by Reqnroll 3.3.4. Feature code-behind is generated at build time.
  - The `Simple.Web` Web Forms pages are now minimal endpoints at the same URLs.
  - HtmlAgilityPack 1.13 is configured with the legacy form parsing.
- **Orchard.Profile**: Reqnroll, plus `HttpClient` instead of `HttpWebRequest`.

## Behaviour changes to be aware of
- **Authentication**: the forms authentication cookie became the ASP.NET Core cookie `"Orchard"`, so existing login cookies are invalid. `AntiForgeryConfig.UniqueClaimTypeIdentifier` has no equivalent and was removed.
- **Razor views compile at runtime** with ASP.NET Core Razor. The legacy views (`.cshtml`) were **not** migrated (see "Next steps").
- **Session**: the per-route and per-module `SessionStateBehavior` is ignored. Session is enabled for every request.
- **Removed**:
  - The NHibernate configuration cache is no longer persisted to disk (`BinaryFormatter`).
  - The OWIN `Startup` was replaced by host middleware.
  - Packaging no longer goes through the solution `packages` folder.
  - The WCF Data Services members are gone from NHibernate.Linq.
- **Caches**: output cache items stored by the old version (BinaryFormatter files or Redis entries) cannot be read and must be cleared.
- **Images**: rendered with SkiaSharp. The output may differ slightly from System.Drawing/ImageResizer.
- **Collection order**: Autofac 9 resolves collections inside the shell containers in registration order, whereas Autofac 3 reversed them. The event handler order test was updated to match, so modules that relied on the reversed order need review.
- **Header**: X-Frame-Options is now added by the ASP.NET Core antiforgery (`SAMEORIGIN`).

## Test results (`dotnet test`, Linux)

| Project | Passed | Failed | Cause of the failures |
|---|---|---|---|
| Orchard.Framework.Tests | 607 | 180 | 151 need SQL Server Compact (`System.Data.SqlServerCe`, unavailable on .NET/Linux). 24 file system tests use Windows paths (`\`). 5 date formatting comparisons differ because Linux uses ICU culture data. |
| Orchard.Tests.Modules | 291 | 166 | 102 need SQL Server Compact. 64 are IronRuby tests, and IronRuby 1.1.3 cannot initialize on .NET. |
| Orchard.Core.Tests | 12 | 33 | All need SQL Server Compact. |
| Orchard.Projections.Tests / Orchard.Messaging.Tests | 0 | 12 / 1 | All need SQL Server Compact. |
| Orchard.Tokens.Tests, Tools/Orchard.Tests, Orchard.Web.Tests | 69, 13, 4 | 0 | — |
| Orchard.Specs (WebHosting feature) | 9 | 0 | Other features need a database and the migrated views. |
| Orchard.Azure.Tests | not run | — | Need the Azure Storage emulator. |

Tests whose code was deleted were replaced:
- `OrchardControllerFactoryTests` became `OrchardControllerActivatorTests`.
- The `Global.asax` route tests in Orchard.Web.Tests became `StaticFileAccessPolicyTests`.

## Remaining warnings (full rebuild)
- **NU1701**: these packages have no .NET build:
  - IronRuby 1.1.3 and Mono.CSharp 4.0.0.143 (scripting modules).
  - Lucene.Net 3.0.3.
  - XMLDiffPatch 1.0.8.28 (AuditTrail).
  - Orchard.MSTranslitTools 6.0.0, now a private reference.
  - Orchard.FluentPath 1.0.0.1 (tests).
- **SYSLIB0014**: 8 uses of `WebRequest`, which still works but is obsolete. They are in the reCAPTCHA, Akismet, warmup downloader, OEmbed, image profile and base URL check code.
- **CS0618**: Reqnroll `ScenarioContext.Current` in the specs.
- **Analyzer style warnings in existing code**: CA2263, CA1806, CA2200, CA1820, and SYSLIB0051 on the serialization constructors.

## Next steps
1. **Migrate the Razor views** (`*.cshtml` in Core, modules and themes) to ASP.NET Core Razor. This means removing `@helper`, replacing `Html.RenderPartial`/`Html.Action`/`Html.BeginForm` overloads and `Url.Action` with route value dictionaries, and adapting the `Layout`/`Display` helpers. The specs (`Setup.feature`) are the end-to-end check: the host pipeline already reaches view compilation.
2. **Runtime validation**:
   - Multi-tenancy (URL prefixes moved into `PathBase`, host headers) and named route URL generation.
   - The OpenId providers (Azure AD, ADFS, Google, Facebook, Twitter) and their per-tenant scheme registration.
   - The output cache under load.
   - Razor references to module assemblies that are loaded at runtime (application parts).
3. **Database for tests**: SQL Server Compact is not available on .NET. Move the test data utilities (`DataUtility`, `DatabaseEnabledTestsBase`) to SQLite or SQL Server LocalDB/containers. Make the file system tests path-separator neutral.
4. **Blocked packages**:
   - Replace Orchard.Scripting.Dlr (IronRuby, does not initialize on .NET) and Orchard.Scripting.CSharp (Mono.CSharp) with Roslyn scripting, or retire them.
   - Upgrade Lucene to Lucene.Net 4.8 (API change).
   - Replace XMLDiffPatch.
   - Confirm that MSTranslitTools loads on .NET.
5. **Glimpse**: decide whether to surface the collected diagnostics, for example through a MiniProfiler-style UI or OpenTelemetry, or to retire the module.
6. **Warnings**: replace the remaining `WebRequest` uses with `HttpClient` (`IHttpClientFactory`). Migrate the specs to Reqnroll context injection.
7. **Review**: the Image Editor proxy still accepts any server certificate, as it did before (`ServerCertificateCustomValidationCallback => true`). Decide whether that should remain.
8. **Deployment**: `Web.config` targets the ASP.NET Core Module (in-process). Publish profiles and the old `Web.*.config` transforms need updating. `Orchard.proj` (the legacy MSBuild packaging script) was not migrated.
