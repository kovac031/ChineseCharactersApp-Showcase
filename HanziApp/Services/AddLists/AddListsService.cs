using System.Text;
using ClosedXML.Excel;
using Interfaces.Services;
using Models;
using Models.Database_Entities;

// *****************************************************
//
// UpdateProgressBar() / tracks events for the progress bar
//
// - UPLOAD FROM EXCEL FILE 
// UploadVocabularyListAsync()
// MapExcelSheetToList()
//
// - COPY FROM PUBLIC DECKS
// CopySelectedPublicDeckAsync()
//
// - PROCESS LIST AND SAVE TO DATABASE
// ProcessAndSaveTheIncomingListAsync()
// CleanUpRemainingDuplicatesAsync()
//
// *****************************************************

namespace Services.AddLists
{
    public class AddListsService : IAddListsService
    {
        private readonly ISharedCRUDService _sharedCRUD;
        private readonly IFlashcardService _flashcardService;
        private readonly IDeckService _deckService;
        public AddListsService(ISharedCRUDService sharedCRUD, IFlashcardService flashcardService, IDeckService deckService)
        {
            _sharedCRUD = sharedCRUD;
            _flashcardService = flashcardService;
            _deckService = deckService;
        }

        // PROGRESS BAR TRACKING 
        // event handling

        public event EventHandler<ProgressBarModel> ProgressBarUpdated;
        protected void UpdateProgressBar(int value, string message)
        {
            ProgressBarUpdated?.Invoke(this, new ProgressBarModel
            {
                ProgressValue = value,
                ProgressMessage = message
            });
        }

        // --------------------- UPLOAD FROM EXCEL FILE ---------------------------

        // UPLOAD FROM EXCEL LIST AND SAVE TO DATABASE
        public async Task<ResultModel> UploadVocabularyListAsync(UploadModel model, string stringId)
        {
            ResultModel result = new ResultModel();

            List<FlashcardEntity> rawList = MapExcelSheetToList(model);

            if(rawList == null)
            {
                result.IsSuccess = false;
                result.Message = "Something is wrong with the file you're tying to upload.";
                return result;
            }
            else if(rawList.Count > 10000)
            {
                result.IsSuccess = false;
                result.Message = "Your file has too many rows. To prevent unnecessary strain on the server, the service only accepts uploads of under 10,000 rows. Please adjust and try again.";
                return result;
            }

            bool notAllColumnsIncluded = rawList.Any(flashcard => 
            (string.IsNullOrEmpty(flashcard.Simplified) && string.IsNullOrEmpty(flashcard.Traditional)) 
            || string.IsNullOrEmpty(flashcard.Pinyin) 
            || string.IsNullOrEmpty(flashcard.Translation));

            if (notAllColumnsIncluded)
            {
                result.IsSuccess = false;
                result.Message = "All rows must have values for the character and its pinyin and translation. Please include either simplified or traditional characters, as well as pinyin and translation for all rows, then try again.";
                return result;
            }

            (List<FlashcardEntity> cleanList, List<FlashcardEntity> foundDuplicates) = StaticUtilityMethods.RemoveDuplicatesFromExcelListIfFound(rawList); // the list user is uploading may contain double entries or blank rows, this removes those

            DeckEntity deck = model.Deck;

            result = await ProcessAndSaveTheIncomingListAsync(cleanList, deck, Guid.Parse(stringId));

            // info about duplicates for the user; how many and which words were found more than once in their list and were removed

            int duplicatesCount = foundDuplicates.Count();
            if (duplicatesCount > 0) 
            {
                string duplicatesString = string.Join(", ", foundDuplicates.Select(x => $"({x.Simplified ?? x.Traditional}, {x.Pinyin})"));

                StringBuilder duplicates = new StringBuilder(result.Message);
                result.Message = duplicates.Append($"\nHowever, your list contained {duplicatesCount} words or phrases as duplicates, and were removed so that you don't have duplicate flashcards.").ToString();
                result.Message = duplicates.Append($"\nThese were: {duplicatesString}").ToString();
            }

            return result;
        }

