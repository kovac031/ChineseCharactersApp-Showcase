using System.Security.Claims;
using DocumentFormat.OpenXml.Wordprocessing;
using Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.RateLimiting;
using Models.Database_Entities;
using Newtonsoft.Json;
using Services;

// *****************************************************
//
// ClearSessionData()
//
// - SHOW MAIN PAGE / Flashcard Practice setup
// StartPractice()
// GetTextbooksForDropdownMenu()
// GetLessonUnitsForDropdownMenu()
// RememberSelectedSettings()
//
// # launch practice from profile page
// LaunchFlashcardPracticeForMostDifficultFive()
//
// - FLASHCARD PRACTICE FEATURE
// FlashcardPractice()
// PassDifficultyButtonPressValues()
//
// *****************************************************

namespace MVC.Controllers.FlashcardsPractice
{
    [Authorize]
    public class FlashcardPracticeController : Controller
    {
        private readonly IFlashcardPracticeService _practiceService;
        private readonly IFlashcardService _flashcardService;
        private readonly IDeckService _deckService;
        private readonly ISharedCRUDService _sharedCRUD;
        private readonly ISettingsService _settings;
        public FlashcardPracticeController(IFlashcardPracticeService practice, IFlashcardService flashcardService, IDeckService deckService, ISettingsService settings, ISharedCRUDService sharedCRUD)
        {
            _practiceService = practice;
            _flashcardService = flashcardService;
            _deckService = deckService;
            _sharedCRUD = sharedCRUD;
            _settings = settings;            
        }

        private List<FlashcardsAndDecksViewEntity> _flashcardsToShow;
        private int[] _buttonValues;

        // CLEARS PERSISTING SESSION DATA ON END OF PRACTICE
        public void ClearSessionData()
        {
            HttpContext.Session.Remove("List<DeckEntity>_StartPractice");
            // HttpContext.Session.Remove("List<FlashcardsAndDecksViewEntity>_StartPractice"); // is cleared only on updates and those happen in Flashcard controller
            HttpContext.Session.Remove("List<FlashcardsAndDecksViewEntity>_FlashcardPractice");
            HttpContext.Session.Remove("Settings");
            HttpContext.Session.Remove("ButtonValueArray");
            HttpContext.Session.Remove("HeatmapData");
            Response.Cookies.Delete("FiveMostDifficultFlashcardsCookie"); // set in Flashcard/ShowFiveMostDifficultFlashcards
        }

        // -------------- SHOW MAIN PAGE / Flashcard Practice setup -----------------------

        // SHOW STARTER PAGE WITH OPTION SELECTIONS
        public async Task<ActionResult> StartPractice()
        {
            string urlPath = HttpContext.Request.Path;
            HttpContext.Session.SetString("UrlPath", JsonConvert.SerializeObject(urlPath));

            string stringId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            Guid userId = Guid.Parse(stringId);

            // CHECK IF METHOD IS LOADING FIRST TIME OR RELOADING AFTER SAVING SETTINGS BUTTON
            List<DeckEntity> deckData;
            string hasDeckData = HttpContext.Session.GetString("List<DeckEntity>_StartPractice");
            if (!string.IsNullOrEmpty(hasDeckData))
            {
                deckData = JsonConvert.DeserializeObject<List<DeckEntity>>(hasDeckData);
            }
            else
            {
                // GET COMPLETE LIST FROM USER SO DATABASE IS ACCESSED ONLY ONCE
                List<FlashcardsAndDecksViewEntity> wholeList = await _flashcardService.GetWholeListFromFlashcardsAndDecksViewByUserIdAsync(userId);
                HttpContext.Session.SetString("List<FlashcardsAndDecksViewEntity>_StartPractice", JsonConvert.SerializeObject(wholeList));

                // GET DECK DATA FROM WHOLE LIST
                deckData = StaticUtilityMethods.ConvertListOfFlashcardsAndDecksViewEntityToDeckEntityList(wholeList);
                HttpContext.Session.SetString("List<DeckEntity>_StartPractice", JsonConvert.SerializeObject(deckData));
            }

            // retrieve deck info to initially populate dropdown menu
            var deckNames = deckData.Where(x => x.DeckName != null).Select(x => x.DeckName).Distinct().OrderBy(x => x).ToList();
            var bookNames = deckData.Where(x => x.BookName != null).Select(x => x.BookName).Distinct().OrderBy(x => x).ToList();
            var unitNames = deckData.Where(x => x.UnitName != null).Select(x => x.UnitName).Distinct().OrderBy(x => x).ToList();
            ViewBag.DeckSelection = new SelectList(deckNames);
            ViewBag.BookSelection = new SelectList(bookNames);
            ViewBag.UnitSelection = new SelectList(unitNames);

            // GET USER SAVED SETTINGS
            FlashcardPracticeSettingsEntity settings = await _settings.GetSavedSettingsOfUserForPracticeAsync(userId);

            return View("StartPracticeView", settings);
        }

