using Interfaces.DatabaseAccess;
using Interfaces.Services;
using Models.Database_Entities;

// *****************************************************
//
// SelectFlashcardsBasedOnSettings()
// UpdateDifficultyRatingAsync()
// UpdatePracticeCountForUserAsync()
//
// *****************************************************

namespace Services.FlashcardsPractice
{    
    public class FlashcardPracticeService : IFlashcardPracticeService
    {
        private readonly IExecuteSQLquery _database;
        private readonly IUserService _userService;
        public FlashcardPracticeService(IExecuteSQLquery database, IUserService userService)
        {
            _database = database;
            _userService = userService;
        }

        // RETURN ONLY THE FLASHCARDS WHICH WILL BE SHOWN DURING PRACTICE
        public List<FlashcardsAndDecksViewEntity> SelectFlashcardsBasedOnSettings(FlashcardPracticeSettingsEntity settings, List<FlashcardsAndDecksViewEntity> rawList)
        {
            IEnumerable<FlashcardsAndDecksViewEntity> list = rawList;

            // include only flashcards which can display characters based on the checkboxes ticked/saved
            if(settings.ShowSimplified && settings.ShowTraditional)
            {
                list = list.Where(x => !string.IsNullOrEmpty(x.Simplified) && !string.IsNullOrEmpty(x.Traditional));
            }
            else if(settings.ShowSimplified) 
            {
                list = list.Where(x => !string.IsNullOrEmpty(x.Simplified));
            }
            else if(settings.ShowTraditional)
            {
                list = list.Where(x => !string.IsNullOrEmpty(x.Traditional));
            }

            // NARROWS DOWN THE OVERALL LIST BY UNIT/BOOK/DECK
            if (!string.IsNullOrEmpty(settings.LastUsedUnit))
            {
                list = list.Where(x => x.UnitName == settings.LastUsedUnit);
            }
            else if (!string.IsNullOrEmpty(settings.LastUsedBook))
            {
                list = list.Where(x => x.BookName == settings.LastUsedBook);
            }
            else if (!string.IsNullOrEmpty(settings.LastUsedDeck))
            {
                list = list.Where(x => x.DeckName == settings.LastUsedDeck);
            }

            // because of using View table for model and because same flashcard may be in multiple decks, first include only one entry with same flashcard ID to list
            list = list.GroupBy(x => x.Id).Select(group => group.First());

            int count = list.Count();
            int cardAmount;

            REDACTED FBI KGB CIA

            return list.ToList();
        }

        // RECEIVE DIFFICULTY BUTTON PRESS VALUES AND UPDATES FLASHCARD DIFFICULTY RATING
        public async Task UpdateDifficultyRatingAsync(int[] buttonValues, List<FlashcardsAndDecksViewEntity> flashcards)
        {
            REDACTED FBI KGB CIA
        }

        // ADD +1 TO USER's PRACTICE COUNT; how many time the user practiced flashcards
        public async Task UpdatePracticeCountForUserAsync(Guid id)
        {
            REDACTED FBI KGB CIA
        }
    }
}
