namespace Models.Database_Entities
{
    public class UserEntity
    {
        public Guid Id { get; set; }
        public string Username { get; set; } 
        public string Password { get; set; } 
        public string EmailAddress { get; set; } 
        public string UserRole { get; set; }
        public DateTime TimeAdded { get; set; } 
        public DateTime TimeUpdated { get; set; }
        public int PracticeCount { get; set; }
    }
}