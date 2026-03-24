using BazaarOverlay.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BazaarOverlay.Tests.Infrastructure;

public static class TestDbContextFactory
{
    public static BazaarDbContext Create()
    {
        var options = new DbContextOptionsBuilder<BazaarDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        var context = new BazaarDbContext(options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();
        return context;
    }
}
