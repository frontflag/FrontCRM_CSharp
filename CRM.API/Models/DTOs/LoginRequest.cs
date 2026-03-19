using System.ComponentModel.DataAnnotations;

namespace CRM.API.Models.DTOs
{
    public class LoginRequest
    {
        [Required]
        [MaxLength(100)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
