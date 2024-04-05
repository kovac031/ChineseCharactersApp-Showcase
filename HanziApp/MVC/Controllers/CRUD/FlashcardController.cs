using System.Collections.Generic;
using System.Security.Claims;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Spreadsheet;
using Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Models;
using Models.Database_Entities;
using Newtonsoft.Json;
using Services;

// *****************************************************
//
// ClearSessionData()
//
// - RETURN LIST OF FLASHCARDS / Overview table
// FlashcardsOverviewTable()
// ShowFlashcardsForDeckPartialView()
// ShowFiveMostDifficultFlashcards()
//
// - ADD EXISTING FLASHCARDS TO EXISTING DECK / save new relationships
// [HttpGet] ShowAllFlashcardsNotInDeckForSelectionView()
// [HttpPost] AddSelectedFlashcardsToSelectedDeck()
//
// - CREATE NEW FLASHCARD
// [HttpGet] ShowCreateView()
// [HttpPost] AddNewCard()
// [HttpGet] ShowAddCardToDeckPartialView()
// [HttpPost] AddNewCardFromPartial()
//
// - EDIT FLASHCARD
// [HttpGet] ShowFlashcardEditView()
// [HttpPost] EditFlashcardFromView()
// [HttpGet] ShowFlashcardEditPartialView() // only during FlaschardPractice
// [HttpPost] EditFlashcardFromPartialView()
//
// - DELETE FLASHCARD
// [HttpPost] DeleteCard()
//
// *****************************************************

namespace MVC.Controllers.CRUD
{
    [Authorize]
    public class FlashcardController : Controller
    {
        private readonly IFlashcardService _flashcardService;
        private readonly IDeckService _deckService;
        private readonly ISharedCRUDService _sharedCRUD;
        public FlashcardController(IFlashcardService flashcardService, IDeckService deckService, ISharedCRUDService sharedCRUD)
        {
            _flashcardService = flashcardService;
            _deckService = deckService;
            _sharedCRUD = sharedCRUD;
        }

        public void ClearSessionData()
        {
            HttpContext.Session.Remove("List<FlashcardsAndDecksViewEntity>_FlashcardsOverviewTable");
            HttpContext.Session.Remove("List<DeckEntity>_FlashcardsOverviewTable");
            HttpContext.Session.Remove("List<DeckEntity>_DecksOverviewTable");

            foreach (string key in HttpContext.Session.Keys.ToList())
            {
                if (key.StartsWith("List<FlashcardEntity>_ShowFlashcardsForDeckPartialView_"))
                {
                    HttpContext.Session.Remove(key);
                }
            }

            foreach (string key in HttpContext.Session.Keys.ToList()) // because of ShowAddCardToDeckPartialView
            {
                if (key.StartsWith("DeckEntity_ShowDeckEditView_"))
                {
                    HttpContext.Session.Remove(key);
                }
            }

            foreach (string key in HttpContext.Session.Keys.ToList())
            {
                if (key.StartsWith("DeckEntity_ShowAllFlashcardsNotInDeckForSelectionView_"))
                {
                    HttpContext.Session.Remove(key);
                }
            }

            foreach (string key in HttpContext.Session.Keys.ToList())
            {
                if (key.StartsWith("List<FlashcardEntity>_ShowAllFlashcardsNotInDeckForSelectionView_"))
                {
                    HttpContext.Session.Remove(key);
                }
            }

            HttpContext.Session.Remove("List<FlashcardsAndDecksViewEntity>_StartPractice"); // from FlashcardPracticeController
            Response.Cookies.Delete("FiveMostDifficultFlashcardsCookie");
        }

        // ------------------- RETURN LIST OF FLASHCARDS / Overview table -----------------------

