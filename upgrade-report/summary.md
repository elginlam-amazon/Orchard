# 📊 Executive Report: Orchard CMS Upgrade to .NET 10  
  
## 1. 🧭 Executive Summary  
- 🎯 **Goal:** move Orchard CMS 1.x (`/data/code/src`) from .NET Framework 4.8 to **.NET 10**. The source stack was ASP.NET MVC 5, Web API 2, OWIN, Autofac 3, NHibernate and System.Web.  
- ✅ **Outcome:** **all 89 projects build with 0 errors** on .NET 10.  
  - This holds for `dotnet build src/Orchard.sln` and for each project built on its own.  
  - A full rebuild leaves about 100 warnings, nearly all from legacy packages and code-style analyzers.  
- 🔄 **Process:** the run started on 13 Oct 2026 at 13:24 UTC.  
  - **Microsoft Upgrade Assistant** converted the projects to SDK style: 254 operations succeeded, 0 failed, 162 skipped.  
  - **Kiro CLI** did the rest: rewriting the System.Web-dependent code, fixing every build error, migrating the tests, and validating the result.  
- 🧪 **Validation:** unit tests were run with `dotnet test`.  
  - The real request pipeline was run in process through ASP.NET Core `TestServer`, from warmup through tenant routing, MVC and the Orchard view engine to Razor compilation.  
  - The 9 web-hosting specs pass.  
  - The remaining test failures trace to a missing database engine, Windows-only paths, or packages that have no .NET version.  
- ⚠️ **Not done yet:** the legacy Razor views (`.cshtml`) were not migrated.  
  - Pages will not render until they are.  
  - This is the main item in Next Steps.  
  
## 2. 🏗️ Application Changes  
- 📦 **Project system:**  
  - All `.csproj` files are SDK style.  
  - `packages.config` files were deleted (69) and replaced by pinned `PackageReference`s.  
- 🎯 **Target frameworks:**  
  - `NHibernate.Linq` targets `netstandard2.0`.  
  - `MSBuild.Orchard.Tasks` targets both `net10.0` and `netstandard2.0`.  
  - The Framework, Core, all modules and themes, and WarmupStarter target `net10.0` only. They need the ASP.NET Core shared framework, which `netstandard2.0` cannot reference (KB 01 Step 4).  
- 🌐 **Web host:** `Global.asax` was replaced by `Program.cs`, which wires Kestrel, MVC (convention routing), Razor runtime compilation, cookie authentication, session and antiforgery.  
  - `Web.config` is now an IIS (ASP.NET Core Module) configuration.  
  - App settings moved to `App.config`.  
- 🧩 **Framework rewrites:**  
  - Routing (`IRouter`-based tenant routes).  
  - Tenant middleware pipeline (OWIN replaced by `IApplicationBuilder`).  
  - Controller activation and filters.  
  - Model binding.  
  - View engines.  
  - Autofac 3 → 9.  
  - Security (forms auth replaced by cookie auth; PBKDF2 hashes stay compatible).  
  - WCF moved to CoreWCF.  
- 🔌 **Modules:**  
  - OpenId moved from OWIN to ASP.NET Core authentication, with MSAL for Azure AD.  
  - OutputCache uses response buffering and `MemoryCache`.  
  - Packaging uses NuGet 7.9.  
  - Azure uses Blobs SDK v12.  
  - Images use SkiaSharp.  
  - Glimpse is reduced to a diagnostics log because Glimpse has no .NET version.  
  - SysCache uses `CoreMemoryCache`.  
- 🛠️ **Tools:**  
  - The warmup HTTP module became middleware.  
  - The command line used an AppDomain host; it now runs Orchard inside its own process.  
  - The specs run on `TestServer` and moved from SpecFlow to Reqnroll.  
  
## 3. 🧰 Tools Used  
- ⚙️ **.NET SDK 10.0.401:** `dotnet build`, `dotnet test` and restore, with framework packages pinned to `10.0.12`.  
- 🔁 **Microsoft .NET Upgrade Assistant 1.0.518.26217:** in-place SDK-style conversion of the project files and the first pass of code transformers.  
- 🤖 **Kiro CLI 2.14.2** with model **claude-opus-5.5**:  
  - Fixed build errors and rewrote APIs.  
  - Ran tests and triaged the failures.  
  - Wrote the reports.  
  - Used about 620 tool invocations in this session (mostly shell, plus file read and write and the knowledge base).  
- 📚 **Supporting sources:**  
  - The modernization knowledge base (`/app/knowledge`), for migration patterns and package rules.  
  - CodeKB, an index of the pre-upgrade code, used to understand the original behaviour.  
  - Custom Python bulk-rewrite scripts for namespace and API changes.  
  
## 4. 💻 Code Changes  
- 📈 **Scale:**  
  - 975 files changed: 779 modified, 188 deleted, 46 added, 6 renamed.  
  - +7,441 / −43,177 lines. Most of the deletions are generated SpecFlow files, `packages.config` files and dead System.Web code.  
- 🔀 **API migration:**  
  - `HttpContextBase` → `HttpContext`.  
  - `RouteBase` → `IRouter`.  
  - `IController` → `ControllerBase`.  
  - `ActionResult` and filter context types.  
  - `TempData` and `Session` use JSON serialization.  
  - `ValueProviderResult` semantics.  
- 🐞 **Runtime bugs found and fixed during validation:**  
  - 🔁 The host container could not be created: a circular dependency through logger injection. Injection is now deferred until the whole resolve completes, matching Autofac 3.  
  - 🧱 Shape methods failed on their second call. Component resolution now goes through the registry so sharing applies.  
  - ❓ 27 `ValueProviderResult == null` checks were always false. They affected the feed queries and projection forms.  
  - 💥 A DynamicForms loop cast form entries to `string` and would have thrown on every form post.  
  - 🎮 MVC controllers derived from `ControllerBase` were not being registered.  
  - 🧩 An internal Autofac module was being picked up as a tenant module.  
