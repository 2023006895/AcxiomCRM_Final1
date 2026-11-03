using AcxiomCRM.Models;
using Microsoft.AspNetCore.Identity;
namespace AcxiomCRM.Data;

public static class SeedData
{
    public static readonly string[] Roles = { "Admin", "Manager", "SalesExecutive" };

    public static async Task InitializeAsync(IServiceProvider sp)
    {
        var db = sp.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync();

        var roleMgr = sp.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var r in Roles)
            if (!await roleMgr.RoleExistsAsync(r))
                await roleMgr.CreateAsync(new IdentityRole(r));

        var userMgr = sp.GetRequiredService<UserManager<ApplicationUser>>();
        await EnsureUser(userMgr, "admin@acxiomcrm.com", "System Admin", "Admin@123", "Admin");
        await EnsureUser(userMgr, "manager@acxiomcrm.com", "Test Manager", "Manager@123", "Manager");
        await EnsureUser(userMgr, "sales@acxiomcrm.com", "Test Sales Executive", "Sales@123", "SalesExecutive");
    }

    private static async Task EnsureUser(UserManager<ApplicationUser> mgr, string email, string name, string pwd, string role)
    {
        if (await mgr.FindByEmailAsync(email) != null) return;
        var user = new ApplicationUser { UserName = email, Email = email, FullName = name, EmailConfirmed = true };
        var result = await mgr.CreateAsync(user, pwd);
        if (result.Succeeded) await mgr.AddToRoleAsync(user, role);
    }
}
