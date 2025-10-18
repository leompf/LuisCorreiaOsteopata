using LuisCorreiaOsteopata.WEB.Data.Entities;
using LuisCorreiaOsteopata.WEB.Helpers;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace LuisCorreiaOsteopata.WEB.Data;

public class DataContext : IdentityDbContext<User>
{
    public DbSet<Patient> Patients { get; set; }

    public DbSet<Staff> Staff { get; set; }

    public DbSet<Appointment> Appointments { get; set; }

    public DbSet<GoogleCalendar> GoogleCalendar { get; set; }

    public DbSet<Product> Products { get; set; }

    public DbSet<Order> Orders { get; set; }

    public DbSet<OrderDetail> OrderDetails { get; set; }

    public DbSet<OrderDetailTemp> OrderDetailsTemp { get; set; }

    public DbSet<BillingDetail> BillingDetails { get; set; }

    public DbSet<Invoice> Invoices { get; set; }

    public DbSet<Payment> Payments { get; set; }

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

        modelbuilder.HasSequence<int>("OrderNumberSequence", "dbo")
            .StartsAt(1)
            .IncrementsBy(1);
    }
}
