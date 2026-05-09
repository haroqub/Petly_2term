using Microsoft.EntityFrameworkCore;
using Petly.DataAccess.Data;
using Petly.Models;

namespace Petly.Business.Services;

public class SuccessStoryService
{
    private readonly ApplicationDbContext _context;

    public SuccessStoryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<SuccessStory>> GetAllStoriesAsync()
    {
        return await _context.SuccessStories
            .Include(s => s.Pet)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task CreateStoryAsync(SuccessStory story)
    {
        _context.SuccessStories.Add(story);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Pet>> GetAvailablePetsAsync()
    {
        return await _context.Pets
            .Where(p => p.Status == "Прилаштований")
            .ToListAsync();
    }

    public async Task<SuccessStory?> GetStoryByIdAsync(int id)
    {
        return await _context.SuccessStories
            .Include(s => s.Pet)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task UpdateStoryAsync(SuccessStory story)
    {
        _context.SuccessStories.Update(story);
        await _context.SaveChangesAsync();
    }
}