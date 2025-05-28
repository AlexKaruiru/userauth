namespace userauth.DTOs
{
    public class UserProfileDto
    {
        public string Id { get; set; } // IdentityUser uses string for Id
        public string FullName { get; set; } // Derived from FirstName and LastName
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsAdmin { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}