- 🔐 **Security and hygiene:**  
  - Vulnerable SharpZipLib 0.86 was upgraded to 1.4.2, which cleared the NU1902/NU1903 advisories.  
  - `BinaryFormatter` and `RNGCryptoServiceProvider` were removed, along with the obsolete crypto factory calls.  
- 🧪 **Tests:**  
  - NUnit 2 → 3.14 and RhinoMocks → Moq 4.20.  
  - Stubs for `HttpContext`, routes and controllers were rewritten for ASP.NET Core.  
  - Tests of deleted code were replaced: `OrchardControllerActivatorTests` and `StaticFileAccessPolicyTests`.  
- 📊 **Test results on Linux:**  
  - ✅ Tokens 69/69, Tools 13/13, Web 4/4, Specs WebHosting 9/9.  
  - 🟡 Framework 607 passed / 180 failed: 151 need SQL Server Compact, 24 use Windows `\` paths, 5 are ICU culture differences.  
  - 🟡 Modules 291 passed / 166 failed: 102 need SQL Server Compact, 64 need IronRuby, which cannot start on .NET.  
  - 🟡 Core, Projections and Messaging failures all need SQL Server Compact.  
  - ⏭️ The Azure tests were not run because they need the storage emulator.  
  
## 5. ⏱️ Time Savings Estimate  
- 🧮 **Scope:** 89 projects, about 975 files, and a framework-level rewrite.  
  - The rewrite covers routing, DI, the hosting model, authentication, view engines and test infrastructure.  
  - A further 9 subsystems had no drop-in replacement: OWIN, Web API, AppDomain hosting, Glimpse, SpecFlow, WCF, ADAL, ImageResizer and NuGet.Core.  
- 👩‍💻 **Manual estimate:** about **10–14 engineer-weeks**, assuming senior engineers who know ASP.NET Core.  
  - Project conversion and package pinning: ~1–2 weeks.  
  - Framework and host rewrite: ~4–5 weeks.  
  - Module fixes across 70+ modules: ~3–4 weeks.  
  - Test and spec infrastructure: ~2–3 weeks.  
- 🤖 **Automated effort:** the final Kiro session ran for about **3.7 hours** (223 minutes, 1,125 credits). Earlier sessions came before it, and the whole run took about one working day.  
- 🚀 **Estimated saving:** roughly **85–90% of the hands-on effort** for reaching a compiling, test-running baseline. This is an estimate, not a measurement.  
- ⚠️ **Not included:** view migration, runtime and UAT validation, and production cutover. These still need human effort; see below.  
  
## 6. 🔜 Next Steps  
- 🖼️ **Migrate the Razor views** in Core, modules and themes to ASP.NET Core Razor.  
  - Remove `@helper`.  
  - Update the `Html.*` and `Url.*` overloads and the layout helpers.  
  - Use `Setup.feature` as the end-to-end gate.  
- 🧪 **Runtime validation:**  
  - Multi-tenancy: URL prefix, host headers, named routes.  
  - All OpenId providers.  
  - The output cache under load.  
  - Razor references to module assemblies that are loaded at runtime.  
- 🗄️ **Test database:** replace SQL Server Compact in the test utilities with SQLite or containerized SQL Server, and make the file-system tests path-separator neutral.  
- 📦 **Blocked packages:**  
  - Replace IronRuby (Scripting.Dlr) and Mono.CSharp with Roslyn scripting, or retire those modules.  
  - Upgrade Lucene.Net 3.0.3 to 4.8.  
  - Replace XMLDiffPatch.  
  - Confirm that MSTranslitTools works on .NET.  
- 🔐 **Security and behaviour review:**  
  - Users must log in again because the old cookies are invalid.  
  - Session is now on for every request.  
  - Tenant collection order changed under Autofac 9 (now registration order); modules that relied on the old order need review.  
  - The Image Editor still accepts any TLS certificate, as before.  
  - Old output-cache and Redis entries must be cleared.  
- 🧹 **Warnings:** replace the 8 `WebRequest` uses with `IHttpClientFactory`, move the specs to Reqnroll context injection, and address the CA analyzer findings.  
- 🚢 **Deployment:**  
  - Update the publish profiles and `Web.*.config` transforms.  
  - Migrate or retire `Orchard.proj`.  
  - Add CI that runs `dotnet build` and `dotnet test` on Linux and Windows.  
- 📄 **Details:** see `/data/code/summary.md` for the full technical write-up.  

## Kiro credit usage

💳 Kiro CLI credits used: 3390.44  

## Codebase index refresh

- Outcome: Index current
- Reason: Index current at 8c47962e3c1ae025fc65d585f41d0ad9897d90b2.
- Previously indexed commit: 8c47962e3c1ae025fc65d585f41d0ad9897d90b2
- Source commit at transformation start: 8c47962e3c1ae025fc65d585f41d0ad9897d90b2
- Duration: 0.0s

## Requirements register refresh

- Outcome: Timed out
- Reason: Register refresh exceeded 300000ms.
- Duration: 300.1s

> The requirements register was not refreshed for this transformation, so feature parity was checked against the **previous** register content. The transformation proceeded regardless.

## Feature Parity Verification

> **Parity results missing.** The feature-parity check did not produce a report, so parity could not be verified. Transformation sign-off is blocked.
>
> Detail: Parity re-derivation was aborted (300s refresh deadline) before the upgraded analysis was obtained.
