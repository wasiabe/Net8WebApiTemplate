using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<WebApiRequestLog> WebApiRequestLogs => Set<WebApiRequestLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<WebApiRequestLog>(e =>
        {
            e.ToTable("WebApiRequestLog", "dbo");
            e.HasKey(x => x.Id);

            e.Property(x => x.TraceId).HasMaxLength(64);
            e.Property(x => x.RequestHeader).HasColumnType("nvarchar(max)");
            e.Property(x => x.RequestBody).HasColumnType("nvarchar(max)");
            e.Property(x => x.ResponseBody).HasColumnType("nvarchar(max)");
        });
    }
}
