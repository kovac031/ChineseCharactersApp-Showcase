using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Claims;
using DocumentFormat.OpenXml.Math;
using DocumentFormat.OpenXml.Spreadsheet;
using Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using Models;
using Models.Database_Entities;
using Newtonsoft.Json;

// *****************************************************
//
// ClearSessionData()
//
// - ADD LIST FROM EXCEL FILE 
// [HttpGet] UploadExcelFile()
// [HttpPost] UploadExcelFile()
//
// - ADD LIST FROM EXISTING PUBLIC DECK
// PreviewPublicDecks()
// AddSelectedPublicDeck()
//
// - PROGRESS BAR "WIDGET" // for displaying progress during wait time
// GetProgressData()
//
// *****************************************************

namespace MVC.Controllers.AddLists
{
    [Authorize]
    public class AddListsController : Controller
    {
        private readonly IAddListsService _service;
        private readonly IDeckService _deckService;
        private readonly ConcurrentDictionary<string, IMemoryCache> _userCaches = new ConcurrentDictionary<string, IMemoryCache>();

        public AddListsController(IAddListsService service, IDeckService deckService, ConcurrentDictionary<string, IMemoryCache> userCaches)
        {
            _service = service;
            _service.ProgressBarUpdated += OnProgressBarUpdate; // subscribed to event
            _deckService = deckService;
            _userCaches = userCaches;
        }

        // event handling       
        private void OnProgressBarUpdate(object sender, ProgressBarModel e) // fixed signature, can't remove sender
        {
            string userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            if (!_userCaches.ContainsKey(userId))
            {
                _userCaches[userId] = new MemoryCache(new MemoryCacheOptions());
            }

            _userCaches[userId].Set("lastProgressPercentage", e.ProgressValue);
            _userCaches[userId].Set("lastProgressMessage", e.ProgressMessage);            
        }

        // CLEAR SESSION DATA
        public void ClearSessionData()
        {
            HttpContext.Session.Remove("List<PublicDecksOverviewViewEntity>_PreviewPublicDecks");
            HttpContext.Session.Remove("bool_flag_ShowDeckDetails"); // set in Deck/ShowDeckDetails
            HttpContext.Session.Remove("List<DeckEntity>_DecksOverviewTable"); // set in Deck/DecksOverviewTable

            HttpContext.Session.Remove("List<FlashcardsAndDecksViewEntity>_FlashcardsOverviewTable"); // to refresh the flashcards overview after adding new lists
            HttpContext.Session.Remove("List<DeckEntity>_FlashcardsOverviewTable"); // to refresh the flashcards overview after adding new lists

            string userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            _userCaches[userId].Remove("lastProgressPercentage");
            _userCaches[userId].Remove("lastProgressMessage");
        }

        //----------------- ADD LIST FROM EXCEL FILE ----------------------

        [HttpGet]
        public ActionResult UploadExcelFile()
        {          
            return View("UploadExcelFileView");
        }
        [HttpPost]
        public async Task<ActionResult> UploadExcelFile([FromForm]UploadModel file)
        {           
            string userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;

            ResultModel result = await _service.UploadVocabularyListAsync(file, userId);

            if (result == null)
            {
                TempData["FailureMessage"] = "Something went wrong!";
                return RedirectToAction("UploadExcelFile", "AddLists");
            }
            else if (!result.IsSuccess)
            {
                TempData["FailureMessage"] = result.Message;
                return RedirectToAction("UploadExcelFile", "AddLists");
            }
            TempData["SuccessMessage"] = result.Message;

            ClearSessionData();

            return RedirectToAction("UploadExcelFile", "AddLists");
        }

