using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ESD_PROJECT.Models
{
    public class UserUpdateModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        public string Role { get; set; }
    }
}