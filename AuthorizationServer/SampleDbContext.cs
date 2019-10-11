using Microsoft.EntityFrameworkCore;

namespace AuthorizationServer
{
  public class SampleDbContext : DbContext
  {
    private const string ConnectionString = "Server=docker; Port=5432; User Id=postgres; Password=; Database=sample;";

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
      base.OnConfiguring(options);
      options.UseNpgsql(ConnectionString);
      options.UseOpenIddict();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);

      modelBuilder.UseOpenIddict();
    }
  }
}