        // MAIN METHOD FOR PAGING, SORTING, FILTERING VOCABULARY LIST BASED ON PARAMETERS
        public async Task<ActionResult> FlashcardsOverviewTable(int? page, string sortBy, string filterBy, string deckName, string bookName, string lessonName)
        {
            string stringId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Guid.TryParse(stringId, out Guid userId);
            
            ViewBag.UserId = userId;

            ViewBag.CurrentUrl = HttpContext.Request.Path;

            List<FlashcardsAndDecksViewEntity> entireList;
            string hasList = HttpContext.Session.GetString("List<FlashcardsAndDecksViewEntity>_FlashcardsOverviewTable");

            List<DeckEntity> deckData;
            string hasDeckData = HttpContext.Session.GetString("List<DeckEntity>_FlashcardsOverviewTable");

            // CHECK SESSION FOR LISTS
            if (string.IsNullOrEmpty(hasList) || string.IsNullOrEmpty(hasDeckData))
            {
                // GET COMPLETE LIST FROM USER SO DATABASE IS ACCESSED ONLY ONCE
                entireList = await _flashcardService.GetWholeListFromFlashcardsAndDecksViewByUserIdAsync(userId);

                entireList = entireList.Where(flashcard => !(flashcard.Simplified == "THIS"
                                                            && flashcard.Traditional == "IS"
                                                            && flashcard.Pinyin == "A"
                                                            && flashcard.Translation == "PLACEHOLDER")).ToList();

                HttpContext.Session.SetString("List<FlashcardsAndDecksViewEntity>_FlashcardsOverviewTable", JsonConvert.SerializeObject(entireList));

                // GET DECK DATA FROM ENTIRE LIST
                deckData = StaticUtilityMethods.ConvertListOfFlashcardsAndDecksViewEntityToDeckEntityList(entireList);
                HttpContext.Session.SetString("List<DeckEntity>_FlashcardsOverviewTable", JsonConvert.SerializeObject(deckData));
            }
            else
            {
                entireList = JsonConvert.DeserializeObject<List<FlashcardsAndDecksViewEntity>>(hasList);
                deckData = JsonConvert.DeserializeObject<List<DeckEntity>>(hasDeckData);
            }

            // retrieve deck info to initially populate dropdown menu
            var deckNames = deckData.Where(x => x.DeckName != null).Select(x => x.DeckName).Distinct().OrderBy(x => x).ToList();
            var bookNames = deckData.Where(x => x.BookName != null).Select(x => x.BookName).Distinct().OrderBy(x => x).ToList();
            var unitNames = deckData.Where(x => x.UnitName != null).Select(x => x.UnitName).Distinct().OrderBy(x => x).ToList();
            ViewBag.DeckSelection = new SelectList(deckNames);
            ViewBag.BookSelection = new SelectList(bookNames);
            ViewBag.UnitSelection = new SelectList(unitNames);

            ViewBag.DeckName = deckName;
            ViewBag.TextbookName = bookName;
            ViewBag.LessonUnit = lessonName;

            // FILTER LIST BASED ON filterBy, deckName, bookName, lessonName
            List<FlashcardsAndDecksViewEntity> filteredList = _flashcardService.FilterListByParameters(entireList, filterBy, deckName, bookName, lessonName);
            int selectionCount = filteredList.Count();

            ViewBag.SelectionCount = selectionCount;

            ViewBag.SearchFilter = filterBy;

            //SORT LIST BASED ON sortBy
            IEnumerable<FlashcardsAndDecksViewEntity> sortedList = _flashcardService.SortListByParameter(filteredList, sortBy);

            ViewBag.SortByCharacters = string.IsNullOrEmpty(sortBy) ? "char_desc" : "";
            ViewBag.SortByPinyin = sortBy == "pinyin_asc" ? "pinyin_desc" : "pinyin_asc";
            ViewBag.SortByTranslation = sortBy == "transl_asc" ? "transl_desc" : "transl_asc";
            ViewBag.SortByTimeUpdated = sortBy == "updated_asc" ? "updated_desc" : "updated_asc";
            ViewBag.SortByTimeAdded = sortBy == "added_asc" ? "added_desc" : "added_asc";
            ViewBag.SortByDifficulty = sortBy == "score_asc" ? "score_desc" : "score_asc";
            ViewBag.CurrentSort = sortBy;

            //GET THE CORRECT PAGE FROM PAGE NUMBER
            var (pagedList, pageNumber, totalPages) = _flashcardService.GetTheSpecificPage(sortedList, page);

            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = totalPages;

            return View("FlashcardsOverviewTableView", pagedList);
        }

