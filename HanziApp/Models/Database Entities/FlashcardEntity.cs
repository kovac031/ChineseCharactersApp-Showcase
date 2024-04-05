using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Database_Entities
{
    public class FlashcardEntity
    {
        public Guid Id { get; set; }
        public string Simplified { get; set; }
        public string Traditional { get; set; }
        public string Pinyin { get; set; }
        public string Translation { get; set; }

        [DataType(DataType.MultilineText)]
        public string Note { get; set; }

        public DateTime TimeAdded { get; set; } // not null
        public DateTime TimeUpdated { get; set; }
        public int DifficultyRating { get; set; }
        public int RatingMultiplier { get; set; }
        public int PracticeCount { get; set; }
    }
}
