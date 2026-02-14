using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

namespace UserManagement.Models.DTOs
{
    public class RegisterDTO
    {
        public string FirstName { get; set; }  
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }

    }
}
