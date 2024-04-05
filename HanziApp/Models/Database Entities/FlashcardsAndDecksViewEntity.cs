using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Database_Entities
{
    public class FlashcardsAndDecksViewEntity // all nullable due to being View
    {
        public Guid Id { get; set; }
        public string Simplified { get; set; }
        public string Traditional { get; set; }
        public string Pinyin { get; set; }
        public string Translation { get; set; }

        [DataType(DataType.MultilineText)]
        public string Note { get; set; }

        public DateTime FlashcardTimeUpdated { get; set; }
        public DateTime FlashcardTimeAdded { get; set; }
        public int DifficultyRating { get; set; }
        public int RatingMultiplier { get; set; }
        public int FlashcardPracticeCount { get; set; }
        public Guid DeckId { get; set; }
        public string DeckName { get; set; }
        public string BookName { get; set; }
        public string UnitName { get; set; }
        public Guid AuthorId { get; set; }
        public int UserPracticeCount { get; set; }
    }
}
