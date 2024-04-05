using System.Security.Claims;
using Interfaces.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Models.Database_Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using DocumentFormat.OpenXml.EMMA;

// *****************************************************
//
// - AUTHENTICATION / Login - Logout 
// [HttpGet] Login()
// [HttpPost] Login()
// Logout()
//
// *****************************************************

namespace MVC.Controllers.Authentication
{
    public class AuthenticationController : Controller
    {
        private readonly IUserService _userService;
        private readonly ISharedCRUDService _sharedCRUD;
        public AuthenticationController(IUserService users, ISharedCRUDService sharedCRUD)
        {
            _userService = users;
            _sharedCRUD = sharedCRUD;
        }

        public void ClearSessionData()
        {
            HttpContext.Session.Clear();
            Response.Cookies.Delete("UserCookie");
            Response.Cookies.Delete("FiveMostDifficultFlashcardsCookie");
        }

        //------------------ AUTHENTICATION / Login - Logout ------------------

        public ActionResult Login()
        {
            return View("LoginView");
        }
        [HttpPost]
        public async Task<ActionResult> Login(UserEntity incomingUser)
        {
            // all fields required
            if (string.IsNullOrEmpty(incomingUser.Username) || string.IsNullOrEmpty(incomingUser.Password))
            {
                TempData["FailureMessage"] = "All fields are required!";
                return View("LoginView");
            }

            // VERIFYING PASSWORD HASH
            UserEntity foundUser = await _userService.FindAndReturnUserAsync(incomingUser.Username);

            if (foundUser == null)
            {
                TempData["FailureMessage"] = "Incorrect username or password.";
                return View("LoginView");
            }

            IdentityUser identityUser = new IdentityUser { UserName = incomingUser.Username };
            PasswordHasher<IdentityUser> passwordHasher = new PasswordHasher<IdentityUser>();

            PasswordVerificationResult verificationResult = passwordHasher.VerifyHashedPassword(identityUser, foundUser.Password, incomingUser.Password);
            // -----------------------

            if (verificationResult == PasswordVerificationResult.Success)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, foundUser.Username),
                    new Claim(ClaimTypes.NameIdentifier, foundUser.Id.ToString()),
                    // Add more claims as needed
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync( // od .NET default metoda
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    new AuthenticationProperties
                    {
                        IsPersistent = false,
                        ExpiresUtc = DateTime.UtcNow.AddMinutes(720) // 12 hours
                    });

                await _sharedCRUD.LogUserActivtyAsync(foundUser.Id, "LoggedIn");

                ClearSessionData();

                return RedirectToAction("Index", "Home");                
            }
            TempData["FailureMessage"] = "Verification failed. Incorrect username or password.";
            return View("LoginView");
        }

        [HttpPost]
        public async Task<ActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            ClearSessionData();

            return RedirectToAction("Index", "Home");
        }
    }
}
