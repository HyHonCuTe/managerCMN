-- ================================================
-- SCRIPT CẬP NHẬT/TẠO NHÂN VIÊN ADMIN CAO NHẤT
-- Email: chatgpt244@gmail.com
-- Đã sửa: Không dùng UserPermissions
-- ================================================

BEGIN TRANSACTION;

DECLARE @EmployeeId INT;
DECLARE @UserId INT;
DECLARE @Email NVARCHAR(255) = 'chatgpt244@gmail.com';

-- ======================================
-- Bước 1: Xử lý Employee
-- ======================================
SELECT @EmployeeId = EmployeeId 
FROM Employees 
WHERE Email = @Email;

IF @EmployeeId IS NULL
BEGIN
    PRINT 'Employee chưa tồn tại, đang tạo mới...';
    
    INSERT INTO Employees (
        EmployeeCode, 
        FullName, 
        Email, 
        Gender, 
        DateOfBirth,
        Phone,
        DepartmentId, 
        JobTitleId, 
        PositionId, 
        Status, 
        StartWorkingDate,
        CreatedAt,
        IsApprover
    )
    VALUES (
        'SUPERADMIN',           -- Mã nhân viên
        N'Super Administrator', -- Họ tên
        @Email,
        0,                      -- Nam
        '1990-01-01',
        '0123456789',
        NULL,                   -- Không thuộc phòng ban cụ thể
        1,                      -- JobTitleId = 1 (Ban Giám Đốc)
        NULL,
        0,                      -- Đang làm việc
        GETDATE(),
        GETDATE(),
        1                       -- Có quyền duyệt
    );
    
    SET @EmployeeId = SCOPE_IDENTITY();
    PRINT 'Đã tạo Employee mới với ID: ' + CAST(@EmployeeId AS NVARCHAR(10));
END
ELSE
BEGIN
    PRINT 'Employee đã tồn tại với ID: ' + CAST(@EmployeeId AS NVARCHAR(10));
    PRINT 'Đang cập nhật thông tin...';
    
    UPDATE Employees
    SET 
        FullName = N'Super Administrator',
        Status = 0,         -- Đang làm việc
        IsApprover = 1,     -- Có quyền duyệt
        JobTitleId = 1,     -- Ban Giám Đốc
        Phone = COALESCE(Phone, '0123456789')
    WHERE EmployeeId = @EmployeeId;
    
    PRINT 'Đã cập nhật Employee';
END

-- ======================================
-- Bước 2: Xử lý User
-- ======================================
SELECT @UserId = UserId 
FROM Users 
WHERE Email = @Email;

IF @UserId IS NULL
BEGIN
    PRINT 'User chưa tồn tại, đang tạo mới...';
    
    INSERT INTO Users (
        Email, 
        FullName, 
        IsActive, 
        CreatedAt,
        EmployeeId
    )
    VALUES (
        @Email,
        N'Super Administrator',
        1,
        GETDATE(),
        @EmployeeId
    );
    
    SET @UserId = SCOPE_IDENTITY();
    PRINT 'Đã tạo User mới với ID: ' + CAST(@UserId AS NVARCHAR(10));
END
ELSE
BEGIN
    PRINT 'User đã tồn tại với ID: ' + CAST(@UserId AS NVARCHAR(10));
    PRINT 'Đang cập nhật User...';
    
    UPDATE Users
    SET 
        FullName = N'Super Administrator',
        IsActive = 1,
        EmployeeId = @EmployeeId
    WHERE UserId = @UserId;
    
    PRINT 'Đã cập nhật User';
END

-- ======================================
-- Bước 3: Gán Role Admin
-- ======================================
IF NOT EXISTS (
    SELECT 1 FROM UserRoles 
    WHERE UserId = @UserId AND RoleId = 1
)
BEGIN
    PRINT 'Đang gán Role Admin (RoleId=1)...';
    
    INSERT INTO UserRoles (UserId, RoleId, AssignedDate)
    VALUES (@UserId, 1, GETDATE());
    
    PRINT 'Đã gán Role Admin';
END
ELSE
BEGIN
    PRINT 'User đã có Role Admin';
END

-- ======================================
-- Bước 4: Đảm bảo Role Admin có TOÀN BỘ quyền
-- ======================================
PRINT 'Đang kiểm tra quyền của Role Admin...';