        // MAPPING EXCEL FILE TO LIST
        public List<FlashcardEntity> MapExcelSheetToList(UploadModel model)
        {
            if (model.ExcelFile == null || model.ExcelFile.Length == 0)
            {
                return null;
            }

            List<FlashcardEntity> excelSheetList = new List<FlashcardEntity>();

            using (Stream stream = model.ExcelFile.OpenReadStream())
            {
                using (XLWorkbook workbook = new XLWorkbook(stream))
                {
                    IXLWorksheet worksheet = workbook.Worksheet(1);
                    IXLRow firstRow = worksheet.Row(1); // inicira headerRow, provjeravat cu kako se zove svaki stupac, to je u prvom redu

                    int simplifiedIndex = -1, traditionalIndex = -1, pinyinIndex = -1, translationIndex = -1, noteIndex = -1; // -1 znaci nema ga, u listi je prvi [0] tako da -1 nikad ne postoji

                    for (int columnNb = 1; columnNb <= firstRow.LastCellUsed().Address.ColumnNumber; columnNb++)
                    {
                        string columnName = firstRow.Cell(columnNb).GetString().ToLower();
                        if (columnName == "simplified".ToLower()) simplifiedIndex = columnNb; // kada pronadje "simplified" spremit ce columNb (broj stupca) na simplifiedIndex, i ic dalje
                        if (columnName == "traditional".ToLower()) traditionalIndex = columnNb;
                        if (columnName == "pinyin".ToLower()) pinyinIndex = columnNb;
                        if (columnName == "translation".ToLower()) translationIndex = columnNb;
                        if (columnName == "note".ToLower()) noteIndex = columnNb;
                    }

                    int rowCount = worksheet.LastRowUsed().RowNumber();
                    int tenThousandCheck = 0;

                    for (int row = 2; row <= rowCount; row++) // Start from row 2 to skip the header
                    {
                        IXLRow xlRow = worksheet.Row(row);
                        FlashcardEntity flashcard = new FlashcardEntity();

                        if (simplifiedIndex != -1) // dakle ako ga je pronasao nece biti -1 nego broj stupca gdje ga je pronasao
                            flashcard.Simplified = xlRow.Cell(simplifiedIndex).GetString();

                        if (traditionalIndex != -1)
                            flashcard.Traditional = xlRow.Cell(traditionalIndex).GetString();

                        if (pinyinIndex != -1)
                            flashcard.Pinyin = xlRow.Cell(pinyinIndex).GetString();

                        if (translationIndex != -1)
                            flashcard.Translation = xlRow.Cell(translationIndex).GetString();

                        if (noteIndex != -1)
                            flashcard.Note = xlRow.Cell(noteIndex).GetString();

                        if // only adds to list IF row is not completely empty
                        (
                            !string.IsNullOrEmpty(flashcard.Simplified) ||
                            !string.IsNullOrEmpty(flashcard.Traditional) ||
                            !string.IsNullOrEmpty(flashcard.Pinyin) ||
                            !string.IsNullOrEmpty(flashcard.Translation) ||
                            !string.IsNullOrEmpty(flashcard.Note)
                        )
                        {
                            excelSheetList.Add(flashcard);
                            tenThousandCheck++;
                            if (tenThousandCheck == 10001) 
                            { break; }
                        }
                    }
                }
            }
            return excelSheetList;           
        }


        // --------------------- COPY FROM PUBLIC DECKS ---------------------------

