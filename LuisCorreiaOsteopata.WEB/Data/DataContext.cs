using LuisCorreiaOsteopata.WEB.Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace LuisCorreiaOsteopata.WEB.Data;

public class DataContext : IdentityDbContext<User>
{
    public DbSet<Patient> Patients { get; set; }

    public DbSet<Staff> Staff { get; set; }

    public DbSet<Appointment> Appointments { get; set; }

    public DbSet<GoogleCalendar> GoogleCalendar { get; set; }

    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelbuilder)
    {
        base.OnModelCreating(modelbuilder);

        modelbuilder.Entity<User>()
            .HasIndex(u => u.Nif)
            .IsUnique();

        modelbuilder.Entity<Appointment>()
            .HasOne(a => a.Patient)
            .WithMany()  
            .HasForeignKey(a => a.PatientId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        modelbuilder.Entity<Appointment>()
            .HasOne(a => a.Staff)
            .WithMany()  
            .HasForeignKey(a => a.StaffId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
