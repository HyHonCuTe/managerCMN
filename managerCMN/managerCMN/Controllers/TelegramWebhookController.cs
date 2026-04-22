using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using managerCMN.Data;

namespace managerCMN.Controllers;

[ApiController]
[Route("telegram")]
public class TelegramWebhookController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _config;
    private readonly ILogger<TelegramWebhookController> _logger;

    public TelegramWebhookController(
        ApplicationDbContext db,
        IMemoryCache cache,
        IConfiguration config,
        ILogger<TelegramWebhookController> logger)
    {
        _db = db;
        _cache = cache;
        _config = config;
        _logger = logger;
    }

    [HttpPost("webhook")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Webhook(
        [FromHeader(Name = "X-Telegram-Bot-Api-Secret-Token")] string? secretHeader,
        [FromBody] JsonElement update)
    {
        var expectedSecret = _config["Telegram:WebhookSecret"];
        if (!string.IsNullOrWhiteSpace(expectedSecret) && secretHeader != expectedSecret)
            return Unauthorized();

        try
        {
            if (!update.TryGetProperty("message", out var message))
                return Ok();

            if (!message.TryGetProperty("chat", out var chat)) return Ok();
            var chatId = chat.GetProperty("id").GetInt64().ToString();

            if (!message.TryGetProperty("text", out var textEl)) return Ok();
            var text = textEl.GetString()?.Trim() ?? "";

            string? token = null;
            if (text.StartsWith("/start ", StringComparison.OrdinalIgnoreCase))
                token = text[7..].Trim();
            else if (text.StartsWith("/link ", StringComparison.OrdinalIgnoreCase))
                token = text[6..].Trim();
            else if (text == "/start")
            {
                await SendTelegramReplyAsync(chatId,
                    "Chào mừng bạn đến với bot thông báo! 👋\n\nĐể kết nối tài khoản, vui lòng vào trang web và làm theo hướng dẫn tại mục <b>Hồ sơ → Kết nối Telegram</b>.");
                return Ok();
            }
            else if (text == "/status")
            {
                var linked = await _db.Users.AnyAsync(u => u.TelegramChatId == chatId);
                await SendTelegramReplyAsync(chatId, linked
                    ? "✅ Tài khoản của bạn đã được kết nối với hệ thống."
                    : "❌ Chưa kết nối tài khoản nào với Chat ID này.");
                return Ok();
            }

            if (token == null) return Ok();

            var cacheKey = $"tg_link:{token}";
            if (!_cache.TryGetValue(cacheKey, out int userId))
            {
                await SendTelegramReplyAsync(chatId,
                    "❌ Mã kết nối không hợp lệ hoặc đã hết hạn.\n\nVui lòng tạo mã mới tại trang <b>Hồ sơ → Kết nối Telegram</b>.");
                return Ok();
            }

            var user = await _db.Users.FindAsync(userId);
            if (user == null)
            {
                await SendTelegramReplyAsync(chatId, "❌ Không tìm thấy tài khoản.");
                return Ok();
            }

            user.TelegramChatId = chatId;
            await _db.SaveChangesAsync();
            _cache.Remove(cacheKey);

            _logger.LogInformation("Linked Telegram chatId {ChatId} to userId {UserId}", chatId, userId);

            await SendTelegramReplyAsync(chatId,
                $"✅ <b>Kết nối thành công!</b>\n\nTài khoản <b>{user.FullName}</b> đã được liên kết với Telegram.\nTừ giờ bạn sẽ nhận thông báo qua đây.");

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Telegram webhook error");
            return Ok();
        }
    }

    private async Task SendTelegramReplyAsync(string chatId, string text)
    {
        var botToken = _config["Telegram:BotToken"];
        if (string.IsNullOrWhiteSpace(botToken)) return;

        using var http = new HttpClient();
        var url = $"https://api.telegram.org/bot{botToken}/sendMessage";
        await http.PostAsJsonAsync(url, new { chat_id = chatId, text, parse_mode = "HTML" });
    }
}
