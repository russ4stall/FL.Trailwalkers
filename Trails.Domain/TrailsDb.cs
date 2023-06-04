using Microsoft.EntityFrameworkCore;

namespace Trails.Domain;

public class TrailsDb : DbContext
{
    public TrailsDb(DbContextOptions<TrailsDb> options) : base(options)
    { }

    public DbSet<Trail> Trails => Set<Trail>();
}