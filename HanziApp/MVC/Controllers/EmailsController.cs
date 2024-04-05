using Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Models;
using Models.Database_Entities;
using Models.FormModels;

// *****************************************************
//
// [HttpPost] PasswordRecovery()
//
// *****************************************************

namespace MVC.Controllers
{
    public class EmailsController : Controller
    {
        private readonly IEmailsService _emailsService;
        private readonly IUserService _userService;
        public EmailsController(IEmailsService emailsService, IUserService userService)
        {
            _emailsService = emailsService;
            _userService = userService;
        }
        
        [HttpPost]
        public async Task<ActionResult> PasswordRecovery(TwoStringsModel model) // can't use generics with form submission
        {
            // all fields required
            if (string.IsNullOrEmpty(model.StringOne) || string.IsNullOrEmpty(model.StringTwo))
            {
                TempData["FailureMessage"] = "All fields are required!";
                return RedirectToAction("ShowPasswordRecoveryView", "User");
            }

            string username = model.StringOne; // TwoStringsModel is convenient, but declaring new string variables makes things more readable and clear
            string emailAddress = model.StringTwo;

            UserEntity foundUser = await _userService.FindUserForPasswordRecoveryAsync(username, emailAddress);

            if (foundUser == null) 
            {
                TempData["FailureMessage"] = "No user found.";
                return RedirectToAction("ShowPasswordRecoveryView", "User");
            }

            Guid recoveryToken = await _emailsService.GenerateStoreAndReturnPasswordRecoveryTokenAsync(foundUser.Id);

            string mailTitle = $"Password recovery for user {username}";
            string mailBody = $"To reset your password please follow this link:\n \n https://practice-chinese.azurewebsites.net/User/ResetPassword/{recoveryToken}";
            //string mailBody = $"To reset your password please follow this link:\n \n https://localhost:7010/User/ResetPassword/{recoveryToken}";

            ResultModel result = _emailsService.SendEmail(emailAddress, mailTitle, mailBody);

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction("ShowPasswordRecoveryView","User");
        }
    }
}
