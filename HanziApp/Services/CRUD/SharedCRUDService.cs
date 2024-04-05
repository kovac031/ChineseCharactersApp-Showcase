using Interfaces.DatabaseAccess;
using Interfaces.Services;
using Models;
using Models.Database_Entities;

// *****************************************************
//
// - RETURN LISTS
// GetAllFromTernaryTableByAnyIdAsync()
// FilterOutExistingTernaryTablePrimaryKeyCombinationsAsync()
// GetAllDecksByUserIdAsync()
//
// - RETURN ONE
// GetExistingDeckIdByDeckBookUnitNamesAndUserIdAsync()
// FindPlaceholderFlashcardAndReturnIdAsync()
//
// - CREATE
// SaveRelationshipToTernaryTableAsync() x2
// InsertPlaceholderFlashcardAndReturnIdAsync()
// LogUserActivtyAsync()
//
// - EDIT
// UpdateEntriesCountByDeckIdAsync()
//
// - DELETE
// DeleteTernaryTableRelationshipAsync() x3
// RemoveFlashcardsFromUnnamedDeckAsync()
//
// *****************************************************

namespace Services.CRUD
{
    public class SharedCRUDService : ISharedCRUDService
    {
        private readonly IExecuteSQLquery _database;
        public SharedCRUDService(IExecuteSQLquery database)
        {
            _database = database;
        }

        // -------------- RETURN LISTS --------------

        // RETURN ALL TERNARY ENTRIES WHICH HAVE THE RECEIVED ID
        public async Task<List<CardDeckUserTernaryTableEntity>> GetAllFromTernaryTableByAnyIdAsync(Guid id)
        {
            string sqlQuery = @"SELECT * FROM CardDeckUserTernaryTable
                                WHERE FlashcardId = @Id
                                OR DeckId = @Id
                                OR UserId = @Id";

            var parameters = new
            {
                Id = id
            };

            List<CardDeckUserTernaryTableEntity> list = await _database.ExecuteQueryAsync_ReturnDTOlist<CardDeckUserTernaryTableEntity, object>(parameters, sqlQuery);

            return list;
        }

        // FILTER OUT EXISTING FLASHCARD/DECK/USER COMBINATIONS
        // avoids trying to save duplicate composite primary keys when saving to ternary table during upload
        // not in StaticUtilityMethods because it calls another method
        public async Task<HashSet<Guid>> FilterOutExistingTernaryTablePrimaryKeyCombinationsAsync(Guid userId, Guid deckId, HashSet<Guid> flashcardIds)
        {
            List<CardDeckUserTernaryTableEntity> existingCombinations = await GetAllFromTernaryTableByAnyIdAsync(userId);

            HashSet<Guid> remainingFlashcardIds = new HashSet<Guid>();

            foreach (Guid flashcardId in flashcardIds)
            {
                bool combinationExists = existingCombinations.Any(x => x.FlashcardId == flashcardId
                                                                    && x.DeckId == deckId
                                                                    && x.UserId == userId);
                if (!combinationExists)
                {
                    remainingFlashcardIds.Add(flashcardId);
                }
            }

            return remainingFlashcardIds;
        }

        // GET ALL DECKS OF CURRENT USER
        public async Task<List<DeckEntity>> GetAllDecksByUserIdAsync(Guid userId)
        {
            string sqlQuery = @"SELECT DISTINCT D.* 
                                FROM DecksTable D
                                JOIN CardDeckUserTernaryTable T ON D.Id = T.DeckId
                                WHERE T.UserId = @Id;"; // DISTINCT because ternary table will return one deck per each flashcard entry

            var parameters = new
            {
                Id = userId
            };

            List<DeckEntity> list = await _database.ExecuteQueryAsync_ReturnDTOlist<DeckEntity, object>(parameters, sqlQuery);

            return list;
        }


        // -------------- RETURN ONE --------------

