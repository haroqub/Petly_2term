using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Petly.DataAccess.Data;
using System.Security.Claims;
using Petly.Models;

namespace Petly.Business.Services;

public class AccountService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<int>> _roleManager;


    public AccountService(
        ApplicationDbContext context, 
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<int>> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<ApplicationUser?> GetByCredentialsAsync(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user != null && await _userManager.CheckPasswordAsync(user, password))
        {
            return user;
        }
        return null;
    }

    public async Task<bool> EmailExistsAsync(string email, int? excludeId = null)
    {
        return await _context.Users.AnyAsync(u => u.Email == email && (!excludeId.HasValue || u.Id != excludeId));
    }

    public async Task<ApplicationUser> CreateUserAsync(RegisterViewModel model)
    {
        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            Name = model.Name,
            Surname = model.Surname,
            Status = "Активний",
            RegistrationDate = DateTime.Now
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        
        if (result.Succeeded)
        {
            if (!await _roleManager.RoleExistsAsync("user"))
            {
                await _roleManager.CreateAsync(new IdentityRole<int>("user"));
            }
            await _userManager.AddToRoleAsync(user, "user");
        }
        
        return user;
    }

    public async Task<List<AdminUserViewModel>> GetAllUsersAsync()
    {
        var users = await _context.Users.ToListAsync();
        var shelters = await _context.Shelters.ToDictionaryAsync(s => s.AccountId);
        
        var result = new List<AdminUserViewModel>();
        
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            var role = roles.FirstOrDefault() ?? "user";
            
            if (role == "user" || role == "shelter_admin")
            {
                shelters.TryGetValue(u.Id, out var shelter);
                var isShelterAdmin = role == "shelter_admin";

                result.Add(new AdminUserViewModel
                {
                    AccountId = u.Id,
                    Name = isShelterAdmin ? shelter?.AdminName ?? shelter?.ShelterName : u.Name,
                    Surname = isShelterAdmin ? null : u.Surname,
                    Email = u.Email,
                    Role = role,
                    Status = isShelterAdmin ? "Активний" : u.Status ?? "Активний",
                    RegistrationDate = u.RegistrationDate,
                    ShelterName = shelter?.ShelterName
                });
            }
        }
        return result.OrderBy(a => a.AccountId).ToList();
    }

    public async Task<UserEditViewModel?> GetUserAsync(int accountId)
    {
        var user = await _userManager.FindByIdAsync(accountId.ToString());
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "user";

        var shelter = await _context.Shelters.FirstOrDefaultAsync(s => s.AccountId == accountId);

        return new UserEditViewModel
        {
            AccountId = user.Id,
            Name = user.Name ?? shelter?.AdminName ?? shelter?.ShelterName ?? string.Empty,
            Surname = user.Surname ?? string.Empty,
            Email = user.Email,
            Role = role,
            Status = user.Status ?? "Активний",
            Password = string.Empty
        };
    }

    public async Task UpdateUserAsync(UserEditViewModel model)
    {
        var user = await _userManager.FindByIdAsync(model.AccountId.ToString());
        if (user == null) throw new InvalidOperationException("Акаунт не знайдено");

        user.Email = model.Email;
        user.UserName = model.Email;
        user.Name = model.Name;
        user.Surname = model.Surname;
        user.Status = model.Status;

        await _userManager.UpdateAsync(user);

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            await _userManager.ResetPasswordAsync(user, token, model.Password);
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (!currentRoles.Contains(model.Role))
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!await _roleManager.RoleExistsAsync(model.Role))
            {
                await _roleManager.CreateAsync(new IdentityRole<int>(model.Role));
            }
            await _userManager.AddToRoleAsync(user, model.Role);
        }

        var shelter = await _context.Shelters.FirstOrDefaultAsync(s => s.AccountId == model.AccountId);
        if (shelter != null)
        {
            shelter.AdminName = model.Name;
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteUserAsync(int accountId)
    {
        var user = await _userManager.FindByIdAsync(accountId.ToString());
        
        var shelter = await _context.Shelters.FirstOrDefaultAsync(s => s.AccountId == accountId);
        if (shelter != null)
        {
            var pets = await _context.Pets.Where(p => p.ShelterId == accountId).ToListAsync();
            var needs = await _context.ShelterNeeds.Where(n => n.ShelterId == accountId).ToListAsync();

            if (needs.Any()) _context.ShelterNeeds.RemoveRange(needs);
            if (pets.Any()) _context.Pets.RemoveRange(pets);

            _context.Shelters.Remove(shelter);
            await _context.SaveChangesAsync();
        }

        if (user != null)
        {
            await _userManager.DeleteAsync(user);
        }
    }

    public async Task<ApplicationUser?> GetByEmailAsync(string email)
    {
        return await _userManager.FindByEmailAsync(email);
    }

    public async Task<ApplicationUser?> GetCurrentUserAsync(ClaimsPrincipal userPrincipal)
{
    return await _userManager.GetUserAsync(userPrincipal);
}

    public async Task UpdateProfileAsync(ApplicationUser user, EditProfileViewModel model, string? imagePath)
    {
        user.Name = model.Name;
        user.Surname = model.Surname;
        user.Email = model.Email;
        user.UserName = model.Email;

        if (imagePath != null)
        {
            user.ImagePath = imagePath;
        }

        await _userManager.UpdateAsync(user);
    }
}