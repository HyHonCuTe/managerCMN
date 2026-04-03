using System.Globalization;
using managerCMN.Models.Entities;
using managerCMN.Models.Enums;
using managerCMN.Models.ViewModels;

namespace managerCMN.Helpers;

public static class BirthdayCelebrationHelper
{
    private const int CountdownWindowDays = 5;
    private static readonly CultureInfo VietnameseCulture = CultureInfo.GetCultureInfo("vi-VN");

    public static BirthdayCelebrationViewModel? Build(Employee employee, DateTime? referenceDate = null)
    {
        if (!employee.DateOfBirth.HasValue)
            return null;

        var today = DateOnly.FromDateTime((referenceDate ?? DateTimeHelper.VietnamToday).Date);
        var nextBirthday = GetNextBirthdayDate(employee.DateOfBirth.Value, today);

        var daysUntilBirthday = nextBirthday.DayNumber - today.DayNumber;
        if (daysUntilBirthday < 0 || daysUntilBirthday > CountdownWindowDays)
            return null;

        var upcomingAge = Math.Max(1, nextBirthday.Year - employee.DateOfBirth.Value.Year);
        var isBirthdayToday = daysUntilBirthday == 0;
        var addressPronoun = GetAddressPronoun(employee.Gender);

        return new BirthdayCelebrationViewModel
        {
            FullName = employee.FullName,
            BirthdayDateLabel = nextBirthday.ToString("dd/MM"),
            CountdownTargetIso = $"{nextBirthday:yyyy-MM-dd}T00:00:00",
            DaysUntilBirthday = daysUntilBirthday,
            UpcomingAge = upcomingAge,
            IsBirthdayToday = isBirthdayToday,
            Title = isBirthdayToday
                ? $"Chúc mừng sinh nhật {employee.FullName}!"
                : $"Còn {daysUntilBirthday} ngày nữa là tới sinh nhật của bạn",
            Subtitle = isBirthdayToday
                ? $"Hôm nay là ngày đặc biệt của tuổi {upcomingAge}."
                : $"Ngày sinh nhật năm nay của bạn là {nextBirthday.ToString("dddd, dd/MM/yyyy", VietnameseCulture)}.",
            Message = isBirthdayToday
                ? $"Chúc {addressPronoun} có một ngày thật rực rỡ, nhiều niềm vui, thêm thật nhiều năng lượng tích cực và những điều may mắn đẹp nhất cùng đại gia đình CMN."
                : "Một bí mật dành riêng cho bạn khi đăng nhập đúng ngày: ngày đặc biệt đang đến gần. Chúc bạn sớm có một sinh nhật thật ấm áp, nhiều tiếng cười và thật nhiều điều mong muốn thành hiện thực.",
            CountdownLabel = isBirthdayToday
                ? "Hôm nay là ngày của bạn"
                : "Đếm ngược tới sinh nhật",
            AddressPronoun = addressPronoun,
            TodaySpecialWish = $"Em Võ Đào Huy Hoàng chúc {addressPronoun} {employee.FullName} sinh nhật tròn tuổi mới thật nhiều niềm vui, thật nhiều may mắn và thật nhiều năng lượng tích cực."
        };
    }

    public static DateOnly GetNextBirthdayDate(DateTime birthday, DateOnly referenceDate)
    {
        var nextBirthday = ResolveBirthdayDate(birthday, referenceDate.Year);

        if (nextBirthday < referenceDate)
            nextBirthday = ResolveBirthdayDate(birthday, referenceDate.Year + 1);

        return nextBirthday;
    }

    private static string GetAddressPronoun(Gender gender) => gender switch
    {
        Gender.Male => "anh",
        Gender.Female => "chị",
        _ => "bạn"
    };

    private static DateOnly ResolveBirthdayDate(DateTime birthday, int year)
    {
        var month = birthday.Month;
        var day = birthday.Day;

        if (month == 2 && day == 29 && !DateTime.IsLeapYear(year))
            day = 28;

        return new DateOnly(year, month, day);
    }
}
