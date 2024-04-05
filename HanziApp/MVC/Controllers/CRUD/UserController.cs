using System.Security.Claims;
using Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Models.Database_Entities;
using Models;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Identity;
using Models.FormModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using DocumentFormat.OpenXml.Spreadsheet;

// *****************************************************
//
// ClearSessionData();
//
// - CREATE NEW USER / REGISTER / SIGN UP
// [HttpGet] ShowCreateNewUserView()
// [HttpPost] CreateNewUser()
//
// - RETURN ONE USER / Get details
// [HttpGet] ShowUserDetails()
//
// - EDIT USER INFO
// [HttpGet] ChangeUserInfo()
// [HttpPost] EditUsername()
// [HttpPost] EditEmailAddress()
// [HttpPost] EditPassword()
//
// # password recovery
// [HttpGet] ShowPasswordRecoveryView()
// [HttpGet] ShowResetPasswordView()
// [HttpPost] ResetPassword()
//
// *****************************************************

namespace MVC.Controllers.CRUD
{
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        private readonly ISharedCRUDService _sharedCRUD;
        public UserController(IUserService users, ISharedCRUDService sharedCRUD)
        {
            _userService = users;
            _sharedCRUD = sharedCRUD;
        }

        public void ClearSessionData()
        {
            Response.Cookies.Delete("UserCookie");
            HttpContext.Session.Remove("RecoveryToken");
        }

        // ---------- CREATE NEW USER / REGISTER / SIGN UP -------

        [HttpGet]
        public ActionResult ShowCreateNewUserView()
        {
            return View("CreateNewUserView");
        }
        [HttpPost]
        public async Task<ActionResult> CreateNewUser(UserEntity user)
        {
            // all fields required
            if (string.IsNullOrEmpty(user.Username) || string.IsNullOrEmpty(user.Password) || string.IsNullOrEmpty(user.EmailAddress)) 
            {
                TempData["FailureMessage"] = "All fields are required!";
                return RedirectToAction("ShowCreateNewUserView");
            }

            // CHECKS IF USERNAME EXISTS
            bool usernameExists = await _userService.CheckIfUsernameIsTakenAsync(user.Username);

            if (usernameExists) 
            {
                TempData["FailureMessage"] = $"The username {user.Username} is taken";
                return RedirectToAction("ShowCreateNewUserView");
            }

            bool isCreated = await _userService.CreateNewUserAsync(user);
            if (!isCreated)
            {
                TempData["FailureMessage"] = "Something went wrong";
                return RedirectToAction("ShowCreateNewUserView");
            }
            TempData["SuccessMessage"] = "User is now registered!";
            return RedirectToAction("ShowCreateNewUserView");
            
        }


        // ------ RETURN ONE USER / Get details ------

        [Authorize]
        [HttpGet]
        public async Task<ActionResult> ShowUserDetails()
        {
            string stringId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; // dohvaca Id ulogiranog usera
            Guid.TryParse(stringId, out Guid userId);

            UserEntity user;

            string hasUser = Request.Cookies["UserCookie"];

            if (string.IsNullOrEmpty(hasUser))
            {
                user = await _userService.FindAndReturnUserAsync(userId);
                Response.Cookies.Append("UserCookie", JsonConvert.SerializeObject(user)); 
            }
            else
            {
                user = JsonConvert.DeserializeObject<UserEntity>(hasUser);
            }

            return PartialView("_ShowUserDetailsPartialView", user);
        }


        // -------- EDIT USER INFO -----------

        // CHANGE USERNAME, PASSWORD, EMAIL ADDRESS

