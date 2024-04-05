using System.Text;
using Interfaces.DatabaseAccess;
using Interfaces.Services;
using Models;
using Models.Database_Entities;

// *****************************************************
//
// - RETURN LIST OF FLASHCARDS
// GetAllFlashcardsByUserIdAsync()
// GetWholeListFromFlashcardsAndDecksViewByUserIdAsync()
// FilterSortAndPageFlashcardList()
// --> FilterListByParameters()
// --> SortListByParameter()
// --> GetTheSpecificPage()
// GetAllFlashcardsFromDeckByIdAsync()
// GetAllFlashcardsNotInDeckByDeckIdAsync()
// GetFiveMostDifficultFlashcardsByUserIdAsync()
//
// - RETURN SINGLE FLASHCARD
// GetOneFlashcardByIdAsync()
//
// - CREATE NEW FLASHCARD
// AddNewFlashcardAsync()
// SaveToFlashcardsTableAndReturnNewIdsAsync()
//
// - EDIT FLASHCARDS
// EditOneFlashcardByIdAsync()
// AppendExistingPinyinAndTranslationAndNoteValuesAsync()
//
// - DELETE FLASHCARD
// DeleteFlashcardByIdAsync()
// DeleteMultipleFlashcardsByFlashcardIdsAsync()
//
// - OTHER
// CountFlashcardsByUserIdAsync()
//
// *****************************************************

namespace Services.CRUD
{
    public class FlashcardService : IFlashcardService
    {
        private readonly IExecuteSQLquery _database;
        private readonly ISharedCRUDService _sharedCRUD;
        public FlashcardService(IExecuteSQLquery database, ISharedCRUDService sharedCRUD)
        {
            _database = database;
            _sharedCRUD = sharedCRUD; // Flashcard and Deck Services may only have dependency on sharedCRUD for CRUD, move all relevant Flashcard and Deck method to Shared since these methods are used by both
        }

        // -------------- RETURN LIST OF FLASHCARDS --------------

        // FIND FLASHCARD LIST BY AUTHOR ID 
        // metode nisu generic jer su samo dvije i preglednije je i jednostavnije ovako umjesto da silim generic, saljem string parametre za table name isl.
        public async Task<List<FlashcardEntity>> GetAllFlashcardsByUserIdAsync(Guid id)
        {
            string sqlQuery = @"SELECT DISTINCT F.*
                                FROM FlashcardsTable F
                                JOIN CardDeckUserTernaryTable T ON F.Id = T.FlashcardId
                                WHERE T.UserId = @Id;";

            var parameters = new
            {
                Id = id
            };

            List<FlashcardEntity> list = await _database.ExecuteQueryAsync_ReturnDTOlist<FlashcardEntity, object>(parameters, sqlQuery);

            return list;
        }
        public async Task<List<FlashcardsAndDecksViewEntity>> GetWholeListFromFlashcardsAndDecksViewByUserIdAsync(Guid id)
        {
            string sqlQuery = @"SELECT * FROM FlashcardsAndDecksView                                
                                WHERE AuthorId = @Id;";

            var parameters = new
            {
                Id = id
            };

            List<FlashcardsAndDecksViewEntity> list = await _database.ExecuteQueryAsync_ReturnDTOlist<FlashcardsAndDecksViewEntity, object>(parameters, sqlQuery);

            return list;
        }

        // FILTERING, SORTING, PAGING OF LIST 
        public (IEnumerable<FlashcardsAndDecksViewEntity>, int, int) FilterSortAndPageFlashcardList(List<FlashcardsAndDecksViewEntity> list, int? page, string sortBy, string filterBy)
        {
            // filtering
            IEnumerable<FlashcardsAndDecksViewEntity> filteredList = FilterListByParameters(list, filterBy, null, null, null);

            // sorting
            IEnumerable<FlashcardsAndDecksViewEntity> sortedList = SortListByParameter(filteredList, sortBy);

            // paging
            var (pagedList, pageNumber, totalPages) = GetTheSpecificPage(sortedList, page);

            return (pagedList, pageNumber, totalPages);
        }

        // FILTERING
        public List<FlashcardsAndDecksViewEntity> FilterListByParameters(List<FlashcardsAndDecksViewEntity> entireList, string filterBy, string deck, string textbook, string lessonUnit)
        {
            REDACTED FBI KGB CIA
        }

