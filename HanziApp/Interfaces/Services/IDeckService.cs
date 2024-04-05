using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models;
using Models.Database_Entities;

namespace Interfaces.Services
{
    public interface IDeckService
    {
        Task<List<PublicDecksOverviewViewEntity>> GetAllPublicDecksAsync();
        Task<ResultModel> AddNewDeckAsync(DeckEntity deck, Guid userId);
        Task<Guid> SaveToDecksTableAndReturnIdAsync(DeckEntity deck);

        REDACTED FBI KGB CIA
        Task<bool> MoveFlashcardsFromDeckBeingDeletedToNullDeckAsync(Guid deckId, Guid userId);
        Task MoveSingleFlashcardToNullDeckAsync(Guid currentDeckId, Guid flashcardId, Guid userId);

    }
}
