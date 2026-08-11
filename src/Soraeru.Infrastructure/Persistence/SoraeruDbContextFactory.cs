using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Soraeru.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for <c>dotnet ef migrations</c>.
/// </summary>
public sealed class SoraeruDbContextFactory : IDesignTimeDbContextFactory<SoraeruDbContext>
{
    public SoraeruDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SoraeruDbContext>()
            .UseSqlite("Data Source=soraeru.db")
            .Options;

        return new SoraeruDbContext(options);
    }
}
