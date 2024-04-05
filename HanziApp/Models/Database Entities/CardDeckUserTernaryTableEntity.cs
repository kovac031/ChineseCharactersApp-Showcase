using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Database_Entities
{
    public class CardDeckUserTernaryTableEntity
    {
        public Guid FlashcardId { get; set; }
        public Guid DeckId { get; set; }
        public Guid UserId { get; set; }
        public DateTime TimeAdded { get; set; }
    }
}
