using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Database_Entities
{
    public class UserActivityEntity
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public DateTime ActivityTime { get; set; }
        public bool LoggedIn { get; set; }
        public bool ChangedUsername { get; set; }
        public bool ChangedEmail { get; set; }
        public bool ChangedPassword { get; set; }
        public bool DidFlashcardPractice { get; set; }
    }
}
