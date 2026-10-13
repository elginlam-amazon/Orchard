using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Orchard.Environment.Configuration;
using Orchard.Environment.Extensions;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.Mvc.Extensions;
using Orchard.OpenId.Security;
using IAuthenticationService = Orchard.Security.IAuthenticationService;
using Orchard.OpenId.Services;
using Orchard.Security;
using Orchard.Themes;
using Orchard.Users.Events;

namespace Orchard.OpenId.Controllers
{
    [Themed]
    [AlwaysAccessible]
    [OrchardFeature("Orchard.OpenId")]
    public class AccountController : Controller
    {
        private readonly IEnumerable<IOpenIdProvider> _openIdProviders;
        private readonly IAuthenticationService _authenticationService;
        private readonly IMembershipService _membershipService;
        private readonly IOrchardServices _orchardServices;
        private readonly IUserEventHandler _userEventHandler;

        private readonly ShellSettings _shellSettings;

        public AccountController(
            ShellSettings shellSettings,
            IEnumerable<IOpenIdProvider> openIdProviders,
            IAuthenticationService authenticationService,
            IMembershipService membershipService,
            IOrchardServices orchardServices,
            IUserEventHandler userEventHandler)
        {

            _shellSettings = shellSettings;
            _openIdProviders = openIdProviders;
            _authenticationService = authenticationService;
            _membershipService = membershipService;
            _orchardServices = orchardServices;
            _userEventHandler = userEventHandler;

            Logger = NullLogger.Instance;
            T = NullLocalizer.Instance;
        }

        public ILogger Logger { get; set; }
        public Localizer T { get; set; }

        [AlwaysAccessible]
        [HttpGet]
        public ActionResult LogOn(string returnUrl)
        {
            if ((HttpContext.User.Identity?.IsAuthenticated ?? false))
            {
                return Redirect("~/");
            }

            ViewData["ReturnUrl"] = returnUrl;

            return View(_openIdProviders);
        }

        [HttpPost]
        [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", Justification = "Needs to take same parameter type as Controller.Redirect()")]
        public ActionResult LogOn(string userNameOrEmail, string password, string returnUrl, bool rememberMe = false)
        {
            _userEventHandler.LoggingIn(userNameOrEmail, password);

            var user = ValidateLogOn(userNameOrEmail, password);
            if (!ModelState.IsValid)
            {
                return View(_openIdProviders);
            }

            var membershipSettings = _membershipService.GetSettings();
            if (user != null &&
                membershipSettings.EnableCustomPasswordPolicy &&
                membershipSettings.EnablePasswordExpiration &&
                _membershipService.PasswordIsExpired(user, membershipSettings.PasswordExpirationTimeInDays))
            {
                return RedirectToAction("ChangeExpiredPassword", new { username = user.UserName });
            }

            _authenticationService.SignIn(user, rememberMe);
            _userEventHandler.LoggedIn(user);

            return this.RedirectLocal(returnUrl);
        }

        [AlwaysAccessible]
        [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", Justification = "Needs to take same parameter type as Controller.Redirect()")]
        public ActionResult Challenge(string openIdProvider, string returnUrl)
        {
            _userEventHandler.LoggingIn(openIdProvider, string.Empty);

            if (string.IsNullOrWhiteSpace(openIdProvider))
                openIdProvider = OpenIdConnectDefaults.AuthenticationScheme;

            if ((HttpContext.User.Identity?.IsAuthenticated ?? false))
            {
                return this.RedirectLocal(returnUrl);
            }
            else
            {
                TempData["ReturnUrl"] = returnUrl;
            }

            // the tenant url prefix is part of the path base, the url helper adds it
            var redirectUri = Url.Content("~" + Constants.General.LogonCallbackUrl);

            return Challenge(new AuthenticationProperties { RedirectUri = redirectUri }, OpenIdAuthenticationSchemes.GetScheme(_shellSettings, openIdProvider));
        }

        public ActionResult LogOff(string openIdProvider)
        {
            if (string.IsNullOrWhiteSpace(openIdProvider))
                openIdProvider = OpenIdConnectDefaults.AuthenticationScheme;

            HttpContext.SignOutAsync(OpenIdAuthenticationSchemes.GetScheme(_shellSettings, OpenIdAuthenticationSchemes.ExternalCookie)).GetAwaiter().GetResult();
            try
            {
                // remote sign out, only supported by some providers (OpenID Connect)
                HttpContext.SignOutAsync(OpenIdAuthenticationSchemes.GetScheme(_shellSettings, openIdProvider)).GetAwaiter().GetResult();
            }
            catch (System.NotSupportedException)
            {
            }
            _authenticationService.SignOut();

            var loggedUser = _authenticationService.GetAuthenticatedUser();
            if (loggedUser != null)
            {
                _userEventHandler.LoggedOut(loggedUser);
            }

            return Redirect("~/");
        }

        public ActionResult LogonCallback()
        {
            var user = _authenticationService.GetAuthenticatedUser();

            if (user != null)
            {
                _userEventHandler.LoggedIn(user);
            }

            if (TempData.ContainsKey("ReturnUrl"))
                return this.RedirectLocal((string)TempData["ReturnUrl"]);
            else
                return Redirect("~/");
        }

        public ActionResult AccessDenied()
        {
            var returnUrl = Request.Query["ReturnUrl"].ToString();
            var currentUser = _authenticationService.GetAuthenticatedUser();

            if (currentUser == null)
            {
                return RedirectToAction("Logon", new { returnUrl = returnUrl });
            }

            _userEventHandler.AccessDenied(currentUser);

            return View();
        }

        public ActionResult Error()
        {
            return View();
        }

        private IUser ValidateLogOn(string userNameOrEmail, string password)
        {
            bool validate = true;

            if (string.IsNullOrEmpty(userNameOrEmail))
            {
                ModelState.AddModelError("userNameOrEmail", T("You must specify a username or e-mail."));
                validate = false;
            }
            if (string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("password", T("You must specify a password."));
                validate = false;
            }

            if (!validate)
                return null;

            List<LocalizedString> validationErrors;
            var user = _membershipService.ValidateUser(userNameOrEmail, password, out validationErrors);
            if (user == null)
            {
                ModelState.AddModelError("password", T("The username or e-mail or password provided is incorrect."));
            }

            return user;
        }
    }
}