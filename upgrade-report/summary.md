# .NET Version Upgrade - Code Transformation Summary

> ⚠️ **Post-upgrade build failed.** The upgraded code does not build: the post-upgrade verification build failed (see post-upgrade-verification.log). Fix the build errors before you validate or merge the pull request.


## 📋 Executive Summary

_(A line claiming the upgrade had succeeded was removed: the post-upgrade verification build failed.)_

**Risk Assessment:** 🔴 **HIGH RISK** - Build failure indicates compilation errors that require immediate attention. Manual intervention is required to complete this upgrade successfully.

## 🔧 Next Steps

1. **🚨 PRIORITY: Fix Build Errors** - Review and resolve all compilation errors in the upgraded code
2. **🔍 Review Code Changes** - Examine the 3,563 lines of code changes across 31 files for correctness
3. **📦 Validate Dependencies** - Verify that all 49 newly added dependencies are appropriate and properly configured
4. **✅ Verify Functionality** - Once build succeeds, conduct thorough testing to ensure application behavior is preserved
5. **📝 Update Documentation** - Document any breaking changes or migration considerations

## 📊 Key Metrics

| Metric | Value |
|--------|-------|
| **Framework** | .NET |
| **Transformation Engine** | DOTNET_UPGRADE_ASSISTANT |
| **Confidence Score** | 🔴 Low |
| **Transform Duration** | ⏱️ 22 minutes |
| **Files Changed** | 📄 31 |
| **Lines of Code Changed** | 📝 3,563 |
| **Dependencies Changed** | 📦 49 |
| **Estimated Time Saved** | ⏰ 0 minutes |

## 🏗️ Build Summary

**Status:** ❌ **FAILURE**

**The post-upgrade verification build FAILED.** The upgraded code does not compile successfully. Build errors must be fixed before proceeding with validation or deployment. This transformation cannot be considered complete until all compilation errors are resolved and a successful build is achieved.

## 🎯 Project Types

| Project Type | Count | Target Framework |
|--------------|-------|------------------|
| 📚 **Library Projects** | 88 | netstandard2.0 |
| 🚀 **Application Projects** | 1 | net10.0 |
| **Total Projects** | **89** | |

## 📦 Dependencies Summary

### ➖ Dependencies Removed (0)

No dependencies were removed during this upgrade.

### ➕ Dependencies Added (49)

- nuget:Microsoft.CSharp (version 4.7.0)
- nuget:Antlr3.Runtime (version 3.5.1)
- nuget:Iesi.Collections (version 4.1.1)
- nuget:NHibernate (version 5.6.0)
- nuget:Remotion.Linq (version 2.2.0)
- nuget:Remotion.Linq.EagerFetching (version 2.2.0)
- nuget:System.Configuration.ConfigurationManager (version 10.0.13)
- nuget:CoreWCF.Primitives (version 1.9.1)
- nuget:CoreWCF.ConfigurationManager (version 1.9.1)
- nuget:CoreWCF.Http (version 1.9.1)
- nuget:CoreWCF.WebHttp (version 1.9.1)
- nuget:CoreWCF.NetTcp (version 1.9.1)
- nuget:Autofac (version 9.3.4)
- nuget:Castle.Core (version 5.2.1)
- nuget:FluentNHibernate (version 3.4.1)
- nuget:IronRuby (version 1.1.3)
- nuget:Lucene.Net (version 4.8.0-beta00018)
- nuget:Moq (version 4.21.0)
- nuget:Newtonsoft.Json (version 13.0.4)
- nuget:NUnit (version 2.7.1)
- nuget:Orchard.FluentPath (version 1.0.0.1)
- nuget:Orchard.NuGet.Core (version 1.1.0.0)
- nuget:System.ComponentModel.Annotations (version 5.0.0)
- nuget:Microsoft.AspNetCore.SystemWebAdapters (version 2.3.0)
- nuget:Microsoft.AspNetCore.SystemWebAdapters.CoreServices (version 2.3.0)
- nuget:log4net (version 3.3.1)
- nuget:Microsoft.Azure.KeyVault.Core (version 3.0.5)
- nuget:Microsoft.Data.Edm (version 5.8.4)
- nuget:Microsoft.Data.OData (version 5.8.4)
- nuget:Microsoft.Data.Services.Client (version 5.8.4)
- nuget:Microsoft.Rest.ClientRuntime (version 2.3.24)
- nuget:Microsoft.Rest.ClientRuntime.Azure (version 3.3.19)
- nuget:System.Data.DataSetExtensions (version 4.5.0)
- nuget:System.Spatial (version 5.8.4)
- nuget:WindowsAzure.Storage (version 9.3.3)
- nuget:Autofac.Web (version 3.2.0)
- nuget:Microsoft.AspNet.WebApi.Client (version 6.0.0)
- nuget:Microsoft.AspNet.WebApi.Core (version 5.3.0)
- nuget:Newtonsoft.Json.Bson (version 1.0.2)
- nuget:NUnit.Runners (version 2.7.1)
- nuget:NUnitTestAdapter (version 2.3.0)
- nuget:System.Runtime.CompilerServices.Unsafe (version 6.1.2)
- nuget:SpecFlow (version 4.0.31-beta)
- nuget:SpecFlow.Tools.MsBuild.Generation (version 2.4.1)
- nuget:HtmlAgilityPack (version 1.13.1)
- nuget:Autofac.Configuration (version 8.0.1)
- nuget:Microsoft.AspNet.WebApi.WebHost (version 5.3.0)
- nuget:Microsoft.Owin (version 4.2.3)
- nuget:Owin (version 1.0)

