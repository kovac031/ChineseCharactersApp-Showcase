using DocumentFormat.OpenXml.Spreadsheet;
using Interfaces.DatabaseAccess;
using Interfaces.Services;
using Models;
using Models.Database_Entities;

// *****************************************************
//
// - RETURN LIST OF DECKS
// GetAllPublicDecksAsync()
// FilterPublicDecksBySearchTerm()
// SortDecksByParameter()
// SortPublicDecksByParameter()
// PageThePublicDecks()
//
// - RETURN SINGLE DECK
// GetOneDeckByIdAsync()
//
// - CREATE NEW DECK
// AddNewDeckAsync()
// SaveToDecksTableAndReturnIdAsync()
//
// - EDIT DECK
// EditOneDeckByIdAsync()
// UpdateTimeUpdatedForDeckByIdAsync()
// UpdateDeckUsageCountByDeckIdAsync()
//
// - DELETE DECK
// DeleteDeckByIdAsync()
//
// - OTHER
// MoveFlashcardsFromDeckBeingDeletedToNullDeckAsync()
// MoveSingleFlashcardToNullDeckAsync()
//
// *****************************************************

namespace Services.CRUD
{
    public class DeckService : IDeckService
    {
        private readonly IExecuteSQLquery _database;
        private readonly ISharedCRUDService _sharedCRUD;
        public DeckService(IExecuteSQLquery database, ISharedCRUDService sharedCRUD)
        {
            _database = database;
            _sharedCRUD = sharedCRUD; // Flashcard and Deck Services may only have dependency on sharedCRUD for CRUD, move all relevant Flashcard and Deck method to Shared since these methods are used by both
        }

        // -------------- RETURN LIST OF DECKS -----------------
                
        // GET ALL PUBLIC DECKS
        // zašto ovako, jer svakako bi trebala nova metoda zbog novog querya, a istovremeno mi treba view koji ima i podatke autora
        // jednostavnije je da ima view za to koji samo dohvatim
        public async Task<List<PublicDecksOverviewViewEntity>> GetAllPublicDecksAsync()
        {
            string sqlQuery = @"SELECT DISTINCT * FROM PublicDecksOverviewView
                                WHERE MakePublic = @IsPublic;"; // DISTINCT because ternary table will return one deck per each flashcard entry

            var parameters = new
            {
                IsPublic = 1 // loads faster if "1" instead of "true"
            };

            List<PublicDecksOverviewViewEntity> list = await _database.ExecuteQueryAsync_ReturnDTOlist<PublicDecksOverviewViewEntity, object>(parameters, sqlQuery);

            return list;
        }

        // FILTERING
        public List<PublicDecksOverviewViewEntity> FilterPublicDecksBySearchTerm(List<PublicDecksOverviewViewEntity> decks, string filterBy)
        {
            REDACTED FBI KGB CIA
        }

        // SORTING
        // mogao sam napraviti jednu generic metodu, ali nisam jer zelim drugaciji default sort u switch petlji
        public List<DeckEntity> SortDecksByParameter(List<DeckEntity> decks, string sortBy)
        {
            REDACTED FBI KGB CIA
        }
        public List<PublicDecksOverviewViewEntity> SortPublicDecksByParameter(List<PublicDecksOverviewViewEntity> decks, string sortBy)
        {
            REDACTED FBI KGB CIA
        }

        // PAGING
        public (List<PublicDecksOverviewViewEntity>, int, int) PageThePublicDecks(List<PublicDecksOverviewViewEntity> decks, int? page)
        {
            IEnumerable<PublicDecksOverviewViewEntity> list = decks;

            int count = list.Count();
            int pageNumber = page ?? 1;
            int pageSize = 20; // fixed size, rows per page
            int totalPages = (int)Math.Ceiling((double)count / pageSize);

            list = list
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize);

            return (list.ToList(), pageNumber, totalPages);
        }


        // -------------- RETURN SINGLE DECK -----------------

        // GET DECK BY ID 
        public async Task<DeckEntity> GetOneDeckByIdAsync(Guid id)
        {
            string sqlQuery = @"SELECT * FROM DecksTable
                                WHERE Id = @Id;";

            var parameters = new
            {
                Id = id
            };

            DeckEntity deck = await _database.ExecuteQueryAsync_ReturnDTO<DeckEntity, object>(parameters, sqlQuery);
            return deck;
        }


        // -------------- CREATE NEW DECK -----------------

        // standalone view
        public async Task<ResultModel> AddNewDeckAsync(DeckEntity deck, Guid userId)
        {
            ResultModel result = new ResultModel(); // instantiating the return object

            // we will always need to establish the card-deck-user ternary relationship
            // need deckId, cardId, userId

            Guid deckId = await _sharedCRUD.GetExistingDeckIdByDeckBookUnitNamesAndUserIdAsync(deck.DeckName, deck.BookName, deck.UnitName, userId);
            if (deckId != Guid.Empty)
            {
                // deck with these names exists
                result.IsSuccess = false;
                result.Message = "Cannot add this deck because you already have a deck with this exact name.";
                return result;
            }

            string sqlQuery = @"INSERT INTO DecksTable (Id, DeckName, BookName, UnitName, Deckscription, MakePublic, TimeAdded)
                            VALUES (@Id, @DeckName, @BookName, @UnitName, @Deckscription, @MakePublic, @TimeAdded);";

            deckId = Guid.NewGuid();

            var parameters = new
            {
                Id = deckId,
                DeckName = deck.DeckName,
                BookName = deck.BookName,
                UnitName = deck.UnitName,
                Deckscription = deck.Deckscription,
                MakePublic = deck.MakePublic,
                TimeAdded = DateTime.Now
            };

            int affectedRows = await _database.ExecuteQueryAsync_ReturnInt(parameters, sqlQuery);

            // SAVE TO TERNARY
            if (affectedRows > 0)
            {
                Guid flashcardId = await _sharedCRUD.InsertPlaceholderFlashcardAndReturnIdAsync();
                await _sharedCRUD.SaveRelationshipToTernaryTableAsync(userId, deckId, flashcardId);

                result.IsSuccess = true;
                result.Message = "Your deck was added!";

                return result;
            }
            else
            {
                return null; // must implement reversal method
            }

        }

