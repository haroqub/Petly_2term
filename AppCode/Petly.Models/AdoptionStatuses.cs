namespace Petly.Models;

public static class AdoptionStatuses
{
    public const string Pending = "Очікує";
    public const string Approved = "Схвалено";
    public const string Rejected = "Відхилено";
    public const string AutoRejected = "Автоматично відхилено";

    public static string Normalize(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return Pending;
        }

        return status.Trim() switch
        {
            "Pending" => Pending,
            "Approved" => Approved,
            "Rejected" => Rejected,
            "Auto-rejected" => AutoRejected,
            "Авто-відхилено" => AutoRejected,
            _ => status.Trim()
        };
    }
}
