using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Models;

namespace PropertyManagement.Data;

public class PropertyManagementDbContext(DbContextOptions<PropertyManagementDbContext> options) : IdentityDbContext<AspNetUser>(options)
{
    public DbSet<Property> Properties { get; set; }
    public DbSet<Unit> Units { get; set; }
    public DbSet<UnitType> UnitTypes { get; set; }
    public DbSet<RentalApplication> RentalApplications { get; set; }
    public DbSet<Residence> Residences { get; set; }
    public DbSet<Lease> Leases { get; set; }
    public DbSet<ApplicationStatusHistory> ApplicationStatusHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Unit>()
            .HasOne(u => u.Property)
            .WithMany(p => p.Units)
            .HasForeignKey(u => u.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Unit>()
            .HasOne(u => u.UnitType)
            .WithMany(ut => ut.Units)
            .HasForeignKey(u => u.UnitTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Unit>()
            .HasIndex(u => new { u.PropertyId, u.UnitNumber })
            .IsUnique();

        modelBuilder.Entity<RentalApplication>()
            .HasOne(a => a.Applicant)
            .WithMany()
            .HasForeignKey(a => a.ApplicantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RentalApplication>()
            .HasOne(a => a.Unit)
            .WithMany()
            .HasForeignKey(a => a.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Residence>()
            .HasOne(r => r.RentalApplication)
            .WithMany(a => a.Residences)
            .HasForeignKey(r => r.RentalApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ApplicationStatusHistory>()
            .HasOne(h => h.RentalApplication)
            .WithMany(a => a.StatusHistory)
            .HasForeignKey(h => h.RentalApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ApplicationStatusHistory>()
            .HasOne(h => h.ChangedByUser)
            .WithMany()
            .HasForeignKey(h => h.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Lease>()
            .HasOne(l => l.RentalApplication)
            .WithOne()
            .HasForeignKey<Lease>(l => l.RentalApplicationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Lease>()
            .HasOne(l => l.Unit)
            .WithMany()
            .HasForeignKey(l => l.UnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}