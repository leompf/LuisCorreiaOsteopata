using LuisCorreiaOsteopata.Library.Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace LuisCorreiaOsteopata.Library.Data;

public class DataContext : IdentityDbContext<User>
{
    public DbSet<Patient> Patients { get; set; }

    public DbSet<Staff> Staff {  get; set; }

    public DbSet<Appointment> Appointments { get; set; }

    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelbuilder)
    {
        base.OnModelCreating(modelbuilder);

        modelbuilder.Entity<Appointment>()
             .HasOne(a => a.Patient)
             .WithMany()
             .OnDelete(DeleteBehavior.Restrict);

        modelbuilder.Entity<Appointment>()
             .HasOne(a => a.Staff)
             .WithMany()
             .OnDelete(DeleteBehavior.Restrict);
    }
}