        // when uploading vocabulary and needing to create a deck
        // when deleting an existing deck and no unnamed deck yet exists
        public async Task<Guid> SaveToDecksTableAndReturnIdAsync(DeckEntity deck)
        {
            Guid deckId = Guid.NewGuid(); 

            string sqlQuery = @"INSERT INTO [DecksTable] 
                            (Id, DeckName, BookName, UnitName, Deckscription, TimeAdded) 
                            VALUES 
                            (@Id, @DeckName, @BookName, @UnitName, @Deckscription, @TimeAdded)";
                        
            var parameters = new
            {
                // nullchecks added because when deleting deck when no "unnamed deck" yet exist will pass null for the deck parameter
                Id = deckId,
                DeckName = deck?.DeckName,
                BookName = deck?.BookName,
                UnitName = deck?.UnitName,
                Deckscription = deck?.Deckscription,
                TimeAdded = DateTime.Now
            };

            int affectedRows = await _database.ExecuteQueryAsync_ReturnInt(parameters, sqlQuery);

            if (affectedRows > 0)
            {
                return deckId;
            }
            else
            {
                return Guid.Empty; // must implement removal of just added items logic
            }
        }
                

        // -------------- EDIT DECK --------------

        // by deck.Id
        public async Task<bool> EditOneDeckByIdAsync(DeckEntity deck)
        {
            string sqlQuery = @"UPDATE DecksTable SET 
                                DeckName = @Deck, 
                                BookName = @Textbook,
                                UnitName = @Lesson,
                                Deckscription = @Description,
                                MakePublic = @MakePublic,
                                TimeUpdated = @TimeUpdated
                                WHERE Id = @Id";

            var parameters = new
            {
                Id = deck.Id,
                Deck = deck.DeckName,
                Textbook = deck.BookName,
                Lesson = deck.UnitName,
                Description = deck.Deckscription,
                MakePublic = deck.MakePublic,
                TimeUpdated = DateTime.Now
            };

            int affectedRows = await _database.ExecuteQueryAsync_ReturnInt(parameters, sqlQuery);

            return affectedRows > 0;
        }

        // UPDATE TIME UPDATED FOR SPECIFIC DECK BY ID
        public async Task UpdateTimeUpdatedForDeckByIdAsync(Guid deckId)
        {
            Console.WriteLine("updating deck time");
            string sqlQuery = @"UPDATE DecksTable SET
                                TimeUpdated = @TimeUpdated
                                WHERE Id = @Id";

            var parameters = new
            {
                Id = deckId,
                TimeUpdated = DateTime.Now
            };

            int affectedRows = await _database.ExecuteQueryAsync_ReturnInt(parameters, sqlQuery);
            Console.WriteLine("finished updating deck time");
        }

        // UPDATE DECK USAGE COUNTER (PUBLIC DECK TIMES COPIED)
        public async Task UpdateDeckUsageCountByDeckIdAsync(Guid deckId, int usageCount)
        {
            string sqlQuery = @"UPDATE DecksTable SET
                                UsageCount = @UsageCount
                                WHERE Id = @Id";

            var parameters = new
            {
                Id = deckId,
                UsageCount = usageCount + 1
            };

            int affectedRows = await _database.ExecuteQueryAsync_ReturnInt(parameters, sqlQuery);
        }


        // -------------- DELETE DECK --------------

        // DELETE DECK BY ID
        public async Task DeleteDeckByIdAsync(Guid deckId)
        {           
            await _sharedCRUD.DeleteTernaryTableRelationshipAsync(deckId);

            string sqlQuery = @"DELETE FROM DecksTable
                                WHERE Id = @Id";

            var parameters = new
            {
                Id = deckId
            };

            int affectedRows = await _database.ExecuteQueryAsync_ReturnInt(parameters, sqlQuery);
        }


        // ------------- OTHER -------------               

        // REPLACE DECK ID FROM DECK BEING DELETED WITH THE DECK ID FROM THE NO-NAMES DECK (flashcards with no assigned deck)
        // cannot be in sharedCRUD due to causing circular dependency with SaveToDecksTableAndReturnIdAsync()
        // SharedCRUDService may only have dependency on DatabaseAccess
        public async Task<bool> MoveFlashcardsFromDeckBeingDeletedToNullDeckAsync(Guid deckId, Guid userId)
        {
            REDACTED FBI KGB CIA
        } 

        public async Task MoveSingleFlashcardToNullDeckAsync(Guid currentDeckId, Guid flashcardId, Guid userId)
        {
            REDACTED FBI KGB CIA
        }


    }
}
