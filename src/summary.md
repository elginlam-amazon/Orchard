# .NET Framework 4.8 → net10.0 Migration Summary

## Status: ZERO Compilation Errors

The full solution (`Orchard.sln`) compiles with `0 Error(s)` on .NET 10.

---

## Unresolved Runtime Issues

The following items compile successfully but will not function correctly at runtime on .NET 10 without further work:

### 1. Orchard.Specs — In-Process ASP.NET Hosting

**Files:** `Orchard.Specs/Hosting/WebHost.cs`, `WebHostAgent.cs`, `RequestExtensions.cs`

The integration test suite used `ApplicationHost.CreateApplicationHost` and `HttpRuntime.ProcessRequest` to run a full ASP.NET 4.x application in-process. These cross-AppDomain APIs do not exist in .NET Core.

**Impact:** All SpecFlow integration tests in `Orchard.Specs` will fail at runtime because requests cannot actually be processed via the old `SimpleWorkerRequest` path.

**Required work:** Rewrite the test host infrastructure to use `Microsoft.AspNetCore.TestHost` / `WebApplicationFactory<T>` from `Microsoft.AspNetCore.Mvc.Testing`.

---

### 2. OutputCache & Redis — BinaryFormatter Replacement

**Files:** `Orchard.OutputCache/Services/FileSystemOutputCacheProvider.cs`, `Orchard.Redis/OutputCache/RedisOutputCacheStorageProvider.cs`

`BinaryFormatter` (used for cache item serialization) was removed in .NET 9/10. It was replaced with `Newtonsoft.Json` serialization. The serialization format changed, so existing cache files written by the .NET Framework version will not be readable.

**Impact:** Cache entries written by old builds cannot be deserialized. Rolling deployments (old → new) will cause cache misses for all existing entries. After full switchover this resolves itself.

---

### 3. Orchard.OutputCache — `HttpRuntime.UsingIntegratedPipeline`

**File:** `Orchard.OutputCache/Filters/OutputCacheFilter.cs` (lines 246, 613, 620)

`HttpRuntime.UsingIntegratedPipeline` was replaced with `true` (hard-coded). On .NET Core this is semantically correct (there is only the integrated pipeline), but it cannot be overridden.

**Impact:** None expected. .NET Core always uses an integrated pipeline equivalent.

---

### 4. Orchard.Recipes / Orchard.Setup / Orchard.ImportExport — ScriptTimeout

**Files:** `RecipeManager.cs`, `SetupController.cs`, `ExecuteRecipeAction.cs`

`HttpServerUtilityBase.ScriptTimeout` does not exist in SystemWebAdapters. The assignment to set the script timeout for long-running recipe execution was commented out.

**Impact:** Long-running recipe executions may time out prematurely if the host has a short default request timeout. Configure the ASP.NET Core request timeout via `RequestTimeoutMiddleware` or Kestrel timeout settings instead.

---

### 5. Orchard.Users — Forms Authentication

**Files:** `AuthenticationRedirectionFilter.cs`

`HttpResponseBase.SuppressFormsAuthenticationRedirect` does not exist in SystemWebAdapters. The assignment was commented out.

**Impact:** When a 401 response is generated, the runtime may redirect to the login page instead of returning the raw 401. This affects AJAX requests that check for 401 responses. Implement using `IStatusCodePagesMiddleware` or `IAuthenticationHandler` patterns in ASP.NET Core.

---

### 6. Orchard.Glimpse — Legacy Profiler

The Glimpse profiling module uses packages (`Glimpse`, `Glimpse.AspNet`, `Glimpse.Mvc5`, `Glimpse.Ado`) that were last updated in 2015 and target ASP.NET MVC 5. These packages restore via .NET Framework compatibility fallback.

**Impact:** The Glimpse profiler will not function on .NET Core. Consider replacing with a modern alternative like MiniProfiler or OpenTelemetry.

---

### 7. System.Web.Mvc HTML Helpers — System.Web.Mvc.Html Namespace

**Files:** `Orchard.AuditTrail/Helpers/HtmlExtensions.cs`, `Orchard.Roles/Services/AssignRoleUserManagementActionsProvider.cs`

`using System.Web.Mvc.Html;` was commented out as the namespace does not exist in the compatibility stubs (HTML extension methods are defined in `HtmlHelper` class directly). `ActionLink` and other HTML helpers work via the stub but are no-ops.

**Impact:** HTML helper calls compile but return empty strings at runtime (stub implementations). Full rendering requires a working MVC view engine.

---

### 8. Orchard.Templates — Razor Template Compilation

**File:** `Orchard.Templates/Services/Razor/RazorTemplateProcessor.cs`

The `RazorCodeLanguage`, `CSharpRazorCodeLanguage`, `RazorEngineHost`, `RazorTemplateEngine`, and `GeneratorResults` stubs in `System.Web.Razor` are no-ops. The runtime Razor template compilation via the old WebPages Razor APIs will not work.

**Impact:** The `Orchard.Templates` module's dynamic Razor template compilation feature will not function. Consider replacing with `Microsoft.AspNetCore.Razor.Language` and the Roslyn compiler.

