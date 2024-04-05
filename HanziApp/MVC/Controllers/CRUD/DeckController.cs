using System.Security.Claims;
using DocumentFormat.OpenXml.EMMA;
using Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Models;
using Models.Database_Entities;
using Newtonsoft.Json;

// *****************************************************
//
// ClearSessionData()
//
// - RETURN LIST OF DECK / Decks overview
// DecksOverviewTable()
//
// - RETURN ONE DECK / Deck details
// ShowDeckDetails()
//
// - CREATE NEW DECK
// [HttpGet] ShowCreateView()
// [HttpPost] AddNewDeck
//
// - EDIT DECK
// [HttpGet] ShowDeckEditView
// [HttpPost] EditDeckFromView
//
// - DELETE DECK
// RemoveFlashcardFromDeck()
// DeleteDeck()
//
// *****************************************************

namespace MVC.Controllers.CRUD
{
    [Authorize]
    public class DeckController : Controller
    {
        private readonly IFlashcardService _flashcardService;
        private readonly IDeckService _deckService;
        private readonly ISharedCRUDService _sharedCRUD;
        public DeckController(IFlashcardService flashcardService, IDeckService deckService, ISharedCRUDService sharedCRUD)
        {
            _flashcardService = flashcardService;
            _deckService = deckService;
            _sharedCRUD = sharedCRUD;
        }

        public void ClearSessionData()
        {
            HttpContext.Session.Remove("List<DeckEntity>_DecksOverviewTable");
            // bool_flag_ShowDeckDetails cleared in AddListsController

            foreach (string key in HttpContext.Session.Keys.ToList()) // cleared when flashcard is removed from deck
            {
                if (key.StartsWith("List<FlashcardEntity>_ShowFlashcardsForDeckPartialView_"))
                {
                    HttpContext.Session.Remove(key);
                }
            }

            foreach (string key in HttpContext.Session.Keys.ToList()) 
            {
                if (key.StartsWith("DeckEntity_ShowDeckEditView_"))
                {
                    HttpContext.Session.Remove(key);
                }
            }

            // DeckEntity_ShowDeckDetails is not cleared on user-made changes, because it is only used with public decks and the user does not change those; cleared on login/logoff

            HttpContext.Session.Remove("List<DeckEntity>_StartPractice"); // set in FlashcardPractice; should be cleared on deck updates
        }

        // ------------------- RETURN LIST OF DECK / Decks overview -----------------------

        public async Task<ActionResult> DecksOverviewTable(string sortBy)
        {
            string stringId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Guid.TryParse(stringId, out Guid userId);

            List<DeckEntity> decks;
            string hasDeckData = HttpContext.Session.GetString("List<DeckEntity>_DecksOverviewTable");

            // CHECK SESSION FOR LISTS
            if (string.IsNullOrEmpty(hasDeckData))
            {
                decks = await _sharedCRUD.GetAllDecksByUserIdAsync(userId);
                HttpContext.Session.SetString("List<DeckEntity>_DecksOverviewTable", JsonConvert.SerializeObject(decks));
            }
            else
            {
                decks = JsonConvert.DeserializeObject<List<DeckEntity>>(hasDeckData);
            }            

			//SORT LIST BASED ON sortBy
			List<DeckEntity> sortedDecks = _deckService.SortDecksByParameter(decks, sortBy);

			ViewBag.SortByDeck = string.IsNullOrEmpty(sortBy) ? "deck_desc" : "";
			ViewBag.SortByTextbook = sortBy == "textbook_asc" ? "textbook_desc" : "textbook_asc";
			ViewBag.SortByLesson = sortBy == "lesson_asc" ? "lesson_desc" : "lesson_asc";
			ViewBag.SortByCount = sortBy == "count_asc" ? "count_desc" : "count_asc";
			ViewBag.CurrentSort = sortBy;

			int flashcardsCount = await _flashcardService.CountFlashcardsByUserIdAsync(userId);

            ViewBag.TotalCount = flashcardsCount;

            return View("DecksOverviewTableView", sortedDecks);
        }

        // ------------------- RETURN ONE DECK / Deck details -----------------------

        // used only for Public Decks; reached from PreviewPublicDecksView
        // the user sees their deck details from DecksOverviewTableView -> DeckEditView
        [HttpGet]
        public async Task<ActionResult> ShowDeckDetails(Guid deckId, int? page, string sortBy, string filterBy)
        {
            ViewBag.DeckId = deckId;

            DeckEntity deck;
            string hasDeck = HttpContext.Session.GetString($"DeckEntity_ShowDeckDetails_{deckId}");

            // CHECK SESSION FOR DECK
            if (string.IsNullOrEmpty(hasDeck))
            {
                deck = await _deckService.GetOneDeckByIdAsync(deckId);
                string sessionKey = $"DeckEntity_ShowDeckDetails_{deckId}";
                HttpContext.Session.SetString(sessionKey, JsonConvert.SerializeObject(deck));
            }
            else
            {
                deck = JsonConvert.DeserializeObject<DeckEntity>(hasDeck);
            }

            // passing filtering/sorting/paging data for partial view (added to DeckDetailsView)
            ViewBag.CurrentPage = page;
            ViewBag.CurrentSort = sortBy;
            ViewBag.SearchFilter = filterBy;

            ViewBag.CurrentUrl = HttpContext.Request.Path;

            bool copyPublicDeckFromDeckDetailsView = true;
            HttpContext.Session.SetString("bool_flag_ShowDeckDetails", JsonConvert.SerializeObject(copyPublicDeckFromDeckDetailsView));

            return View("DeckDetailsView", deck);
        }

