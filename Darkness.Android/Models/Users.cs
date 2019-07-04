using SQLite;

namespace Darkness.Android.Models
{
    public class Users
    {
        [PrimaryKey,AutoIncrement]
        public int ID { get; set; }  
        [Unique]
        public string Username { get; set; }
        public string Password { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailAddress { get; set; }
        public int Age { get; set; }
        public string TimeZone { get; set; }
        public string Guid { get; set; }
    }
}