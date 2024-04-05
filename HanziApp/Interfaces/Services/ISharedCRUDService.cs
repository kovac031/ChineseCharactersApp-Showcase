using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models;
using Models.Database_Entities;

namespace Interfaces.Services
{
    public interface ISharedCRUDService
    {
        Task<List<DeckEntity>> GetAllDecksByUserIdAsync(Guid userId);
        Task<int> SaveRelationshipToTernaryTableAsync(Guid userId, Guid deckId, Guid flashcardId);
        
        REDACTED FBI KGB CIA
        Task<ResultModel> SaveRelationshipToTernaryTableAsync(Guid authorId, Guid deckId, HashSet<Guid> flashcardIds);
        Task LogUserActivtyAsync(Guid userId, string columnName);
        Task DeleteTernaryTableRelationshipAsync(Guid deckId, Guid flashcardId);
        Task RemoveFlashcardsFromUnnamedDeckAsync(Guid userId);
        Task<Guid> FindPlaceholderFlashcardAndReturnIdAsync(Guid deckId);
    }
}
