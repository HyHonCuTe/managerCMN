using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using managerCMN.Services.Interfaces;
using managerCMN.Attributes;
using managerCMN.Helpers;

namespace managerCMN.Controllers;

[ApiController]
[Route("api/attendance")]
[ApiKeyAuthentication]
[IgnoreAntiforgeryToken]
public class AttendanceApiController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;
    private readonly IPostHistoryService _postHistoryService;
    private readonly IEmployeeService _employeeService;

    public AttendanceApiController(
        IAttendanceService attendanceService,
        IPostHistoryService postHistoryService,
        IEmployeeService employeeService)
    {
        _attendanceService = attendanceService;
        _postHistoryService = postHistoryService;
        _employeeService = employeeService;
    }

    /// <summary>
    /// POST /api/attendance/punch
    /// Body: array of { UserId, Time }
    /// UserId = mã chấm công (AttendanceCode) of the employee
    /// </summary>
    [HttpPost("punch")]
    public async Task<IActionResult> PostPunchRecords([FromBody] PunchRequest[] records)
    {
        string? ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        string? userAgent = HttpContext.Request.Headers["User-Agent"].FirstOrDefault();
        int originalCount = records?.Length ?? 0;

        try
        {
            if (records == null || records.Length == 0)
            {
                await _postHistoryService.LogApiPostAsync(
                    recordsCount: 0,
                    processedCount: 0,
                    ipAddress: ipAddress,
                    userAgent: userAgent,
                    isSuccess: false,
                    errorMessage: "Dữ liệu trống."
                );
                return BadRequest(new { error = "Dữ liệu trống." });
            }

            var punchRecords = new List<(string AttendanceCode, DateTime PunchTime)>();
            var attendanceCodes = new HashSet<string>();

            foreach (var r in records)
            {
                if (string.IsNullOrWhiteSpace(r.UserId))
                    continue;

                // Convert to Vietnam time and ensure it's stored without timezone conversion
                var vietnamTime = r.Time.ToVietnamUnspecified();
                var code = r.UserId.Trim();

                punchRecords.Add((code, vietnamTime));
                attendanceCodes.Add(code);
            }

            if (punchRecords.Count == 0)
            {
                await _postHistoryService.LogApiPostAsync(
                    recordsCount: originalCount,
                    processedCount: 0,
                    ipAddress: ipAddress,
                    userAgent: userAgent,
                    isSuccess: false,
                    errorMessage: "Không có bản ghi hợp lệ."
                );
                return BadRequest(new { error = "Không có bản ghi hợp lệ." });
            }

            // Resolve employee names for logging
            var employeeInfoList = new List<string>();
            var allEmployees = await _employeeService.GetAllAsync();
            var employeeByCode = allEmployees
                .Where(e => e.AttendanceCode != null)
                .ToDictionary(e => e.AttendanceCode!, e => e);

            foreach (var code in attendanceCodes)
            {
                if (employeeByCode.TryGetValue(code, out var emp))
                {
                    employeeInfoList.Add($"{emp.FullName} (#{code})");
                }
                else
                {
                    employeeInfoList.Add($"Unknown (#{code})");
                }
            }
            var employeeInfo = string.Join(", ", employeeInfoList);

            // Get earliest and latest punch times for logging
            var earliestPunch = punchRecords.Min(pr => pr.PunchTime);
            var latestPunch = punchRecords.Max(pr => pr.PunchTime);

            // Process the punch records
            await _attendanceService.ProcessPunchRecordsAsync(punchRecords);

            // Log successful API call
            await _postHistoryService.LogApiPostAsync(
                recordsCount: originalCount,
                processedCount: punchRecords.Count,
                ipAddress: ipAddress,
                userAgent: userAgent,
                isSuccess: true,
                earliestPunchTime: earliestPunch,
                latestPunchTime: latestPunch,
                employeeInfo: employeeInfo
            );

            return Ok(new { message = "Import thành công.", count = punchRecords.Count });
        }
        catch (Exception ex)
        {
            // Log failed API call
            await _postHistoryService.LogApiPostAsync(
                recordsCount: originalCount,
                processedCount: 0,
                ipAddress: ipAddress,
                userAgent: userAgent,
                isSuccess: false,
                errorMessage: ex.Message
            );

            return StatusCode(500, new { error = "Lỗi server khi xử lý dữ liệu.", details = ex.Message });
        }
    }
}

public class PunchRequest
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public DateTime Time { get; set; }
}
