using System;
using System.Collections.Generic;

namespace Darkness.WebAPI.Models
{
    public partial class Users
    {
        public int Id { get; set; }
        public required string Username { get; set; }
        public required string Password { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string EmailAddress { get; set; }
        public int Age { get; set; }
        public required string TimeZone { get; set; }
        public required string Guid { get; set; }
        public int? Xp { get; set; }
        public int? PlayerLevel { get; set; }
        public DateTime? Creation { get; set; }
    }
}