### ⬆️ Dependencies Upgraded (0)

No dependencies were upgraded during this transformation.

## 📝 Code Changes

### Changed Files (31)

- src/Libraries/NHibernate/NHibernate.Linq/AssemblyInfo.cs
- src/Libraries/NHibernate/NHibernate.Linq/NHibernate.Linq.csproj
- src/Libraries/NHibernate/NHibernate.Linq/packages.config
- src/Orchard.Azure.Tests/Orchard.Azure.Tests.csproj
- src/Orchard.Azure.Tests/packages.config
- src/Orchard.Core.Tests/Orchard.Core.Tests.csproj
- src/Orchard.Core.Tests/packages.config
- src/Orchard.Profile/Orchard.Profile.csproj
- src/Orchard.Profile/packages.config
- src/Orchard.Specs/Orchard.Specs.csproj
- src/Orchard.Specs/packages.config
- src/Orchard.Tests.Modules/Orchard.Tests.Modules.csproj
- src/Orchard.Tests.Modules/Values.cs
- src/Orchard.Tests.Modules/packages.config
- src/Orchard.Tests/Orchard.Framework.Tests.csproj
- src/Orchard.Tests/packages.config
- src/Orchard.WarmupStarter/Orchard.WarmupStarter.csproj
- src/Orchard.Web.Tests/Orchard.Web.Tests.csproj
- src/Orchard.Web.Tests/packages.config
- src/Orchard.Web/Modules/Orchard.Projections/Tests/Orchard.Projections.Tests.csproj
- src/Orchard.Web/Modules/Orchard.Projections/Tests/packages.config
- src/Orchard.Web/Modules/Orchard.Tokens/Tests/Orchard.Tokens.Tests.csproj
- src/Orchard.Web/Modules/Orchard.Tokens/Tests/packages.config
- src/Orchard.sln
- src/Orchard/Orchard.Framework.csproj
- src/Orchard/WorkContextExtensions.cs
- src/Orchard/packages.config
- src/Tools/MSBuild.Orchard.Tasks/MSBuild.Orchard.Tasks.csproj
- src/Tools/Orchard.Tests/Orchard.Tests.csproj
- src/Tools/Orchard.Tests/packages.config
- src/Tools/Orchard/Orchard.csproj

**Total Lines Changed:** 3,563 lines across 31 files

## Codebase index refresh

- Outcome: Index current
- Reason: Index current at 8c47962e3c1ae025fc65d585f41d0ad9897d90b2.
- Previously indexed commit: 8c47962e3c1ae025fc65d585f41d0ad9897d90b2
- Source commit at transformation start: 8c47962e3c1ae025fc65d585f41d0ad9897d90b2
- Duration: 0.0s
