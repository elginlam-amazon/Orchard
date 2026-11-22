using System;

namespace Orchard.Security.Providers
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
    public class OrchardRoleProvider
    {
        public bool IsUserInRole(string username, string roleName) => throw new NotImplementedException();
        public string[] GetRolesForUser(string username) => throw new NotImplementedException();
        public void CreateRole(string roleName) => throw new NotImplementedException();
        public bool DeleteRole(string roleName, bool throwOnPopulatedRole) => throw new NotImplementedException();
        public bool RoleExists(string roleName) => throw new NotImplementedException();
        public void AddUsersToRoles(string[] usernames, string[] roleNames) => throw new NotImplementedException();
        public void RemoveUsersFromRoles(string[] usernames, string[] roleNames) => throw new NotImplementedException();
        public string[] GetUsersInRole(string roleName) => throw new NotImplementedException();
        public string[] GetAllRoles() => throw new NotImplementedException();
        public string[] FindUsersInRole(string roleName, string usernameToMatch) => throw new NotImplementedException();
        public string ApplicationName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }
}
