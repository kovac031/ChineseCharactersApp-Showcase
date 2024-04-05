using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Database_Entities
{
    public class PublicDecksOverviewViewEntity
    {
        public Guid AuthorId { get; set; }
        public string CreatedBy { get; set; }
        public Guid DeckId { get; set; }
        public string DeckName { get; set; }
        public string BookName { get; set; }
        public string UnitName { get; set; }

        [DataType(DataType.MultilineText)]
        public string Deckscription { get; set; }

        public DateTime TimeAdded { get; set; } // not null
        public DateTime TimeUpdated { get; set; }
        public int EntriesCount { get; set; }
        public int MakePublic { get; set; } // bit type in database, but having it bool makes it load slow (I suspect Dapper)
        public int UsageCount { get; set; }
    }
}
