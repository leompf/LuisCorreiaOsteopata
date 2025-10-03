using LuisCorreiaOsteopata.WEB.Data.Entities;
using Microsoft.AspNetCore.Identity;

namespace LuisCorreiaOsteopata.WEB.Helpers;

public interface IUserHelper
{
    Task<User> GetUserByEmailAsync(string email);

    Task<User> GetUserByNifAsync(string nif);

    Task<User> GetUserByIdAsync(string id);

    Task<User> GetCurrentUserAsync();

    Task<IdentityResult> AddUserAsync(User user, string password);

    Task<string> GenerateEmailConfirmationTokenAsync(User user);

    Task<IdentityResult> ConfirmEmailAsync(User user, string token);

    Task<bool> IsEmailConfirmedAsync(User user);

    Task<IdentityResult> UpdateUserAsync(User user);

    Task<SignInResult> LoginAsync(string username, string password, bool rememberMe);

    Task LogoutAsync();

    Task CheckRoleAsync(string roleName);

    Task AddUserToRoleAsync(User user, string rolename);

    Task<bool> IsUserInRoleAsync(User user, string rolename);

    Task<string> GetUserRoleAsync(User user);

    string GenerateRandomPassword(PasswordOptions options = null);
}
