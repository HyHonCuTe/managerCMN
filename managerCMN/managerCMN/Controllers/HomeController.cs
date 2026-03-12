using Microsoft.AspNetCore.Mvc;

namespace managerCMN.Controllers;

public class HomeController : Controller
{
    public IActionResult Error()
    {
        return View();
    }
}
