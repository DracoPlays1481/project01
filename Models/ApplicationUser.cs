using Microsoft.AspNetCore.Identity;

namespace ESD_PROJECT.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Add any additional user properties here
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }
}