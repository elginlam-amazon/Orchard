using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Newtonsoft.Json;
using Orchard.Environment.Configuration;
using Orchard.Logging;
using Orchard.Mvc;
using Orchard.Mvc.Extensions;
using Orchard.Services;
using Orchard.Utility.Extensions;

namespace Orchard.Security.Providers
{
    public class FormsAuthenticationService : IAuthenticationService
    {
        private const int _cookieVersion = 4;
        private const string _orchardAuthCookieName = ".ORCHARDAUTH";

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

        public TimeSpan ExpirationTimeSpan { get; set; }

        public TimeSpan GetExpirationTimeSpan()
        {
            if (ExpirationTimeSpan != TimeSpan.Zero)
            {
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

            var httpContext = _httpContextAccessor.Current();
            var cookieName = GetCookieName();
            var rFormsCookie = new HttpCookie(cookieName, "")
            {
                Expires = DateTime.Now.AddYears(-1),
            };

            if (!string.IsNullOrEmpty(_settings.RequestUrlPrefix))
            {
                rFormsCookie.Path = GetCookiePath(httpContext);
            }

            httpContext.Response.Cookies.Add(rFormsCookie);
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
            if (httpContext.IsBackgroundContext() || !httpContext.Request.IsAuthenticated)
            {
                return null;
            }

            // Read and validate the auth cookie
            var cookieName = GetCookieName();
            var cookie = httpContext.Request.Cookies[cookieName];
            if (cookie == null)
                return null;

            Dictionary<string, string> userDataDictionary;
            try
            {
                userDataDictionary = DeserializeUserData(cookie.Value);
            }
            catch
            {
                return null;
            }

            if (!userDataDictionary.ContainsKey("UserName"))
                return null;

            var userName = userDataDictionary["UserName"];
            _signedInUser = _membershipService.GetUser(userName);
            if (_signedInUser == null || !_membershipValidationService.CanAuthenticateWithCookie(_signedInUser))
            {
                _isNonOrchardUser = true;
                return null;
            }

            var validLogin = _userDataProviders.All(udp => udp.IsValid(_signedInUser, userDataDictionary));
            if (!validLogin)
            {
                _signedInUser = null;
                return null;
            }

            _isAuthenticated = true;
            return _signedInUser;
        }

        private string GetCookieName()
        {
            if (!string.IsNullOrEmpty(_settings.RequestUrlPrefix))
                return _orchardAuthCookieName + "_" + _settings.Name;
            return _orchardAuthCookieName;
        }

        private HttpCookie CreateAndAddAuthCookie(IUser user, bool createPersistentCookie)
        {
            var now = _clock.UtcNow.ToLocalTime();
            var expiration = now.Add(GetExpirationTimeSpan());
            var userData = ComputeUserData(user);

            var cookieName = GetCookieName();
            var cookie = new HttpCookie(cookieName, SerializeUserDataDictionary(new Dictionary<string, string>
            {
                ["UserName"] = user.UserName,
                ["Data"] = userData,
                ["Version"] = _cookieVersion.ToString()
            }))
            {
                HttpOnly = true,
                Secure = _sslSettingsProvider.GetRequiresSSL(),
                Path = "/"
            };

            if (createPersistentCookie)
            {
                cookie.Expires = expiration;
            }

            var httpContext = _httpContextAccessor.Current();

            if (!string.IsNullOrEmpty(_settings.RequestUrlPrefix))
            {
                cookie.Path = GetCookiePath(httpContext);
            }

            httpContext.Response.Cookies.Add(cookie);
            return cookie;
        }

        private string GetCookiePath(HttpContextBase httpContext)
        {
            var cookiePath = httpContext.Request.ApplicationPath;
            if (cookiePath != null && cookiePath.Length > 1)
            {
                cookiePath += '/';
            }
            cookiePath += _settings.RequestUrlPrefix;
            return cookiePath;
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
            return SerializeUserDataDictionary(ComputeUserDataDictionary(user));
        }

        private string SerializeUserDataDictionary(IDictionary<string, string> userDataDictionary)
        {
            return JsonConvert.SerializeObject(userDataDictionary, Formatting.None);
        }

        private Dictionary<string, string> DeserializeUserData(string userData)
        {
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(userData);
        }
    }
}
