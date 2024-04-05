using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Database_Entities
{
    public class FlashcardPracticeSettingsEntity
    {
        public Guid UserId { get; set; }
        public int HowMany { get; set; }
        public string LastUsedDeck {  get; set; }
        public string LastUsedBook { get; set; }
        public string LastUsedUnit { get; set; }
        public bool ShowSimplified { get; set; }
        public bool ShowTraditional { get; set; }
        public bool ShowPinyin { get; set; }
        public bool ShowTranslation { get; set; }
        public bool ShowNote { get; set; }
    }
}