        // GET ALL FLASHCARDS FOR A SPECIFIC DECK BY DECK ID 
        // partial view for DeckEditView and DeckDetailsView
        [HttpGet]
        public async Task<ActionResult> ShowFlashcardsForDeckPartialView(Guid deckId, int? page, string sortBy, string filterBy, string urlString)
        {
            ViewBag.DeckId = deckId;
            ViewBag.CurrentUrl = urlString;

            List<FlashcardEntity> flashcards;
            string hasList = HttpContext.Session.GetString($"List<FlashcardEntity>_ShowFlashcardsForDeckPartialView_{deckId}");

            // CHECK SESSION FOR LISTS
            if (string.IsNullOrEmpty(hasList))
            {
                // GET COMPLETE LIST FROM USER SO DATABASE IS ACCESSED ONLY ONCE
                flashcards = await _flashcardService.GetAllFlashcardsFromDeckByIdAsync(deckId);
                string sessionKey = $"List<FlashcardEntity>_ShowFlashcardsForDeckPartialView_{deckId}";
                HttpContext.Session.SetString(sessionKey, JsonConvert.SerializeObject(flashcards));
            }
            else
            {
                flashcards = JsonConvert.DeserializeObject<List<FlashcardEntity>>(hasList);
            }

            // CHECK IF LIST ONLY HAS PLACEHOLDER
            if (flashcards.Count() == 1)
            {
                bool isPlaceholder = flashcards.Any(flashcard => flashcard.Simplified == "THIS"
                                                                && flashcard.Traditional == "IS"
                                                                && flashcard.Pinyin == "A"
                                                                && flashcard.Translation == "PLACEHOLDER");
                if (isPlaceholder) { return new EmptyResult(); }  // don't show anything if only placeholder exists                  
            }
            // convert to other model to reuse filtering/sorting/paging methods
            // technically not DRY, but even if the FilterListByParameterAsync method was generic, I'd still have to implement a conversion logic because code doesn't know when FlashcardEntity.Id is the same as FlashcardsAndDecksViewEntity.Id, so this is ok
            List<FlashcardsAndDecksViewEntity> list = StaticUtilityMethods.ConvertListOfFlashcardEntityToFlashcardsAndDecksViewEntity(flashcards);

            // filtering
            IEnumerable<FlashcardsAndDecksViewEntity> filteredList = _flashcardService.FilterListByParameters(list, filterBy, null, null, null);

            ViewBag.SearchFilter = filterBy;

            // sorting
            IEnumerable<FlashcardsAndDecksViewEntity> sortedList = _flashcardService.SortListByParameter(filteredList, sortBy);

            ViewBag.SortByCharacters = string.IsNullOrEmpty(sortBy) ? "char_desc" : "";
            ViewBag.SortByPinyin = sortBy == "pinyin_asc" ? "pinyin_desc" : "pinyin_asc";
            ViewBag.SortByTranslation = sortBy == "transl_asc" ? "transl_desc" : "transl_asc";
            ViewBag.SortByTimeUpdated = sortBy == "updated_asc" ? "updated_desc" : "updated_asc";
            ViewBag.SortByTimeAdded = sortBy == "added_asc" ? "added_desc" : "added_asc";
            ViewBag.SortByDifficulty = sortBy == "score_asc" ? "score_desc" : "score_asc";
            ViewBag.CurrentSort = sortBy;

            // paging
            var (pagedList, pageNumber, totalPages) = _flashcardService.GetTheSpecificPage(sortedList, page);

            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = totalPages;

            // convert back to FlashcardEntity
            flashcards = StaticUtilityMethods.ConvertListOfFlashcardsAndDecksViewEntityToFlashcardEntity(pagedList.ToList());

            // check are we coming from public decks or user deck overview
            bool copyPublicDeckFromDeckDetailsView = JsonConvert.DeserializeObject<bool>(HttpContext.Session.GetString("bool_flag_ShowDeckDetails") ?? "false");
            
            if (!copyPublicDeckFromDeckDetailsView)
            {
                // check is it a deck or is the list without deck
                DeckEntity thisDeck = JsonConvert.DeserializeObject<DeckEntity>(HttpContext.Session.GetString($"DeckEntity_ShowDeckEditView_{deckId}"));
                bool isNoDeck = string.IsNullOrEmpty(thisDeck.DeckName) && string.IsNullOrEmpty(thisDeck.BookName) && string.IsNullOrEmpty(thisDeck.UnitName);
                ViewBag.IsNoDeck = isNoDeck;
            }
            else
            {
                ViewBag.IsNoDeck = false; // ViewBag value is always needed in the partial view; if coming from public decks, it's never a null-deck
            }

            return PartialView("_FlashcardsTablePartialView", flashcards);
        }