        // GET ID FROM EXISTING
        // needed for when adding new upload list OR new single flashcard
        public async Task<Guid> GetExistingDeckIdByDeckBookUnitNamesAndUserIdAsync(string deckName, string bookName, string lessonName, Guid authorId)
        {
            string sqlQuery = @"SELECT D.Id
                                FROM DecksTable D
                                JOIN CardDeckUserTernaryTable T ON D.Id = T.DeckId
                                WHERE T.UserId = @Id
                                AND (D.DeckName = @DeckName OR D.DeckName IS NULL AND @DeckName IS NULL)
                                AND (D.BookName = @BookName OR D.BookName IS NULL AND @BookName IS NULL)
                                AND (D.UnitName = @UnitName OR D.UnitName IS NULL AND @UnitName IS NULL);";

            var parameters = new
            {
                Id = authorId,
                DeckName = deckName,
                BookName = bookName,
                UnitName = lessonName
            };

            Guid deckId = await _database.ExecuteQueryAsync_ReturnDTO<Guid, object>(parameters, sqlQuery);

            return deckId;
        }

        // FINDS PLACEHOLDER FOR DELETION
        public async Task<Guid> FindPlaceholderFlashcardAndReturnIdAsync(Guid deckId)
        {
            string sqlQuery = @"SELECT * FROM FlashcardsAndDecksView
                                WHERE DeckId = @Id
                                AND Simplified = 'THIS'
                                AND Traditional = 'IS'
                                AND Pinyin = 'A'
                                AND Translation = 'PLACEHOLDER'";

            var parameters = new
            {
                Id = deckId
            };

            Guid flashcardId = await _database.ExecuteQueryAsync_ReturnDTO<Guid, object>(parameters, sqlQuery);

            return flashcardId;
        }

        // -------------- CREATE --------------

        // TAKE IDs AND CREATE TERNARY TABLE ENTRY; establish relationship
        // method overloading
        public async Task<int> SaveRelationshipToTernaryTableAsync(Guid userId, Guid deckId, Guid flashcardId)
        {
            string sqlQuery = @"INSERT INTO CardDeckUserTernaryTable (FlashcardId, DeckId, UserId, TimeAdded)
                                VALUES (@FlashcardId, @DeckId, @UserId, @TimeAdded)";

            var parameters = new
            {
                FlashcardId = flashcardId,
                DeckId = deckId,
                UserId = userId,
                TimeAdded = DateTime.Now
            };

            return await _database.ExecuteQueryAsync_ReturnInt(parameters, sqlQuery);
        }
        public async Task<ResultModel> SaveRelationshipToTernaryTableAsync(Guid authorId, Guid deckId, HashSet<Guid> flashcardIds)
        {
            string sqlQuery = @"INSERT INTO CardDeckUserTernaryTable (FlashcardId, DeckId, UserId, TimeAdded) 
                                VALUES (@FlashcardId, @DeckId, @UserId, @TimeAdded)";

            var parameters = flashcardIds.Select(id => new
            {
                FlashcardId = id,
                DeckId = deckId,
                UserId = authorId,
                TimeAdded = DateTime.Now
            }).ToList();

            int affectedRows = await _database.ExecuteQueryAsync_ReturnInt(parameters, sqlQuery);

            ResultModel result = new ResultModel();
            if(affectedRows == flashcardIds.Count() && affectedRows > 0)
            {
                result.IsSuccess = true;
                result.Message = $"All of your {affectedRows} selected flashcards are now in this deck";
            }
            else if (affectedRows != flashcardIds.Count() && affectedRows > 0)
            {
                result.IsSuccess = false;
                result.Message = $"Something went wrong! Only {affectedRows} flashcards were added.";
            }
            else
            {
                return null;
            }
            return result;
        }

        // PLACEHOLDER CREATION AND ID RETRIEVAL DURING DECK CREATION
        public async Task<Guid> InsertPlaceholderFlashcardAndReturnIdAsync()
        {
            string sqlQuery = @"INSERT INTO FlashcardsTable (Id, Simplified, Traditional, Pinyin, Translation, TimeAdded)
                                VALUES (@Id, @Simplified, @Traditional, @Pinyin, @Translation, @TimeAdded);";

            Guid flashcardId = Guid.NewGuid();
            var parameters = new
            {
                Id = flashcardId,
                Simplified = "THIS",
                Traditional = "IS",
                Pinyin = "A",
                Translation = "PLACEHOLDER",
                TimeAdded = DateTime.Now
            };

            int affectedRows = await _database.ExecuteQueryAsync_ReturnInt(parameters, sqlQuery);
            if (affectedRows > 0)
            {
                return flashcardId;
            }
            else return Guid.Empty;
        }

