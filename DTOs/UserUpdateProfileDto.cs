using System.ComponentModel.DataAnnotations;

namespace userauth.DTOs
{
    public class UserUpdateProfileDto
    {
        [StringLength(100)]
        public string FirstName { get; set; }

        [StringLength(100)]
        public string LastName { get; set; }

        [Phone]
        public string PhoneNumber { get; set; }
    }
}