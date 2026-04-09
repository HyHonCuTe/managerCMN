using System.Globalization;
using managerCMN.Models.Entities;
using managerCMN.Models.Enums;
using managerCMN.Models.ViewModels;

namespace managerCMN.Helpers;

public static class BirthdayCelebrationHelper
{
    private const int CountdownWindowDays = 5;

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
            BirthdayDateIso = nextBirthday.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            DaysUntilBirthday = daysUntilBirthday,
            UpcomingAge = upcomingAge,
            IsBirthdayToday = isBirthdayToday,
            AddressPronoun = addressPronoun
        };
    }

    public static DateOnly GetNextBirthdayDate(DateTime birthday, DateOnly referenceDate)
    {
        var nextBirthday = ResolveBirthdayDate(birthday, referenceDate.Year);

        if (nextBirthday < referenceDate)
            nextBirthday = ResolveBirthdayDate(birthday, referenceDate.Year + 1);

        return nextBirthday;
    }

    private static string GetAddressPronoun(Gender gender)
    {
        if (gender == Gender.Male)
            return "\u0061nh";

        if (gender == Gender.Female)
            return "\u0063h\u1ecb";

        return "\u0062\u1ea1n";
    }
    private static DateOnly ResolveBirthdayDate(DateTime birthday, int year)
    {
        var month = birthday.Month;
        var day = birthday.Day;

        if (month == 2 && day == 29 && !DateTime.IsLeapYear(year))
            day = 28;

        return new DateOnly(year, month, day);
    }
}
