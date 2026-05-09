using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Petly.DataAccess.Data;

namespace Petly.Controllers;

[Authorize]
public class ViewedPetsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ViewedPetsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> My()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

        var viewedPets = await _context.ViewedPets
            .Where(v => v.UserId == userId)
            .OrderByDescending(v => v.ViewedAt)
            .Include(v => v.Pet)
            .Select(v => v.Pet)
            .ToListAsync();

        return View(viewedPets);
    }
}