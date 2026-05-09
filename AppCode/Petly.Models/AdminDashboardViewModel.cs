namespace Petly.Models;

public class AdminDashboardViewModel
{
    public int SelectedPeriodDays { get; set; }

    public int? SelectedShelterId { get; set; }

    public int TotalUsers { get; set; }

    public int TotalShelterAdmins { get; set; }

    public int TotalShelters { get; set; }

    public int TotalPets { get; set; }

    public int AvailablePets { get; set; }

    public int AdoptedPets { get; set; }

    public int TotalNeeds { get; set; }

    public int TotalApplications { get; set; }

    public int PendingApplications { get; set; }

    public int ApprovedApplications { get; set; }

    public int RejectedApplications { get; set; }

    public int PeriodNewUsers { get; set; }

    public int PeriodNewPets { get; set; }

    public int PeriodApplications { get; set; }

    public int ActiveSheltersInPeriod { get; set; }

    public double AdoptionSuccessRate { get; set; }

    public string? SelectedShelterName { get; set; }

    public List<DashboardFilterOptionViewModel> ShelterOptions { get; set; } = new();

    public List<DashboardSeriesPointViewModel> ApplicationsByDay { get; set; } = new();

    public List<DashboardSeriesPointViewModel> ApprovedApplicationsByDay { get; set; } = new();

    public List<DashboardBreakdownItemViewModel> ApplicationStatusBreakdown { get; set; } = new();

    public List<DashboardBreakdownItemViewModel> PetsByShelter { get; set; } = new();

    public List<DashboardBreakdownItemViewModel> PendingByShelter { get; set; } = new();

    public List<DashboardRecentUserViewModel> RecentUsers { get; set; } = new();

    public List<DashboardRecentPetViewModel> RecentPets { get; set; } = new();
}

public class DashboardSeriesPointViewModel
{
    public string Label { get; set; } = string.Empty;

    public int Value { get; set; }
}

public class DashboardBreakdownItemViewModel
{
    public string Label { get; set; } = string.Empty;

    public int Value { get; set; }

    public string? Meta { get; set; }
}

public class DashboardRecentUserViewModel
{
    public int AccountId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime RegisteredAt { get; set; }
}

public class DashboardRecentPetViewModel
{
    public int PetId { get; set; }

    public string PetName { get; set; } = string.Empty;

    public string PhotoUrl { get; set; } = string.Empty;

    public string ShelterName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

public class DashboardFilterOptionViewModel
{
    public int Id { get; set; }

    public string Label { get; set; } = string.Empty;
}