        // ADD FROM EXISTING PUBLIC DECKS
        // copy selected public deck with flashcard values
        public async Task<ResultModel> CopySelectedPublicDeckAsync(Guid userId, Guid deckId)
        {
            List<FlashcardEntity> flashcardsInSelectedDeck = await _flashcardService.GetAllFlashcardsFromDeckByIdAsync(deckId);

            DeckEntity selectedDeck = await _deckService.GetOneDeckByIdAsync(deckId);

            List<DeckEntity> existingDecks = await _sharedCRUD.GetAllDecksByUserIdAsync(userId);

            ResultModel result = new ResultModel();

            foreach (DeckEntity deck in existingDecks)
            {
                bool sameDeckName = (deck.DeckName?.Trim().ToLower() ?? "") == (selectedDeck.DeckName?.Trim().ToLower() ?? ""); // if null treat as ""
                bool sameBookName = (deck.BookName?.Trim().ToLower() ?? "") == (selectedDeck.BookName?.Trim().ToLower() ?? "");
                bool sameUnitName = (deck.UnitName?.Trim().ToLower() ?? "") == (selectedDeck.UnitName?.Trim().ToLower() ?? "");
                if (sameDeckName && sameBookName && sameUnitName)
                {
                    result.IsSuccess = false;
                    result.Message = "Cannot copy this deck becaue you already have a deck named exactly the same as this one. Please check.";
                    return result;
                }
            }

            result = await ProcessAndSaveTheIncomingListAsync(flashcardsInSelectedDeck, selectedDeck, userId);
            
            // only add to counter if the deck is actually being added
            if (result.IsSuccess = true && result.Message == "Success! Everything was added without issues.")
            {
                await _deckService.UpdateDeckUsageCountByDeckIdAsync(deckId, selectedDeck.UsageCount);
            }            

            return result;
        }


        // --------------------- PROCESS LIST AND SAVE TO DATABASE ---------------------------