        // SORTING
        public IEnumerable<FlashcardsAndDecksViewEntity> SortListByParameter(IEnumerable<FlashcardsAndDecksViewEntity> filteredList, string sortBy)
        {
            REDACTED FBI KGB CIA
        }

        // PAGING
        public (IEnumerable<FlashcardsAndDecksViewEntity>, int, int) GetTheSpecificPage(IEnumerable<FlashcardsAndDecksViewEntity> sortedList, int? page)
        {
            REDACTED FBI KGB CIA
        }
                
        // GET ALL FLASHCARDS FROM SPECIFIED DECK BY DECK ID
        // for partial view for deck overview
        public async Task<List<FlashcardEntity>> GetAllFlashcardsFromDeckByIdAsync(Guid deckId)
        {
            REDACTED FBI KGB CIA
        }

        // GET ALL FLASHCARDS NOT IN DECK BY DECK ID
        // for when adding flashcards to deck from add to deck page
        public async Task<List<FlashcardEntity>> GetAllFlashcardsNotInDeckByDeckIdAsync(Guid userId, Guid deckId)
        {
            REDACTED FBI KGB CIA
        }

        public async Task<List<FlashcardEntity>> GetFiveMostDifficultFlashcardsByUserIdAsync(Guid userId)
        {
            string sqlQuery = @"SELECT DISTINCT TOP 5 F.*
                                FROM FlashcardsTable F
                                JOIN CardDeckUserTernaryTable T ON F.Id = T.FlashcardId
                                WHERE T.UserId = @UserId
                                AND F.DifficultyRating <> 1
                                ORDER BY F.DifficultyRating ASC;"; // != <> // smaller means more difficult, bigger means easier

            var parameters = new
            {
                UserId = userId
            };

            List<FlashcardEntity> list = await _database.ExecuteQueryAsync_ReturnDTOlist<FlashcardEntity, object>(parameters, sqlQuery);

            return list;
        }


        // -------------- RETURN SINGLE FLASHCARD --------------

        // GET FLASHCARD BY ID 
        public async Task<FlashcardEntity> GetOneFlashcardByIdAsync(Guid id)
        {
            string sqlQuery = @"SELECT * FROM FlashcardsTable
                                WHERE Id = @Id;";

            var parameters = new
            {
                Id = id
            };

            FlashcardEntity flashcard = await _database.ExecuteQueryAsync_ReturnDTO<FlashcardEntity, object>(parameters, sqlQuery);
            return flashcard;
        }


        // -------------- CREATE NEW FLASHCARD --------------

        //  ADD NEW FLASHCARD 
        public async Task<ResultModel> AddNewFlashcardAsync(FlashcardsAndDecksViewEntity model, Guid authorId)
        {
            REDACTED FBI KGB CIA
        }

        // SAVE INCOMING FLASHCARD LIST TO DATABASE
        public async Task<HashSet<Guid>> SaveToFlashcardsTableAndReturnNewIdsAsync(List<FlashcardEntity> brandNewFlashcards)
        {
            REDACTED FBI KGB CIA
        }


        // -------------- EDIT FLASHCARDS --------------

        // EDIT FLASHCARD BY ID 
        public async Task<bool> EditOneFlashcardByIdAsync(FlashcardEntity flashcard)
        {
            string sqlQuery = @"UPDATE FlashcardsTable SET 
                                Simplified = @Simplified, 
                                Traditional = @Traditional,
                                Pinyin = @Pinyin,
                                Translation = @Translation,
                                Note = @Note,
                                TimeUpdated = @TimeUpdated
                                WHERE Id = @Id";

            var parameters = new
            {
                Id = flashcard.Id,
                Simplified = flashcard.Simplified,
                Traditional = flashcard.Traditional,
                Pinyin = flashcard.Pinyin,
                Translation = flashcard.Translation,
                Note = flashcard.Note,
                TimeUpdated = DateTime.Now
            };

            int affectedRows = await _database.ExecuteQueryAsync_ReturnInt(parameters, sqlQuery);

            return affectedRows > 0;
        }