-- Gán tất cả permissions cho Role Admin (bao gồm cả System.ALL = PermissionId 25)
INSERT INTO RolePermissions (RoleId, PermissionId, AssignedDate)
SELECT 1, PermissionId, GETDATE()
FROM Permissions
WHERE PermissionId NOT IN (
    SELECT PermissionId FROM RolePermissions WHERE RoleId = 1
);

DECLARE @PermissionsAdded INT = @@ROWCOUNT;
IF @PermissionsAdded > 0
BEGIN
    PRINT 'Đã gán thêm ' + CAST(@PermissionsAdded AS NVARCHAR(10)) + ' quyền cho Role Admin';
END
ELSE
BEGIN
    PRINT 'Role Admin đã có đầy đủ quyền';
END

-- ======================================
-- HOÀN TẤT
-- ======================================
COMMIT TRANSACTION;

PRINT '';
PRINT '========================================';
PRINT '✓ HOÀN TẤT THÀNH CÔNG!';
PRINT '========================================';
PRINT 'Employee ID: ' + CAST(@EmployeeId AS NVARCHAR(10));
PRINT 'User ID: ' + CAST(@UserId AS NVARCHAR(10));
PRINT 'Email: ' + @Email;
PRINT 'Role: Admin (ID=1)';
PRINT 'Permissions: TOÀN BỘ (bao gồm System.ALL)';
PRINT '';
PRINT 'Bây giờ có thể đăng nhập với Google OAuth!';
PRINT '========================================';



-- ========================================
-- KIỂM TRA KẾT QUẢ
-- ========================================

PRINT '';
PRINT '=== THÔNG TIN CHI TIẾT ===';
PRINT '';

-- Thông tin Employee
SELECT 
    'EMPLOYEE' as InfoType,
    e.EmployeeId,
    e.EmployeeCode,
    e.FullName,
    e.Email,
    CASE e.Status 
        WHEN 0 THEN N'Đang làm việc'
        WHEN 1 THEN N'Nghỉ việc'
        ELSE N'Khác'
    END as Status,
    CASE e.IsApprover 
        WHEN 1 THEN N'Có'
        ELSE N'Không'
    END as IsApprover,
    jt.JobTitleName
FROM Employees e
LEFT JOIN JobTitles jt ON e.JobTitleId = jt.JobTitleId
WHERE e.Email = 'chatgpt244@gmail.com';

PRINT '';

-- Thông tin User
SELECT 
    'USER' as InfoType,
    u.UserId,
    u.Email,
    u.FullName,
    CASE u.IsActive 
        WHEN 1 THEN N'Active'
        ELSE N'Inactive'
    END as Status,
    u.EmployeeId as LinkedEmployeeId
FROM Users u
WHERE u.Email = 'chatgpt244@gmail.com';

PRINT '';

-- Roles của User
SELECT 
    'USER_ROLES' as InfoType,
    r.RoleId,
    r.RoleName,
    r.Description,
    ur.AssignedDate
FROM Users u
JOIN UserRoles ur ON u.UserId = ur.UserId
JOIN Roles r ON ur.RoleId = r.RoleId
WHERE u.Email = 'chatgpt244@gmail.com';

PRINT '';

-- Permissions của Admin role
SELECT 
    'ROLE_PERMISSIONS' as InfoType,
    COUNT(*) as TotalPermissions,
    CASE WHEN EXISTS (
        SELECT 1 FROM RolePermissions rp 
        JOIN Permissions p ON rp.PermissionId = p.PermissionId
        WHERE rp.RoleId = 1 AND p.PermissionKey = 'System.ALL'
    ) THEN N'Có' ELSE N'Không' END as HasSystemALL
FROM Users u
JOIN UserRoles ur ON u.UserId = ur.UserId
JOIN RolePermissions rp ON ur.RoleId = rp.RoleId
WHERE u.Email = 'chatgpt244@gmail.com';

PRINT '';

-- Danh sách chi tiết quyền
SELECT 
    'DETAILED_PERMISSIONS' as InfoType,
    p.PermissionKey,
    p.PermissionName,
    p.Category
FROM Users u
JOIN UserRoles ur ON u.UserId = ur.UserId
JOIN RolePermissions rp ON ur.RoleId = rp.RoleId
JOIN Permissions p ON rp.PermissionId = p.PermissionId
WHERE u.Email = 'chatgpt244@gmail.com'
ORDER BY p.Category, p.SortOrder;

