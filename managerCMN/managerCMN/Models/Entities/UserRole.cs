using System.ComponentModel.DataAnnotations;

namespace managerCMN.Models.Entities;

public class UserRole
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
}
