using Interfaces.DatabaseAccess;
using Interfaces.Services;
using Models.Database_Entities;

// *****************************************************
//
// CreateFlashcardPracticeSettingsEntryAsync()
// GetSavedSettingsOfUserForPracticeAsync()
// UpdateSettingsAsync()
//
// *****************************************************

namespace Services.CRUD
{
    public class SettingsService : ISettingsService
    {
        private readonly IExecuteSQLquery _database;
        public SettingsService(IExecuteSQLquery database)
        {
            _database = database;
        }

        // ADDS ENTRY IN FlashcardPracticeSettingsTable ON USER CREATION
        public async Task<bool> CreateFlashcardPracticeSettingsEntryAsync(Guid userId)
        {
            string sqlQuery = @"INSERT INTO FlashcardPracticeSettingsTable (UserId)
                                VALUES (@Id);";

            var parameters = new
            {
                Id = userId
            };

            int affectedRows = await _database.ExecuteQueryAsync_ReturnInt(parameters, sqlQuery);
            return affectedRows > 0;            
        }

        // GET SAVED SETTINGS OF SPECIFIC USER BY ID
        public async Task<FlashcardPracticeSettingsEntity> GetSavedSettingsOfUserForPracticeAsync(Guid id)
        {
            string sqlQuery = @"SELECT * FROM FlashcardPracticeSettingsTable
                                WHERE UserId = @Id;";

            var parameters = new
            {
                Id = id
            };

            FlashcardPracticeSettingsEntity settings = await _database.ExecuteQueryAsync_ReturnDTO<FlashcardPracticeSettingsEntity, object>(parameters, sqlQuery);
            return settings;
        }

        // UPDATE SETTINGS FOR GIVEN USER (user Id contained in settings entity)
        public async Task<bool> UpdateSettingsAsync(FlashcardPracticeSettingsEntity settings)
        {
            string sqlQuery = @"UPDATE FlashcardPracticeSettingsTable SET
                                HowMany = @HowMany,
                                LastUsedDeck = @Deck,
                                LastUsedBook = @Textbook,
                                LastUsedUnit = @Lesson,
                                ShowSimplified = @Simp,
                                ShowTraditional = @Trad,
                                ShowPinyin = @Pin,
                                ShowTranslation = @Trans,
                                ShowNote = @Note
                                WHERE UserId = @Id;";

            var parameters = new
            {
                Id = settings.UserId,
                HowMany = settings.HowMany,
                Deck = settings.LastUsedDeck,
                Textbook = settings.LastUsedBook,
                Lesson = settings.LastUsedUnit,
                Simp = settings.ShowSimplified,
                Trad = settings.ShowTraditional,
                Pin = settings.ShowPinyin,
                Trans = settings.ShowTranslation,
                Note = settings.ShowNote
            };

            int affectedRows = await _database.ExecuteQueryAsync_ReturnInt(parameters, sqlQuery);
            return affectedRows > 0;
        }
    }
}