        public async Task<ResultModel> ProcessAndSaveTheIncomingListAsync(List<FlashcardEntity> newList, DeckEntity deck, Guid userId)
        {
            ResultModel result = new ResultModel(); // instantiating the return object
            bool onlyAppendingFlashcards = false;
            bool madeNewDeck = false;

            UpdateProgressBar(10, "Fetching your existing lists.");
            List<FlashcardEntity> existingCards = await _flashcardService.GetAllFlashcardsByUserIdAsync(userId);

            // FIND DUPLICATES
            // ARE THERE INCOMING FLASHCARDS WHICH ALREADY EXIST IN EXISTING VOCABULARY
            // incoming list may contain characters which already exist in the user's vocabulary, those won't be added but will get updated
            
            UpdateProgressBar(20, "Looking for duplicates.");
            List<FlashcardEntity> duplicatesFoundInExistingVocabulary = StaticUtilityMethods.FindDuplicateCharactersOfIncomingListInExistingList(newList, existingCards); 

            // USE FOUND DUPLICATES TO SEPARATE INCOMING LIST INTO BRAND NEW FLASHCARD AND THOSE TO JUST APPEND INFO
            (List<FlashcardEntity> brandNewFlashcards, List<FlashcardEntity> newInfoForOldFlashcards) incomingFlashcardsTuple = StaticUtilityMethods.SeparateIncomingFlashcardsBetweenBrandNewAndNot(newList, duplicatesFoundInExistingVocabulary);

            UpdateProgressBar(30, "Saving brand new vocabulary to database.");
            HashSet<Guid> newFlashcardIds = await _flashcardService.SaveToFlashcardsTableAndReturnNewIdsAsync(incomingFlashcardsTuple.brandNewFlashcards);

            // if no flashcardIds, check if there were duplicates to append existing info
            // will append later
            if (newFlashcardIds.Count() == 0 && incomingFlashcardsTuple.newInfoForOldFlashcards.Count() > 0)
            {
                onlyAppendingFlashcards = true;
            }

            // rollback unless we're appending existing flashcards
            if ((newFlashcardIds.Count() + incomingFlashcardsTuple.newInfoForOldFlashcards.Count()) != newList.Count() && !onlyAppendingFlashcards)
            {
                // rollback, because adding flashcards failed
                await _flashcardService.DeleteMultipleFlashcardsByFlashcardIdsAsync(newFlashcardIds);
                result.IsSuccess = false;
                result.Message = "Failed to save flashcards. Rolling back attempted changes.";
                return result;
            }

            // ADDING TO EXISTING DECK (retrieving existing Id) OR MAKING A NEW ONE
            UpdateProgressBar(40, "Saving your deck data.");
            Guid deckId = await _sharedCRUD.GetExistingDeckIdByDeckBookUnitNamesAndUserIdAsync(deck.DeckName, deck.BookName, deck.UnitName, userId);

            if (deckId == Guid.Empty)
            {
                // CREATES A NEW DECK AND RETURNS ITS ID
                deckId = await _deckService.SaveToDecksTableAndReturnIdAsync(deck);
                madeNewDeck = true;
            }

            // Add newFlashcardIds and duplicatesFoundInExistingVocabulary Ids to new list for the purpose of making relationships in ternary table
            HashSet<Guid> flashcardIdsForTernary = new HashSet<Guid>();
            foreach (FlashcardEntity flashcard in duplicatesFoundInExistingVocabulary)
            {
                flashcardIdsForTernary.Add(flashcard.Id);
            }
            foreach (Guid flashcardId in newFlashcardIds)
            {
                flashcardIdsForTernary.Add(flashcardId);
            }

            // must avoid pairing existing flashcardIds with existing deckIds
            // also must avoid excluding existing flashcardIds early, because it will also exclude them from pairing with NEW decks
            // only solution is new filtering method - to filter out already existing user/deck/flashcard Id combos in the TernaryTable
            UpdateProgressBar(50, "Establishing database table relationships.");
            HashSet<Guid> safeFlashcardIds = await _sharedCRUD.FilterOutExistingTernaryTablePrimaryKeyCombinationsAsync(userId, deckId, flashcardIdsForTernary);

            result = await _sharedCRUD.SaveRelationshipToTernaryTableAsync(userId, deckId, safeFlashcardIds);

            // saving new relation only if there was a new deck or at least 1 new flashcard
            if (!result.IsSuccess)
            {
                if (!onlyAppendingFlashcards)
                {
                    // rollback, adding ternary relationships failed
                    await _sharedCRUD.DeleteTernaryTableRelationshipAsync(userId, deckId, newFlashcardIds);

                    if (madeNewDeck)
                    {
                        // rollback, relationship removed, cleaning up deck
                        await _deckService.DeleteDeckByIdAsync(deckId);
                    }

                    result.IsSuccess = false;
                    result.Message = "Failed SaveToTernaryTableAsync. Rolling back attempted changes.";
                    return result;
                }
                else // did not save new relationships only because all flashcards are duplicates that only need appending
                {
                    UpdateProgressBar(60, "New information found for your existing flashcards, now updating.");
                    // appending values found in incomingList duplicates to existing flashcards
                    await _flashcardService.AppendExistingPinyinAndTranslationAndNoteValuesAsync(incomingFlashcardsTuple.newInfoForOldFlashcards, duplicatesFoundInExistingVocabulary);

                    result.IsSuccess = true;
                    result.Message = "All these flashcards already exist in your vocabulary. Only changes to their pinyin, translation or note fields was made if something different was found.";
                    return result;
                }

            }
            else // saved new relationships
            {
                if (!madeNewDeck) // update TimeUpdated for DecksTable since new entries were added to existing deck
                {
                    Task updateTimeUpdated = _deckService.UpdateTimeUpdatedForDeckByIdAsync(deckId);
                    Task removePlaceholder = _sharedCRUD.RemovePlaceholderFlashcardAsync(deckId);
                    await Task.WhenAll(updateTimeUpdated, removePlaceholder);
                }

                if (incomingFlashcardsTuple.newInfoForOldFlashcards.Count() > 0)
                {
                    UpdateProgressBar(60, "New information found for your existing flashcards, now updating.");
                    // appending values found in incomingList duplicates to existing flashcards
                    await _flashcardService.AppendExistingPinyinAndTranslationAndNoteValuesAsync(incomingFlashcardsTuple.newInfoForOldFlashcards, duplicatesFoundInExistingVocabulary);
                }

                // cleanup remaining duplicates if any were missed on initial filtering
                // must be done after appending new values to existing --> means will need a new complete list, can't reuse existingCards from the start as they don't have the new info to compare on
                // must be done after brand new vocabulary is separated
                // since a new database call will be done either way, it is simpler to work on one retrieved list than to have one retrieved list and one list with only brand new cards, yet to be saved
                UpdateProgressBar(80, "Cleaning up ...");
                await CleanUpRemainingDuplicatesAsync(userId, deckId);

                UpdateProgressBar(90, "Almost done ...");
                await _sharedCRUD.RemoveFlashcardsFromUnnamedDeckAsync(userId);

                UpdateProgressBar(99, "Finishing up ...");
                // recount how many flashcards the deck now has
                await _sharedCRUD.UpdateEntriesCountByDeckIdAsync(deckId);       
                                
                // everything went well
                result.IsSuccess = true;
                result.Message = "Success! Everything was added without issues.";
                return result;
            }
        }

