using Microsoft.EntityFrameworkCore;
using Petly.DataAccess.Data;
using Petly.Models;

namespace Petly.Business.Services;

public class DashboardService
{
    private static readonly int[] AllowedPeriods = new[] { 1, 7, 30 };

    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AdminDashboardViewModel> GetAnalyticsAsync(int periodDays, int? shelterId = null, int adminId = 0)
    {
        // Логувати доступ до аналітики
        var accessLog = new AnalyticsAccessLog
        {
            AdminId = adminId,
            PeriodDays = periodDays,
            ShelterId = shelterId,
            AccessTime = DateTime.Now
        };
        _context.AnalyticsAccessLogs.Add(accessLog);
        await _context.SaveChangesAsync();

        int normalizedPeriod = AllowedPeriods.Contains(periodDays) ? periodDays : 30;
        DateTime periodStart = DateTime.Today.AddDays(-(normalizedPeriod - 1));
        DateTime nextDay = DateTime.Today.AddDays(1);
        Shelter? selectedShelter = shelterId.HasValue
            ? await _context.Shelters.FirstOrDefaultAsync(s => s.AccountId == shelterId.Value)
            : null;
        int? normalizedShelterId = selectedShelter?.AccountId;

        Dictionary<string, int> roleCounts = await GetRoleCountsAsync();

        int totalUsers = await _context.Users.CountAsync();
        int totalShelterAdmins = roleCounts.GetValueOrDefault("shelter_admin");
        IQueryable<Pet> petsQuery = _context.Pets;
        IQueryable<ShelterNeed> needsQuery = _context.ShelterNeeds;
        IQueryable<AdoptionApplication> applicationsQuery = _context.AdoptionApplications;

        if (normalizedShelterId.HasValue)
        {
            petsQuery = petsQuery.Where(p => p.ShelterId == normalizedShelterId.Value);
            needsQuery = needsQuery.Where(n => n.ShelterId == normalizedShelterId.Value);
            applicationsQuery = applicationsQuery.Join(
                _context.Pets.Where(p => p.ShelterId == normalizedShelterId.Value),
                application => application.PetId,
                pet => pet.PetId,
                (application, _) => application);
        }

        int totalPets = await petsQuery.CountAsync();
        int availablePets = await petsQuery.CountAsync(p => p.Status == "Available" || p.Status == "Доступний");
        int adoptedPets = await petsQuery.CountAsync(p => p.Status == "Прилаштований");
        int totalNeeds = await needsQuery.CountAsync();
        int totalApplications = await applicationsQuery.CountAsync();
        int pendingApplications = await applicationsQuery.CountAsync(a => a.Status == AdoptionStatuses.Pending || a.Status == "Pending");
        int approvedApplications = await applicationsQuery.CountAsync(a => a.Status == AdoptionStatuses.Approved || a.Status == "Approved");
        int rejectedApplications = await applicationsQuery.CountAsync(a =>
            a.Status == AdoptionStatuses.Rejected
            || a.Status == "Rejected"
            || a.Status == AdoptionStatuses.AutoRejected
            || a.Status == "Auto-rejected"
            || a.Status == "Авто-відхилено");

        List<Pet> periodPets = await petsQuery
            .Where(p => p.CreatedAt >= periodStart && p.CreatedAt < nextDay)
            .ToListAsync();

        List<AdoptionApplication> periodApplications = await applicationsQuery
            .Where(a => a.SubmissionDate >= periodStart && a.SubmissionDate < nextDay)
            .ToListAsync();

        int periodNewUsers = await _context.Users.CountAsync(u => u.RegistrationDate >= periodStart && u.RegistrationDate < nextDay);
        int periodApplicationsCount = periodApplications.Count;
        int periodNewPets = periodPets.Count;

        HashSet<int> activeShelters = normalizedShelterId.HasValue
            ? new HashSet<int>(periodPets.Any() || periodApplications.Any() ? new[] { normalizedShelterId.Value } : Array.Empty<int>())
            : periodPets
                .Select(p => p.ShelterId)
                .Concat(await _context.AdoptionApplications
                    .Where(a => a.SubmissionDate >= periodStart && a.SubmissionDate < nextDay)
                    .Join(_context.Pets, a => a.PetId, p => p.PetId, (_, p) => p.ShelterId)
                    .Distinct()
                    .ToListAsync())
                .ToHashSet();

        return new AdminDashboardViewModel
        {
            SelectedPeriodDays = normalizedPeriod,
            SelectedShelterId = normalizedShelterId,
            TotalUsers = totalUsers,
            TotalShelterAdmins = totalShelterAdmins,
            TotalShelters = await _context.Shelters.CountAsync(),
            TotalPets = totalPets,
            AvailablePets = availablePets,
            AdoptedPets = adoptedPets,
            TotalNeeds = totalNeeds,
            TotalApplications = totalApplications,
            PendingApplications = pendingApplications,
            ApprovedApplications = approvedApplications,
            RejectedApplications = rejectedApplications,
            PeriodNewUsers = periodNewUsers,
            PeriodNewPets = periodNewPets,
            PeriodApplications = periodApplicationsCount,
            ActiveSheltersInPeriod = activeShelters.Count,
            AdoptionSuccessRate = totalApplications == 0
                ? 0
                : Math.Round((double)approvedApplications / totalApplications * 100, 1),
            SelectedShelterName = selectedShelter?.ShelterName,
            ShelterOptions = await BuildShelterOptionsAsync(),
            ApplicationsByDay = BuildDailyApplications(periodApplications, periodStart, normalizedPeriod),
            ApprovedApplicationsByDay = BuildDailyApplications(
                periodApplications.Where(a => a.Status == AdoptionStatuses.Approved || a.Status == "Approved"),
                periodStart,
                normalizedPeriod),
            ApplicationStatusBreakdown = BuildStatusBreakdown(
                periodApplicationsCount,
                periodApplications.Count(IsPendingStatus),
                periodApplications.Count(IsApprovedStatus),
                periodApplications.Count(IsRejectedStatus)),
            PetsByShelter = await BuildPetsByShelterAsync(normalizedShelterId),
            PendingByShelter = await BuildPendingByShelterAsync(normalizedShelterId),
            RecentUsers = await BuildRecentUsersAsync(),
            RecentPets = await BuildRecentPetsAsync(normalizedShelterId)
        };
    }

