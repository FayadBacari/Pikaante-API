using Microsoft.EntityFrameworkCore;
using Pikaante_Backend.Models;

namespace Pikaante_Backend.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Sauce> Sauces { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // La base refuse toute sauce dont le piquant n'est pas entre 1 et 10.
        builder.Entity<Sauce>().ToTable(table =>
            table.HasCheckConstraint("CK_Sauce_Heat", "\"Heat\" BETWEEN 1 AND 10"));
    }
}