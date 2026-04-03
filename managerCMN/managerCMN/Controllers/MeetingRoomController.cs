using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using managerCMN.Helpers;
using managerCMN.Models.Entities;
using managerCMN.Models.ViewModels;
using managerCMN.Services.Interfaces;

namespace managerCMN.Controllers;

[Authorize(Policy = "Authenticated")]
public class MeetingRoomController : Controller
{
    private readonly IMeetingRoomService _meetingRoomService;

    public MeetingRoomController(IMeetingRoomService meetingRoomService)
    {
        _meetingRoomService = meetingRoomService;
    }

    public async Task<IActionResult> Index(DateTime? date)
    {
        var selectedDate = (date ?? DateTimeHelper.VietnamToday).Date;
        var model = await BuildViewModelAsync(selectedDate);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateBooking([Bind(Prefix = "NewBooking")] MeetingRoomBookingCreateViewModel model)
    {
        var selectedDate = (model.BookingDate == default ? DateTimeHelper.VietnamToday : model.BookingDate).Date;
        var employeeId = GetCurrentEmployeeId();

        if (employeeId <= 0)
        {
            TempData["Error"] = "Không xác định được hồ sơ nhân viên hiện tại.";
            return RedirectToAction(nameof(Index), new { date = selectedDate.ToString("yyyy-MM-dd") });
        }

        var startTime = CombineDateAndTime(selectedDate, model.StartClock, "NewBooking.StartClock", "Vui lòng chọn giờ bắt đầu hợp lệ.");
        var endTime = CombineDateAndTime(selectedDate, model.EndClock, "NewBooking.EndClock", "Vui lòng chọn giờ kết thúc hợp lệ.");

        if (!ModelState.IsValid || startTime is null || endTime is null)
        {
            var invalidModel = await BuildViewModelAsync(selectedDate, bookingForm: model);
            return View("Index", invalidModel);
        }

        try
        {
            await _meetingRoomService.CreateBookingAsync(new MeetingRoomBooking
            {
                MeetingRoomId = model.MeetingRoomId,
                EmployeeId = employeeId,
                Title = model.Title,
                Description = model.Description,
                StartTime = startTime.Value,
                EndTime = endTime.Value
            });

            TempData["Success"] = "Đặt phòng họp thành công.";
            return RedirectToAction(nameof(Index), new { date = selectedDate.ToString("yyyy-MM-dd") });
        }
        catch (ValidationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var invalidModel = await BuildViewModelAsync(selectedDate, bookingForm: model);
            return View("Index", invalidModel);
        }
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRoom([Bind(Prefix = "NewRoom")] MeetingRoomCreateViewModel model, DateTime selectedDate)
    {
        selectedDate = (selectedDate == default ? DateTimeHelper.VietnamToday : selectedDate).Date;

        if (!ModelState.IsValid)
        {
            var invalidModel = await BuildViewModelAsync(selectedDate, roomForm: model);
            return View("Index", invalidModel);
        }

        try
        {
            await _meetingRoomService.CreateRoomAsync(new MeetingRoom
            {
                Name = model.Name,
                Location = model.Location,
                Capacity = model.Capacity,
                ColorHex = model.ColorHex,
                IsActive = true
            });

            TempData["Success"] = "Đã thêm phòng họp mới.";
            return RedirectToAction(nameof(Index), new { date = selectedDate.ToString("yyyy-MM-dd") });
        }
        catch (ValidationException ex)
        {
            ModelState.AddModelError("NewRoom.Name", ex.Message);
            var invalidModel = await BuildViewModelAsync(selectedDate, roomForm: model);
            return View("Index", invalidModel);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelBooking(int id, DateTime selectedDate)
    {
        selectedDate = (selectedDate == default ? DateTimeHelper.VietnamToday : selectedDate).Date;
        var employeeId = GetCurrentEmployeeId();
        var isAdmin = User.IsInRole("Admin");

        if (employeeId <= 0 && !isAdmin)
        {
            TempData["Error"] = "Không xác định được hồ sơ nhân viên hiện tại.";
            return RedirectToAction(nameof(Index), new { date = selectedDate.ToString("yyyy-MM-dd") });
        }

        try
        {
            await _meetingRoomService.CancelBookingAsync(id, employeeId, isAdmin);
            TempData["Success"] = "Đã hủy lịch họp.";
        }
        catch (ValidationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { date = selectedDate.ToString("yyyy-MM-dd") });
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleRoomStatus(int id, bool isActive, DateTime selectedDate)
    {
        selectedDate = (selectedDate == default ? DateTimeHelper.VietnamToday : selectedDate).Date;

        try
        {
            await _meetingRoomService.UpdateRoomStatusAsync(id, isActive);
            TempData["Success"] = isActive
                ? "Đã kích hoạt lại phòng họp."
                : "Đã ngưng sử dụng phòng họp.";
        }
        catch (ValidationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { date = selectedDate.ToString("yyyy-MM-dd") });
    }

    private async Task<MeetingRoomScheduleViewModel> BuildViewModelAsync(
        DateTime selectedDate,
        MeetingRoomBookingCreateViewModel? bookingForm = null,
        MeetingRoomCreateViewModel? roomForm = null)
    {
        var isAdmin = User.IsInRole("Admin");
        var employeeId = GetCurrentEmployeeId();
        var activeRooms = await _meetingRoomService.GetActiveRoomsAsync();
        var allRooms = isAdmin
            ? await _meetingRoomService.GetAllRoomsAsync()
            : activeRooms;
        var bookings = await _meetingRoomService.GetBookingsByDateAsync(selectedDate);
        var upcoming = await _meetingRoomService.GetUpcomingBookingsAsync(employeeId, isAdmin);

        var bookingLookup = bookings
            .GroupBy(b => b.MeetingRoomId)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.StartTime).ToList());

        var model = new MeetingRoomScheduleViewModel
        {
            SelectedDate = selectedDate.Date,
            IsAdmin = isAdmin,
            AllRooms = allRooms.ToList(),
            UpcomingBookings = upcoming
                .Select(b => new MeetingRoomBookingSummaryViewModel
                {
                    Booking = b,
                    CanCancel = isAdmin || b.EmployeeId == employeeId
                })
                .ToList(),
            NewBooking = bookingForm ?? CreateDefaultBookingForm(selectedDate, activeRooms.FirstOrDefault()?.MeetingRoomId),
            NewRoom = roomForm ?? new MeetingRoomCreateViewModel()
        };

        foreach (var room in activeRooms)
        {
            model.Rooms.Add(new MeetingRoomColumnViewModel
            {
                Room = room,
                Bookings = bookingLookup.TryGetValue(room.MeetingRoomId, out var roomBookings)
                    ? roomBookings
                    : []
            });
        }

        model.NewBooking.BookingDate = selectedDate.Date;
        if (model.NewBooking.MeetingRoomId <= 0 && activeRooms.Count > 0)
        {
            model.NewBooking.MeetingRoomId = activeRooms[0].MeetingRoomId;
        }

        return model;
    }

    private static MeetingRoomBookingCreateViewModel CreateDefaultBookingForm(DateTime selectedDate, int? roomId)
    {
        var startClock = new TimeSpan(9, 0, 0);
        if (selectedDate.Date == DateTimeHelper.VietnamToday)
        {
            startClock = RoundUpToFiveMinutes(DateTimeHelper.VietnamNow.AddMinutes(30).TimeOfDay);
        }

        var endClock = startClock.Add(TimeSpan.FromHours(1));
        if (endClock > new TimeSpan(23, 59, 0))
        {
            endClock = new TimeSpan(23, 59, 0);
        }

        return new MeetingRoomBookingCreateViewModel
        {
            MeetingRoomId = roomId ?? 0,
            BookingDate = selectedDate.Date,
            StartClock = startClock.ToString(@"hh\:mm"),
            EndClock = endClock.ToString(@"hh\:mm")
        };
    }

    private static TimeSpan RoundUpToFiveMinutes(TimeSpan time)
    {
        var totalMinutes = (int)Math.Ceiling(time.TotalMinutes / 5d) * 5;
        if (totalMinutes >= (24 * 60))
        {
            return new TimeSpan(23, 59, 0);
        }

        return TimeSpan.FromMinutes(totalMinutes);
    }

    private DateTime? CombineDateAndTime(DateTime date, string? clock, string modelKey, string invalidMessage)
    {
        if (string.IsNullOrWhiteSpace(clock))
        {
            ModelState.AddModelError(modelKey, invalidMessage);
            return null;
        }

        if (!TimeSpan.TryParse(clock, out var time))
        {
            ModelState.AddModelError(modelKey, invalidMessage);
            return null;
        }

        return date.Date.Add(time);
    }

    private int GetCurrentEmployeeId()
    {
        var claim = User.FindFirst("EmployeeId")?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }
}