        [Authorize]
        [HttpGet]
        public ActionResult ChangeUserInfo(string selection) // show the correct edit page
        {
            UserEntity user = JsonConvert.DeserializeObject<UserEntity>(Request.Cookies["UserCookie"]);
                
            EditUserInfoModel model = new EditUserInfoModel()
            {
                User = user
            };

            if (selection == "username")
            {
                return View("ChangeUsernameView", model);
            }
            else if (selection == "email")
            {
                return View("ChangeEmailAddressView", model);
            }
            else if (selection == "password")
            {
                return View("ChangePasswordView", model);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult> EditUsername(EditUserInfoModel input)
        {
            // all fields required
            if (string.IsNullOrEmpty(input.Strings.StringOne) || string.IsNullOrEmpty(input.Strings.StringTwo) || string.IsNullOrEmpty(input.Strings.StringThree))
            {
                TempData["FailureMessage"] = "All fields are required!";
                return View("ChangeUsernameView", input);
            }

            // for clarity
            string existingUsername = input.User.Username;
            string existingPassword = input.User.Password;
            string newUsername = input.Strings.StringOne;
            string passwordCheck1 = input.Strings.StringTwo;
            string passwordCheck2 = input.Strings.StringThree;

            // for extra clarity if a person has a typo in one of the entered passwords
            if (passwordCheck1 != passwordCheck2)
            {
                TempData["FailureMessage"] = "The entered passwords did not match. You may try again.";
                return View("ChangeUsernameView", input);
            }

            // VERIFYING PASSWORD HASH
            IdentityUser identityUser = new IdentityUser { UserName = existingUsername };
            PasswordHasher<IdentityUser> passwordHasher = new PasswordHasher<IdentityUser>();

            PasswordVerificationResult verificationResult = passwordHasher.VerifyHashedPassword(identityUser, existingPassword, passwordCheck1); //passwordCheck1 and passwordCheck1 should be the same at this point
            // -----------------------

            // ConfirmPasswordOne is hashed above and checked against existing hashed password from input.User
            if (verificationResult == PasswordVerificationResult.Success)
            {
                // passwords matched

                UserEntity userInfoToUpdate = new UserEntity()
                {
                    Id = input.User.Id,
                    Username = newUsername,
                    Password = input.User.Password,
                    EmailAddress = input.User.EmailAddress
                };

                ResultModel result = await _userService.EditUserInfoAsync(userInfoToUpdate);

                if (result == null)
                {
                    TempData["FailureMessage"] = "Something went wrong!";
                    return RedirectToAction("ShowProfilePage", "ProfilePage");
                }
                else if (!result.IsSuccess)
                {
                    TempData["FailureMessage"] = result.Message;
                    return RedirectToAction("ShowProfilePage", "ProfilePage");
                }

                TempData["SuccessMessage"] = result.Message;

                ClearSessionData();

                await _sharedCRUD.LogUserActivtyAsync(input.User.Id, "ChangedUsername");

                return RedirectToAction("ShowProfilePage", "ProfilePage");
            }

            // passwords did not match
            TempData["FailureMessage"] = "The password you entered was incorrect.";

            return View("ChangeUsernameView", input);
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult> EditEmailAddress(EditUserInfoModel input)
        {
            // all fields required
            if (string.IsNullOrEmpty(input.Strings.StringOne) || string.IsNullOrEmpty(input.Strings.StringTwo) || string.IsNullOrEmpty(input.Strings.StringThree))
            {
                TempData["FailureMessage"] = "All fields are required!";
                return View("ChangeEmailAddressView", input);
            }

            // for clarity
            string existingUsername = input.User.Username;
            string existingPassword = input.User.Password;
            string newEmailAddress = input.Strings.StringOne;
            string passwordCheck1 = input.Strings.StringTwo;
            string passwordCheck2 = input.Strings.StringThree;

            // for extra clarity if a person has a typo in one of the entered passwords
            if (passwordCheck1 != passwordCheck2)
            {
                TempData["FailureMessage"] = "The entered passwords did not match. You may try again.";
                return View("ChangeEmailAddressView", input);
            }

            // VERIFYING PASSWORD HASH
            IdentityUser identityUser = new IdentityUser { UserName = existingUsername };
            PasswordHasher<IdentityUser> passwordHasher = new PasswordHasher<IdentityUser>();

            PasswordVerificationResult verificationResult = passwordHasher.VerifyHashedPassword(identityUser, existingPassword, passwordCheck1); //passwordCheck1 and passwordCheck1 should be the same at this point
            // -----------------------

            // ConfirmPasswordOne is hashed above and checked against existing hashed password from input.User
            if (verificationResult == PasswordVerificationResult.Success)
            {
                // passwords matched

                UserEntity userInfoToUpdate = new UserEntity()
                {
                    Id = input.User.Id,
                    Username = input.User.Username,
                    Password = input.User.Password,
                    EmailAddress = newEmailAddress
                };

                ResultModel result = await _userService.EditUserInfoAsync(userInfoToUpdate);

                if (result == null)
                {
                    TempData["FailureMessage"] = "Something went wrong!";
                    return RedirectToAction("ShowProfilePage", "ProfilePage");
                }
                else if (!result.IsSuccess)
                {
                    TempData["FailureMessage"] = result.Message;
                    return RedirectToAction("ShowProfilePage", "ProfilePage");
                }

                TempData["SuccessMessage"] = result.Message;

                ClearSessionData();

                await _sharedCRUD.LogUserActivtyAsync(input.User.Id, "ChangedEmail");
                return RedirectToAction("ShowProfilePage", "ProfilePage");
            }

            // passwords did not match
            TempData["FailureMessage"] = "The password you entered was incorrect.";

            return View("ChangeEmailAddressView", input);
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult> EditPassword(EditUserInfoModel input)
        {
            // all fields required
            if (string.IsNullOrEmpty(input.Strings.StringOne) || string.IsNullOrEmpty(input.Strings.StringTwo) || string.IsNullOrEmpty(input.Strings.StringThree))
            {
                TempData["FailureMessage"] = "All fields are required!";
                return View("ChangePasswordView", input);
            }

            // for clarity
            string existingUsername = input.User.Username;
            string existingPassword = input.User.Password;
            string oldPasswordInput = input.Strings.StringOne;
            string newPasswordInput1 = input.Strings.StringTwo;
            string newPasswordInput2 = input.Strings.StringThree;

            // VERIFYING PASSWORD HASH
            IdentityUser identityUser = new IdentityUser { UserName = existingUsername };
            PasswordHasher<IdentityUser> passwordHasher = new PasswordHasher<IdentityUser>();

            PasswordVerificationResult verificationResult = passwordHasher.VerifyHashedPassword(identityUser, existingPassword, oldPasswordInput); 
            // -----------------------

            // NewInfo (old password being input) is hashed above and checked against existing hashed password from input.User
            if (verificationResult == PasswordVerificationResult.Success)
            {
                // passwords matched, check if new password is correctly typed in
                if(newPasswordInput1 != newPasswordInput2)
                {
                    TempData["FailureMessage"] = "New passwords did not match. You may try again.";
                    return View("ChangePasswordView", input);
                }

                // ENCRYPT NEW PASSWORD
                string newHashedPassword = passwordHasher.HashPassword(identityUser, newPasswordInput1); // newPasswordInput1 and newPasswordInput2 are the same new password
                // --------------------

                UserEntity userInfoToUpdate = new UserEntity()
                {
                    Id = input.User.Id,
                    Username = input.User.Username,
                    Password = newHashedPassword,
                    EmailAddress = input.User.EmailAddress
                };

                ResultModel result = await _userService.EditUserInfoAsync(userInfoToUpdate);

                if (result == null)
                {
                    TempData["FailureMessage"] = "Something went wrong!";
                    return RedirectToAction("ShowProfilePage", "ProfilePage");
                }
                else if (!result.IsSuccess)
                {
                    TempData["FailureMessage"] = result.Message;
                    return RedirectToAction("ShowProfilePage", "ProfilePage");
                }
                TempData["SuccessMessage"] = result.Message;

                ClearSessionData();

                await _sharedCRUD.LogUserActivtyAsync(input.User.Id, "ChangedPassword");

                return RedirectToAction("ShowProfilePage", "ProfilePage");
            }

            // passwords did not match
            TempData["FailureMessage"] = "The password you entered was incorrect.";

            return View("ChangePasswordView", input);
        }

        // # PASSWORD RECOVERY
        // the method for generating and sending recovery email is in EmailsController
        [HttpGet]
        public ActionResult ShowPasswordRecoveryView() // from Forgot password link on Login page
        {
            return View("PasswordRecoveryView"); 
        }

        [HttpGet]
        [Route("user/resetpassword/{recoveryToken}")]
        public ActionResult ShowResetPasswordView(Guid recoveryToken)
        {
            HttpContext.Session.SetString("RecoveryToken", JsonConvert.SerializeObject(recoveryToken));
            return View("ResetPasswordView");
        }
        [HttpPost] // this will be triggered by the form when the user submits the new password
        public async Task<ActionResult> ResetPassword(TwoStringsModel model)
        {
            // all fields required
            if (string.IsNullOrEmpty(model.StringOne) || string.IsNullOrEmpty(model.StringTwo))
            {
                TempData["FailureMessage"] = "All fields are required!";
                return View("ResetPasswordView");
            }

            Guid recoveryToken = JsonConvert.DeserializeObject<Guid>(HttpContext.Session.GetString("RecoveryToken"));

            // check if new password is correctly typed in
            if (model.StringOne != model.StringTwo)
            {
                TempData["FailureMessage"] = "New passwords did not match. You may try again.";
                return View("ResetPasswordView");
            }

            string username = await _userService.GetUsernameByRecoveryTokenAsync(recoveryToken);

            // PASSWORD HASHING PROCESS
            IdentityUser identityUser = new IdentityUser { UserName = username };
            PasswordHasher<IdentityUser> passwordHasher = new PasswordHasher<IdentityUser>();

            string hashedPassword = passwordHasher.HashPassword(identityUser, model.StringOne);
            // ------------------------

            ResultModel result = await _userService.ResetPasswordAsync(hashedPassword, recoveryToken);
            if (result == null)
            {
                TempData["FailureMessage"] = "Something went wrong!";
                return View("ResetPasswordView");
            }
            else if (!result.IsSuccess)
            {
                TempData["FailureMessage"] = result.Message;
                return View("ResetPasswordView");
            }
            TempData["SuccessMessage"] = result.Message;

            ClearSessionData();

            return View("ResetPasswordView");
        }
    }
}
