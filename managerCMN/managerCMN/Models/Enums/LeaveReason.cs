namespace managerCMN.Models.Enums;

public enum LeaveReason
{
    PaidLeave = 0,           // Nghỉ tính phép → Tính công
    UnpaidLeave = 1,         // Nghỉ không phép → Không tính công
    SickLeaveWithCert = 2,   // Nghỉ ốm có giấy → Tính công
    BereavementLeave = 3,    // Nghỉ tang (tối đa 3 ngày) → Tính công
    MaternityLeave = 4,      // Nghỉ thai sản → Tính công
    MarriageLeave = 5,       // Nghỉ kết hôn (tối đa 3 ngày) → Tính công
    OtherLeave = 6,          // Khác → Tính công

    // ── Checkin/out (RequestType.CheckInOut) ──
    ForgotCheckInOut = 10,   // Quên checkin/out → Tính công

    // ── Vắng mặt (RequestType.Absence) ──
    CompanyBusiness = 20,    // Đi công việc CTY → Tính công
    PersonalBusiness = 21,   // Việc cá nhân → Tính công

    // ── Làm ở nhà (RequestType.WorkFromHome) ──
    WorkFromHome = 30        // Xin làm ở nhà → Tính công
}
