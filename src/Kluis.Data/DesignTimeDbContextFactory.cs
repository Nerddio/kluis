using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Kluis.Data;

/// <summary>
/// Used only by the `dotnet ef` tool to build the model when generating migrations.
/// It never opens a database, so it needs no passphrase.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<KluisDbContext>
{
    public KluisDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<KluisDbContext>().UseSqlite("Data Source=design-time-only.db").Options);
}
