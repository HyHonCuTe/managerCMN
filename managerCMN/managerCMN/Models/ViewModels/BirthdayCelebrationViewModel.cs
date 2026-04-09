using System.Globalization;
using System.Text.Json.Serialization;
using managerCMN.Helpers;

namespace managerCMN.Models.ViewModels;

public class BirthdayCelebrationViewModel
{
    private static readonly CultureInfo VietnameseCulture = CultureInfo.GetCultureInfo("vi-VN");
    private const string DefaultAddressPronoun = "\u0062\u1ea1n";

    public string FullName { get; set; } = string.Empty;
    public string BirthdayDateIso { get; set; } = string.Empty;
    public int DaysUntilBirthday { get; set; }
    public int UpcomingAge { get; set; }
    public bool IsBirthdayToday { get; set; }

    [JsonIgnore]
    public string BirthdayDateLabel => ResolveBirthdayDate().ToString("dd/MM");

    [JsonIgnore]
    public string CountdownTargetIso => $"{ResolveBirthdayDate():yyyy-MM-dd}T00:00:00";

    [JsonIgnore]
    public string Title => IsBirthdayToday
        ? $"\u0043h\u00fac m\u1eebng sinh nh\u1eadt {FullName}!"
        : $"\u0043\u00f2n {DaysUntilBirthday} ng\u00e0y n\u1eefa l\u00e0 t\u1edbi sinh nh\u1eadt c\u1ee7a b\u1ea1n";

    [JsonIgnore]
    public string Subtitle => IsBirthdayToday
        ? $"\u0048\u00f4m nay l\u00e0 ng\u00e0y \u0111\u1eb7c bi\u1ec7t c\u1ee7a tu\u1ed5i {UpcomingAge}."
        : $"\u004e\u0067\u00e0y sinh nh\u1eadt n\u0103m nay c\u1ee7a b\u1ea1n l\u00e0 {ResolveBirthdayDate().ToString("dddd, dd/MM/yyyy", VietnameseCulture)}.";

    [JsonIgnore]
    public string Message => IsBirthdayToday
        ? $"\u0043h\u00fac {AddressPronounOrDefault()} c\u00f3 m\u1ed9t ng\u00e0y th\u1eadt r\u1ef1c r\u1ee1, nhi\u1ec1u ni\u1ec1m vui, th\u00eam th\u1eadt nhi\u1ec1u n\u0103ng l\u01b0\u1ee3ng t\u00edch c\u1ef1c v\u00e0 nh\u1eefng \u0111i\u1ec1u may m\u1eafn \u0111\u1eb9p nh\u1ea5t c\u00f9ng \u0111\u1ea1i gia \u0111\u00ecnh CMN."
        : "\u004d\u1ed9t l\u1eddi nh\u1eafc b\u00ed m\u1eadt d\u00e0nh ri\u00eang cho b\u1ea1n: ng\u00e0y \u0111\u1eb7c bi\u1ec7t \u0111ang \u0111\u1ebfn g\u1ea7n. Ch\u00fac b\u1ea1n s\u1edbm c\u00f3 m\u1ed9t sinh nh\u1eadt th\u1eadt \u1ea5m \u00e1p, nhi\u1ec1u ti\u1ebfng c\u01b0\u1eddi v\u00e0 th\u1eadt nhi\u1ec1u \u0111i\u1ec1u mong mu\u1ed1n th\u00e0nh hi\u1ec7n th\u1ef1c.";

    [JsonIgnore]
    public string CountdownLabel => IsBirthdayToday
        ? "\u0048\u00f4m nay l\u00e0 ng\u00e0y c\u1ee7a b\u1ea1n"
        : "\u0110\u1ebfm ng\u01b0\u1ee3c t\u1edbi sinh nh\u1eadt";
    public string AddressPronoun { get; set; } = DefaultAddressPronoun;
    [JsonIgnore]
    public string TodaySpecialWish
        => $"\u0045m V\u00f5 \u0110\u00e0o Huy Ho\u00e0ng ch\u00fac {AddressPronounOrDefault()} {FullName} sinh nh\u1eadt tr\u00f2n tu\u1ed5i m\u1edbi th\u1eadt nhi\u1ec1u ni\u1ec1m vui, th\u1eadt nhi\u1ec1u may m\u1eafn v\u00e0 th\u1eadt nhi\u1ec1u n\u0103ng l\u01b0\u1ee3ng t\u00edch c\u1ef1c.";

    private string AddressPronounOrDefault()
        => string.IsNullOrWhiteSpace(AddressPronoun) ? DefaultAddressPronoun : AddressPronoun;

    private DateOnly ResolveBirthdayDate()
    {
        if (DateOnly.TryParseExact(BirthdayDateIso, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var birthdayDate))
            return birthdayDate;

        if (DateOnly.TryParse(BirthdayDateIso, out birthdayDate))
            return birthdayDate;

        return DateOnly.FromDateTime(DateTimeHelper.VietnamToday);
    }
}