    private async Task<Dictionary<string, int>> GetRoleCountsAsync()
    {
        return await _context.UserRoles
            .Join(_context.Roles,
                userRole => userRole.RoleId,
                role => role.Id,
                (userRole, role) => role.Name)
            .Where(roleName => roleName != null)
            .GroupBy(roleName => roleName!)
            .Select(group => new { Role = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Role, item => item.Count);
    }

    private static List<DashboardSeriesPointViewModel> BuildDailyApplications(
        IEnumerable<AdoptionApplication> applications,
        DateTime periodStart,
        int periodDays)
    {
        Dictionary<DateTime, int> totalsByDay = applications
            .GroupBy(a => a.SubmissionDate.Date)
            .ToDictionary(group => group.Key, group => group.Count());

        List<DashboardSeriesPointViewModel> points = new();
        for (int i = 0; i < periodDays; i++)
        {
            DateTime day = periodStart.AddDays(i);
            points.Add(new DashboardSeriesPointViewModel
            {
                Label = day.ToString("dd.MM"),
                Value = totalsByDay.GetValueOrDefault(day)
            });
        }

        return points;
    }

    private async Task<List<DashboardFilterOptionViewModel>> BuildShelterOptionsAsync()
    {
        return await _context.Shelters
            .OrderBy(s => s.ShelterName)
            .Select(s => new DashboardFilterOptionViewModel
            {
                Id = s.AccountId,
                Label = s.ShelterName ?? $"Притулок #{s.AccountId}"
            })
            .ToListAsync();
    }

    private static List<DashboardBreakdownItemViewModel> BuildStatusBreakdown(
        int total,
        int pending,
        int approved,
        int rejected)
    {
        return
        [
            new DashboardBreakdownItemViewModel
            {
                Label = "Очікують",
                Value = pending,
                Meta = BuildPercentageMeta(total, pending)
            },
            new DashboardBreakdownItemViewModel
            {
                Label = "Схвалені",
                Value = approved,
                Meta = BuildPercentageMeta(total, approved)
            },
            new DashboardBreakdownItemViewModel
            {
                Label = "Відхилені",
                Value = rejected,
                Meta = BuildPercentageMeta(total, rejected)
            }
        ];
    }

    private async Task<List<DashboardBreakdownItemViewModel>> BuildPetsByShelterAsync(int? selectedShelterId)
    {
        IQueryable<DashboardBreakdownItemViewModel> query = _context.Pets
            .Join(_context.Shelters,
                pet => pet.ShelterId,
                shelter => shelter.AccountId,
                (pet, shelter) => new { shelter.ShelterName })
            .GroupBy(item => item.ShelterName ?? "Без назви")
            .Select(group => new DashboardBreakdownItemViewModel
            {
                Label = group.Key,
                Value = group.Count(),
                Meta = "тварин"
            });

        if (selectedShelterId.HasValue)
        {
            Shelter? shelter = await _context.Shelters.FirstOrDefaultAsync(s => s.AccountId == selectedShelterId.Value);
            int value = await _context.Pets.CountAsync(p => p.ShelterId == selectedShelterId.Value);
            return value == 0
                ? new List<DashboardBreakdownItemViewModel>()
                : new List<DashboardBreakdownItemViewModel>
                {
                    new DashboardBreakdownItemViewModel
                    {
                        Label = shelter?.ShelterName ?? $"Притулок #{selectedShelterId.Value}",
                        Value = value,
                        Meta = "тварин"
                    }
                };
        }

        return await query
            .OrderByDescending(item => item.Value)
            .ThenBy(item => item.Label)
            .Take(5)
            .ToListAsync();
    }

    private async Task<List<DashboardBreakdownItemViewModel>> BuildPendingByShelterAsync(int? selectedShelterId)
    {
        IQueryable<DashboardBreakdownItemViewModel> query = _context.AdoptionApplications
            .Where(a => a.Status == AdoptionStatuses.Pending || a.Status == "Pending")
            .Join(_context.Pets,
                application => application.PetId,
                pet => pet.PetId,
                (application, pet) => new { pet.ShelterId })
            .Join(_context.Shelters,
                item => item.ShelterId,
                shelter => shelter.AccountId,
                (item, shelter) => shelter.ShelterName)
            .GroupBy(name => name ?? "Без назви")
            .Select(group => new DashboardBreakdownItemViewModel
            {
                Label = group.Key,
                Value = group.Count(),
                Meta = "заявок чекають"
            });

        if (selectedShelterId.HasValue)
        {
            Shelter? shelter = await _context.Shelters.FirstOrDefaultAsync(s => s.AccountId == selectedShelterId.Value);
            int value = await _context.AdoptionApplications
                .Where(a => a.Status == AdoptionStatuses.Pending || a.Status == "Pending")
                .Join(_context.Pets.Where(p => p.ShelterId == selectedShelterId.Value),
                    application => application.PetId,
                    pet => pet.PetId,
                    (application, _) => application)
                .CountAsync();

            return value == 0
                ? new List<DashboardBreakdownItemViewModel>()
                : new List<DashboardBreakdownItemViewModel>
                {
                    new DashboardBreakdownItemViewModel
                    {
                        Label = shelter?.ShelterName ?? $"Притулок #{selectedShelterId.Value}",
                        Value = value,
                        Meta = "заявок чекають"
                    }
                };
        }

        return await query
            .OrderByDescending(item => item.Value)
            .ThenBy(item => item.Label)
            .Take(5)
            .ToListAsync();
    }

    private async Task<List<DashboardRecentUserViewModel>> BuildRecentUsersAsync()
    {
        List<(ApplicationUser User, string Role)> users = await _context.Users
            .GroupJoin(
                _context.UserRoles.Join(_context.Roles,
                    userRole => userRole.RoleId,
                    role => role.Id,
                    (userRole, role) => new { userRole.UserId, RoleName = role.Name }),
                user => user.Id,
                roleData => roleData.UserId,
                (user, roles) => new { User = user, RoleName = roles.Select(r => r.RoleName).FirstOrDefault() })
            .OrderByDescending(item => item.User.RegistrationDate)
            .Take(5)
            .Select(item => new ValueTuple<ApplicationUser, string>(item.User, item.RoleName ?? "user"))
            .ToListAsync();

        return users
            .Select(item => new DashboardRecentUserViewModel
            {
                AccountId = item.User.Id,
                DisplayName = string.Join(" ", new[] { item.User.Name, item.User.Surname }.Where(v => !string.IsNullOrWhiteSpace(v))).Trim()
                    is { Length: > 0 } fullName ? fullName : item.User.Email ?? $"User #{item.User.Id}",
                Email = item.User.Email ?? string.Empty,
                Role = item.Role,
                Status = item.User.Status ?? "Активний",
                RegisteredAt = item.User.RegistrationDate
            })
            .ToList();
    }

    private async Task<List<DashboardRecentPetViewModel>> BuildRecentPetsAsync(int? selectedShelterId)
    {
        IQueryable<DashboardRecentPetViewModel> query = _context.Pets
            .Where(p => !selectedShelterId.HasValue || p.ShelterId == selectedShelterId.Value)
            .Join(_context.Shelters,
                pet => pet.ShelterId,
                shelter => shelter.AccountId,
                (pet, shelter) => new { Pet = pet, ShelterName = shelter.ShelterName })
            .OrderByDescending(item => item.Pet.CreatedAt)
            .Take(5)
            .Select(item => new DashboardRecentPetViewModel
            {
                PetId = item.Pet.PetId,
                PetName = item.Pet.PetName,
                PhotoUrl = string.IsNullOrWhiteSpace(item.Pet.PhotoUrl) ? "/images/pets/no-photo.svg" : item.Pet.PhotoUrl!,
                ShelterName = item.ShelterName ?? "Без назви",
                Status = item.Pet.Status,
                CreatedAt = item.Pet.CreatedAt
            });

        return await query.ToListAsync();
    }

    private static string BuildPercentageMeta(int total, int value)
    {
        if (total == 0)
        {
            return "0%";
        }

        double percentage = Math.Round((double)value / total * 100, 1);
        return $"{percentage:0.#}%";
    }

    private static bool IsPendingStatus(AdoptionApplication application) =>
        application.Status == AdoptionStatuses.Pending || application.Status == "Pending";

    private static bool IsApprovedStatus(AdoptionApplication application) =>
        application.Status == AdoptionStatuses.Approved || application.Status == "Approved";

    private static bool IsRejectedStatus(AdoptionApplication application) =>
        application.Status == AdoptionStatuses.Rejected
        || application.Status == "Rejected"
        || application.Status == AdoptionStatuses.AutoRejected
        || application.Status == "Auto-rejected"
        || application.Status == "Авто-відхилено";
}
