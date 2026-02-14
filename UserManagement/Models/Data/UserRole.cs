namespace UserManagement.Models.Data
{
    public class UserRole
    {
        public int UserId { get; set; }
        // Navigation
        public User User { get; set; }

        public int RoleId { get; set; }
        // Navigation
        public Role Role { get; set; }
    }
}
