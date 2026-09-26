using CreditWorks.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace CreditWorks.Web.Data;

public sealed class CreditWorksDbContext(DbContextOptions<CreditWorksDbContext> options) : DbContext(options)
{
    public DbSet<Manufacturer> Manufacturers => Set<Manufacturer>();
    public DbSet<VehicleCategory> Categories => Set<VehicleCategory>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Manufacturer>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(80).IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<VehicleCategory>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(60).IsRequired();
            entity.Property(x => x.Icon).HasMaxLength(32).IsRequired();
            entity.Property(x => x.MinimumWeight).HasPrecision(10, 2);
            entity.Property(x => x.MaximumWeight).HasPrecision(10, 2);
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.Property(x => x.OwnerName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.WeightKg).HasPrecision(10, 2);
            entity.HasOne(x => x.Manufacturer).WithMany(x => x.Vehicles)
                .HasForeignKey(x => x.ManufacturerId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
