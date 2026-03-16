using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using managerCMN.Models.Enums;

namespace managerCMN.Controllers;

[Authorize]
public class LeaveController : Controller
{
    public IActionResult Index() => RedirectToAction("Index", "Request");

    public IActionResult Create() => RedirectToAction("Create", "Request", new { type = RequestType.Leave });

    public IActionResult Pending() => RedirectToAction("Approve", "Request");
}