        // partial view for Views/ProfilePage/ProfilePageView
        public async Task<ActionResult> ShowFiveMostDifficultFlashcards()
        {
            string stringId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Guid.TryParse(stringId, out Guid userId);

            List<FlashcardEntity> flashcards;

            string hasFiveMostDifficultFlashcards = Request.Cookies["FiveMostDifficultFlashcardsCookie"];
            if (string.IsNullOrEmpty(hasFiveMostDifficultFlashcards))
            {
                flashcards = await _flashcardService.GetFiveMostDifficultFlashcardsByUserIdAsync(userId);

                Response.Cookies.Append("FiveMostDifficultFlashcardsCookie", JsonConvert.SerializeObject(flashcards));
            }
            else
            {
                flashcards = JsonConvert.DeserializeObject<List<FlashcardEntity>>(hasFiveMostDifficultFlashcards);
            }

            return PartialView("_MostDifficultFlashcardsPartialView", flashcards);
        }


        // ------------------- ADD EXISTING FLASHCARDS TO EXISTING DECK / save new relationships -----------------------

        [HttpGet]
        public async Task<ActionResult> ShowAllFlashcardsNotInDeckForSelectionView(Guid deckId, int? page, string sortBy, string filterBy)
        {
            ViewBag.DeckId = deckId;

            DeckEntity selectedDeck;

            // CHECK SESSION FOR DECK DATA
            string hasDeck = HttpContext.Session.GetString($"DeckEntity_ShowAllFlashcardsNotInDeckForSelectionView_{deckId}");
            if (string.IsNullOrEmpty(hasDeck))
            {
                // GET COMPLETE LIST FROM USER SO DATABASE IS ACCESSED ONLY ONCE
                selectedDeck = await _deckService.GetOneDeckByIdAsync(deckId);
                string sessionKey = $"DeckEntity_ShowAllFlashcardsNotInDeckForSelectionView_{deckId}";
                HttpContext.Session.SetString(sessionKey, JsonConvert.SerializeObject(selectedDeck));
            }
            else
            {
                selectedDeck = JsonConvert.DeserializeObject<DeckEntity>(hasDeck);
            }

            ViewBag.DeckName = selectedDeck.DeckName;
            ViewBag.BookName = selectedDeck.BookName;
            ViewBag.UnitName = selectedDeck.UnitName;

            List<FlashcardEntity> flashcards;

            // CHECK SESSION FOR LISTS
            string hasList = HttpContext.Session.GetString($"List<FlashcardEntity>_ShowAllFlashcardsNotInDeckForSelectionView_{deckId}");
            if (string.IsNullOrEmpty(hasList))
            {
                string stringId = User.FindFirst(ClaimTypes.NameIdentifier).Value; // dohvati Id ulogiranog usera
                Guid.TryParse(stringId, out Guid userId);

                // GET COMPLETE LIST FROM USER SO DATABASE IS ACCESSED ONLY ONCE
                List<FlashcardEntity> flashcardsNotInDeck = await _flashcardService.GetAllFlashcardsNotInDeckByDeckIdAsync(userId, deckId);
                List<FlashcardEntity> flashcardsInDeck = await _flashcardService.GetAllFlashcardsFromDeckByIdAsync(deckId);

                flashcards = StaticUtilityMethods.FilterOutAndReturnOnlyDistinctByIdFromFirstList(flashcardsNotInDeck, flashcardsInDeck);
                string sessionKey = $"List<FlashcardEntity>_ShowAllFlashcardsNotInDeckForSelectionView_{deckId}";
                HttpContext.Session.SetString(sessionKey, JsonConvert.SerializeObject(flashcards));
            }
            else
            {
                flashcards = JsonConvert.DeserializeObject<List<FlashcardEntity>>(hasList);
            }

            ViewBag.TotalFound = flashcards.Count;

            // convert to other model to reuse filtering/sorting/paging methods
            // technically not DRY, but even if the FilterListByParameterAsync method was generic, I'd still have to implement a conversion logic because code doesn't know when FlashcardEntity.Id is the same as FlashcardsAndDecksViewEntity.Id, so this is ok
            List<FlashcardsAndDecksViewEntity> list = StaticUtilityMethods.ConvertListOfFlashcardEntityToFlashcardsAndDecksViewEntity(flashcards);

            // filtering, sorting, paging
            var (pagedList, pageNumber, totalPages) = _flashcardService.FilterSortAndPageFlashcardList(list, page, sortBy, filterBy);

            ViewBag.SearchFilter = filterBy;

            ViewBag.SortByCharacters = string.IsNullOrEmpty(sortBy) ? "char_desc" : "";
            ViewBag.SortByPinyin = sortBy == "pinyin_asc" ? "pinyin_desc" : "pinyin_asc";
            ViewBag.SortByTranslation = sortBy == "transl_asc" ? "transl_desc" : "transl_asc";
            ViewBag.SortByTimeUpdated = sortBy == "updated_asc" ? "updated_desc" : "updated_asc";
            ViewBag.SortByTimeAdded = sortBy == "added_asc" ? "added_desc" : "added_asc";
            ViewBag.SortByDifficulty = sortBy == "score_asc" ? "score_desc" : "score_asc";
            ViewBag.CurrentSort = sortBy;

            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = totalPages;

            // convert back to FlashcardEntity
            flashcards = StaticUtilityMethods.ConvertListOfFlashcardsAndDecksViewEntityToFlashcardEntity(pagedList.ToList());

            return View("FlashcardsAddToSelectedDeckView", flashcards);
        }
        [HttpPost]
        public async Task<ActionResult> AddSelectedFlashcardsToSelectedDeck(Guid deckId, string flashcardIds)
        {
            if(string.IsNullOrEmpty(flashcardIds))
            {
                TempData["FailureMessage"] = "You haven't selected anything.";
                return RedirectToAction("ShowAllFlashcardsNotInDeckForSelectionView", "Flashcard", new { deckId });
            }

            string userId = User.FindFirst(ClaimTypes.NameIdentifier).Value; // dohvati Id ulogiranog usera

            string[] flashcardIdArray = flashcardIds.Split(',').Select(id => id.Trim()).ToArray();
            HashSet<Guid> flashcardIdSet = new HashSet<Guid>();

            foreach (string flashcardId in flashcardIdArray)
            {
                flashcardIdSet.Add(Guid.Parse(flashcardId));
            }

            ResultModel result = await _sharedCRUD.SaveRelationshipToTernaryTableAsync(Guid.Parse(userId), deckId, flashcardIdSet);

            if (result == null)
            {
                TempData["FailureMessage"] = "Something went wrong! No flashcards were added.";
            }
            else if (!result.IsSuccess)
            {
                await _sharedCRUD.RemovePlaceholderFlashcardAsync(deckId);
                await _sharedCRUD.RemoveFlashcardsFromUnnamedDeckAsync(Guid.Parse(userId));
                await _sharedCRUD.UpdateEntriesCountByDeckIdAsync(deckId);
                TempData["FailureMessage"] = result.Message;
                
                ClearSessionData();
            }
            await _sharedCRUD.RemovePlaceholderFlashcardAsync(deckId);
            await _sharedCRUD.RemoveFlashcardsFromUnnamedDeckAsync(Guid.Parse(userId));
            await _sharedCRUD.UpdateEntriesCountByDeckIdAsync(deckId);
            TempData["SuccessMessage"] = result.Message;

            ClearSessionData();
            return RedirectToAction("ShowAllFlashcardsNotInDeckForSelectionView", "Flashcard", new { deckId });
        }


