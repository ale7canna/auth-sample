using Microsoft.EntityFrameworkCore;

namespace AuthorizationServer
{
  public class SampleDbContext : DbContext
  {
    public SampleDbContext(DbContextOptions options) : base(options)
    {

    }
  }
}