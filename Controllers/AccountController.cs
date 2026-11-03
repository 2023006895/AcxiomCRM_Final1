using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
namespace AcxiomCRM.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly IAuditService _audit;

    public AccountController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn, IAuditService audit)
    { _users = users; _signIn = signIn; _audit = audit; }

    [HttpGet, AllowAnonymous]
    public IActionResult Login(string? returnUrl = null) { ViewData["ReturnUrl"] = returnUrl; return View(); }

    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(model);

        var user = await _users.FindByEmailAsync(model.Email);
        if (user == null)
        {
            await _audit.LogAsync("Failed Login", "User", null, null, $"Unknown email: {model.Email}");
            ModelState.AddModelError("", "Invalid email or password.");
            return View(model);
        }

        var result = await _signIn.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            await _audit.LogAsync("Login", "User", user.Id, userId: user.Id);
            return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction("Index", "Home");
        }
        if (result.IsLockedOut)
        {
            await _audit.LogAsync("Lockout", "User", user.Id, userId: user.Id);
            ModelState.AddModelError("", "Account locked due to repeated failed attempts. Try again in 15 minutes.");
            return View(model);
        }
        await _audit.LogAsync("Failed Login", "User", user.Id, userId: user.Id);
        ModelState.AddModelError("", "Invalid email or password.");
        return View(model);
    }

    [HttpGet, AllowAnonymous]
    public IActionResult Register() => View();

    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = new ApplicationUser { UserName = model.Email, Email = model.Email, FullName = model.FullName, EmailConfirmed = true };
        var result = await _users.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
            return View(model);
        }
        await _users.AddToRoleAsync(user, "SalesExecutive");
        await _audit.LogAsync("Register", "User", user.Id, null, user.Email, user.Id);
        await _signIn.SignInAsync(user, isPersistent: false);
        return RedirectToAction("Index", "Home");
    }

    [HttpPost, Authorize]
    public async Task<IActionResult> Logout()
    {
        await _audit.LogAsync("Logout", "User");
        await _signIn.SignOutAsync();
        return RedirectToAction("Login");
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();
}
