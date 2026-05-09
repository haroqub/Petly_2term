using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Petly.Business.Services;
using Petly.Models;
using Petly.DataAccess.Data;
using Microsoft.EntityFrameworkCore;

namespace Petly.Controllers;

[Authorize]
public class PetsController : Controller
{
    private readonly PetService _petService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly NotificationService _notificationService;

    public PetsController(
        PetService petService,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        NotificationService notificationService)
    {
        _petService = petService;
        _userManager = userManager;
        _context = context;
        _notificationService = notificationService;
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        return await _userManager.GetUserAsync(User);
    }

    private async Task<Shelter?> GetCurrentShelterAsync()
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
        {
            return null;
        }

        return await _context.Shelters.FirstOrDefaultAsync(s => s.AccountId == currentUser.Id);
    }

    public async Task<IActionResult> Index()
    {
        var currentUser = await GetCurrentUserAsync();
        var accountId = currentUser?.Id;

        List<Pet> pets = await _petService.GetAllPetsAsync();

        string? shelterName = null;

        if (User.IsInRole("shelter_admin") && accountId != null)
        {
            pets = pets
                .Where(p => p.ShelterId == accountId.Value)
                .ToList();

            var shelter = await _context.Shelters
                .FirstOrDefaultAsync(s => s.AccountId == accountId.Value);

            shelterName = shelter?.ShelterName;
        }

        ViewBag.AccountId = accountId;
        ViewBag.ShelterName = shelterName;

        return View(pets);
    }

    public async Task<IActionResult> Details(int id)
    {
        var pet = await _petService.GetPetAsync(id);
        if (pet == null) return NotFound();
        if (User.Identity.IsAuthenticated)
{
    var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);

    var existing = await _context.ViewedPets
        .FirstOrDefaultAsync(v => v.UserId == userId && v.PetId == id);

    if (existing != null)
    {
        existing.ViewedAt = DateTime.Now;
    }
    else
    {
        _context.ViewedPets.Add(new ViewedPet
        {
            UserId = userId,
            PetId = id,
            ViewedAt = DateTime.Now
        });
    }

    await _context.SaveChangesAsync();
}
return View(pet);
    }

    [Authorize(Roles = "user")]
    public IActionResult Adopt(int id)
    {
        return RedirectToAction("Create", "Adoption", new { petId = id });
    }

    [Authorize(Roles = "shelter_admin")]
    public async Task<IActionResult> Create()
    {
        var shelter = await GetCurrentShelterAsync();
        if (shelter == null)
        {
            return Forbid();
        }

        ViewBag.ShelterName = shelter.ShelterName;
        return View();
    }

    [HttpPost]
    [Authorize(Roles = "shelter_admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Pet pet)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null || !User.IsInRole("shelter_admin"))
        {
            return Forbid();
        }

        var shelter = await _context.Shelters.FirstOrDefaultAsync(s => s.AccountId == currentUser.Id);
        if (shelter == null)
        {
            return Forbid();
        }

        pet.ShelterId = currentUser.Id;
        pet.PetName = pet.PetName?.Trim() ?? string.Empty;
        pet.Type = string.IsNullOrWhiteSpace(pet.Type) ? null : pet.Type.Trim();
        pet.Breed = string.IsNullOrWhiteSpace(pet.Breed) ? null : pet.Breed.Trim();
        pet.Gender = string.IsNullOrWhiteSpace(pet.Gender) ? null : pet.Gender.Trim();
        pet.Size = string.IsNullOrWhiteSpace(pet.Size) ? null : pet.Size.Trim();
        pet.PhotoUrl = string.IsNullOrWhiteSpace(pet.PhotoUrl) ? null : pet.PhotoUrl.Trim();
        pet.Description = string.IsNullOrWhiteSpace(pet.Description) ? null : pet.Description.Trim();
        pet.Status = string.IsNullOrWhiteSpace(pet.Status) ? "Доступний" : pet.Status.Trim();

        if (!ModelState.IsValid)
        {
            ViewBag.ShelterName = shelter.ShelterName;
            return View(pet);
        }

        await _petService.AddPetAsync(pet);
        var users = await _userManager.Users.ToListAsync();

        foreach (var user in users)
        {
            await _notificationService.CreateAsync(
                user.Id,
                $"Нова тварина",
                $"🐾 Нова тварина: {pet.PetName} тепер доступна для адопції"
                );
        }
        TempData["Success"] = "Тварину додано";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "shelter_admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var pet = await _petService.GetPetAsync(id);
        if (pet == null)
        {
            return NotFound();
        }

        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null || !User.IsInRole("shelter_admin"))
        {
            return Forbid();
        }

        if (pet.ShelterId != currentUser.Id) return Forbid();

        return View(pet);
    }

    [HttpPost]
    [Authorize(Roles = "shelter_admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Pet pet)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null || !User.IsInRole("shelter_admin")) return Forbid();

        var existingPet = await _petService.GetPetAsync(pet.PetId);
        if (existingPet == null) return NotFound();
        if (existingPet.ShelterId != currentUser.Id) return Forbid();

        if (!ModelState.IsValid)
        {
            pet.ShelterId = existingPet.ShelterId;
            pet.Status = existingPet.Status;
            pet.PhotoUrl ??= existingPet.PhotoUrl;
            pet.CreatedAt = existingPet.CreatedAt;
            return View(pet);
        }

        existingPet.PetName = pet.PetName;
        existingPet.Type = pet.Type;
        existingPet.Breed = pet.Breed;
        existingPet.Gender = pet.Gender;
        existingPet.Age = pet.Age;
        existingPet.Size = pet.Size;
        existingPet.Vaccinated = pet.Vaccinated;
        existingPet.Sterilized = pet.Sterilized;
        existingPet.PhotoUrl = string.IsNullOrWhiteSpace(pet.PhotoUrl) ? existingPet.PhotoUrl : pet.PhotoUrl.Trim();
        existingPet.Description = pet.Description;

        await _petService.UpdatePetAsync(existingPet);
        TempData["Success"] = "Дані тварини оновлено";

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "shelter_admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var pet = await _petService.GetPetAsync(id);
        if (pet == null) return NotFound();

        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null || !User.IsInRole("shelter_admin")) return Forbid();
        if (pet.ShelterId != currentUser.Id) return Forbid();

        return View(pet);
    }

    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = "shelter_admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var pet = await _petService.GetPetAsync(id);
        if (pet == null) return NotFound();

        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null || !User.IsInRole("shelter_admin")) return Forbid();
        if (pet.ShelterId != currentUser.Id) return Forbid();

        await _petService.DeletePetAsync(id);
        TempData["Success"] = "Тварину видалено";
        return RedirectToAction(nameof(Index));
    }
}
