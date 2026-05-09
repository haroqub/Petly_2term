using Microsoft.EntityFrameworkCore;
using Petly.DataAccess.Data;
using Petly.Models;

namespace Petly.Business.Services;

public class PetService
{
    private readonly ApplicationDbContext _context;

    public PetService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Pet>> GetAllPetsAsync()
    {
        return await _context.Pets.ToListAsync();
    }

    public async Task<List<Pet>> GetPetsByShelterAsync(int shelterId)
    {
        return await _context.Pets
            .Where(p => p.ShelterId == shelterId)
            .ToListAsync();
    }

   public async Task<List<Pet>> GetPetsAsync(string? typeFilter, string? searchTerm)
{
    var query = _context.Pets.AsQueryable();
    if (!string.IsNullOrWhiteSpace(searchTerm))
    {
        query = query.Where(p => p.PetName.Contains(searchTerm) || p.Breed.Contains(searchTerm));
    }

    if (typeFilter == "Прилаштовані")
    {
        query = query.Where(p => p.Status == "Прилаштований");
    }
    else
    {
        query = query.Where(p => p.Status == "Доступний" || p.Status == "Available");

        if (typeFilter == "Собака" || typeFilter == "Кіт")
        {
            query = query.Where(p => p.Type == typeFilter);
        }
    }

    return await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
}

    public async Task<Pet?> GetPetAsync(int petId)
    {
        return await _context.Pets.FirstOrDefaultAsync(p => p.PetId == petId);
    }

    public async Task AddPetAsync(Pet pet)
    {
        _context.Pets.Add(pet);
        await _context.SaveChangesAsync();
    }

    public async Task UpdatePetAsync(Pet pet)
    {
        _context.Pets.Update(pet);
        await _context.SaveChangesAsync();
    }

    public async Task DeletePetAsync(int petId)
    {
        var pet = await GetPetAsync(petId);
        if (pet != null)
        {
            _context.Pets.Remove(pet);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<Pet>> GetAvailablePetsAsync()
    {
        return await _context.Pets
            .Where(p => p.Status == "Available")
            .ToListAsync();
    }
}