        // ------------------- CREATE NEW DECK -----------------------
        [HttpGet]
        public ActionResult ShowCreateView()
        {
            return View("DeckAddView");
        }
        [HttpPost]
        public async Task<ActionResult> AddNewDeck(DeckEntity model)
        {            
            if (string.IsNullOrEmpty(model.DeckName) && string.IsNullOrEmpty(model.BookName) && string.IsNullOrEmpty(model.UnitName))
            {
                TempData["FailureMessage"] = "Cannot add a deck with no names";
                return RedirectToAction("ShowCreateView");
            }

            string userId = User.FindFirst(ClaimTypes.NameIdentifier).Value; // dohvati Id ulogiranog usera
            Guid.TryParse(userId, out Guid id);

            ResultModel result = await _deckService.AddNewDeckAsync(model, id);
            if (result == null)
            {
                TempData["FailureMessage"] = "Something went wrong!";
                return RedirectToAction("ShowCreateView");
            }
            else if(!result.IsSuccess) 
            {
                TempData["FailureMessage"] = result.Message;
                return RedirectToAction("ShowCreateView");
            }
            TempData["SuccessMessage"] = result.Message;

            ClearSessionData();

            return RedirectToAction("ShowCreateView");
        }

        // ------------------- EDIT DECK -----------------------
        [HttpGet]
        public async Task<ActionResult> ShowDeckEditView(Guid deckId, int? page, string sortBy, string filterBy)
        {
            ViewBag.DeckId = deckId;

            DeckEntity deck;
            string hasDeck = HttpContext.Session.GetString($"DeckEntity_ShowDeckEditView_{deckId}");

            // CHECK SESSION FOR DECK
            if (string.IsNullOrEmpty(hasDeck))
            {
                deck = await _deckService.GetOneDeckByIdAsync(deckId);
                string sessionKey = $"DeckEntity_ShowDeckEditView_{deckId}";
                HttpContext.Session.SetString(sessionKey, JsonConvert.SerializeObject(deck));
            }
            else
            {
                deck = JsonConvert.DeserializeObject<DeckEntity>(hasDeck);
            }

            bool isNoDeck = deck.DeckName == null && deck.BookName == null && deck.UnitName == null;
            ViewBag.IsNoDeck = isNoDeck;

            // passing filtering/sorting/paging data for partial view (added to DeckEditView)
            ViewBag.CurrentPage = page;
            ViewBag.CurrentSort = sortBy;
            ViewBag.SearchFilter = filterBy;

            ViewBag.CurrentUrl = HttpContext.Request.Path;

            return View("DeckEditView", deck);
        }
        [HttpPost]
        public async Task<ActionResult> EditDeckFromView(DeckEntity deck)
        {
            if (string.IsNullOrEmpty(deck.DeckName) && string.IsNullOrEmpty(deck.BookName) && string.IsNullOrEmpty(deck.UnitName))
            {
                TempData["FailureMessage"] = "Cannot remove all names from deck. Decks must always have at least one name identifier.";
                return RedirectToAction("ShowDeckEditView", new { deckId = deck.Id });
            }

            bool result = await _deckService.EditOneDeckByIdAsync(deck);
            if (result)
            {
                TempData["SuccessMessage"] = "Saved!";
                ClearSessionData();
                return RedirectToAction("ShowDeckEditView", new { deckId = deck.Id });
            }
            else
            {
                TempData["FailureMessage"] = "Something went wrong!";
                return RedirectToAction("ShowDeckEditView", new { deckId = deck.Id });
            }
        }

        // ------------------- DELETE DECK -----------------------

        // REMOVE SINGLE FLASHCARD FROM DECK
        public async Task<ActionResult> RemoveFlashcardFromDeck(Guid deckId, Guid flashcardId, int? page, string sortBy, string filterBy)
        {
            string stringId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Guid.TryParse(stringId, out Guid userId);

            await _deckService.MoveSingleFlashcardToNullDeckAsync(deckId, flashcardId, userId);            

            ClearSessionData();

            return RedirectToAction("ShowDeckEditView", new {deckId, page, sortBy, filterBy});
        }

        // DELETE ONE DECK BY ID (just the deck, not its flashcards)
        public async Task<ActionResult> DeleteDeck(Guid deckId)
        {
            string stringId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Guid.TryParse(stringId, out Guid userId);

            bool success = await _deckService.MoveFlashcardsFromDeckBeingDeletedToNullDeckAsync(deckId, userId);
            
            if (success) 
            { 
                await _deckService.DeleteDeckByIdAsync(deckId);
                ClearSessionData();
            }
            return RedirectToAction("DecksOverviewTable");
        }

    }
}