        // APPEND PINYIN, TRANSLATION, NOTE VALUES ON EXISTING FLASHCARDS, BY INCOMING ENTRY VALUES
        public async Task AppendExistingPinyinAndTranslationAndNoteValuesAsync(IEnumerable<FlashcardEntity> incomingList, IEnumerable<FlashcardEntity> oldButDuplicates)
        {
            if (incomingList != null && oldButDuplicates != null)
            {
                IEnumerable<FlashcardEntity> flashcardsToBeEdited = oldButDuplicates.DistinctBy(flashcard => flashcard.Id);
                List<FlashcardEntity> list = new List<FlashcardEntity>();

                string sqlQuery = @"UPDATE FlashcardsTable SET
                                Simplified = @Simplified,
                                Traditional = @Traditional,
                                Pinyin = @Pinyin,
                                Translation = @Translation,
                                Note = @Note,
                                TimeUpdated = @TimeUpdated
                                WHERE Id = @Id";

                foreach (FlashcardEntity flashcardToBeEdited in flashcardsToBeEdited)
                {
                    // FIRST FIND MATCHING CHARACTERS
                    FlashcardEntity matchingFlashcard = incomingList.FirstOrDefault(flashcard =>
                    (!string.IsNullOrEmpty(flashcard.Simplified) && !string.IsNullOrEmpty(flashcardToBeEdited.Simplified) && flashcard.Simplified.Trim() == flashcardToBeEdited.Simplified.Trim())
                    ||
                    (!string.IsNullOrEmpty(flashcard.Traditional) && !string.IsNullOrEmpty(flashcardToBeEdited.Traditional) && flashcard.Traditional.Trim() == flashcardToBeEdited.Traditional.Trim())
                    ); // ne mora biti kompliciranije jer je Upload logika vec odradila bolju filtraciju, ovdje su zaista identicni (osim polifona)

                    // CHECK IF POLYPHONES
                    // if both Simplified and Traditional field exists, and Simplified is identical but Traditional is not, skip
                    bool hasSimplifiedAndHasTraditional = (!string.IsNullOrEmpty(flashcardToBeEdited.Simplified) && !string.IsNullOrEmpty(matchingFlashcard.Simplified)) && (!string.IsNullOrEmpty(flashcardToBeEdited.Traditional) && !string.IsNullOrEmpty(matchingFlashcard.Traditional));
                    if (hasSimplifiedAndHasTraditional)
                    {
                        bool simplifiedIsSameButTraditionalIsDifferent = (flashcardToBeEdited.Simplified.Trim() == matchingFlashcard.Simplified.Trim()) && (flashcardToBeEdited.Traditional.Trim() != matchingFlashcard.Traditional.Trim());
                        if (simplifiedIsSameButTraditionalIsDifferent)
                        {
                            // it's a polyphone so move on
                            continue;
                        }
                    }

                    // CHECK FOR IDENTICAL VALUES TO SKIP INSTEAD OF APPENDING ALL BY DEFAULT
                    bool pinyinMatched;
                    bool translationMatched;
                    bool noteMatched;

                    StringBuilder pinyin = new StringBuilder(flashcardToBeEdited.Pinyin);
                    StringBuilder translation = new StringBuilder(flashcardToBeEdited.Translation);
                    StringBuilder note = new StringBuilder(flashcardToBeEdited.Note);

                    // append only if pinyin match is NOT found
                    pinyinMatched = StaticUtilityMethods.FindSameValueBetweenStrings(matchingFlashcard.Pinyin, flashcardToBeEdited.Pinyin);

                    if (!pinyinMatched)
                    {
                        if (string.IsNullOrEmpty(flashcardToBeEdited.Pinyin) && !string.IsNullOrEmpty(matchingFlashcard.Pinyin))
                        {
                            flashcardToBeEdited.Pinyin = matchingFlashcard.Pinyin;
                        }
                        else if (!string.IsNullOrEmpty(matchingFlashcard.Pinyin))
                        {
                            flashcardToBeEdited.Pinyin = pinyin.Append(" | " + matchingFlashcard.Pinyin).ToString();
                        }
                    }

                    // append only if translation match is NOT found
                    translationMatched = StaticUtilityMethods.FindSameValueBetweenStrings(matchingFlashcard.Translation, flashcardToBeEdited.Translation);

                    if (!translationMatched)
                    {
                        if (string.IsNullOrEmpty(flashcardToBeEdited.Translation) && !string.IsNullOrEmpty(matchingFlashcard.Translation))
                        {
                            flashcardToBeEdited.Translation = matchingFlashcard.Translation;
                        }
                        else if (!string.IsNullOrEmpty(matchingFlashcard.Translation))
                        {
                            flashcardToBeEdited.Translation = translation.Append(" | " + matchingFlashcard.Translation).ToString();
                        }
                    }

                    // append only if note match is NOT found
                    noteMatched = StaticUtilityMethods.FindSameValueBetweenStrings(matchingFlashcard.Note, flashcardToBeEdited.Note);

                    if (!noteMatched)
                    {
                        if (string.IsNullOrEmpty(flashcardToBeEdited.Note) && !string.IsNullOrEmpty(matchingFlashcard.Note))
                        {
                            flashcardToBeEdited.Note = matchingFlashcard.Note;
                        }
                        else if (!string.IsNullOrEmpty(matchingFlashcard.Note))
                        {
                            flashcardToBeEdited.Note = note.Append(" | " + matchingFlashcard.Note).ToString();
                        }
                    }

                    // check if one has an empty simplified/traditionnal field the other has, to copy over
                    bool addedCharacters = false;
                    if (string.IsNullOrEmpty(flashcardToBeEdited.Traditional) && !string.IsNullOrEmpty(matchingFlashcard.Traditional))
                    {
                        flashcardToBeEdited.Traditional = matchingFlashcard.Traditional;
                        addedCharacters = true;
                    }
                    if (string.IsNullOrEmpty(flashcardToBeEdited.Simplified) && !string.IsNullOrEmpty(matchingFlashcard.Simplified))
                    {
                        flashcardToBeEdited.Simplified = matchingFlashcard.Simplified;
                        addedCharacters = true;
                    }

                    // note TimeUpdated if any change was made
                    if (!pinyinMatched || !translationMatched || !noteMatched || addedCharacters)
                    {
                        flashcardToBeEdited.TimeUpdated = DateTime.Now;

                        list.Add(flashcardToBeEdited);
                    }
                }
                int affectedRows = await _database.ExecuteQueryAsync_ReturnInt(list, sqlQuery);
            }
        }