        // ------------------- CREATE NEW FLASHCARD -----------------------

        [HttpGet]
        public ActionResult ShowCreateView()
        {
            return View("FlashcardAddView");
        }
        [HttpPost]
        public async Task<ActionResult> AddNewCard(FlashcardsAndDecksViewEntity model)
        {
            if((string.IsNullOrEmpty(model.Simplified) && string.IsNullOrEmpty(model.Traditional)) || string.IsNullOrEmpty(model.Pinyin) || string.IsNullOrEmpty(model.Translation))
            {
                TempData["FailureMessage"] = "Flashcard must contain required values - either simplified or traditional characters, as well as pinyin and translation. Please try again.";
                return RedirectToAction("ShowCreateView");
            }

            string stringId = User.FindFirst(ClaimTypes.NameIdentifier).Value; // dohvati Id ulogiranog usera
            Guid.TryParse(stringId, out Guid userId);

            ResultModel result = await _flashcardService.AddNewFlashcardAsync(model, userId);
            if (result == null)
            {
                TempData["FailureMessage"] = "Something went wrong!";
                return RedirectToAction("ShowCreateView");
            }
            else if (!result.IsSuccess)
            {
                TempData["FailureMessage"] = result.Message;
                return RedirectToAction("ShowCreateView");
            }
            TempData["SuccessMessage"] = result.Message;

            ClearSessionData();

            return RedirectToAction("ShowCreateView");
        }

