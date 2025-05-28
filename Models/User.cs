using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace userauth.Models
{
    public class User : IdentityUser
    {
        // IdentityUser already provides Id, Username (mapped from Email by default), Email, PhoneNumber.
        // We'll add custom fields.

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(100)]
        public string LastName { get; set; }

        // Role will be handled by Identity's roles system.
        // isAdmin can be a custom property or managed through roles. Let's make it a custom property for simplicity.
        public bool IsAdmin { get; set; } = false;

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Custom property to represent the combined username (FirstName + LastName)
        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";
    }
}