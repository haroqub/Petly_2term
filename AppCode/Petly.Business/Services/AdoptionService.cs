using Microsoft.EntityFrameworkCore;
using Petly.DataAccess.Data;
using Petly.Models;

namespace Petly.Business.Services;

public class AdoptionService
{
    private readonly ApplicationDbContext _context;

    public AdoptionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<AdoptionApplication>> GetAllApplicationsAsync()
    {
        var applications = await _context.AdoptionApplications
            .Include(a => a.Pet)
            .Include(a => a.UserProfile)
            .OrderByDescending(a => a.SubmissionDate)
            .ToListAsync();

        NormalizeStatuses(applications);
        return applications;
    }

    public async Task<List<AdoptionApplication>> GetUserApplicationsAsync(int userId)
    {
        var applications = await _context.AdoptionApplications
            .Include(a => a.Pet)
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.SubmissionDate)
            .ToListAsync();

        NormalizeStatuses(applications);
        return applications;
    }

    public async Task CreateApplicationAsync(int petId, int userId, string applicantName, string applicantSurname, int applicantAge, string contactInfo)
    {
        var pet = await _context.Pets.FirstOrDefaultAsync(p => p.PetId == petId);
        if (pet == null)
        {
            throw new KeyNotFoundException("Тварину не знайдено.");
        }

        if (pet.Status == "Прилаштований")
        {
            throw new InvalidOperationException("Для цієї тварини адопція вже недоступна.");
        }

        var hasActiveApplication = await _context.AdoptionApplications
            .AnyAsync(a => a.PetId == petId
                        && a.UserId == userId
                        && (a.Status == AdoptionStatuses.Pending || a.Status == "Pending"));

        if (hasActiveApplication)
        {
            throw new InvalidOperationException("Ви вже подали заявку на цю тварину.");
        }

        var application = new AdoptionApplication
        {
            PetId = petId,
            UserId = userId,
            Status = AdoptionStatuses.Pending,
            SubmissionDate = DateTime.Now,
            ApplicantName = applicantName.Trim(),
            ApplicantSurname = applicantSurname.Trim(),
            ApplicantAge = applicantAge,
            ContactInfo = contactInfo.Trim()
        };

        _context.AdoptionApplications.Add(application);
        await _context.SaveChangesAsync();
    }

    public async Task<List<AdoptionApplication>> GetShelterApplicationsAsync(int shelterAccountId)
    {
        var shelterPetIds = await _context.Pets
            .Where(p => p.ShelterId == shelterAccountId)
            .Select(p => p.PetId)
            .ToHashSetAsync();

        var applications = await _context.AdoptionApplications
            .Include(a => a.Pet)
            .Include(a => a.UserProfile)
            .ToListAsync();

        applications = applications
            .Where(a => shelterPetIds.Contains(a.PetId))
            .OrderByDescending(a => a.SubmissionDate)
            .ToList();

        NormalizeStatuses(applications);
        return applications;
    }

    public async Task<ApplicationUser?> GetApplicantDetailsAsync(int adoptId, int currentUserId, bool isSystemAdmin = false)
    {
        var application = await GetApplicationForManagementAsync(adoptId);

        if (application == null)
        {
            return null;
        }

        if (application.Pet == null)
        {
            throw new InvalidOperationException("Заявка не містить прив'язаної тварини.");
        }

        if (!isSystemAdmin && application.Pet.ShelterId != currentUserId)
        {
            throw new UnauthorizedAccessException("Немає доступу до цього кандидата.");
        }

        return application.UserProfile;
    }

    public async Task<AdoptionApplication?> GetApplicationDetailsAsync(int adoptId, int currentUserId, bool isSystemAdmin = false)
    {
        var application = await GetApplicationForManagementAsync(adoptId);

        if (application == null)
        {
            return null;
        }

        if (application.Pet == null)
        {
            throw new InvalidOperationException("Заявка не містить прив'язаної тварини.");
        }

        if (!isSystemAdmin && application.Pet.ShelterId != currentUserId)
        {
            throw new UnauthorizedAccessException("Немає доступу до цієї заявки.");
        }

        application.Status = AdoptionStatuses.Normalize(application.Status);
        return application;
    }

    public async Task UpdateApplicationStatusAsync(int adoptId, string newStatus, int currentUserId)
    {
        var application = await _context.AdoptionApplications
            .Include(a => a.Pet)
            .FirstOrDefaultAsync(a => a.AdoptId == adoptId);

        if (application == null)
        {
            throw new KeyNotFoundException("Заявку не знайдено.");
        }

        if (application.Pet == null)
        {
            throw new InvalidOperationException("Заявка не містить прив'язаної тварини.");
        }

        if (application.Pet.ShelterId != currentUserId)
        {
            throw new UnauthorizedAccessException("Немає доступу до цієї заявки.");
        }

        var normalizedCurrentStatus = AdoptionStatuses.Normalize(application.Status);
        if (normalizedCurrentStatus != AdoptionStatuses.Pending)
        {
            throw new InvalidOperationException("Цю заявку вже було оброблено.");
        }

        var normalizedNewStatus = AdoptionStatuses.Normalize(newStatus);
        application.Status = normalizedNewStatus;

        if (normalizedNewStatus == AdoptionStatuses.Approved)
        {
            application.Pet.Status = "Прилаштований";
            var otherApplications = await _context.AdoptionApplications
                .Where(a => a.PetId == application.PetId
                         && a.AdoptId != adoptId)
                .ToListAsync();

            foreach (var otherApp in otherApplications)
            {
                if (AdoptionStatuses.Normalize(otherApp.Status) == AdoptionStatuses.Pending)
                {
                    otherApp.Status = AdoptionStatuses.AutoRejected;
                }
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task DeleteApplicationAsync(int adoptId, int userId)
    {
        var application = await _context.AdoptionApplications
            .FirstOrDefaultAsync(a => a.AdoptId == adoptId && a.UserId == userId);

        if (application == null)
        {
            return;
        }

        if (AdoptionStatuses.Normalize(application.Status) != AdoptionStatuses.Pending)
        {
            throw new InvalidOperationException("Можна скасувати лише заявку, що очікує на розгляд.");
        }

        _context.AdoptionApplications.Remove(application);
        await _context.SaveChangesAsync();
    }

    private static void NormalizeStatuses(IEnumerable<AdoptionApplication> applications)
    {
        foreach (var application in applications)
        {
            application.Status = AdoptionStatuses.Normalize(application.Status);
        }
    }

    private Task<AdoptionApplication?> GetApplicationForManagementAsync(int adoptId)
    {
        return _context.AdoptionApplications
            .Include(a => a.Pet)
            .Include(a => a.UserProfile)
            .FirstOrDefaultAsync(a => a.AdoptId == adoptId);
    }

    public async Task<int> GetApplicantUserIdAsync(int adoptId)
{
    var application = await _context.AdoptionApplications
        .FirstOrDefaultAsync(a => a.AdoptId == adoptId);

    if (application == null)
    {
        throw new KeyNotFoundException("Заявку не знайдено.");
    }

    return application.UserId;
}
}