        // LOG USER ACTIVITY
        public async Task LogUserActivtyAsync(Guid userId, string columnName)
        {
            string sqlQuery = $@"INSERT INTO UserActivityTable (UserId, ActivityTime, {columnName})
                                VALUES (@UserId, @ActivityTime, @Value)";

            var parameters = new
            {
                UserId = userId,
                ActivityTime = DateTime.Now,
                Value = true
            };

            int affectedRows = await _database.ExecuteQueryAsync_ReturnInt(parameters, sqlQuery);
        }


        // -------------- EDIT --------------

        // UPDATE ENTRIES COUNT IN DECK ON ANY CHANGE, BY DECK ID
        public async Task UpdateEntriesCountByDeckIdAsync(Guid deckId)
        {
            List<CardDeckUserTernaryTableEntity> list = await GetAllFromTernaryTableByAnyIdAsync(deckId);
            int newCount = list.Count();

            string sqlQuery = @"UPDATE DecksTable SET 
                                EntriesCount = @Count
                                WHERE Id = @Id";

            var parameters = new
            {
                Id = deckId,
                Count = newCount
            };

            int affectedRows = await _database.ExecuteQueryAsync_ReturnInt(parameters, sqlQuery);
        }


        // -------------- DELETE --------------

        // CLEAR PLACEHOLDER WHEN DECK IS POPULATED
        public async Task RemovePlaceholderFlashcardAsync(Guid deckId)
        {
            REDACTED FBI KGB CIA

            int affectedRows = await _database.ExecuteQueryAsync_ReturnInt(parameters, sqlQuery);
        }

        // REMOVES ENTRY FROM TERNARY TABLE; deletes relationship
        // method overloading

        // WARNING - only use the single parameter one when removing ALL entries for that Id; that Id can be any column Id
        public async Task<bool> DeleteTernaryTableRelationshipAsync(Guid id)
        {
            // looks for any matching Id, ensures method reusability when deleting either flashcard/deck/user 
            string sqlQuery = @"DELETE FROM CardDeckUserTernaryTable
                                WHERE FlashcardId = @Id
                                OR DeckId = @Id
                                OR UserId = @Id";

            var parameters = new
            {
                Id = id
            };

            int affectedRows = await _database.ExecuteQueryAsync_ReturnInt(parameters, sqlQuery);
            return affectedRows > 0;            
        }
        public async Task DeleteTernaryTableRelationshipAsync(Guid deckId, Guid flashcardId)
        {
            string sqlQuery = @"DELETE FROM CardDeckUserTernaryTable
                                WHERE DeckId = @DeckId
                                AND FlashcardId = @CardId";

            var parameters = new
            {
                DeckId = deckId,
                CardId = flashcardId
            };

            int affectedRows = await _database.ExecuteQueryAsync_ReturnInt(parameters, sqlQuery);
        }
        public async Task DeleteTernaryTableRelationshipAsync(Guid userId, Guid deckId, HashSet<Guid> flashcardIds)
        {

            string sqlQuery = @"DELETE FROM CardDeckUserTernaryTable
                                WHERE FlashcardId = @FlashcardId
                                AND DeckId = @DeckId
                                AND UserId = @UserId";

            var parameters = flashcardIds.Select(flashcardId => new
            {
                FlashcardId = flashcardId,
                DeckId = deckId,
                UserId = userId
            }).ToList();

            int affectedRows = await _database.ExecuteQueryAsync_ReturnInt(parameters, sqlQuery);
        }

        // REMOVE FLASHCARDS FROM NO-NAME DECK IF FOUND IN ANOTHER DECK
        // basically, delete the ternary relationship with the unnamed deck
        public async Task RemoveFlashcardsFromUnnamedDeckAsync(Guid userId)
        {
            REDACTED FBI KGB CIA

            await UpdateEntriesCountByDeckIdAsync(nullDeckId.Value); // .Value jer ocekuje Guid a saljem Guid? tip
        }
    }
}
