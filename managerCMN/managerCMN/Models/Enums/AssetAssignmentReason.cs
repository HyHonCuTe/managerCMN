namespace managerCMN.Models.Enums;

public enum AssetAssignmentReason
{
    NewEmployee = 0,      // Nhân viên mới
    ProjectNeeds = 1,     // Nhu cầu dự án
    Replacement = 2,      // Thay thế tài sản cũ
    Upgrade = 3,          // Nâng cấp
    Temporary = 4,        // Sử dụng tạm thời
    Other = 99            // Lý do khác
}