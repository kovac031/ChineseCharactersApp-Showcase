using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models;
using Models.Database_Entities;

namespace Interfaces.Services
{
    public interface IFlashcardService
    {
        Task<List<FlashcardEntity>> GetAllFlashcardsByUserIdAsync(Guid id);
        Task<List<FlashcardsAndDecksViewEntity>> GetWholeListFromFlashcardsAndDecksViewByUserIdAsync(Guid id);
        (IEnumerable<FlashcardsAndDecksViewEntity>, int, int) FilterSortAndPageFlashcardList(List<FlashcardsAndDecksViewEntity> list, int? page, string sortBy, string filterBy);
        List<FlashcardsAndDecksViewEntity> FilterListByParameters(List<FlashcardsAndDecksViewEntity> entireList, string filterBy, string deck, string textbook, string lessonUnit);
        IEnumerable<FlashcardsAndDecksViewEntity> SortListByParameter(IEnumerable<FlashcardsAndDecksViewEntity> filteredList, string sortBy);
        (IEnumerable<FlashcardsAndDecksViewEntity>, int, int) GetTheSpecificPage(IEnumerable<FlashcardsAndDecksViewEntity> sortedList, int? page);
        Task<FlashcardEntity> GetOneFlashcardByIdAsync(Guid id);
        Task<bool> EditOneFlashcardByIdAsync(FlashcardEntity flashcard);

        REDACTED FBI KGB CIA
    }
}
