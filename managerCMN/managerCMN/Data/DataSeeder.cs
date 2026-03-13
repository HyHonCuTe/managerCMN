using managerCMN.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace managerCMN.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db)
    {
        // Only seed lookup/category data — employees will be imported via Excel

        // ── 1. Positions (Vị trí) ──
        if (!await db.Positions.AnyAsync())
        {
            db.Positions.AddRange(new[]
            {
                new Position { PositionName = "Tổng Giám Đốc", SortOrder = 1 },
                new Position { PositionName = "Phó Tổng Giám Đốc", SortOrder = 2 },
                new Position { PositionName = "Phó Giám Đốc điều hành", SortOrder = 3 },
                new Position { PositionName = "Chủ Tịch HĐQT", SortOrder = 4 },
                new Position { PositionName = "Thành viên HĐQT", SortOrder = 5 },
                new Position { PositionName = "Giám Đốc ISC", SortOrder = 6 },
                new Position { PositionName = "Giám đốc BD", SortOrder = 7 },
                new Position { PositionName = "CTO GĐ Kỹ thuật", SortOrder = 8 },
                new Position { PositionName = "Kế Toán Trưởng", SortOrder = 9 },
                new Position { PositionName = "Trưởng phòng thiết kế", SortOrder = 10 },
                new Position { PositionName = "Trưởng Phòng Mạng", SortOrder = 11 },
                new Position { PositionName = "PM (Product Manager)", SortOrder = 12 },
                new Position { PositionName = "HCNS", SortOrder = 13 },
                new Position { PositionName = "Hành chính nhân sự", SortOrder = 14 },
                new Position { PositionName = "Kế toán", SortOrder = 15 },
                new Position { PositionName = "Kỹ thuật", SortOrder = 16 },
                new Position { PositionName = "IT Helpdesk", SortOrder = 17 },
                new Position { PositionName = "System Admin", SortOrder = 18 },
                new Position { PositionName = "Frontend Developer", SortOrder = 19 },
                new Position { PositionName = "Tester", SortOrder = 20 },
                new Position { PositionName = "SDK - RD Minigame fake app", SortOrder = 21 },
                new Position { PositionName = "Sale", SortOrder = 22 },
                new Position { PositionName = "NV marketing", SortOrder = 23 },
                new Position { PositionName = "Digital Marketing", SortOrder = 24 },
                new Position { PositionName = "NV thiết kế", SortOrder = 25 },
                new Position { PositionName = "CS", SortOrder = 26 },
                new Position { PositionName = "Dịch thuật", SortOrder = 27 },
                new Position { PositionName = "Điều hành", SortOrder = 28 },
                new Position { PositionName = "Cộng đồng", SortOrder = 29 },
                new Position { PositionName = "Tạp Vụ", SortOrder = 30 },
            });
            await db.SaveChangesAsync();
        }

        // ── 2. Departments (Phòng ban) ──
        if (!await db.Departments.AnyAsync())
        {
            db.Departments.AddRange(new[]
            {
                new Department { DepartmentName = "BOD", Description = "Ban Giám Đốc" },
                new Department { DepartmentName = "AF", Description = "Phòng Kế toán - Tài chính" },
                new Department { DepartmentName = "BOM", Description = "Ban điều hành" },
                new Department { DepartmentName = "BD&RAL", Description = "Phòng Kinh doanh & RAL" },
                new Department { DepartmentName = "CAD", Description = "Phòng Thiết kế" },
                new Department { DepartmentName = "CS", Description = "Phòng Chăm sóc khách hàng" },
                new Department { DepartmentName = "GBC1", Description = "Phòng GBC1" },
                new Department { DepartmentName = "GBC2", Description = "Phòng GBC2" },
                new Department { DepartmentName = "GBC3", Description = "Phòng GBC3" },
                new Department { DepartmentName = "ISC", Description = "Phòng Kỹ thuật ISC" },
                new Department { DepartmentName = "MAC", Description = "Phòng MAC" },
                new Department { DepartmentName = "MAR & CS", Description = "Phòng Marketing & CS" },
                new Department { DepartmentName = "MOBILE", Description = "Phòng Mobile" },
                new Department { DepartmentName = "RAL", Description = "Phòng RAL" },
                new Department { DepartmentName = "TTCSCĐ", Description = "Trung tâm CSCĐ" },
            });
            await db.SaveChangesAsync();
        }

        // ── 3. JobTitles (Chức vụ) ──
        if (!await db.JobTitles.AnyAsync())
        {
            db.JobTitles.AddRange(new[]
            {
                new JobTitle { JobTitleName = "Ban Giám Đốc", SortOrder = 1 },
                new JobTitle { JobTitleName = "Trưởng phòng", SortOrder = 2 },
                new JobTitle { JobTitleName = "Manager", SortOrder = 3 },
                new JobTitle { JobTitleName = "Nhân viên", SortOrder = 4 },
                new JobTitle { JobTitleName = "Thực tập", SortOrder = 5 },
            });
            await db.SaveChangesAsync();
        }

        // ── 4. Asset Categories ──
        if (!await db.AssetCategories.AnyAsync())
        {
            db.AssetCategories.AddRange(new[]
            {
                new AssetCategory { CategoryName = "Laptop" },
                new AssetCategory { CategoryName = "Màn hình" },
                new AssetCategory { CategoryName = "Điện thoại" },
                new AssetCategory { CategoryName = "Máy in" },
                new AssetCategory { CategoryName = "Nội thất văn phòng" },
                new AssetCategory { CategoryName = "Máy tính để bàn" },
                new AssetCategory { CategoryName = "Thiết bị mạng" },
                new AssetCategory { CategoryName = "Tablet" },
                new AssetCategory { CategoryName = "Phụ kiện" },
            });
            await db.SaveChangesAsync();
        }

        // ── 5. Brands ──
        if (!await db.Brands.AnyAsync())
        {
            db.Brands.AddRange(new[]
            {
                new Brand { BrandName = "Dell" },
                new Brand { BrandName = "HP" },
                new Brand { BrandName = "Apple" },
                new Brand { BrandName = "Lenovo" },
                new Brand { BrandName = "Samsung" },
                new Brand { BrandName = "LG" },
                new Brand { BrandName = "Canon" },
                new Brand { BrandName = "Hòa Phát" },
                new Brand { BrandName = "Sihoo" },
                new Brand { BrandName = "Asus" },
                new Brand { BrandName = "Acer" },
                new Brand { BrandName = "MSI" },
            });
            await db.SaveChangesAsync();
        }

        // ── 6. Suppliers ──
        if (!await db.Suppliers.AnyAsync())
        {
            db.Suppliers.AddRange(new[]
            {
                new Supplier { SupplierName = "Dell Vietnam",    Phone = "1800 545 454", Address = "Tầng 15, Vincom Center, Q.1, TP.HCM" },
                new Supplier { SupplierName = "HP Vietnam",      Phone = "1800 588 868", Address = "Tầng 10, Bitexco Tower, Q.1, TP.HCM" },
                new Supplier { SupplierName = "Apple Vietnam",   Phone = "1800 1192",    Address = "Tầng 2, Takashimaya, Q.1, TP.HCM" },
                new Supplier { SupplierName = "Lenovo Vietnam",  Phone = "1800 588 822", Address = "Tầng 8, Saigon Centre, Q.1, TP.HCM" },
                new Supplier { SupplierName = "Samsung Vietnam", Phone = "1800 588 889", Address = "68 Lê Lợi, Q.1, TP.HCM" },
                new Supplier { SupplierName = "LG Vietnam",      Phone = "1800 599 888", Address = "72 Lê Thánh Tôn, Q.1, TP.HCM" },
                new Supplier { SupplierName = "Canon Vietnam",   Phone = "1800 585 828", Address = "Tầng 12, Viettel Tower, Q.10, TP.HCM" },
                new Supplier { SupplierName = "Hòa Phát",        Phone = "028 3925 1234", Address = "39 Nguyễn Trãi, Q.1, TP.HCM" },
                new Supplier { SupplierName = "Ergonomic VN",    Phone = "028 7300 6789", Address = "150 Hoàng Văn Thụ, Q.Phú Nhuận, TP.HCM" },
                new Supplier { SupplierName = "Phong Vũ",        Phone = "1800 6865",     Address = "117 Nguyễn Thị Minh Khai, Q.1, TP.HCM" },
            });
            await db.SaveChangesAsync();
        }
    }
}
