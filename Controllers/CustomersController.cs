using System.Security.Claims;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace AcxiomCRM.Controllers;

[Authorize]
public class CustomersController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;
    public CustomersController(ApplicationDbContext db, IAuditService audit) { _db = db; _audit = audit; }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // Sales Executives only see their own records; Admin/Manager see all.
    private IQueryable<Customer> Scoped() =>
        User.IsInRole("SalesExecutive") ? _db.Customers.Where(c => c.CreatedBy == UserId) : _db.Customers;

    public async Task<IActionResult> Index(string? search, string? status)
    {
        var q = Scoped();
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(c => c.CustomerName.Contains(search) || c.Email.Contains(search) ||
                             c.Phone.Contains(search) || (c.CompanyName != null && c.CompanyName.Contains(search)));
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(c => c.Status == status);
        ViewBag.Search = search; ViewBag.Status = status;
        return View(await q.OrderByDescending(c => c.CreatedDate).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var c = await Scoped().FirstOrDefaultAsync(x => x.CustomerId == id);
        return c == null ? NotFound() : View(c);
    }

    public IActionResult Create() => View(new Customer());

    [HttpPost]
    public async Task<IActionResult> Create([Bind("CustomerName,Email,Phone,CompanyName,Address,City,State,Status")] Customer model)
    {
        await ValidateBusinessRules(model, null);
        if (!ModelState.IsValid) return View(model);

        model.CreatedBy = UserId;
        model.CreatedDate = DateTime.UtcNow;
        _db.Customers.Add(model);
        await _db.SaveChangesAsync();
        model.CustomerCode = $"CUST{model.CustomerId:D5}";
        await _db.SaveChangesAsync();

        await _audit.LogAsync("Create", "Customer", model.CustomerId.ToString(), null, $"{model.CustomerName} | {model.Email}");
        TempData["Success"] = "Customer created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var c = await Scoped().FirstOrDefaultAsync(x => x.CustomerId == id);
        return c == null ? NotFound() : View(c);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, [Bind("CustomerId,CustomerName,Email,Phone,CompanyName,Address,City,State,Status")] Customer model)
    {
        if (id != model.CustomerId) return BadRequest();
        var existing = await Scoped().FirstOrDefaultAsync(x => x.CustomerId == id);
        if (existing == null) return NotFound();

        await ValidateBusinessRules(model, id);
        if (!ModelState.IsValid) return View(model);

        var old = $"{existing.CustomerName} | {existing.Email} | {existing.Phone} | {existing.Status}";
        existing.CustomerName = model.CustomerName; existing.Email = model.Email; existing.Phone = model.Phone;
        existing.CompanyName = model.CompanyName; existing.Address = model.Address;
        existing.City = model.City; existing.State = model.State; existing.Status = model.Status;
        await _db.SaveChangesAsync();

        await _audit.LogAsync("Update", "Customer", id.ToString(), old, $"{existing.CustomerName} | {existing.Email} | {existing.Phone} | {existing.Status}");
        TempData["Success"] = "Customer updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await _db.Customers.FindAsync(id);
        if (c == null) return NotFound();
        _db.Customers.Remove(c);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Delete", "Customer", id.ToString(), $"{c.CustomerName} | {c.Email}", null);
        TempData["Success"] = "Customer deleted.";
        return RedirectToAction(nameof(Index));
    }

    // Server-side business validation: allowed status set + email/phone uniqueness.
    private async Task ValidateBusinessRules(Customer m, int? currentId)
    {
        if (!Customer.Statuses.Contains(m.Status))
            ModelState.AddModelError(nameof(m.Status), "Invalid status.");
        if (await _db.Customers.AnyAsync(c => c.Email == m.Email && c.CustomerId != currentId))
            ModelState.AddModelError(nameof(m.Email), "A customer with this email already exists.");
        if (await _db.Customers.AnyAsync(c => c.Phone == m.Phone && c.CustomerId != currentId))
            ModelState.AddModelError(nameof(m.Phone), "A customer with this phone already exists.");
    }
}
