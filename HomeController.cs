using System.Security.Claims;
using AcxiomCRM.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace AcxiomCRM.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;
    public HomeController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var q = _db.Customers.AsQueryable();
        if (User.IsInRole("SalesExecutive"))
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            q = q.Where(c => c.CreatedBy == uid);
        }
        ViewBag.Total = await q.CountAsync();
        ViewBag.Active = await q.CountAsync(c => c.Status == "Active");
        ViewBag.Inactive = await q.CountAsync(c => c.Status == "Inactive");
        return View();
    }

    [AllowAnonymous]
    public IActionResult Error() => Content("Something went wrong. Please try again.");
}
