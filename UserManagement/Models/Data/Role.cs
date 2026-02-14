namespace UserManagement.Models.Data
{
    public class Role
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }

        //Navigation property

        public ICollection<UserRole> UserRoles { get; set; }
    }
}
