using managerCMN.Helpers;

namespace managerCMN.Models.Entities;

public class RolePermission
{
    public int RolePermissionId { get; set; }

    public int RoleId { get; set; }

    public int PermissionId { get; set; }

    public DateTime AssignedDate { get; set; } = DateTimeHelper.VietnamNow;

    // Navigation properties
    public Role Role { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}
