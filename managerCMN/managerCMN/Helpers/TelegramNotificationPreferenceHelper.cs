using managerCMN.Models.Entities;
using managerCMN.Models.Enums;

namespace managerCMN.Helpers;

public sealed class TelegramNotificationOption
{
    public TelegramNotificationCategory Category { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsMandatory { get; init; }
}

public static class TelegramNotificationPreferenceHelper
{
    private static readonly TelegramNotificationCategory[] MandatoryCategories =
    [
        TelegramNotificationCategory.Task,
        TelegramNotificationCategory.Ticket
    ];

    private static readonly TelegramNotificationCategory[] LegacyBroadcastCategories =
    [
        TelegramNotificationCategory.Announcement,
        TelegramNotificationCategory.SystemLifecycle,
        TelegramNotificationCategory.EmployeeProfileUpdate
    ];

    private static readonly TelegramNotificationOption[] AllOptions =
    [
        new()
        {
            Category = TelegramNotificationCategory.Task,
            Title = "Task",
            Description = "Giao task, cập nhật task, task hoàn thành và nhắc deadline task.",
            IsMandatory = true
        },
        new()
        {
            Category = TelegramNotificationCategory.Ticket,
            Title = "Ticket",
            Description = "Ticket mới, phản hồi, chuyển tiếp, cập nhật trạng thái và nhắc deadline ticket.",
            IsMandatory = true
        },
        new()
        {
            Category = TelegramNotificationCategory.Request,
            Title = "Đơn từ",
            Description = "Đơn mới cần duyệt, duyệt/từ chối đơn và hoàn duyệt.",
            IsMandatory = false
        },
        new()
        {
            Category = TelegramNotificationCategory.LeaveGrant,
            Title = "Cộng phép",
            Description = "Thông báo hệ thống cộng thêm phép cho tài khoản của bạn.",
            IsMandatory = false
        },
        new()
        {
            Category = TelegramNotificationCategory.Announcement,
            Title = "Thông báo nội bộ",
            Description = "Thông báo nội bộ hoặc thông báo đại trà được lên lịch gửi qua Telegram.",
            IsMandatory = false
        },
        new()
        {
            Category = TelegramNotificationCategory.SystemLifecycle,
            Title = "Trạng thái hệ thống",
            Description = "Thông báo hệ thống đang cập nhật hoặc đã sẵn sàng trở lại.",
            IsMandatory = false
        },
        new()
        {
            Category = TelegramNotificationCategory.EmployeeProfileUpdate,
            Title = "Cập nhật hồ sơ nhân viên",
            Description = "Thông báo khi nhân viên chỉnh sửa thông tin hồ sơ cá nhân.",
            IsMandatory = false
        }
    ];

    public static IReadOnlyList<TelegramNotificationOption> GetOptions(bool includeEmployeeProfileUpdates)
        => includeEmployeeProfileUpdates
            ? AllOptions
            : AllOptions.Where(x => x.Category != TelegramNotificationCategory.EmployeeProfileUpdate).ToArray();

    public static bool IsMandatory(TelegramNotificationCategory category)
        => MandatoryCategories.Contains(category);

    public static bool IsEnabled(User user, TelegramNotificationCategory category)
    {
        if (category == TelegramNotificationCategory.General)
        {
            return true;
        }

        if (IsMandatory(category))
        {
            return true;
        }

        var disabledCategories = ParseDisabledCategories(user.TelegramDisabledNotificationTypes);
        if (disabledCategories.Contains(category))
        {
            return false;
        }

        return !user.TelegramMuteBroadcast || !LegacyBroadcastCategories.Contains(category);
    }

    public static HashSet<TelegramNotificationCategory> ParseDisabledCategories(string? rawValue)
    {
        var results = new HashSet<TelegramNotificationCategory>();
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return results;
        }

        foreach (var token in rawValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!Enum.TryParse<TelegramNotificationCategory>(token, true, out var category))
            {
                continue;
            }

            if (category == TelegramNotificationCategory.General || IsMandatory(category))
            {
                continue;
            }

            results.Add(category);
        }

        return results;
    }

    public static string? SerializeDisabledCategories(IEnumerable<TelegramNotificationCategory> categories)
    {
        var items = categories
            .Where(category => category != TelegramNotificationCategory.General && !IsMandatory(category))
            .Distinct()
            .OrderBy(category => (int)category)
            .Select(category => category.ToString())
            .ToArray();

        return items.Length == 0 ? null : string.Join(',', items);
    }
}