        // -------------- DELETE FLASHCARDS --------------

        //  DELETE FLASHCARD BY ID 
        public async Task<bool> DeleteFlashcardByIdAsync(Guid id)
        {
            bool removeRelationship = await _sharedCRUD.DeleteTernaryTableRelationshipAsync(id);

            if (!removeRelationship)
            {
                return false;
            }

            string sqlQuery = @"DELETE FROM FlashcardsTable
                                WHERE Id = @Id";

            var parameters = new
            {
                Id = id
            };

            int affectedRows = await _database.ExecuteQueryAsync_ReturnInt(parameters, sqlQuery);

            return affectedRows > 0;
        }
        public async Task DeleteMultipleFlashcardsByFlashcardIdsAsync(HashSet<Guid> flashcardIds)
        {
            string sqlQuery = @"DELETE FROM FlashcardsTable
                                WHERE Id = @Id";

            var parameters = flashcardIds.Select(id => new
            {
                Id = id
            }).ToList();

            int affectedRows = await _database.ExecuteQueryAsync_ReturnInt(parameters, sqlQuery);
        }


        // -------------- OTHER --------------               
                
        // COUNT HOW MANY FLASHCARD A USER HAS
        public async Task<int> CountFlashcardsByUserIdAsync(Guid userId)
        {
            string sqlQuery = @"SELECT COUNT(*) AS NumberOfFlashcards
                                FROM (
                                    SELECT DISTINCT F.*
                                    FROM FlashcardsTable F
                                    JOIN CardDeckUserTernaryTable T ON F.Id = T.FlashcardId
                                    WHERE T.UserId = @UserId
                                    AND NOT (
                                        F.Simplified = 'THIS' 
                                        AND F.Traditional = 'IS'
                                        AND F.Pinyin = 'A' 
                                        AND F.Translation = 'PLACEHOLDER'
                                            )) AS Subquery;";

            var parameters = new
            {
                UserId = userId
            };

            int affectedRows = await _database.ExecuteQueryAsync_ReturnDTO<int, object>(parameters, sqlQuery);

            return affectedRows;
        }

    }
}
