using LuisCorreiaOsteopata.Library.Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LuisCorreiaOsteopata.Library.Data;

public class DataContext : IdentityDbContext<User>
{
    public DbSet<Patient> Patients { get; set; }

    public DbSet<Staff> Staff {  get; set; }

    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
    }
}
