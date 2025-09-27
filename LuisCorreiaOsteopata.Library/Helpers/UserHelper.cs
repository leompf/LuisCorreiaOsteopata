using LuisCorreiaOsteopata.Library.Data;
using LuisCorreiaOsteopata.Library.Data.Entities;
using Microsoft.AspNetCore.Identity;

namespace LuisCorreiaOsteopata.Library.Helpers;

public class UserHelper : IUserHelper
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UserHelper(UserManager<User> userManager, 
        SignInManager<User> signInManager, 
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
    }

    public async Task<IdentityResult> AddUserAsync(User user, string password)
    {
        return await _userManager.CreateAsync(user, password);
    }

    public async Task AddUserToRoleAsync(User user, string rolename)
    {
        await _userManager.AddToRoleAsync(user, rolename);
    }

    public async Task CheckRoleAsync(string roleName)
    {
        var roleExists = await _roleManager.RoleExistsAsync(roleName);

        if (!roleExists)
        {
            await _roleManager.CreateAsync(new IdentityRole
            {
                Name = roleName
            });
        }
    }

    public async Task<Patient> CreatePatientAsync(User user, string roleName)
    {
        var isInrole = await _userManager.IsInRoleAsync(user,  roleName);   
        if (!isInrole)
        {
            return null;
        }

        var patient = new Patient
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            User = user,
        };

        return patient;
    }

    public async Task<User> GetUserByEmailAsync(string email)
    {
        return await _userManager.FindByEmailAsync(email);
    }

    public async Task<User> GetUserByNifAsync(string nif)
    {
        var user = _userManager.Users.FirstOrDefault(u => u.Nif == nif);
        return user;
    }

    public async Task<bool> IsUserInRoleAsync(User user, string rolename)
    {
        return await _userManager.IsInRoleAsync(user, rolename);
    }

    public async Task<SignInResult> LoginAsync(string username, string password, bool rememberMe)
    {
        return await _signInManager.PasswordSignInAsync(
            username,
            password,
            rememberMe,
            false);
    }

    public async Task LogoutAsync()
    {
        await _signInManager.SignOutAsync();
    }

    public Task<IdentityResult> UpdateUserAsync(User user)
    {
        throw new NotImplementedException();
    }

}