        // METHODS FOR CASCADING DROPDOWN MENUS DECK->TEXTBOOKS, TEXTBOOK -> LESSON UNITS
        public JsonResult GetTextbooksForDropdownMenu(string deckName)
        {
            List<DeckEntity> deckData = JsonConvert.DeserializeObject<List<DeckEntity>>(HttpContext.Session.GetString("List<DeckEntity>_StartPractice"));
            List<string> textbookTitles;

            if (string.IsNullOrEmpty(deckName))
            {
                textbookTitles = deckData.Where(x => !string.IsNullOrEmpty(x.BookName))
                                                        .Select(x => x.BookName).OrderBy(x => x)
                                                        .Distinct().ToList();
            }
            else
            {
                textbookTitles = deckData.Where(x => x.DeckName == deckName && !string.IsNullOrEmpty(x.BookName))
                                                            .Select(x => x.BookName).OrderBy(x => x)
                                                            .Distinct().ToList();
            }                        
            return Json(textbookTitles);
        }
        public JsonResult GetLessonUnitsForDropdownMenu(string textbookName)
        {
            List<DeckEntity> deckData = JsonConvert.DeserializeObject<List<DeckEntity>>(HttpContext.Session.GetString("List<DeckEntity>_StartPractice"));
            List<string> lessonUnitNames;

            if(string.IsNullOrEmpty(textbookName))
            {
                lessonUnitNames = deckData.Where(x => !string.IsNullOrEmpty(x.UnitName))
                                                        .Select(x => x.UnitName).OrderBy(x => x)
                                                        .Distinct().ToList();
            }
            else
            {
                lessonUnitNames = deckData.Where(x => x.BookName == textbookName && !string.IsNullOrEmpty(x.UnitName))
                                                        .Select(x => x.UnitName).OrderBy(x => x)
                                                        .Distinct().ToList();
            }                      
            return Json(lessonUnitNames);
        }

        // SAVING SETTINGS SELECTION FOR CONVENIENCE
        public async Task<ActionResult> RememberSelectedSettings(FlashcardPracticeSettingsEntity settings)
        {
            bool isSaved = await _settings.UpdateSettingsAsync(settings);

            if (isSaved)
            {
                TempData["SuccessMessage"] = "Saved!";
            }
            else
            {
                TempData["FailureMessage"] = "Something went wrong...";
            }

            ClearSessionData();

            return RedirectToAction("StartPractice");           
        }

        // LAUNCH PRACTICE OF 5 MOST DIFFUCLT CHARACTERS FROM PROFILE PAGE
        [HttpPost]
        public async Task<ActionResult> LaunchFlashcardPracticeForMostDifficultFive()
        {
            List<FlashcardEntity> flashcards = JsonConvert.DeserializeObject<List<FlashcardEntity>>(Request.Cookies["FiveMostDifficultFlashcardsCookie"]);

            flashcards = flashcards.OrderBy(x => Guid.NewGuid()).ToList();

            string stringId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            Guid userId = Guid.Parse(stringId);

            // serializes these randomized 5 to session
            HttpContext.Session.SetString("List<FlashcardsAndDecksViewEntity>_FlashcardPractice", JsonConvert.SerializeObject(flashcards));

            // GET USER SAVED SETTINGS
            FlashcardPracticeSettingsEntity settings = await _settings.GetSavedSettingsOfUserForPracticeAsync(userId);
            HttpContext.Session.SetString("Settings", JsonConvert.SerializeObject(settings));

            return RedirectToAction("FlashcardPractice", settings);
        }


        // -------------- FLASHCARD PRACTICE FEATURE -----------------------

