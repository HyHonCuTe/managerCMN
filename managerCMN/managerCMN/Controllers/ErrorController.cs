using Microsoft.AspNetCore.Mvc;

namespace managerCMN.Controllers;

public class ErrorController : Controller
{
    [Route("Error/{statusCode}")]
    public IActionResult HttpStatusCode(int statusCode)
    {
        ViewBag.StatusCode = statusCode;
        ViewBag.ErrorInfo = GetErrorInfo(statusCode);
        return View("StatusCode");
    }

    [Route("Error")]
    public IActionResult Index()
    {
        ViewBag.StatusCode = 500;
        ViewBag.ErrorInfo = GetErrorInfo(500);
        return View("StatusCode");
    }

    private static (string Title, string Message, string Icon, string Color) GetErrorInfo(int statusCode)
    {
        return statusCode switch
        {
            400 => ("Bad Request", "Yêu cầu không hợp lệ. Vui lòng kiểm tra lại thông tin.", "bi-x-circle", "warning"),
            401 => ("Unauthorized", "Bạn cần đăng nhập để truy cập trang này.", "bi-lock", "warning"),
            403 => ("Forbidden", "Bạn không có quyền truy cập tài nguyên này.", "bi-shield-exclamation", "danger"),
            404 => ("Not Found", "Trang bạn tìm kiếm không tồn tại hoặc đã bị di chuyển.", "bi-search", "info"),
            408 => ("Request Timeout", "Yêu cầu đã hết thời gian chờ. Vui lòng thử lại.", "bi-hourglass-split", "warning"),
            429 => ("Too Many Requests", "Bạn đã gửi quá nhiều yêu cầu. Vui lòng đợi một lát.", "bi-speedometer2", "warning"),
            500 => ("Internal Server Error", "Đã xảy ra lỗi máy chủ. Chúng tôi đang khắc phục.", "bi-bug", "danger"),
            502 => ("Bad Gateway", "Máy chủ đang gặp sự cố kết nối. Vui lòng thử lại sau.", "bi-hdd-network", "danger"),
            503 => ("Service Unavailable", "Dịch vụ tạm thời không khả dụng. Vui lòng thử lại sau.", "bi-tools", "warning"),
            504 => ("Gateway Timeout", "Máy chủ phản hồi quá chậm. Vui lòng thử lại sau.", "bi-clock-history", "danger"),
            _ => ("Error", "Đã xảy ra lỗi không xác định.", "bi-exclamation-triangle", "secondary")
        };
    }
}
