namespace Darkness.Core.Models
{
    public class Ally
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int AllyUserId { get; set; }
        public string AllyUsername { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending"; // "Pending", "Accepted"
    }
}