using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Petly.Models;

[Table("analytics_access_log")]
public class AnalyticsAccessLog
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("adminId")]
    public int AdminId { get; set; }

    [Column("periodDays")]
    public int PeriodDays { get; set; }

    [Column("shelterId")]
    public int? ShelterId { get; set; }

    [Column("accessTime")]
    public DateTime AccessTime { get; set; }
}
