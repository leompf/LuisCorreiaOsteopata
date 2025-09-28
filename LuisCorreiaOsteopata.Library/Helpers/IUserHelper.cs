using LuisCorreiaOsteopata.Library.Data.Entities;
using Microsoft.AspNetCore.Identity;

namespace LuisCorreiaOsteopata.Library.Helpers;

public interface IUserHelper
{
    Task<User> GetUserByEmailAsync(string email);

    Task<User> GetUserByNifAsync(string nif);

    Task<IdentityResult> AddUserAsync(User user, string password);

    Task<IdentityResult> UpdateUserAsync(User user);

    Task<SignInResult> LoginAsync(string username, string password, bool rememberMe);

    Task LogoutAsync();

    Task CheckRoleAsync(string roleName);

    Task AddUserToRoleAsync(User user, string rolename);

    Task<bool> IsUserInRoleAsync(User user, string rolename);

    string GenerateRandomPassword(PasswordOptions options = null);
}
