using Kluis.Core.Accounts;
using Microsoft.EntityFrameworkCore;

namespace Kluis.Data;

public sealed class KluisDbContext(DbContextOptions<KluisDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(account =>
        {
            account.ToTable("accounts");
            account.Property(a => a.Name).HasMaxLength(100);
            account.Property(a => a.Currency).HasMaxLength(3).IsFixedLength();
        });
    }
}
