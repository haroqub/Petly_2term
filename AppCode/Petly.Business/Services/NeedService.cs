using Microsoft.EntityFrameworkCore;
using Petly.DataAccess.Data;
using Petly.Models;

namespace Petly.Business.Services;

public class NeedService
{
    private readonly ApplicationDbContext _context;

    public NeedService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ShelterNeedGroupViewModel>> GetNeedsAsync(int? currentUserId, string? role)
    {
        var needs = await _context.ShelterNeeds
            .AsNoTracking()
            .Include(need => need.Shelter)
            .OrderBy(need => need.Shelter!.ShelterName)
            .ThenBy(need => need.NeedId)
            .ToListAsync();

        return needs
            .GroupBy(need => new
            {
                ShelterName = need.Shelter != null && need.Shelter.ShelterName != null
                    ? LocalizeShelterName(need.ShelterId, need.Shelter.ShelterName)
                    : $"Притулок #{need.ShelterId}",
                Location = need.Shelter != null && need.Shelter.Location != null
                    ? LocalizeLocation(need.Shelter.Location)
                    : "Локацію не вказано",
            })
            .Select(group => new ShelterNeedGroupViewModel
            {
                ShelterId = group
                    .OrderByDescending(need => role == "shelter_admin" && currentUserId == need.ShelterId)
                    .ThenBy(need => need.ShelterId)
                    .Select(need => need.ShelterId)
                    .First(),
                ShelterName = group.Key.ShelterName,
                Location = group.Key.Location,
                CanManage = role == "system_admin" || group.Any(need => role == "shelter_admin" && currentUserId == need.ShelterId),
                Needs = group
                    .OrderBy(need => need.IsFulfilled)
                    .ThenBy(need => need.NeedId)
                    .Select(need => new ShelterNeedListItemViewModel
                    {
                        NeedId = need.NeedId,
                        Description = LocalizeDescription(need.Description),
                        PaymentDetails = LocalizePaymentDetails(need.PaymentDetails),
                        IsFulfilled = need.IsFulfilled,
                        FulfilledAt = need.FulfilledAt,
                    })
                    .ToList(),
            })
            .ToList();
    }

    public async Task<ShelterNeed?> GetNeedAsync(int needId)
    {
        var need = await _context.ShelterNeeds
            .Include(need => need.Shelter)
            .FirstOrDefaultAsync(need => need.NeedId == needId);

        if (need == null)
        {
            return null;
        }

        need.Description = LocalizeDescription(need.Description);
        need.PaymentDetails = LocalizePaymentDetails(need.PaymentDetails);

        if (need.Shelter != null)
        {
            need.Shelter.ShelterName = LocalizeShelterName(need.ShelterId, need.Shelter.ShelterName);
            need.Shelter.Location = LocalizeLocation(need.Shelter.Location);
        }

        return need;
    }

    public async Task AddNeedAsync(ShelterNeed need)
    {
        _context.ShelterNeeds.Add(need);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateNeedAsync(ShelterNeed need)
    {
        _context.ShelterNeeds.Update(need);
        await _context.SaveChangesAsync();
    }

    public async Task SetNeedFulfillmentAsync(ShelterNeed need, bool isFulfilled)
    {
        need.IsFulfilled = isFulfilled;
        need.FulfilledAt = isFulfilled ? DateTime.UtcNow : null;

        _context.ShelterNeeds.Update(need);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteNeedAsync(int needId)
    {
        var need = await GetNeedAsync(needId);
        if (need == null)
        {
            return;
        }

        _context.ShelterNeeds.Remove(need);
        await _context.SaveChangesAsync();
    }

    private static string LocalizeShelterName(int shelterId, string shelterName)
    {
        return shelterName.Trim() switch
        {
            "Happy Paws Shelter" => "Притулок Щасливі Лапи",
            "Domivka Shelter" => "Притулок Домівка",
            "Animal Rescue Ukraine" => "Порятунок Тварин Україна",
            "Pets Home Kyiv" => "Дім для Тварин Київ",
            _ when shelterId == 6 => "Притулок Щасливі Лапи",
            _ when shelterId == 7 => "Притулок Домівка",
            _ when shelterId == 8 => "Порятунок Тварин Україна",
            _ when shelterId == 9 => "Дім для Тварин Київ",
            _ => shelterName
        };
    }

    private static string LocalizeLocation(string location)
    {
        return location.Trim() switch
        {
            "Lviv" => "Львів",
            "Kyiv" => "Київ",
            _ => location
        };
    }

    private static string LocalizeDescription(string description)
    {
        return description.Trim() switch
        {
            "Dry food and blankets needed for dogs" => "Потрібні сухий корм та ковдри для собак",
            "Cat food and litter supplies needed" => "Потрібні корм для котів та наповнювач",
            "Medical supplies needed for rescued animals" => "Потрібні медичні препарати для врятованих тварин",
            "Funds needed for shelter renovation" => "Потрібні кошти на ремонт притулку",
            _ => description
        };
    }

    private static string LocalizePaymentDetails(string paymentDetails)
    {
        return paymentDetails.Trim() switch
        {
            "Bank transfer available" => "Можливий банківський переказ",
            "Donation box available at shelter" => "У притулку є скринька для пожертв",
            "Veterinary help needed" => "Потрібна ветеринарна допомога",
            "Online donation available" => "Доступна онлайн-пожертва",
            _ => paymentDetails
        };
    }
}
