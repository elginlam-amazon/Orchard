using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Orchard.Environment.Configuration;
using Orchard.Logging;
using Orchard.Mvc;
using Orchard.Mvc.Extensions;
using Orchard.Services;
using Orchard.Utility.Extensions;
using IHttpContextAccessor = Orchard.Mvc.IHttpContextAccessor;

namespace Orchard.Security.Providers
{
    public class FormsAuthenticationService : IAuthenticationService
    {
        /// <summary>
        /// The ASP.NET Core cookie authentication scheme of the Orchard users (formerly the forms authentication cookie).
        /// </summary>
        public const string AuthenticationScheme = "Orchard";

        private const string UserDataClaimType = "Orchard.UserData";
        private const string VersionClaimType = "Orchard.UserDataVersion";
        private const int _cookieVersion = 4;

        private readonly ShellSettings _settings;
        private readonly IClock _clock;
        private readonly IMembershipService _membershipService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ISslSettingsProvider _sslSettingsProvider;
        private readonly IMembershipValidationService _membershipValidationService;
        private readonly IEnumerable<IUserDataProvider> _userDataProviders;
        private readonly ISecurityService _securityService;

        private IUser _signedInUser;
        private bool _isAuthenticated;

        // This fixes a performance issue when the forms authentication cookie is set to a
        // user name not mapped to an actual Orchard user content item. If the request is
        // authenticated but a null user is returned, multiple calls to GetAuthenticatedUser
        // will cause multiple DB invocations, slowing down the request. We therefore
        // remember if the current user is a non-Orchard user between invocations.
        private bool _isNonOrchardUser;

        public FormsAuthenticationService(
            ShellSettings settings,
            IClock clock,
            IMembershipService membershipService,
            IHttpContextAccessor httpContextAccessor,
            ISslSettingsProvider sslSettingsProvider,
            IMembershipValidationService membershipValidationService,
            IEnumerable<IUserDataProvider> userDataProviders,
            ISecurityService securityService)
        {

            _settings = settings;
            _clock = clock;
            _membershipService = membershipService;
            _httpContextAccessor = httpContextAccessor;
            _sslSettingsProvider = sslSettingsProvider;
            _membershipValidationService = membershipValidationService;
            _userDataProviders = userDataProviders;
            _securityService = securityService;

            Logger = NullLogger.Instance;

            ExpirationTimeSpan = TimeSpan.Zero;
        }

        public ILogger Logger { get; set; }

        public TimeSpan ExpirationTimeSpan
        {
            get; set;
            // The public setter allows injecting this from Sites.MyTenant.Config or Sites.config, by using
            // an AutoFac component 
        }

        public TimeSpan GetExpirationTimeSpan()
        {
            if (ExpirationTimeSpan != TimeSpan.Zero)
            {
                // Basically here we are checking whether a value has been injected. If that is the case
                // that takes priority over possible services. The idea is to make the existence of those
                // services not-breaking, so that the introduction of new ones will not affect tenants where
                // the value from the Sites.Config file has been used. Implementers of those services should
                // take care of noting this in whatever UI they provide for their configuration, for the sake
                // of clarity towards whoever handles the tenant's configuration.
                return ExpirationTimeSpan;
            }
            return _securityService.GetAuthenticationCookieLifeSpan();
        }

        public void SignIn(IUser user, bool createPersistentCookie)
        {

            CreateAndAddAuthCookie(user, createPersistentCookie);

            _isAuthenticated = true;
            _isNonOrchardUser = false;
            _signedInUser = user;
        }

        public void SignOut()
        {
            _signedInUser = null;
            _isAuthenticated = false;

            // removing the authentication cookie for the given tenant (the cookie path is the tenant path base)
            var httpContext = _httpContextAccessor.Current();
            if (!httpContext.IsBackgroundContext())
            {
                httpContext.SignOutAsync(AuthenticationScheme).GetAwaiter().GetResult();
            }
        }

        public void SetAuthenticatedUserForRequest(IUser user)
        {
            _signedInUser = user;
            _isAuthenticated = true;
            _isNonOrchardUser = false;
        }

        public IUser GetAuthenticatedUser()
        {

            if (_isNonOrchardUser)
                return null;

            if (_signedInUser != null || _isAuthenticated)
                return _signedInUser;

            var httpContext = _httpContextAccessor.Current();
            var identity = httpContext.IsBackgroundContext() ? null : httpContext.User?.Identities.FirstOrDefault(i => i.IsAuthenticated && i.AuthenticationType == AuthenticationScheme);
            if (identity == null)
            {
                return null;
            }

            var userData = identity.FindFirst(UserDataClaimType)?.Value ?? "";
            Dictionary<string, string> userDataDictionary;
            try
            {
                userDataDictionary = DeserializeUserData(userData);
            }
            catch (Exception)
            {
                return null;
            }

            // 1. Take the username
            if (userDataDictionary == null || !userDataDictionary.ContainsKey("UserName"))
            {
                return null; // should never happen, unless the cookie has been tampered with
            }
            var userName = userDataDictionary["UserName"];
            _signedInUser = _membershipService.GetUser(userName);
            if (_signedInUser == null || !_membershipValidationService.CanAuthenticateWithCookie(_signedInUser))
            {
                _isNonOrchardUser = true;
                return null;
            }
            // 2. Check the other stuff from the dictionary
            var validLogin = _userDataProviders.All(udp => udp.IsValid(_signedInUser, userDataDictionary));
            if (!validLogin)
            {
                _signedInUser = null;
                return null;
            }

            _isAuthenticated = true;
            return _signedInUser;
        }

        private void CreateAndAddAuthCookie(IUser user, bool createPersistentCookie)
        {
            var httpContext = _httpContextAccessor.Current();
            if (httpContext.IsBackgroundContext())
            {
                return;
            }

            var now = _clock.UtcNow;

            var identity = new ClaimsIdentity(new[] {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(UserDataClaimType, ComputeUserData(user)),
                new Claim(VersionClaimType, _cookieVersion.ToString(System.Globalization.CultureInfo.InvariantCulture))
            }, AuthenticationScheme);

            var properties = new AuthenticationProperties
            {
                IsPersistent = createPersistentCookie,
                IssuedUtc = now,
                ExpiresUtc = now.Add(GetExpirationTimeSpan()),
                AllowRefresh = false
            };

            // The cookie path is the request path base, which contains the tenant url prefix,
            // and the cookie is marked secure when the request is.
            httpContext.SignInAsync(AuthenticationScheme, new ClaimsPrincipal(identity), properties).GetAwaiter().GetResult();
        }

        private Dictionary<string, string> ComputeUserDataDictionary(IUser user)
        {
            var userDataDictionary = new Dictionary<string, string>
            {
                { "UserName", user.UserName }
            };
            foreach (var userDataProvider in _userDataProviders)
            {
                var key = userDataProvider.Key;
                var value = userDataProvider.ComputeUserDataElement(user);
                if (key != null && value != null)
                {
                    userDataDictionary.Add(key, value);
                }
            }
            return userDataDictionary;
        }

        private string ComputeUserData(IUser user)
        {
            // serialize dictionary to userData string
            return SerializeUserDataDictionary(ComputeUserDataDictionary(user));
        }

        #region Serialization of UserData Dictionary
        // Use Newtonsoft.Json to handle this
        private string SerializeUserDataDictionary(IDictionary<string, string> userDataDictionary)
        {
            return JsonConvert.SerializeObject(userDataDictionary, Formatting.None);
        }

        private Dictionary<string, string> DeserializeUserData(string userData)
        {
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(userData);
        }

        #endregion
    }
}
