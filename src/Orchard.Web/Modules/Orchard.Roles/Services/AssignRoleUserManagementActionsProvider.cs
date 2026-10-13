using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Orchard.ContentManagement;
using Orchard.Localization;
using Orchard.Roles.Models;
using Orchard.Security;
using Orchard.Users.Services;
using Orchard.Utility.Extensions;

namespace Orchard.Roles.Services
{
    public class AssignRoleUserManagementActionsProvider : IUserManagementActionsProvider
    {
        private readonly IRoleService _roleService;
        private readonly IAuthorizationService _authorizationService;
        private readonly IAuthenticationService _authenticationService;
        private readonly IWorkContextAccessor _workContextAccessor;

        private Lazy<IEnumerable<RoleRecord>> _allRoles;

        public AssignRoleUserManagementActionsProvider(
            IRoleService roleService,
            IAuthorizationService authorizationService,
            IAuthenticationService authenticationService,
            IWorkContextAccessor workContextAccessor)
        {

            _roleService = roleService;
            _authorizationService = authorizationService;
            _authenticationService = authenticationService;
            _workContextAccessor = workContextAccessor;

            T = NullLocalizer.Instance;

            _allRoles = new Lazy<IEnumerable<RoleRecord>>(() => _roleService.GetRoles());
        }

        public Localizer T { get; set; }

        public IEnumerable<Func<IHtmlHelper, IHtmlContent>> UserActionLinks(IUser user)
        {
            // Get the user whose roles we want to assign
            var userRolesPart = user.As<UserRolesPart>();
            if (userRolesPart == null)
            {
                yield break;
            }
            var currentUser = _authenticationService.GetAuthenticatedUser();
            // Get the roles we are authorized to assign
            var authorizedRoleIds = _allRoles.Value
                .Where(rr => _authorizationService.TryCheckAccess(
                    Permissions.CreatePermissionForAssignRole(rr.Name),
                    currentUser,
                    userRolesPart))
                .Select(rr => rr.Id).ToList();
            // If the user has no roles they can assign, we will show nothing
            if (!authorizedRoleIds.Any())
            {
                yield break;
            }

            yield return (Func<IHtmlHelper, IHtmlContent>)
                (Html => Html.ActionLink(
                    T("Roles").ToString(),
                    "Assign",
                    new
                    {
                        Area = "Orchard.Roles",
                        Controller = "Admin",
                        id = user.Id,
                        returnUrl = Html.ViewContext.HttpContext.Request.GetRawUrl()
                    }));
        }
    }
}