---

### 9. Orchard.OpenId — Azure Active Directory

The OpenId module uses `Microsoft.IdentityModel.Clients.ActiveDirectory` (ADAL) which is deprecated. The Azure AD functionality may not work correctly with modern Azure AD endpoints.

**Impact:** Azure AD authentication via ADAL may fail. Microsoft recommends migrating to MSAL (`Microsoft.Identity.Client`).

---

### 10. OWIN Middleware

**File:** `Orchard/Owin/OwinCompat.cs`

The `GetOwinContext(this HttpContextBase)` extension method is a stub that returns `null`. Real OWIN context integration with ASP.NET Core requires using `IHttpContextAccessor` and ASP.NET Core middleware pipeline instead.

**Impact:** Any code that calls `HttpContext.GetOwinContext()` will receive `null` and will fail at runtime.

---

## Changes Made (Compilation Fixes)

### Core Infrastructure
- Added `Owin 1.0` and `Microsoft.Owin 4.2.3` packages to `Orchard.Framework.csproj`
- Added `Microsoft.Data.SqlClient 5.2.2` to `Orchard.Framework.csproj`
- Added `System.Drawing.Common 9.0.6` to `Orchard.MediaLibrary.csproj`
- Added `Glimpse.Ado 1.7.3` to `Orchard.Glimpse.csproj`
- Upgraded SpecFlow from 2.4.1 to 3.9.74 (with generation tools)
- Deleted stale SpecFlow 2.4.0-generated `.feature.cs` files (regenerated by 3.9.74)

### Compatibility Stubs (`Orchard/Mvc/SystemWebMvcCompat.cs`)
Major additions:
- `System.Web.Mvc.AuthorizeAttribute`, `OutputCacheAttribute`, `OutputCacheLocation`
- `System.Web.WebPages.WebPageContext`, `WebPageRenderingBase`
- `System.Web.Mvc.PartialView()` overloads, `OnActionExecuting/Executed`, `File()` overloads
- `System.Web.Mvc.FileResult` hierarchy (`FileResult`, `FileContentResult`, `FilePathResult`, `FileStreamResult`)
- `System.Web.Mvc.BindAttribute`, `DefaultModelBinder`, `ModelMetadataProviders`, `NameValueCollectionValueProvider`
- `System.Web.Mvc.Filters.IAuthenticationFilter`, `AuthenticationContext`, `AuthenticationChallengeContext`
- `System.Web.Security.Membership.GeneratePassword()`, `MembershipCreateStatus`
- `System.Web.Helpers.AntiForgeryConfig`, `Crypto`
- `System.Web.Razor.*` stubs: `RazorCodeLanguage`, `RazorEngineHost`, `RazorTemplateEngine`, `GeneratorResults`
- `System.Web.Http.*` result types: `IHttpActionResult`, `OkResult`, `NotFoundResult`, etc.
- `System.Web.Hosting.SimpleWorkerRequest` stub
- `System.Web.Routing.RouteCollectionMvcExtensions.IgnoreRoute()`
- `System.Web.WebPages.StringExtensions.IsEmpty()` extension
- `HtmlHelper.ActionLink(5-arg)` overload
- `UrlHelper.AbsoluteAction(Func<string>)` overload
- `Controller.Server` and `Controller.Session` properties
- `ViewDataDictionary.ModelState` property
- `Controller.View()` returns `ViewResult` (changed from `ActionResult`)

### OWIN Stubs (`Orchard/Owin/OwinCompat.cs`)
- Removed internal OWIN stubs (now provided by real `Owin 1.0` / `Microsoft.Owin 4.2.3` packages)
- Kept `AppBuilder` implementation
- Added `GetOwinContext()` extension stub

### Source File Fixes
- Replaced `HttpRuntime.UsingIntegratedPipeline` with `true` in `OutputCacheFilter.cs`
- Replaced `BinaryFormatter` with `Newtonsoft.Json` in `FileSystemOutputCacheProvider.cs` and `RedisOutputCacheStorageProvider.cs`
- Fixed `request.RequestContext.RouteData` → `request.RequestContext().RouteData` in multiple files
- Commented out unavailable APIs: `ScriptTimeout`, `SuppressFormsAuthenticationRedirect`, `System.Web.Mvc.Html`
- Fixed `using System.Data.SqlClient` → `using Microsoft.Data.SqlClient` in `SqlDatabaseConnectionStringAttribute.cs`
- Changed `using System.Web.Security` → `using Orchard.Security` in Users module files
- Added `Compile Remove` for `Core/`, `Modules/`, `Themes/` in `Orchard.Web.csproj`
- Added `GenerateAssemblyInfo>false` to `Orchard.Tests.Modules.csproj`
- Fixed Warmup module project reference path in `Orchard.Tests.Modules.csproj`
- Added `using System.Web.Mvc` to `FormTokens.cs` for `IsValidField` extension
- Changed `using Microsoft.AspNetCore.Mvc.ModelBinding` → `using System.Web.Mvc` in test `Values.cs`
- Added `using System.Web` to files missing `RequestContext()` extension method import