        [HttpGet] // for DeckEditView; to add flashcards to the deck we're editing
        public async Task<ActionResult> ShowAddCardToDeckPartialView(Guid deckId)
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

            FlashcardsAndDecksViewEntity model = StaticUtilityMethods.ConvertDeckEntityToFlashcardsAndDecksViewEntity(deck);

            return PartialView("_FlashcardAddPartialView", model);
        }
        [HttpPost]
        public async Task<ActionResult> AddNewCardFromPartial(FlashcardsAndDecksViewEntity model)
        {
            if ((string.IsNullOrEmpty(model.Simplified) && string.IsNullOrEmpty(model.Traditional)) || string.IsNullOrEmpty(model.Pinyin) || string.IsNullOrEmpty(model.Translation))
            {
                TempData["FailureMessage"] = "Flashcard must contain required values - either simplified or traditional characters, as well as pinyin and translation. Please try again.";
                return RedirectToAction("ShowDeckEditView", "Deck", new { model.DeckId });
            }

            string stringId = User.FindFirst(ClaimTypes.NameIdentifier).Value; // dohvati Id ulogiranog usera
            Guid.TryParse(stringId, out Guid userId);

            ResultModel result = await _flashcardService.AddNewFlashcardAsync(model, userId);
            if (result == null)
            {
                TempData["FailureMessage"] = "Something went wrong!";
                return RedirectToAction("ShowDeckEditView", "Deck", new { model.DeckId });
            }
            else if (!result.IsSuccess)
            {
                TempData["FailureMessage"] = result.Message;
                return RedirectToAction("ShowDeckEditView", "Deck", new { model.DeckId });
            }
            TempData["SuccessMessage"] = result.Message;

            ClearSessionData();

            return RedirectToAction("ShowDeckEditView", "Deck", new { model.DeckId });
        }


