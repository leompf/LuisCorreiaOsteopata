using LuisCorreiaOsteopata.WEB.Data.Entities;
using LuisCorreiaOsteopata.WEB.Helpers;
using Microsoft.AspNetCore.Identity;

namespace LuisCorreiaOsteopata.WEB.Data;

public class SeedDB
{
    private readonly DataContext _context;
    private readonly IUserHelper _userHelper;

    public SeedDB(DataContext context, IUserHelper userHelper)
    {
        _context = context;
        _userHelper = userHelper;
    }

    public async Task SeedAsync()
    {
        await _context.Database.EnsureCreatedAsync();

        await _userHelper.CheckRoleAsync("Utente");
        await _userHelper.CheckRoleAsync("Colaborador");
        await _userHelper.CheckRoleAsync("Administrador");

        var adminUser = await _userHelper.GetUserByEmailAsync("leonardex97@hotmail.com");

        if (adminUser == null)
        {
            adminUser = new User
            {
                Names = "Leonardo Miguel Petricciuolo",
                LastName = "Fraqueiro",
                Email = "leonardex97@hotmail.com",
                UserName = "leonardex97@hotmail.com",
                EmailConfirmed = true
            };

            var result = await _userHelper.AddUserAsync(adminUser, "123456");

            if (result != IdentityResult.Success)
            {
                throw new InvalidOperationException("Não foi possível criar o utitlizador no Seeder");
            }

            await _userHelper.AddUserToRoleAsync(adminUser, "Administrador");

            var isInRole = await _userHelper.IsUserInRoleAsync(adminUser, "Administrador");
            if (!isInRole)
            {
                await _userHelper.AddUserToRoleAsync(adminUser, "Administrador");
            }
        }

        var patientUser = await _userHelper.GetUserByEmailAsync("lmpfraqueiro@gmail.com");

        if (patientUser == null)
        {
            patientUser = new User
            {
                Names = "Leonardo Miguel Petricciuolo",
                LastName = "Fraqueiro",
                Email = "lmpfraqueiro@gmail.com",
                UserName = "lmpfraqueiro@gmail.com",
                EmailConfirmed = true,
                Nif = "255408650",
                PhoneNumber = "+351965571434"
            };

            var result = await _userHelper.AddUserAsync(patientUser, "123456");

            if (result != IdentityResult.Success)
            {
                throw new InvalidOperationException("Não foi possível criar o utitlizador no Seeder");
            }

            await _userHelper.AddUserToRoleAsync(patientUser, "Utente");

            var isInRole = await _userHelper.IsUserInRoleAsync(patientUser, "Utente");
            if (!isInRole)
            {
                await _userHelper.AddUserToRoleAsync(patientUser, "Utente");
            }
        }

        var staffUser = await _userHelper.GetUserByEmailAsync("leozao997@hotmail.com");

        if (staffUser == null)
        {
            staffUser = new User
            {
                Names = "Luis Miguel",
                LastName = "Correia",
                Email = "leozao997@hotmail.com",
                UserName = "leozao997@hotmail.com",
                EmailConfirmed = true,
                Nif = "231029012",
                PhoneNumber = "+351932458961"
            };

            var result = await _userHelper.AddUserAsync(staffUser, "123456");

            if (result != IdentityResult.Success)
            {
                throw new InvalidOperationException("Não foi possível criar o utitlizador no Seeder");
            }

            await _userHelper.AddUserToRoleAsync(staffUser, "Colaborador");

            var isInRole = await _userHelper.IsUserInRoleAsync(staffUser, "Colaborador");
            if (!isInRole)
            {
                await _userHelper.AddUserToRoleAsync(staffUser, "Colaborador");
            }
        }

        if (!_context.Staff.Any())
        {
            _context.Staff.Add(new Staff
            {
                Names = staffUser.Names,
                LastName = staffUser.LastName,
                Email = staffUser.Email,
                Phone = staffUser.PhoneNumber,
                Nif = staffUser.Nif,
                User = staffUser
            });
        }

        if (!_context.Patients.Any())
        {
            _context.Patients.Add(new Patient
            {
                Names = patientUser.Names,
                LastName = patientUser.LastName,
                Email = patientUser.Email,
                Phone = patientUser.PhoneNumber,
                Nif = patientUser.Nif,
                User = patientUser
            });
        }

        await _context.SaveChangesAsync();
    }
}
