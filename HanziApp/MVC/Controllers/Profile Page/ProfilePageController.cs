using System.Security.Claims;
using Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;
using Models.Database_Entities;
using Newtonsoft.Json;
using Services.CRUD;
using Services;

// *****************************************************
//
// ShowProfilePage()
// ShowCalendarActivityHeatmap()
//
// *****************************************************

namespace MVC.Controllers.Profile_Page
{
    [Authorize]
    public class ProfilePageController : Controller
    {
        private readonly IUserService _userService;
        public ProfilePageController(IUserService users)
        {
            _userService = users;
        }

        public ActionResult ShowProfilePage()
        {
            string urlPath = HttpContext.Request.Path;
            HttpContext.Session.SetString("UrlPath", JsonConvert.SerializeObject(urlPath));

            string stringId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; 
            Guid.TryParse(stringId, out Guid userId);

            ViewBag.UserId = userId;

            return View("ProfilePageView");
        }

        public async Task<ActionResult> ShowCalendarActivityHeatmap()
        {
            string stringId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Guid.TryParse(stringId, out Guid userId);

            List<CalendarHeatmapModel> activityLog;
            int betterThanHowMany;

            string hasHeatmapData = HttpContext.Session.GetString("HeatmapData");
            if (hasHeatmapData != null)
            {
                (activityLog, betterThanHowMany) = JsonConvert.DeserializeObject<(List<CalendarHeatmapModel> activityLog, int betterThanHowMany)>(hasHeatmapData);
            }
            else
            {
                (activityLog, betterThanHowMany) = await _userService.GetPracticeHistoryForHeatmapByUserIdAsync(userId);
                HttpContext.Session.SetString("HeatmapData", JsonConvert.SerializeObject((activityLog, betterThanHowMany)));
            }

            ViewBag.BetterThan = betterThanHowMany;

            return PartialView("_CalendarActivityHeatmapPartialView", activityLog);
        }
    }
}
