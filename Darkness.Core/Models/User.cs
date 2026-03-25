using SQLite;

namespace Darkness.Core.Models
{
    public class User
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }  
        
        [Unique]
        public string Username { get; set; } = string.Empty;
        
        public string Password { get; set; } = string.Empty;
        
        public string FirstName { get; set; } = string.Empty;
        
        public string LastName { get; set; } = string.Empty;
        
        public string EmailAddress { get; set; } = string.Empty;
        
        public int Age { get; set; }
        
        public string TimeZone { get; set; } = string.Empty;
        
        public string Guid { get; set; } = string.Empty;
        
        public int Level { get; set; }
        
        public int Experience { get; set; }
    }
}
