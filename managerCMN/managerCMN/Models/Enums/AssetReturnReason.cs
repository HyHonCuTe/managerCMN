namespace managerCMN.Models.Enums;

public enum AssetReturnReason
{
    ProjectEnded = 0,     // Kết thúc dự án
    Resignation = 1,      // Nghỉ việc
    Damaged = 2,          // Hư hỏng
    Upgrade = 3,          // Nâng cấp thiết bị mới
    NoLongerNeeded = 4,   // Không còn cần
    Other = 99            // Lý do khác
}