        //----------------- ADD LIST FROM EXISTING PUBLIC DECK ----------------------
        public async Task<ActionResult> PreviewPublicDecks(string sortBy, string filterBy, int? page)
        {
            HttpContext.Session.Remove("List<FlashcardEntity>_ShowFlashcardsForDeckPartialView"); // clears the flashcards loaded in session in case of going to look at different decks; set in Flashcard/ShowFlashcardsForDeckPartialView, also used in Deck methods
               
            List<PublicDecksOverviewViewEntity> publicDecks;
            string hasPublicDecks = HttpContext.Session.GetString("List<PublicDecksOverviewViewEntity>_PreviewPublicDecks");

            // CHECK SESSION FOR LISTS
            if (string.IsNullOrEmpty(hasPublicDecks))
            {
                publicDecks = await _deckService.GetAllPublicDecksAsync();
                HttpContext.Session.SetString("List<PublicDecksOverviewViewEntity>_PreviewPublicDecks", JsonConvert.SerializeObject(publicDecks));
            }
            else
            {
                publicDecks = JsonConvert.DeserializeObject<List<PublicDecksOverviewViewEntity>>(hasPublicDecks);
            }

            //FILTER LIST BASED ON filterBy
            if(!string.IsNullOrEmpty(filterBy)) 
            { 
                publicDecks = _deckService.FilterPublicDecksBySearchTerm(publicDecks, filterBy);
                ViewBag.SearchFilter = filterBy;
            }

            //SORT LIST BASED ON sortBy
            List<PublicDecksOverviewViewEntity> sortedDecks = _deckService.SortPublicDecksByParameter(publicDecks, sortBy);

            ViewBag.SortByTimesCopied = string.IsNullOrEmpty(sortBy) ? "copy_asc" : "";
            ViewBag.SortByTextbook = sortBy == "textbook_asc" ? "textbook_desc" : "textbook_asc";
            ViewBag.SortByLesson = sortBy == "lesson_asc" ? "lesson_desc" : "lesson_asc";
            ViewBag.SortByCount = sortBy == "count_asc" ? "count_desc" : "count_asc";
            ViewBag.SortByCreatedBy = sortBy == "author_asc" ? "author_desc" : "author_asc";
            ViewBag.SortByDeck = sortBy == "deck_asc" ? "deck_desc" : "deck_asc";
            ViewBag.SortByTimeAdded = sortBy == "added_asc" ? "added_desc" : "added_asc";
            ViewBag.SortByTimeUpdated = sortBy == "updated_asc" ? "updated_desc" : "updated_asc";
            ViewBag.CurrentSort = sortBy;

            //GET THE CORRECT PAGE FROM PAGE NUMBER
            var (pagedList, pageNumber, totalPages) = _deckService.PageThePublicDecks(sortedDecks, page);

            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = totalPages;

            HttpContext.Session.Remove("bool_flag_ShowDeckDetails"); // flag will be set if we were viewing the deck details; needs to be removed for the correct redirect in AddSelectedPublicDeck

            return View("PreviewPublicDecksView", pagedList);
        }
           
        public async Task<ActionResult> AddSelectedPublicDeck(Guid deckId)
        {    
            string userId = User.FindFirst(ClaimTypes.NameIdentifier).Value; // dohvati Id ulogiranog usera

            ResultModel result = await _service.CopySelectedPublicDeckAsync(Guid.Parse(userId), deckId);

            bool copyPublicDeckFromDeckDetailsView = JsonConvert.DeserializeObject<bool>(HttpContext.Session.GetString("bool_flag_ShowDeckDetails") ?? "false");

            if (result == null)
            {
                TempData["FailureMessage"] = "Something went wrong!";
                return RedirectToAction("PreviewPublicDecks");
            }
            else if (!result.IsSuccess)
            {
                TempData["FailureMessage"] = result.Message;

                if (!copyPublicDeckFromDeckDetailsView)
                {
                    return RedirectToAction("PreviewPublicDecks");
                }
                else
                {
                    return RedirectToAction("ShowDeckDetails", "Deck", new { deckId });
                }
            }

            TempData["SuccessMessage"] = result.Message;

            ClearSessionData();

            if (!copyPublicDeckFromDeckDetailsView)
            {
                return RedirectToAction("PreviewPublicDecks");
            }
            else
            {
                return RedirectToAction("ShowDeckDetails", "Deck", new { deckId });
            }            
        }

        // ---------------- PROGRESS BAR "WIDGET" -----------
        [HttpGet]
        public ActionResult GetProgressData()
        {
            string userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;

            if (!_userCaches.ContainsKey(userId))
            {
                // if not found, it is because this method is faster than the event, not an issue                
                ViewBag.Percentage = 0;
                ViewBag.Message = "Please wait ...";

                return PartialView("_UploadProgressPartialView");
            }

            IMemoryCache cache = _userCaches[userId];
            ViewBag.Percentage = cache.Get<int>("lastProgressPercentage");
            ViewBag.Message = cache.Get<string>("lastProgressMessage");

            return PartialView("_UploadProgressPartialView");
        }

    }
}