        // FLASHCARDS PRACTICE MAIN METHOD
        public async Task<ActionResult> FlashcardPractice(FlashcardPracticeSettingsEntity settings, int index)
        {          
            // for handling correct redirect on completion
            // method is launched from /StartPractice and from /LaunchFlashcardPracticeForMostDifficultFive (actually profile page); urlPath is set in ProfilePage/ShowProfilePage and here in StartPractice
            string urlPath = JsonConvert.DeserializeObject<string>(HttpContext.Session.GetString("UrlPath"));
            string[] urlSegments = urlPath.Split('/');
            string controllerName = urlSegments[1];
            string methodName = urlSegments[2];            

            // FETCHES MY SELECTED FLASHCARDS IF FOUND, ELSE IT GOES TO NARROW DOWN THE USER'S LIST BASED ON SETTINGS
            string hasFlashcardsToShow = HttpContext.Session.GetString("List<FlashcardsAndDecksViewEntity>_FlashcardPractice");
            if (!string.IsNullOrEmpty(hasFlashcardsToShow))
            {
                _flashcardsToShow = JsonConvert.DeserializeObject<List<FlashcardsAndDecksViewEntity>>(hasFlashcardsToShow);

                //
                settings = JsonConvert.DeserializeObject<FlashcardPracticeSettingsEntity>(HttpContext.Session.GetString("Settings"));
                //
            }
            else
            {
                List<FlashcardsAndDecksViewEntity> rawList = JsonConvert.DeserializeObject<List<FlashcardsAndDecksViewEntity>>(HttpContext.Session.GetString("List<FlashcardsAndDecksViewEntity>_StartPractice"));

                _flashcardsToShow = _practiceService.SelectFlashcardsBasedOnSettings(settings, rawList);

                // check if user has the minimum number of flashcards to even practice
                if (_flashcardsToShow.Count < settings.HowMany)
                {
                    TempData["FailureMessage"] = $"You have less than {settings.HowMany} flashcards to show. Change your selection choices or add more vocabulary before proceeding. The app first loads only the flashcards that match your criteria on the left (show Simplified characters and show Traditional characters), and only then filters based on deck/textbook/lesson unit. You get this message if the final number of flashcards is lower than what you selected to show ({settings.HowMany}).";
                    return RedirectToAction("StartPractice");
                };

                HttpContext.Session.SetString("List<FlashcardsAndDecksViewEntity>_FlashcardPractice", JsonConvert.SerializeObject(_flashcardsToShow));

                //
                HttpContext.Session.SetString("Settings", JsonConvert.SerializeObject(settings)); // Viewbag can't carry complex types, session is a workaround
                //
            }

            // prevent start if both Simplified and Traditional are unchecked
            if (!settings.ShowSimplified && !settings.ShowTraditional)
            {
                TempData["FailureMessage"] = "You have selected or have saved both Simplified and Traditional as unchecked. One or the other must be checked for display to proceed to flashcard practice.";
                return RedirectToAction(methodName, controllerName);
            }

            // DISPLAY LOGIC
            if (index >= 0 && index < _flashcardsToShow.Count)
            {
                ViewBag.CurrentIndex = index;
                ViewBag.NextIndex = index + 1;
                ViewBag.Settings = settings;

                //Console.WriteLine($"{_flashcardsToShow[index].Id}, {_flashcardsToShow[index].Pinyin}");

                return View("FlashcardPracticeView", _flashcardsToShow[index]);
            }
            else if (index == _flashcardsToShow.Count)
            {
                // USER HAS COMPLETED THE PRACTICE SESSION
                await _practiceService.UpdatePracticeCountForUserAsync(settings.UserId);

                ClearSessionData();

                await _sharedCRUD.LogUserActivtyAsync(settings.UserId, "DidFlashcardPractice");
                return RedirectToAction(methodName, controllerName);
            }
            else
            {
                ClearSessionData();

                return RedirectToAction(methodName, controllerName);
            }                       
        }

        // LISTENS TO DIFFICULTY BUTTON PRESSES FOR UPDATING DIFFIULTY SCORE
        public async Task PassDifficultyButtonPressValues(int index, int buttonValue)
        {
            string hasButtonValueArray = HttpContext.Session.GetString("ButtonValueArray");
            if (!string.IsNullOrEmpty(hasButtonValueArray))
            {
                _buttonValues = JsonConvert.DeserializeObject<int[]>(HttpContext.Session.GetString("ButtonValueArray"));
            }
            else
            {
                _flashcardsToShow = JsonConvert.DeserializeObject<List<FlashcardsAndDecksViewEntity>>(HttpContext.Session.GetString("List<FlashcardsAndDecksViewEntity>_FlashcardPractice"));

                _buttonValues = new int[_flashcardsToShow.Count];
            }

            _buttonValues[index] = buttonValue;

            HttpContext.Session.SetString("ButtonValueArray", JsonConvert.SerializeObject(_buttonValues));

            if (index == _buttonValues.Length-1)
            {
                _flashcardsToShow = JsonConvert.DeserializeObject<List<FlashcardsAndDecksViewEntity>>(HttpContext.Session.GetString("List<FlashcardsAndDecksViewEntity>_FlashcardPractice"));

                await _practiceService.UpdateDifficultyRatingAsync(_buttonValues, _flashcardsToShow);
            }
        }        
    }
}