        // CLEANING UP REMAINING DUPLICATES
        public async Task CleanUpRemainingDuplicatesAsync(Guid userId, Guid deckId)
        {
            List<FlashcardsAndDecksViewEntity> entriesToKeepAndAppend;
            List<FlashcardsAndDecksViewEntity> entriesToAddInfoFromAndThenDelete;

            // not very DRY, but won't be reused more than twice (first pass for Simplified, second pass for Traditional) so it's ok

            // first pass, checks by simplified
            List<FlashcardsAndDecksViewEntity> allFlashcardsFirstPass = await _flashcardService.GetWholeListFromFlashcardsAndDecksViewByUserIdAsync(userId);

            HashSet<Guid?> distinctIdsFirstPass = new HashSet<Guid?>(); // for excluding duplicates by Id
            allFlashcardsFirstPass = allFlashcardsFirstPass.Where(flashcard => distinctIdsFirstPass.Add(flashcard.Id)).ToList();

            List<FlashcardsAndDecksViewEntity> duplicatesBySimplified = allFlashcardsFirstPass.Where(flashcard => !string.IsNullOrEmpty(flashcard.Simplified))
                .GroupBy(x => x.Simplified.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(group => group.GroupBy(x => x.Id).Count() > 1) // Filter for groups with multiple distinct Ids
                .OrderBy(group => group.Key)
                .SelectMany(group => group.OrderBy(x => x.Simplified))
                .ToList();

            if (duplicatesBySimplified.Count > 1)
            {
                (entriesToKeepAndAppend, entriesToAddInfoFromAndThenDelete) = StaticUtilityMethods.SeparateOneListOfDuplicatesIntoTwoByTimeAdded(duplicatesBySimplified);

                List<FlashcardEntity> appendThisSimplified = entriesToKeepAndAppend.Select(flashcard => new FlashcardEntity
                {
                    Id = flashcard.Id,
                    Simplified = flashcard.Simplified,
                    Traditional = flashcard.Traditional,
                    Pinyin = flashcard.Pinyin,
                    Translation = flashcard.Translation,
                    Note = flashcard.Note,
                    TimeUpdated = flashcard.FlashcardTimeUpdated
                }).ToList();

                List<FlashcardEntity> hasNewInfoSimplified = entriesToAddInfoFromAndThenDelete.Select(flashcard => new FlashcardEntity
                {
                    Id = flashcard.Id,
                    Simplified = flashcard.Simplified,
                    Traditional = flashcard.Traditional,
                    Pinyin = flashcard.Pinyin,
                    Translation = flashcard.Translation,
                    Note = flashcard.Note,
                    TimeUpdated = flashcard.FlashcardTimeUpdated
                }).ToList();

                await _flashcardService.AppendExistingPinyinAndTranslationAndNoteValuesAsync(hasNewInfoSimplified, appendThisSimplified);

                HashSet<Guid> flashcardIdsForTernarySimplified = new HashSet<Guid>();

                foreach (FlashcardsAndDecksViewEntity flashcard in entriesToKeepAndAppend)
                {
                    // we're deleting flashcards so we need to replace the ones we will delete with the ones we're keeping in the deck
                    // only older entries are in multiple decks, the new one (the one getting deleted) will always only be in the deck of the current adding process
                    // so we only need to bind the old flashcard to the deck of the new flashcard being deleted

                    flashcardIdsForTernarySimplified.Add(flashcard.Id);
                }
                HashSet<Guid> safeFlashcardIdsSimplified = await _sharedCRUD.FilterOutExistingTernaryTablePrimaryKeyCombinationsAsync(userId, deckId, flashcardIdsForTernarySimplified);
                await _sharedCRUD.SaveRelationshipToTernaryTableAsync(userId, deckId, safeFlashcardIdsSimplified);

                HashSet<Guid> flashcardsToDelete = new HashSet<Guid>();
                foreach (FlashcardsAndDecksViewEntity flashcard in entriesToAddInfoFromAndThenDelete)
                {
                    flashcardsToDelete.Add(flashcard.Id);
                }
                await _sharedCRUD.DeleteTernaryTableRelationshipAsync(userId, deckId, flashcardsToDelete);
                await _flashcardService.DeleteMultipleFlashcardsByFlashcardIdsAsync(flashcardsToDelete);
            }

            // second pass, same as above but checks by traditional

            List<FlashcardsAndDecksViewEntity> allFlashcardsSecondPass = await _flashcardService.GetWholeListFromFlashcardsAndDecksViewByUserIdAsync(userId);

            HashSet<Guid?> distinctIdsSecondPass = new HashSet<Guid?>(); // for excluding duplicates by Id
            allFlashcardsSecondPass = allFlashcardsSecondPass.Where(flashcard => distinctIdsSecondPass.Add(flashcard.Id)).ToList();

            List<FlashcardsAndDecksViewEntity> duplicatesByTraditional = allFlashcardsSecondPass.Where(flashcard => !string.IsNullOrEmpty(flashcard.Traditional))
                .GroupBy(x => x.Traditional.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(group => group.GroupBy(x => x.Id).Count() > 1) // Filter for groups with multiple distinct Ids
                .OrderBy(group => group.Key)
                .SelectMany(group => group.OrderBy(x => x.Traditional))
                .ToList();

            if (duplicatesByTraditional.Count > 1)
            {
                (entriesToKeepAndAppend, entriesToAddInfoFromAndThenDelete) = StaticUtilityMethods.SeparateOneListOfDuplicatesIntoTwoByTimeAdded(duplicatesByTraditional);

                List<FlashcardEntity> appendThisTraditional = entriesToKeepAndAppend.Select(flashcard => new FlashcardEntity
                {
                    Id = flashcard.Id,
                    Simplified = flashcard.Simplified,
                    Traditional = flashcard.Traditional,
                    Pinyin = flashcard.Pinyin,
                    Translation = flashcard.Translation,
                    Note = flashcard.Note,
                    TimeUpdated = flashcard.FlashcardTimeUpdated
                }).ToList();

                List<FlashcardEntity> hasNewInfoTraditional = entriesToAddInfoFromAndThenDelete.Select(flashcard => new FlashcardEntity
                {
                    Id = flashcard.Id,
                    Simplified = flashcard.Simplified,
                    Traditional = flashcard.Traditional,
                    Pinyin = flashcard.Pinyin,
                    Translation = flashcard.Translation,
                    Note = flashcard.Note,
                    TimeUpdated = flashcard.FlashcardTimeUpdated
                }).ToList();

                await _flashcardService.AppendExistingPinyinAndTranslationAndNoteValuesAsync(hasNewInfoTraditional, appendThisTraditional);

                HashSet<Guid> flashcardIdsForTernaryTraditional = new HashSet<Guid>();
                foreach (FlashcardsAndDecksViewEntity flashcard in entriesToKeepAndAppend)
                {
                    flashcardIdsForTernaryTraditional.Add(flashcard.Id);
                }
                HashSet<Guid> safeFlashcardIdsTraditional = await _sharedCRUD.FilterOutExistingTernaryTablePrimaryKeyCombinationsAsync(userId, deckId, flashcardIdsForTernaryTraditional);
                await _sharedCRUD.SaveRelationshipToTernaryTableAsync(userId, deckId, safeFlashcardIdsTraditional);

                HashSet<Guid> flashcardsToDelete = new HashSet<Guid>();
                foreach (FlashcardsAndDecksViewEntity flashcard in entriesToAddInfoFromAndThenDelete)
                {
                    flashcardsToDelete.Add(flashcard.Id);
                }
                await _sharedCRUD.DeleteTernaryTableRelationshipAsync(userId, deckId, flashcardsToDelete);
                await _flashcardService.DeleteMultipleFlashcardsByFlashcardIdsAsync(flashcardsToDelete);
            }
        }
               
    }
}
