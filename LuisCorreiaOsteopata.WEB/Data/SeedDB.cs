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

        var user = await _userHelper.GetUserByEmailAsync("lmfraqueiro@gmail.com");

        if (user == null)
        {
            user = new User
            {
                FirstName = "Leonardo",
                LastName = "Fraqueiro",
                Email = "lmfraqueiro@gmail.com",
                UserName = "lmfraqueiro@gmail.com",
                EmailConfirmed = true
            };

            var result = await _userHelper.AddUserAsync(user, "123456");

            if (result != IdentityResult.Success)
            {
                throw new InvalidOperationException("Não foi possível criar o utitlizador no Seeder");
            }

            await _userHelper.AddUserToRoleAsync(user, "Administrador");

            var isInRole = await _userHelper.IsUserInRoleAsync(user, "Administrador");
            if (!isInRole)
            {
                await _userHelper.AddUserToRoleAsync(user, "Administrador");
            }
        }
    }
}
