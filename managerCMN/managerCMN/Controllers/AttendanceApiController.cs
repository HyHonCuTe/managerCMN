using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using managerCMN.Services.Interfaces;
using managerCMN.Attributes;

namespace managerCMN.Controllers;

[ApiController]
[Route("api/attendance")]
[ApiKeyAuthentication]
public class AttendanceApiController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;

    public AttendanceApiController(IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    /// <summary>
    /// POST /api/attendance/punch
    /// Body: array of { UserId, Time }
    /// UserId = mã chấm công (AttendanceCode) of the employee
    /// </summary>
    [HttpPost("punch")]
    public async Task<IActionResult> PostPunchRecords([FromBody] PunchRequest[] records)
    {
        if (records == null || records.Length == 0)
            return BadRequest(new { error = "Dữ liệu trống." });

        var punchRecords = new List<(string AttendanceCode, DateTime PunchTime)>();

        foreach (var r in records)
        {
            if (string.IsNullOrWhiteSpace(r.UserId))
                continue;

            punchRecords.Add((r.UserId.Trim(), r.Time));
        }

        if (punchRecords.Count == 0)
            return BadRequest(new { error = "Không có bản ghi hợp lệ." });

        await _attendanceService.ProcessPunchRecordsAsync(punchRecords);

        return Ok(new { message = "Import thành công.", count = punchRecords.Count });
    }
}

public class PunchRequest
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public DateTime Time { get; set; }
}
