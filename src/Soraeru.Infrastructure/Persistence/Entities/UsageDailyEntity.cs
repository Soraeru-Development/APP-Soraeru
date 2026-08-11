namespace Soraeru.Infrastructure.Persistence.Entities;

public sealed class UsageDailyEntity
{
    public Guid UserId { get; set; }

    public DateOnly UsageDate { get; set; }

    public int AnalyzeCount { get; set; }
}