        // ------------------- EDIT FLASHCARD -----------------------

        [HttpGet]
        public async Task<ActionResult> ShowFlashcardEditView(Guid id, string source, string deck, int? page, string sortBy, string filterBy, string deckName, string bookName, string lessonName)
        {
            FlashcardEntity flashcard = await _flashcardService.GetOneFlashcardByIdAsync(id);

            ViewBag.CurrentUrl = source;
            ViewBag.DeckId = deck;
            ViewBag.CurrentPage = page;
            ViewBag.CurrentSort = sortBy;
            ViewBag.SearchFilter = filterBy;
            ViewBag.DeckName = deckName;
            ViewBag.TextbookName = bookName;
            ViewBag.LessonUnit = lessonName;

            return View("FlashcardEditView", flashcard);
        }
        [HttpPost]
        public async Task<ActionResult> EditFlashcardFromView(FlashcardEntity flashcard, string source, string deck)
        {
            bool result = await _flashcardService.EditOneFlashcardByIdAsync(flashcard);
            if (result)
            {
                TempData["SuccessMessage"] = "Saved!";

                ClearSessionData();

                return RedirectToAction("ShowFlashcardEditView", new { flashcard.Id, source, deck });
            }
            else
            {
                TempData["FailureMessage"] = "Something went wrong!";

                ClearSessionData();

                return RedirectToAction("ShowFlashcardEditView", new { flashcard.Id, source, deck });
            }
        }

        [HttpGet]
        public ActionResult ShowFlashcardEditPartialView(int index)
        {
            List<FlashcardsAndDecksViewEntity> list = JsonConvert.DeserializeObject<List<FlashcardsAndDecksViewEntity>>(HttpContext.Session.GetString("List<FlashcardsAndDecksViewEntity>_FlashcardPractice"));
                        
            FlashcardsAndDecksViewEntity toBeEdited = list[index];
            return PartialView("_FlashcardEditPartialView", toBeEdited);            
        }
        [HttpPost]
        public async Task<ActionResult> EditFlashcardFromPartialView(FlashcardsAndDecksViewEntity updatedCharacter, int index)
        {
            FlashcardEntity flashcard = StaticUtilityMethods.ConvertFlashcardsAndDecksViewEntityToFlashcardEntity(updatedCharacter);
            bool result = await _flashcardService.EditOneFlashcardByIdAsync(flashcard);
            if (result)
            {
                TempData["SuccessMessage"] = "Updates are saved but will only show from next time.";

                ClearSessionData();

                return RedirectToAction("FlashcardPractice", "FlashcardPractice", new { index });
            }
            else
            {
                return null;
            }
        }

        // ------------------- DELETE FLASHCARD -----------------------

        public async Task<ActionResult> DeleteCard(Guid id, string source, string deckId, int? page, string sortBy, string filterBy, string deckName, string bookName, string lessonName)
        {
            await _flashcardService.DeleteFlashcardByIdAsync(id);

            if (!string.IsNullOrEmpty(deckId)) 
            {
                await _sharedCRUD.UpdateEntriesCountByDeckIdAsync(Guid.Parse(deckId));
            }            

            ClearSessionData();

            string[] urlSegments = source.Split('/');
            string controllerName = urlSegments[1];
            string methodName = urlSegments[2];

            return RedirectToAction(methodName, controllerName, new { deckId, page, sortBy, filterBy, deckName, bookName, lessonName });
        }

        
    }
}
