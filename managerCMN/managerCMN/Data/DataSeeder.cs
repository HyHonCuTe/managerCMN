using managerCMN.Models.Entities;
using managerCMN.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace managerCMN.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db)
    {
        if (await db.Employees.AnyAsync())
            return;

        // ── 1. Positions (from company position list) ──
        var positions = new[]
        {
            new Position { PositionName = "T\u1ED5ng Gi\u00E1m \u0110\u1ED1c", SortOrder = 1 },
            new Position { PositionName = "Ph\u00F3 T\u1ED5ng Gi\u00E1m \u0110\u1ED1c", SortOrder = 2 },
            new Position { PositionName = "Ph\u00F3 Gi\u00E1m \u0110\u1ED1c \u0111i\u1EC1u h\u00E0nh", SortOrder = 3 },
            new Position { PositionName = "Ch\u1EE7 T\u1ECBch H\u0110QT", SortOrder = 4 },
            new Position { PositionName = "Th\u00E0nh vi\u00EAn H\u0110QT", SortOrder = 5 },
            new Position { PositionName = "Gi\u00E1m \u0110\u1ED1c ISC", SortOrder = 6 },
            new Position { PositionName = "Gi\u00E1m \u0111\u1ED1c BD", SortOrder = 7 },
            new Position { PositionName = "CTO G\u0110 K\u1EF9 thu\u1EADt", SortOrder = 8 },
            new Position { PositionName = "K\u1EBF To\u00E1n Tr\u01B0\u1EDFng", SortOrder = 9 },
            new Position { PositionName = "Tr\u01B0\u1EDFng ph\u00F2ng thi\u1EBFt k\u1EBF", SortOrder = 10 },
            new Position { PositionName = "Tr\u01B0\u1EDFng Ph\u00F2ng M\u1EA1ng", SortOrder = 11 },
            new Position { PositionName = "PM (Product Manager)", SortOrder = 12 },
            new Position { PositionName = "HCNS", SortOrder = 13 },
            new Position { PositionName = "H\u00E0nh ch\u00EDnh nh\u00E2n s\u1EF1", SortOrder = 14 },
            new Position { PositionName = "K\u1EBF to\u00E1n", SortOrder = 15 },
            new Position { PositionName = "K\u1EF9 thu\u1EADt", SortOrder = 16 },
            new Position { PositionName = "IT Helpdesk", SortOrder = 17 },
            new Position { PositionName = "System Admin", SortOrder = 18 },
            new Position { PositionName = "Frontend Developer", SortOrder = 19 },
            new Position { PositionName = "Tester", SortOrder = 20 },
            new Position { PositionName = "SDK - RD Minigame fake app", SortOrder = 21 },
            new Position { PositionName = "Sale", SortOrder = 22 },
            new Position { PositionName = "NV marketing", SortOrder = 23 },
            new Position { PositionName = "Digital Marketing", SortOrder = 24 },
            new Position { PositionName = "NV thi\u1EBFt k\u1EBF", SortOrder = 25 },
            new Position { PositionName = "CS", SortOrder = 26 },
            new Position { PositionName = "D\u1ECBch thu\u1EADt", SortOrder = 27 },
            new Position { PositionName = "\u0110i\u1EC1u h\u00E0nh", SortOrder = 28 },
            new Position { PositionName = "C\u1ED9ng \u0111\u1ED3ng", SortOrder = 29 },
            new Position { PositionName = "T\u1EA1p V\u1EE5", SortOrder = 30 },
        };
        db.Positions.AddRange(positions);
        await db.SaveChangesAsync();

        var p = await db.Positions.ToDictionaryAsync(x => x.PositionName, x => x.PositionId);

        // ── 2. Departments ──
        var departments = new[]
        {
            new Department { DepartmentName = "Ban Gi\u00E1m \u0110\u1ED1c", Description = "Ban l\u00E3nh \u0111\u1EA1o c\u00F4ng ty" },
            new Department { DepartmentName = "Ph\u00F2ng Nh\u00E2n s\u1EF1", Description = "Qu\u1EA3n l\u00FD nh\u00E2n s\u1EF1, tuy\u1EC3n d\u1EE5ng, \u0111\u00E0o t\u1EA1o, C&B" },
            new Department { DepartmentName = "Ph\u00F2ng K\u1EBF to\u00E1n", Description = "K\u1EBF to\u00E1n t\u00E0i ch\u00EDnh, thu\u1EBF, c\u00F4ng n\u1EE3" },
            new Department { DepartmentName = "Ph\u00F2ng Kinh doanh", Description = "B\u00E1n h\u00E0ng, ch\u0103m s\u00F3c kh\u00E1ch h\u00E0ng" },
            new Department { DepartmentName = "Ph\u00F2ng K\u1EF9 thu\u1EADt", Description = "Ph\u00E1t tri\u1EC3n ph\u1EA7n m\u1EC1m, h\u1EA1 t\u1EA7ng IT" },
            new Department { DepartmentName = "Ph\u00F2ng Marketing", Description = "Truy\u1EC1n th\u00F4ng, qu\u1EA3ng c\u00E1o, th\u01B0\u01A1ng hi\u1EC7u" },
        };
        db.Departments.AddRange(departments);
        await db.SaveChangesAsync();

        var d = await db.Departments.ToDictionaryAsync(x => x.DepartmentName, x => x.DepartmentId);
        int dBGD = d["Ban Giám Đốc"], dNS = d["Phòng Nhân sự"], dKT = d["Phòng Kế toán"],
            dKD = d["Phòng Kinh doanh"], dKyThuat = d["Phòng Kỹ thuật"], dMKT = d["Phòng Marketing"];

        // ── 3. Employees (30) ──
        var employees = new List<Employee>
        {
            // Ban Gi\u00E1m \u0110\u1ED1c \u2014 3
            new() { EmployeeCode = "NV001", AttendanceCode = "AC0001", FullName = "Nguy\u1EC5n V\u0103n H\u00F9ng",   DateOfBirth = new DateTime(1975, 3, 15), Gender = Gender.Male,   Email = "hung.nv@company.com",    Phone = "0901000001", DepartmentId = dBGD,     PositionId = p["T\u1ED5ng Gi\u00E1m \u0110\u1ED1c"], Qualifications = "MBA - \u0110\u1EA1i h\u1ECDc Kinh t\u1EBF TP.HCM",           StartWorkingDate = new DateTime(2015, 1, 1),  BankAccount = "1001000001", BankName = "Vietcombank", TaxCode = "8001000001", PermanentAddress = "123 Nguy\u1EC5n Hu\u1EC7, Q.1, TP.HCM" },
            new() { EmployeeCode = "NV002", AttendanceCode = "AC0002", FullName = "Tr\u1EA7n Th\u1ECB Mai",       DateOfBirth = new DateTime(1978, 7, 20), Gender = Gender.Female, Email = "mai.tt@company.com",     Phone = "0901000002", DepartmentId = dBGD,     PositionId = p["Ph\u00F3 T\u1ED5ng Gi\u00E1m \u0110\u1ED1c"], Qualifications = "Th\u1EA1c s\u0129 Qu\u1EA3n tr\u1ECB kinh doanh",            StartWorkingDate = new DateTime(2016, 3, 1),  BankAccount = "1001000002", BankName = "Vietcombank", TaxCode = "8001000002", PermanentAddress = "45 L\u00EA L\u1EE3i, Q.1, TP.HCM" },
            new() { EmployeeCode = "NV003", AttendanceCode = "AC0003", FullName = "L\u00EA Ho\u00E0ng Nam",       DateOfBirth = new DateTime(1990, 11, 5), Gender = Gender.Male,   Email = "nam.lh@company.com",     Phone = "0901000003", DepartmentId = dBGD,     PositionId = p["\u0110i\u1EC1u h\u00E0nh"], Qualifications = "C\u1EED nh\u00E2n Qu\u1EA3n tr\u1ECB kinh doanh",            StartWorkingDate = new DateTime(2019, 6, 15), BankAccount = "1001000003", BankName = "Techcombank", TaxCode = "8001000003", PermanentAddress = "78 Tr\u1EA7n H\u01B0ng \u0110\u1EA1o, Q.5, TP.HCM" },

            // Ph\u00F2ng Nh\u00E2n s\u1EF1 \u2014 5
            new() { EmployeeCode = "NV004", AttendanceCode = "AC0004", FullName = "Ph\u1EA1m Th\u1ECB H\u01B0\u01A1ng",     DateOfBirth = new DateTime(1982, 5, 10), Gender = Gender.Female, Email = "huong.pt@company.com",   Phone = "0901000004", DepartmentId = dNS,      PositionId = p["HCNS"], Qualifications = "Th\u1EA1c s\u0129 Qu\u1EA3n tr\u1ECB nh\u00E2n l\u1EF1c",              StartWorkingDate = new DateTime(2017, 2, 1),  BankAccount = "1001000004", BankName = "BIDV",        TaxCode = "8001000004", PermanentAddress = "12 C\u00E1ch M\u1EA1ng Th\u00E1ng 8, Q.3, TP.HCM" },
            new() { EmployeeCode = "NV005", AttendanceCode = "AC0005", FullName = "Nguy\u1EC5n Minh Tu\u1EA5n",   DateOfBirth = new DateTime(1992, 8, 25), Gender = Gender.Male,   Email = "tuan.nm@company.com",    Phone = "0901000005", DepartmentId = dNS,      PositionId = p["H\u00E0nh ch\u00EDnh nh\u00E2n s\u1EF1"], Qualifications = "C\u1EED nh\u00E2n Qu\u1EA3n tr\u1ECB nh\u00E2n l\u1EF1c",               StartWorkingDate = new DateTime(2020, 4, 1),  BankAccount = "1001000005", BankName = "BIDV",        TaxCode = "8001000005", PermanentAddress = "56 \u0110i\u1EC7n Bi\u00EAn Ph\u1EE7, Q.B\u00ECnh Th\u1EA1nh, TP.HCM" },
            new() { EmployeeCode = "NV006", AttendanceCode = "AC0006", FullName = "Tr\u1EA7n V\u0103n \u0110\u1EE9c",       DateOfBirth = new DateTime(1991, 1, 12), Gender = Gender.Male,   Email = "duc.tv@company.com",     Phone = "0901000006", DepartmentId = dNS,      PositionId = p["H\u00E0nh ch\u00EDnh nh\u00E2n s\u1EF1"], Qualifications = "C\u1EED nh\u00E2n T\u00E0i ch\u00EDnh",                      StartWorkingDate = new DateTime(2020, 7, 15), BankAccount = "1001000006", BankName = "Techcombank", TaxCode = "8001000006", PermanentAddress = "90 Nguy\u1EC5n Th\u1ECB Minh Khai, Q.3, TP.HCM" },
            new() { EmployeeCode = "NV007", AttendanceCode = "AC0007", FullName = "L\u00EA Th\u1ECB Lan",         DateOfBirth = new DateTime(1993, 4, 18), Gender = Gender.Female, Email = "lan.lt@company.com",     Phone = "0901000007", DepartmentId = dNS,      PositionId = p["H\u00E0nh ch\u00EDnh nh\u00E2n s\u1EF1"], Qualifications = "C\u1EED nh\u00E2n S\u01B0 ph\u1EA1m",                        StartWorkingDate = new DateTime(2021, 1, 10), BankAccount = "1001000007", BankName = "ACB",         TaxCode = "8001000007", PermanentAddress = "34 Hai B\u00E0 Tr\u01B0ng, Q.1, TP.HCM" },
            new() { EmployeeCode = "NV008", AttendanceCode = "AC0008", FullName = "Ho\u00E0ng V\u0103n Ph\u00FA",      DateOfBirth = new DateTime(1995, 9, 3),  Gender = Gender.Male,   Email = "phu.hv@company.com",     Phone = "0901000008", DepartmentId = dNS,      PositionId = p["H\u00E0nh ch\u00EDnh nh\u00E2n s\u1EF1"], Qualifications = "C\u1EED nh\u00E2n H\u00E0nh ch\u00EDnh h\u1ECDc",                  StartWorkingDate = new DateTime(2022, 3, 1),  BankAccount = "1001000008", BankName = "ACB",         TaxCode = "8001000008", PermanentAddress = "67 L\u00FD Th\u01B0\u1EDDng Ki\u1EC7t, Q.T\u00E2n B\u00ECnh, TP.HCM" },

            // Ph\u00F2ng K\u1EBF to\u00E1n \u2014 5
            new() { EmployeeCode = "NV009", AttendanceCode = "AC0009", FullName = "V\u0169 Th\u1ECB Nga",         DateOfBirth = new DateTime(1980, 2, 14), Gender = Gender.Female, Email = "nga.vt@company.com",     Phone = "0901000009", DepartmentId = dKT,      PositionId = p["K\u1EBF To\u00E1n Tr\u01B0\u1EDFng"], Qualifications = "Th\u1EA1c s\u0129 K\u1EBF to\u00E1n - Ki\u1EC3m to\u00E1n",            StartWorkingDate = new DateTime(2016, 5, 1),  BankAccount = "1001000009", BankName = "Vietcombank", TaxCode = "8001000009", PermanentAddress = "23 Pasteur, Q.3, TP.HCM" },
            new() { EmployeeCode = "NV010", AttendanceCode = "AC0010", FullName = "Nguy\u1EC5n V\u0103n B\u00ECnh",    DateOfBirth = new DateTime(1993, 6, 22), Gender = Gender.Male,   Email = "binh.nv@company.com",    Phone = "0901000010", DepartmentId = dKT,      PositionId = p["K\u1EBF to\u00E1n"], Qualifications = "C\u1EED nh\u00E2n K\u1EBF to\u00E1n",                        StartWorkingDate = new DateTime(2021, 8, 1),  BankAccount = "1001000010", BankName = "Vietcombank", TaxCode = "8001000010", PermanentAddress = "150 Nguy\u1EC5n Tr\u00E3i, Q.5, TP.HCM" },
            new() { EmployeeCode = "NV011", AttendanceCode = "AC0011", FullName = "Tr\u1EA7n Th\u1ECB Hoa",       DateOfBirth = new DateTime(1994, 12, 8), Gender = Gender.Female, Email = "hoa.tt@company.com",     Phone = "0901000011", DepartmentId = dKT,      PositionId = p["K\u1EBF to\u00E1n"], Qualifications = "C\u1EED nh\u00E2n K\u1EBF to\u00E1n",                        StartWorkingDate = new DateTime(2022, 1, 1),  BankAccount = "1001000011", BankName = "BIDV",        TaxCode = "8001000011", PermanentAddress = "88 V\u00F5 V\u0103n T\u1EA7n, Q.3, TP.HCM" },
            new() { EmployeeCode = "NV012", AttendanceCode = "AC0012", FullName = "Ph\u1EA1m V\u0103n Long",      DateOfBirth = new DateTime(1990, 3, 30), Gender = Gender.Male,   Email = "long.pv@company.com",    Phone = "0901000012", DepartmentId = dKT,      PositionId = p["K\u1EBF to\u00E1n"], Qualifications = "C\u1EED nh\u00E2n T\u00E0i ch\u00EDnh - K\u1EBF to\u00E1n",            StartWorkingDate = new DateTime(2020, 11, 1), BankAccount = "1001000012", BankName = "Sacombank",   TaxCode = "8001000012", PermanentAddress = "45 Phan X\u00EDch Long, Q.Ph\u00FA Nhu\u1EADn, TP.HCM" },
            new() { EmployeeCode = "NV013", AttendanceCode = "AC0013", FullName = "L\u00EA Th\u1ECB Thu\u1EF7",        DateOfBirth = new DateTime(1996, 7, 15), Gender = Gender.Female, Email = "thuy.lt@company.com",    Phone = "0901000013", DepartmentId = dKT,      PositionId = p["K\u1EBF to\u00E1n"], Qualifications = "C\u1EED nh\u00E2n K\u1EBF to\u00E1n",                        StartWorkingDate = new DateTime(2023, 2, 1),  BankAccount = "1001000013", BankName = "Sacombank",   TaxCode = "8001000013", PermanentAddress = "102 B\u00F9i Vi\u1EC7n, Q.1, TP.HCM" },

            // Ph\u00F2ng Kinh doanh \u2014 6
            new() { EmployeeCode = "NV014", AttendanceCode = "AC0014", FullName = "\u0110\u1ED7 V\u0103n M\u1EA1nh",        DateOfBirth = new DateTime(1983, 10, 5), Gender = Gender.Male,   Email = "manh.dv@company.com",    Phone = "0901000014", DepartmentId = dKD,      PositionId = p["Gi\u00E1m \u0111\u1ED1c BD"], Qualifications = "MBA - \u0110\u1EA1i h\u1ECDc Ngo\u1EA1i th\u01B0\u01A1ng",              StartWorkingDate = new DateTime(2017, 9, 1),  BankAccount = "1001000014", BankName = "Vietcombank", TaxCode = "8001000014", PermanentAddress = "77 Nam K\u1EF3 Kh\u1EDFi Ngh\u0129a, Q.1, TP.HCM" },
            new() { EmployeeCode = "NV015", AttendanceCode = "AC0015", FullName = "Nguy\u1EC5n Th\u1ECB Y\u1EBFn",     DateOfBirth = new DateTime(1994, 5, 20), Gender = Gender.Female, Email = "yen.nt@company.com",     Phone = "0901000015", DepartmentId = dKD,      PositionId = p["Sale"], Qualifications = "C\u1EED nh\u00E2n Qu\u1EA3n tr\u1ECB kinh doanh",            StartWorkingDate = new DateTime(2021, 5, 1),  BankAccount = "1001000015", BankName = "Techcombank", TaxCode = "8001000015", PermanentAddress = "55 L\u00EA V\u0103n S\u1EF9, Q.3, TP.HCM" },
            new() { EmployeeCode = "NV016", AttendanceCode = "AC0016", FullName = "Tr\u1EA7n V\u0103n Khoa",      DateOfBirth = new DateTime(1993, 2, 28), Gender = Gender.Male,   Email = "khoa.tv@company.com",    Phone = "0901000016", DepartmentId = dKD,      PositionId = p["Sale"], Qualifications = "C\u1EED nh\u00E2n Th\u01B0\u01A1ng m\u1EA1i",                     StartWorkingDate = new DateTime(2021, 7, 15), BankAccount = "1001000016", BankName = "Techcombank", TaxCode = "8001000016", PermanentAddress = "200 X\u00F4 Vi\u1EBFt Ngh\u1EC7 T\u0129nh, Q.B\u00ECnh Th\u1EA1nh, TP.HCM" },
            new() { EmployeeCode = "NV017", AttendanceCode = "AC0017", FullName = "Ph\u1EA1m Th\u1ECB Dung",      DateOfBirth = new DateTime(1995, 11, 10),Gender = Gender.Female, Email = "dung.pt@company.com",    Phone = "0901000017", DepartmentId = dKD,      PositionId = p["Sale"], Qualifications = "C\u1EED nh\u00E2n Marketing",                      StartWorkingDate = new DateTime(2022, 4, 1),  BankAccount = "1001000017", BankName = "ACB",         TaxCode = "8001000017", PermanentAddress = "30 Nguy\u1EC5n \u0110\u00ECnh Chi\u1EC3u, Q.3, TP.HCM" },
            new() { EmployeeCode = "NV018", AttendanceCode = "AC0018", FullName = "L\u00EA V\u0103n T\u00F9ng",        DateOfBirth = new DateTime(1991, 8, 8),  Gender = Gender.Male,   Email = "tung.lv@company.com",    Phone = "0901000018", DepartmentId = dKD,      PositionId = p["Sale"], Qualifications = "C\u1EED nh\u00E2n Kinh t\u1EBF",                        StartWorkingDate = new DateTime(2020, 9, 1),  BankAccount = "1001000018", BankName = "VPBank",      TaxCode = "8001000018", PermanentAddress = "160 Ho\u00E0ng V\u0103n Th\u1EE5, Q.Ph\u00FA Nhu\u1EADn, TP.HCM" },
            new() { EmployeeCode = "NV019", AttendanceCode = "AC0019", FullName = "Ho\u00E0ng Th\u1ECB Linh",     DateOfBirth = new DateTime(1996, 1, 25), Gender = Gender.Female, Email = "linh.ht@company.com",    Phone = "0901000019", DepartmentId = dKD,      PositionId = p["CS"], Qualifications = "C\u1EED nh\u00E2n Qu\u1EA3n tr\u1ECB kinh doanh",            StartWorkingDate = new DateTime(2023, 1, 10), BankAccount = "1001000019", BankName = "VPBank",      TaxCode = "8001000019", PermanentAddress = "42 Tr\u01B0\u1EDDng Sa, Q.Ph\u00FA Nhu\u1EADn, TP.HCM" },

            // Ph\u00F2ng K\u1EF9 thu\u1EADt \u2014 6
            new() { EmployeeCode = "NV020", AttendanceCode = "AC0020", FullName = "B\u00F9i V\u0103n Th\u1EAFng",      DateOfBirth = new DateTime(1985, 4, 12), Gender = Gender.Male,   Email = "thang.bv@company.com",   Phone = "0901000020", DepartmentId = dKyThuat, PositionId = p["CTO G\u0110 K\u1EF9 thu\u1EADt"], Qualifications = "Th\u1EA1c s\u0129 Khoa h\u1ECDc m\u00E1y t\u00EDnh",              StartWorkingDate = new DateTime(2018, 1, 1),  BankAccount = "1001000020", BankName = "BIDV",        TaxCode = "8001000020", PermanentAddress = "180 Nguy\u1EC5n Th\u01B0\u1EE3ng Hi\u1EC1n, Q.Ph\u00FA Nhu\u1EADn, TP.HCM" },
            new() { EmployeeCode = "NV021", AttendanceCode = "AC0021", FullName = "Nguy\u1EC5n V\u0103n \u0110\u1EA1t",     DateOfBirth = new DateTime(1990, 9, 17), Gender = Gender.Male,   Email = "dat.nv@company.com",     Phone = "0901000021", DepartmentId = dKyThuat, PositionId = p["K\u1EF9 thu\u1EADt"], Qualifications = "C\u1EED nh\u00E2n C\u00F4ng ngh\u1EC7 th\u00F4ng tin",            StartWorkingDate = new DateTime(2019, 5, 1),  BankAccount = "1001000021", BankName = "Techcombank", TaxCode = "8001000021", PermanentAddress = "99 Tr\u01B0\u1EDDng Chinh, Q.T\u00E2n B\u00ECnh, TP.HCM" },
            new() { EmployeeCode = "NV022", AttendanceCode = "AC0022", FullName = "Tr\u1EA7n Minh Qu\u00E2n",     DateOfBirth = new DateTime(1995, 6, 3),  Gender = Gender.Male,   Email = "quan.tm@company.com",    Phone = "0901000022", DepartmentId = dKyThuat, PositionId = p["Frontend Developer"], Qualifications = "C\u1EED nh\u00E2n C\u00F4ng ngh\u1EC7 ph\u1EA7n m\u1EC1m",             StartWorkingDate = new DateTime(2022, 6, 1),  BankAccount = "1001000022", BankName = "Techcombank", TaxCode = "8001000022", PermanentAddress = "50 C\u1ED9ng H\u00F2a, Q.T\u00E2n B\u00ECnh, TP.HCM" },
            new() { EmployeeCode = "NV023", AttendanceCode = "AC0023", FullName = "Ph\u1EA1m Th\u1ECB H\u1EB1ng",      DateOfBirth = new DateTime(1994, 3, 22), Gender = Gender.Female, Email = "hang.pt@company.com",    Phone = "0901000023", DepartmentId = dKyThuat, PositionId = p["Tester"], Qualifications = "C\u1EED nh\u00E2n C\u00F4ng ngh\u1EC7 th\u00F4ng tin",            StartWorkingDate = new DateTime(2021, 10, 1), BankAccount = "1001000023", BankName = "ACB",         TaxCode = "8001000023", PermanentAddress = "70 L\u00FD Ch\u00EDnh Th\u1EAFng, Q.3, TP.HCM" },
            new() { EmployeeCode = "NV024", AttendanceCode = "AC0024", FullName = "L\u00EA V\u0103n S\u01A1n",         DateOfBirth = new DateTime(1992, 12, 1), Gender = Gender.Male,   Email = "son.lv@company.com",     Phone = "0901000024", DepartmentId = dKyThuat, PositionId = p["System Admin"], Qualifications = "C\u1EED nh\u00E2n M\u1EA1ng m\u00E1y t\u00EDnh",                  StartWorkingDate = new DateTime(2020, 3, 15), BankAccount = "1001000024", BankName = "VPBank",      TaxCode = "8001000024", PermanentAddress = "135 Ho\u00E0ng Hoa Th\u00E1m, Q.T\u00E2n B\u00ECnh, TP.HCM" },
            new() { EmployeeCode = "NV025", AttendanceCode = "AC0025", FullName = "Ho\u00E0ng V\u0103n Ki\u00EAn",     DateOfBirth = new DateTime(1997, 5, 19), Gender = Gender.Male,   Email = "kien.hv@company.com",    Phone = "0901000025", DepartmentId = dKyThuat, PositionId = p["IT Helpdesk"], Qualifications = "C\u1EED nh\u00E2n Khoa h\u1ECDc m\u00E1y t\u00EDnh",              StartWorkingDate = new DateTime(2023, 7, 1),  BankAccount = "1001000025", BankName = "MBBank",      TaxCode = "8001000025", PermanentAddress = "22 Phan \u0110\u0103ng L\u01B0u, Q.Ph\u00FA Nhu\u1EADn, TP.HCM" },

            // Ph\u00F2ng Marketing \u2014 5
            new() { EmployeeCode = "NV026", AttendanceCode = "AC0026", FullName = "Ng\u00F4 Th\u1ECB \u00C1nh",        DateOfBirth = new DateTime(1986, 8, 30), Gender = Gender.Female, Email = "anh.nt@company.com",     Phone = "0901000026", DepartmentId = dMKT,     PositionId = p["PM (Product Manager)"], Qualifications = "Th\u1EA1c s\u0129 Marketing",                      StartWorkingDate = new DateTime(2018, 4, 1),  BankAccount = "1001000026", BankName = "Vietcombank", TaxCode = "8001000026", PermanentAddress = "60 Nguy\u1EC5n C\u00F4ng Tr\u1EE9, Q.1, TP.HCM" },
            new() { EmployeeCode = "NV027", AttendanceCode = "AC0027", FullName = "Nguy\u1EC5n V\u0103n Trung",   DateOfBirth = new DateTime(1993, 10, 14),Gender = Gender.Male,   Email = "trung.nv@company.com",   Phone = "0901000027", DepartmentId = dMKT,     PositionId = p["NV marketing"], Qualifications = "C\u1EED nh\u00E2n Marketing",                      StartWorkingDate = new DateTime(2021, 3, 1),  BankAccount = "1001000027", BankName = "Sacombank",   TaxCode = "8001000027", PermanentAddress = "88 Nguy\u1EC5n V\u0103n C\u1EEB, Q.5, TP.HCM" },
            new() { EmployeeCode = "NV028", AttendanceCode = "AC0028", FullName = "Tr\u1EA7n Th\u1ECB Ph\u01B0\u01A1ng",    DateOfBirth = new DateTime(1996, 2, 5),  Gender = Gender.Female, Email = "phuong.tt@company.com",  Phone = "0901000028", DepartmentId = dMKT,     PositionId = p["Digital Marketing"], Qualifications = "C\u1EED nh\u00E2n Truy\u1EC1n th\u00F4ng \u0111a ph\u01B0\u01A1ng ti\u1EC7n",    StartWorkingDate = new DateTime(2022, 8, 1),  BankAccount = "1001000028", BankName = "MBBank",      TaxCode = "8001000028", PermanentAddress = "15 Nguy\u1EC5n C\u01B0 Trinh, Q.1, TP.HCM" },
            new() { EmployeeCode = "NV029", AttendanceCode = "AC0029", FullName = "Ph\u1EA1m V\u0103n Huy",       DateOfBirth = new DateTime(1994, 7, 11), Gender = Gender.Male,   Email = "huy.pv@company.com",     Phone = "0901000029", DepartmentId = dMKT,     PositionId = p["NV thi\u1EBFt k\u1EBF"], Qualifications = "C\u1EED nh\u00E2n Thi\u1EBFt k\u1EBF \u0111\u1ED3 h\u1ECDa",                StartWorkingDate = new DateTime(2021, 11, 1), BankAccount = "1001000029", BankName = "MBBank",      TaxCode = "8001000029", PermanentAddress = "40 B\u00E0 Huy\u1EC7n Thanh Quan, Q.3, TP.HCM" },
            new() { EmployeeCode = "NV030", AttendanceCode = "AC0030", FullName = "L\u00EA Th\u1ECB Trang",       DateOfBirth = new DateTime(1997, 4, 27), Gender = Gender.Female, Email = "trang.lt@company.com",   Phone = "0901000030", DepartmentId = dMKT,     PositionId = p["Digital Marketing"], Qualifications = "C\u1EED nh\u00E2n Quan h\u1EC7 c\u00F4ng ch\u00FAng",             StartWorkingDate = new DateTime(2023, 5, 1),  BankAccount = "1001000030", BankName = "TPBank",      TaxCode = "8001000030", PermanentAddress = "95 L\u00EA Quang \u0110\u1ECBnh, Q.B\u00ECnh Th\u1EA1nh, TP.HCM" },
        };
        db.Employees.AddRange(employees);
        await db.SaveChangesAsync();

        // ── 3. Assign Department Managers ──
        var empByCode = await db.Employees.ToDictionaryAsync(e => e.EmployeeCode, e => e.EmployeeId);

        var deptList = await db.Departments.ToListAsync();
        foreach (var dept in deptList)
        {
            dept.ManagerId = dept.DepartmentName switch
            {
                "Ban Giám Đốc"     => empByCode["NV001"],
                "Phòng Nhân sự"    => empByCode["NV004"],
                "Phòng Kế toán"    => empByCode["NV009"],
                "Phòng Kinh doanh" => empByCode["NV014"],
                "Phòng Kỹ thuật"   => empByCode["NV020"],
                "Phòng Marketing"  => empByCode["NV026"],
                _ => null
            };
        }
        await db.SaveChangesAsync();

        // ── 4. Contracts ──
        // Reverse lookup: PositionId → PositionName
        var posName = await db.Positions.ToDictionaryAsync(x => x.PositionId, x => x.PositionName);
        var contracts = new List<Contract>();
        foreach (var emp in employees)
        {
            var empId = empByCode[emp.EmployeeCode];
            var pn = emp.PositionId.HasValue ? posName[emp.PositionId.Value] : "";
            var isLeader = pn.Contains("Gi\u00E1m") || pn.Contains("Tr\u01B0\u1EDFng") || pn == "CTO G\u0110 K\u1EF9 thu\u1EADt" || pn == "HCNS";
            var contractType = isLeader
                ? ContractType.Indefinite
                : emp.StartWorkingDate >= new DateTime(2023, 1, 1) ? ContractType.Probation : ContractType.FixedTerm;

            var salary = pn switch
            {
                "T\u1ED5ng Gi\u00E1m \u0110\u1ED1c"        => 80_000_000m,
                "Ph\u00F3 T\u1ED5ng Gi\u00E1m \u0110\u1ED1c"    => 60_000_000m,
                "Ph\u00F3 Gi\u00E1m \u0110\u1ED1c \u0111i\u1EC1u h\u00E0nh" => 55_000_000m,
                "CTO G\u0110 K\u1EF9 thu\u1EADt"           => 50_000_000m,
                "Gi\u00E1m \u0111\u1ED1c BD"                => 45_000_000m,
                "K\u1EBF To\u00E1n Tr\u01B0\u1EDFng"       => 35_000_000m,
                "HCNS"                     => 35_000_000m,
                "PM (Product Manager)"     => 35_000_000m,
                "Tr\u01B0\u1EDFng ph\u00F2ng thi\u1EBFt k\u1EBF" => 35_000_000m,
                "Tr\u01B0\u1EDFng Ph\u00F2ng M\u1EA1ng"    => 35_000_000m,
                "\u0110i\u1EC1u h\u00E0nh"                  => 20_000_000m,
                "System Admin"             => 25_000_000m,
                _ => 18_000_000m,
            };

            contracts.Add(new Contract
            {
                EmployeeId = empId,
                ContractType = contractType,
                StartDate = emp.StartWorkingDate ?? DateTime.UtcNow,
                EndDate = contractType == ContractType.Indefinite ? null : (emp.StartWorkingDate ?? DateTime.UtcNow).AddYears(2),
                Salary = salary,
                Status = ContractStatus.Active,
            });
        }
        db.Contracts.AddRange(contracts);
        await db.SaveChangesAsync();

        // ── 5. Emergency Contacts ──
        db.Set<EmployeeContact>().AddRange(new[]
        {
            new EmployeeContact { EmployeeId = empByCode["NV001"], FullName = "Nguy\u1EC5n Th\u1ECB Lan",    Relationship = "V\u1EE3",    Phone = "0911000001", Address = "123 Nguy\u1EC5n Hu\u1EC7, Q.1" },
            new EmployeeContact { EmployeeId = empByCode["NV002"], FullName = "Tr\u1EA7n V\u0103n H\u1EA3i",   Relationship = "Ch\u1ED3ng", Phone = "0911000002", Address = "45 L\u00EA L\u1EE3i, Q.1" },
            new EmployeeContact { EmployeeId = empByCode["NV003"], FullName = "L\u00EA Th\u1ECB H\u1EB1ng",   Relationship = "M\u1EB9",    Phone = "0911000003", Address = "78 Tr\u1EA7n H\u01B0ng \u0110\u1EA1o, Q.5" },
            new EmployeeContact { EmployeeId = empByCode["NV004"], FullName = "Ph\u1EA1m V\u0103n T\u00E0i",   Relationship = "Ch\u1ED3ng", Phone = "0911000004", Address = "12 CMT8, Q.3" },
            new EmployeeContact { EmployeeId = empByCode["NV005"], FullName = "Nguy\u1EC5n Th\u1ECB H\u00E0",  Relationship = "V\u1EE3",    Phone = "0911000005", Address = "56 \u0110BPh\u1EE7, Q.B\u00ECnh Th\u1EA1nh" },
            new EmployeeContact { EmployeeId = empByCode["NV006"], FullName = "Tr\u1EA7n V\u0103n An",        Relationship = "Anh",    Phone = "0911000006", Address = "90 NTMK, Q.3" },
            new EmployeeContact { EmployeeId = empByCode["NV007"], FullName = "L\u00EA V\u0103n B\u1EA3o",      Relationship = "Ch\u1ED3ng", Phone = "0911000007", Address = "34 HBT, Q.1" },
            new EmployeeContact { EmployeeId = empByCode["NV008"], FullName = "Ho\u00E0ng Th\u1ECB Mai",      Relationship = "M\u1EB9",    Phone = "0911000008", Address = "67 LTK, Q.T\u00E2n B\u00ECnh" },
            new EmployeeContact { EmployeeId = empByCode["NV009"], FullName = "V\u0169 V\u0103n Th\u00E0nh",    Relationship = "Ch\u1ED3ng", Phone = "0911000009", Address = "23 Pasteur, Q.3" },
            new EmployeeContact { EmployeeId = empByCode["NV010"], FullName = "Nguy\u1EC5n Th\u1ECB Xu\u00E2n", Relationship = "M\u1EB9",    Phone = "0911000010", Address = "150 NT, Q.5" },
            new EmployeeContact { EmployeeId = empByCode["NV014"], FullName = "\u0110\u1ED7 Th\u1ECB Nh\u00E0n",   Relationship = "V\u1EE3",    Phone = "0911000014", Address = "77 NKK Ngh\u0129a, Q.1" },
            new EmployeeContact { EmployeeId = empByCode["NV020"], FullName = "B\u00F9i Th\u1ECB Thu",        Relationship = "V\u1EE3",    Phone = "0911000020", Address = "180 NTH, Q.Ph\u00FA Nhu\u1EADn" },
            new EmployeeContact { EmployeeId = empByCode["NV021"], FullName = "Nguy\u1EC5n Th\u1ECB Ng\u1ECDc", Relationship = "V\u1EE3",    Phone = "0911000021", Address = "99 TC, Q.T\u00E2n B\u00ECnh" },
            new EmployeeContact { EmployeeId = empByCode["NV026"], FullName = "Ng\u00F4 V\u0103n Ph\u00FAc",    Relationship = "Ch\u1ED3ng", Phone = "0911000026", Address = "60 NCT, Q.1" },
        });
        await db.SaveChangesAsync();

        // ── 6. Leave Balances (2026) ──
        db.LeaveBalances.AddRange(employees.Select(emp => new LeaveBalance
        {
            EmployeeId = empByCode[emp.EmployeeCode],
            Year = 2026,
            TotalLeave = 12m,
            UsedLeave = 0m,
            RemainingLeave = 12m,
            CarryForward = 0m,
        }));
        await db.SaveChangesAsync();

        // ── 6. Assets (20) ──

        // Seed Asset Categories
        var assetCategories = new[]
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
        };
        db.AssetCategories.AddRange(assetCategories);
        await db.SaveChangesAsync();
        var catMap = await db.AssetCategories.ToDictionaryAsync(c => c.CategoryName, c => c.AssetCategoryId);

        // Seed Brands
        var brandsData = new[]
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
        };
        db.Brands.AddRange(brandsData);
        await db.SaveChangesAsync();
        var brandMap = await db.Brands.ToDictionaryAsync(b => b.BrandName, b => b.BrandId);

        // Seed Suppliers
        var suppliersData = new[]
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
        };
        db.Suppliers.AddRange(suppliersData);
        await db.SaveChangesAsync();
        var supMap = await db.Suppliers.ToDictionaryAsync(s => s.SupplierName, s => s.SupplierId);

        var assets = new[]
        {
            new Asset { AssetCode = "LP-001", AssetName = "Dell Latitude 5540",            AssetCategoryId = catMap["Laptop"],             BrandId = brandMap["Dell"],     SupplierId = supMap["Dell Vietnam"],    PurchaseDate = new DateTime(2024, 1, 15), PurchasePrice = 28_500_000m, Status = AssetStatus.Assigned },
            new Asset { AssetCode = "LP-002", AssetName = "Dell Latitude 5540",            AssetCategoryId = catMap["Laptop"],             BrandId = brandMap["Dell"],     SupplierId = supMap["Dell Vietnam"],    PurchaseDate = new DateTime(2024, 1, 15), PurchasePrice = 28_500_000m, Status = AssetStatus.Assigned },
            new Asset { AssetCode = "LP-003", AssetName = "HP ProBook 450 G10",            AssetCategoryId = catMap["Laptop"],             BrandId = brandMap["HP"],       SupplierId = supMap["HP Vietnam"],      PurchaseDate = new DateTime(2024, 3, 10), PurchasePrice = 22_000_000m, Status = AssetStatus.Assigned },
            new Asset { AssetCode = "LP-004", AssetName = "HP ProBook 450 G10",            AssetCategoryId = catMap["Laptop"],             BrandId = brandMap["HP"],       SupplierId = supMap["HP Vietnam"],      PurchaseDate = new DateTime(2024, 3, 10), PurchasePrice = 22_000_000m, Status = AssetStatus.Assigned },
            new Asset { AssetCode = "LP-005", AssetName = "MacBook Pro 14\" M3",           AssetCategoryId = catMap["Laptop"],             BrandId = brandMap["Apple"],    SupplierId = supMap["Apple Vietnam"],   PurchaseDate = new DateTime(2024, 6, 1),  PurchasePrice = 52_000_000m, Status = AssetStatus.Assigned },
            new Asset { AssetCode = "LP-006", AssetName = "Lenovo ThinkPad T14s",          AssetCategoryId = catMap["Laptop"],             BrandId = brandMap["Lenovo"],   SupplierId = supMap["Lenovo Vietnam"],  PurchaseDate = new DateTime(2024, 2, 20), PurchasePrice = 26_000_000m, Status = AssetStatus.Assigned },
            new Asset { AssetCode = "LP-007", AssetName = "MacBook Pro 16\" M3 Pro",       AssetCategoryId = catMap["Laptop"],             BrandId = brandMap["Apple"],    SupplierId = supMap["Apple Vietnam"],   PurchaseDate = new DateTime(2024, 6, 1),  PurchasePrice = 68_000_000m, Status = AssetStatus.Assigned },
            new Asset { AssetCode = "LP-008", AssetName = "Lenovo ThinkPad X1 Carbon",     AssetCategoryId = catMap["Laptop"],             BrandId = brandMap["Lenovo"],   SupplierId = supMap["Lenovo Vietnam"],  PurchaseDate = new DateTime(2024, 4, 10), PurchasePrice = 35_000_000m, Status = AssetStatus.Assigned },
            new Asset { AssetCode = "LP-009", AssetName = "Dell Latitude 5540",            AssetCategoryId = catMap["Laptop"],             BrandId = brandMap["Dell"],     SupplierId = supMap["Dell Vietnam"],    PurchaseDate = new DateTime(2024, 8, 1),  PurchasePrice = 28_500_000m, Status = AssetStatus.Available },
            new Asset { AssetCode = "LP-010", AssetName = "HP ProBook 450 G10",            AssetCategoryId = catMap["Laptop"],             BrandId = brandMap["HP"],       SupplierId = supMap["HP Vietnam"],      PurchaseDate = new DateTime(2024, 8, 1),  PurchasePrice = 22_000_000m, Status = AssetStatus.Available },
            new Asset { AssetCode = "MH-001", AssetName = "Dell UltraSharp U2723QE 27\"",  AssetCategoryId = catMap["Màn hình"],           BrandId = brandMap["Dell"],     SupplierId = supMap["Dell Vietnam"],    PurchaseDate = new DateTime(2024, 1, 20), PurchasePrice = 14_500_000m, Status = AssetStatus.Assigned },
            new Asset { AssetCode = "MH-002", AssetName = "Dell UltraSharp U2723QE 27\"",  AssetCategoryId = catMap["Màn hình"],           BrandId = brandMap["Dell"],     SupplierId = supMap["Dell Vietnam"],    PurchaseDate = new DateTime(2024, 1, 20), PurchasePrice = 14_500_000m, Status = AssetStatus.Assigned },
            new Asset { AssetCode = "MH-003", AssetName = "LG 27UK850-W 27\" 4K",          AssetCategoryId = catMap["Màn hình"],           BrandId = brandMap["LG"],       SupplierId = supMap["LG Vietnam"],      PurchaseDate = new DateTime(2024, 2, 15), PurchasePrice = 11_000_000m, Status = AssetStatus.Assigned },
            new Asset { AssetCode = "MH-004", AssetName = "Samsung Curved 27\" LC27F591",  AssetCategoryId = catMap["Màn hình"],           BrandId = brandMap["Samsung"],  SupplierId = supMap["Samsung Vietnam"], PurchaseDate = new DateTime(2024, 5, 10), PurchasePrice = 8_500_000m,  Status = AssetStatus.Available },
            new Asset { AssetCode = "DT-001", AssetName = "iPhone 15 Pro 256GB",           AssetCategoryId = catMap["Điện thoại"],         BrandId = brandMap["Apple"],    SupplierId = supMap["Apple Vietnam"],   PurchaseDate = new DateTime(2024, 4, 1),  PurchasePrice = 30_000_000m, Status = AssetStatus.Assigned },
            new Asset { AssetCode = "DT-002", AssetName = "Samsung Galaxy S24 Ultra",      AssetCategoryId = catMap["Điện thoại"],         BrandId = brandMap["Samsung"],  SupplierId = supMap["Samsung Vietnam"], PurchaseDate = new DateTime(2024, 4, 1),  PurchasePrice = 28_000_000m, Status = AssetStatus.Assigned },
            new Asset { AssetCode = "MI-001", AssetName = "HP LaserJet Pro M404dn",        AssetCategoryId = catMap["Máy in"],             BrandId = brandMap["HP"],       SupplierId = supMap["HP Vietnam"],      PurchaseDate = new DateTime(2023, 6, 15), PurchasePrice = 8_000_000m,  Status = AssetStatus.Assigned },
            new Asset { AssetCode = "MI-002", AssetName = "Canon LBP226dw",                AssetCategoryId = catMap["Máy in"],             BrandId = brandMap["Canon"],    SupplierId = supMap["Canon Vietnam"],   PurchaseDate = new DateTime(2023, 6, 15), PurchasePrice = 7_500_000m,  Status = AssetStatus.Assigned },
            new Asset { AssetCode = "NT-001", AssetName = "Bộ bàn ghế giám đốc Hòa Phát", AssetCategoryId = catMap["Nội thất văn phòng"], BrandId = brandMap["Hòa Phát"], SupplierId = supMap["Hòa Phát"],        PurchaseDate = new DateTime(2023, 1, 10), PurchasePrice = 15_000_000m, Status = AssetStatus.Assigned },
            new Asset { AssetCode = "NT-002", AssetName = "Ghế công thái học Sihoo M57",   AssetCategoryId = catMap["Nội thất văn phòng"], BrandId = brandMap["Sihoo"],    SupplierId = supMap["Ergonomic VN"],    PurchaseDate = new DateTime(2024, 3, 1),  PurchasePrice = 4_500_000m,  Status = AssetStatus.Available },
        };
        db.Assets.AddRange(assets);
        await db.SaveChangesAsync();

        var a = await db.Assets.ToDictionaryAsync(x => x.AssetCode, x => x.AssetId);

        // ── 7. Asset Configurations (laptops) ──
        db.AssetConfigurations.AddRange(new[]
        {
            new AssetConfiguration { AssetId = a["LP-001"], CPU = "Intel Core i7-1365U",     RAM = "16GB DDR5",    SSD = "512GB NVMe", VGA = "Intel Iris Xe",          OS = "Windows 11 Pro" },
            new AssetConfiguration { AssetId = a["LP-002"], CPU = "Intel Core i7-1365U",     RAM = "16GB DDR5",    SSD = "512GB NVMe", VGA = "Intel Iris Xe",          OS = "Windows 11 Pro" },
            new AssetConfiguration { AssetId = a["LP-003"], CPU = "Intel Core i5-1335U",     RAM = "8GB DDR4",     SSD = "256GB NVMe", VGA = "Intel Iris Xe",          OS = "Windows 11 Pro" },
            new AssetConfiguration { AssetId = a["LP-004"], CPU = "Intel Core i5-1335U",     RAM = "8GB DDR4",     SSD = "256GB NVMe", VGA = "Intel Iris Xe",          OS = "Windows 11 Pro" },
            new AssetConfiguration { AssetId = a["LP-005"], CPU = "Apple M3 8-Core",         RAM = "16GB Unified", SSD = "512GB",      VGA = "Apple M3 10-Core GPU",   OS = "macOS Sonoma" },
            new AssetConfiguration { AssetId = a["LP-006"], CPU = "AMD Ryzen 7 PRO 7840U",   RAM = "16GB DDR5",    SSD = "512GB NVMe", VGA = "AMD Radeon 780M",        OS = "Windows 11 Pro" },
            new AssetConfiguration { AssetId = a["LP-007"], CPU = "Apple M3 Pro 12-Core",    RAM = "36GB Unified", SSD = "1TB",        VGA = "Apple M3 Pro 18-Core GPU", OS = "macOS Sonoma" },
            new AssetConfiguration { AssetId = a["LP-008"], CPU = "Intel Core i7-1365U",     RAM = "16GB DDR5",    SSD = "512GB NVMe", VGA = "Intel Iris Xe",          OS = "Windows 11 Pro" },
            new AssetConfiguration { AssetId = a["LP-009"], CPU = "Intel Core i7-1365U",     RAM = "16GB DDR5",    SSD = "512GB NVMe", VGA = "Intel Iris Xe",          OS = "Windows 11 Pro" },
            new AssetConfiguration { AssetId = a["LP-010"], CPU = "Intel Core i5-1335U",     RAM = "8GB DDR4",     SSD = "256GB NVMe", VGA = "Intel Iris Xe",          OS = "Windows 11 Pro" },
        });
        await db.SaveChangesAsync();

        // ── 8. Asset Assignments ──
        db.AssetAssignments.AddRange(new[]
        {
            new AssetAssignment { AssetId = a["LP-007"], EmployeeId = empByCode["NV001"], AssignedDate = new DateTime(2024, 6, 5),  Status = AssetAssignmentStatus.Assigned, Condition = "Mới" },
            new AssetAssignment { AssetId = a["LP-005"], EmployeeId = empByCode["NV002"], AssignedDate = new DateTime(2024, 6, 5),  Status = AssetAssignmentStatus.Assigned, Condition = "Mới" },
            new AssetAssignment { AssetId = a["LP-001"], EmployeeId = empByCode["NV004"], AssignedDate = new DateTime(2024, 1, 20), Status = AssetAssignmentStatus.Assigned, Condition = "Mới" },
            new AssetAssignment { AssetId = a["LP-008"], EmployeeId = empByCode["NV009"], AssignedDate = new DateTime(2024, 4, 15), Status = AssetAssignmentStatus.Assigned, Condition = "Mới" },
            new AssetAssignment { AssetId = a["LP-002"], EmployeeId = empByCode["NV014"], AssignedDate = new DateTime(2024, 1, 20), Status = AssetAssignmentStatus.Assigned, Condition = "Mới" },
            new AssetAssignment { AssetId = a["LP-006"], EmployeeId = empByCode["NV020"], AssignedDate = new DateTime(2024, 2, 25), Status = AssetAssignmentStatus.Assigned, Condition = "Mới" },
            new AssetAssignment { AssetId = a["LP-003"], EmployeeId = empByCode["NV026"], AssignedDate = new DateTime(2024, 3, 15), Status = AssetAssignmentStatus.Assigned, Condition = "Mới" },
            new AssetAssignment { AssetId = a["LP-004"], EmployeeId = empByCode["NV021"], AssignedDate = new DateTime(2024, 3, 15), Status = AssetAssignmentStatus.Assigned, Condition = "Mới" },
            new AssetAssignment { AssetId = a["MH-001"], EmployeeId = empByCode["NV001"], AssignedDate = new DateTime(2024, 1, 25), Status = AssetAssignmentStatus.Assigned, Condition = "Mới" },
            new AssetAssignment { AssetId = a["MH-002"], EmployeeId = empByCode["NV020"], AssignedDate = new DateTime(2024, 2, 25), Status = AssetAssignmentStatus.Assigned, Condition = "Mới" },
            new AssetAssignment { AssetId = a["MH-003"], EmployeeId = empByCode["NV021"], AssignedDate = new DateTime(2024, 2, 20), Status = AssetAssignmentStatus.Assigned, Condition = "Mới" },
            new AssetAssignment { AssetId = a["DT-001"], EmployeeId = empByCode["NV001"], AssignedDate = new DateTime(2024, 4, 5),  Status = AssetAssignmentStatus.Assigned, Condition = "Mới" },
            new AssetAssignment { AssetId = a["DT-002"], EmployeeId = empByCode["NV002"], AssignedDate = new DateTime(2024, 4, 5),  Status = AssetAssignmentStatus.Assigned, Condition = "Mới" },
            new AssetAssignment { AssetId = a["MI-001"], EmployeeId = empByCode["NV004"], AssignedDate = new DateTime(2023, 7, 1),  Status = AssetAssignmentStatus.Assigned, Condition = "Tốt", Note = "Máy in chung tầng 2 - Phòng NS & Kế toán" },
            new AssetAssignment { AssetId = a["MI-002"], EmployeeId = empByCode["NV020"], AssignedDate = new DateTime(2023, 7, 1),  Status = AssetAssignmentStatus.Assigned, Condition = "Tốt", Note = "Máy in chung tầng 3 - Phòng KT & MKT" },
            new AssetAssignment { AssetId = a["NT-001"], EmployeeId = empByCode["NV001"], AssignedDate = new DateTime(2023, 1, 15), Status = AssetAssignmentStatus.Assigned, Condition = "Tốt" },
        });
        await db.SaveChangesAsync();

        // ── 9. Users for department managers (Manager role) ──
        var managerEmails = new[] { "hung.nv@company.com", "huong.pt@company.com", "nga.vt@company.com", "manh.dv@company.com", "thang.bv@company.com", "anh.nt@company.com" };
        var managerCodes  = new[] { "NV001", "NV004", "NV009", "NV014", "NV020", "NV026" };
        var managerNames  = new[] { "Nguyễn Văn Hùng", "Phạm Thị Hương", "Vũ Thị Nga", "Đỗ Văn Mạnh", "Bùi Văn Thắng", "Ngô Thị Ánh" };

        for (int i = 0; i < managerEmails.Length; i++)
        {
            db.Users.Add(new User
            {
                Email = managerEmails[i],
                FullName = managerNames[i],
                IsActive = true,
                EmployeeId = empByCode[managerCodes[i]],
            });
        }
        await db.SaveChangesAsync();

        // Assign roles: NV001 (CEO) → Admin, rest → Manager
        var seededUsers = await db.Users
            .Where(u => managerEmails.Contains(u.Email))
            .ToDictionaryAsync(u => u.Email, u => u.UserId);

        foreach (var kv in seededUsers)
        {
            int roleId = kv.Key == "hung.nv@company.com" ? 1 : 2; // Admin : Manager
            db.UserRoles.Add(new UserRole { UserId = kv.Value, RoleId = roleId });
        }
        await db.SaveChangesAsync();

        // ── 10. Attendance data (500+ records) ──
        await SeedAttendanceAsync(db, empByCode);
    }

    private static async Task SeedAttendanceAsync(ApplicationDbContext db, Dictionary<string, int> empByCode)
    {
        var rng = new Random(42); // fixed seed for reproducibility
        var lateThreshold = new TimeSpan(8, 30, 0);
        var attendances = new List<Attendance>();

        // Generate for Feb 2026 (full month) + March 1-11, 2026
        var startDate = new DateOnly(2026, 2, 1);
        var endDate = new DateOnly(2026, 3, 11);

        var empCodes = new[]
        {
            "NV001","NV002","NV003","NV004","NV005","NV006","NV007","NV008","NV009","NV010",
            "NV011","NV012","NV013","NV014","NV015","NV016","NV017","NV018","NV019","NV020",
            "NV021","NV022","NV023","NV024","NV025","NV026","NV027","NV028","NV029","NV030"
        };

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var isSunday = date.DayOfWeek == DayOfWeek.Sunday;
            if (isSunday) continue; // No work on Sunday

            var isSaturday = date.DayOfWeek == DayOfWeek.Saturday;

            foreach (var code in empCodes)
            {
                // ~5% chance of absent
                if (rng.NextDouble() < 0.05) continue;

                var empId = empByCode[code];

                // Check-in: mostly 8:00-8:29, ~15% late (8:31-9:15)
                int checkInMinute;
                bool isLate;
                if (rng.NextDouble() < 0.15)
                {
                    // Late
                    checkInMinute = 8 * 60 + 31 + rng.Next(0, 45); // 8:31 - 9:15
                    isLate = true;
                }
                else
                {
                    // On time
                    checkInMinute = 7 * 60 + 45 + rng.Next(0, 45); // 7:45 - 8:29
                    isLate = false;
                }

                var checkIn = new TimeOnly(checkInMinute / 60, checkInMinute % 60);

                // Check-out
                TimeOnly checkOut;
                if (isSaturday)
                {
                    // Saturday: leave 10:00 - 12:30
                    int coMin = 10 * 60 + rng.Next(0, 150);
                    checkOut = new TimeOnly(coMin / 60, coMin % 60);
                }
                else
                {
                    // Weekday: 17:30-19:30
                    int coMin = 17 * 60 + 30 + rng.Next(0, 120);
                    checkOut = new TimeOnly(coMin / 60, coMin % 60);
                }

                var wh = Math.Round((decimal)(checkOut.ToTimeSpan() - checkIn.ToTimeSpan()).TotalHours, 2);
                var ot = wh > 8 ? Math.Round(wh - 8, 2) : 0m;

                attendances.Add(new Attendance
                {
                    EmployeeId = empId,
                    Date = date,
                    CheckIn = checkIn,
                    CheckOut = checkOut,
                    WorkingHours = wh,
                    OvertimeHours = ot,
                    IsLate = isLate,
                });
            }
        }

        db.Attendances.AddRange(attendances);
        await db.SaveChangesAsync();
    }
}
