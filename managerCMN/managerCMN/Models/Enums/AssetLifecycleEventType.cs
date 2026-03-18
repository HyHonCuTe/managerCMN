namespace managerCMN.Models.Enums;

public enum AssetLifecycleEventType
{
    Created = 0,          // Tạo tài sản
    Assigned = 1,         // Cấp phát
    Returned = 2,         // Thu hồi
    StatusChanged = 3,    // Thay đổi trạng thái
    ConditionUpdated = 4, // Cập nhật tình trạng
    Repaired = 5,         // Sửa chữa
    Moved = 6,            // Di chuyển vị trí
    Disposed = 7          // Thanh lý
}