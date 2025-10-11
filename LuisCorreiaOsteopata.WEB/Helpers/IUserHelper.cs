using LuisCorreiaOsteopata.WEB.Data.Entities;
using Microsoft.AspNetCore.Identity;

namespace LuisCorreiaOsteopata.WEB.Helpers;

public interface IUserHelper
{
    #region Find Users
    Task<User> GetUserByEmailAsync(string email);
    Task<User> GetUserByNifAsync(string nif);
    Task<User> GetUserByIdAsync(string id);
    Task<User> GetCurrentUserAsync();
    Task<List<User>> GetAllUsersAsync();
    #endregion

    #region Authentication Users
    Task<SignInResult> LoginAsync(string username, string password, bool rememberMe);
    Task LogoutAsync();
    Task<string> GenerateEmailConfirmationTokenAsync(User user);
    Task StoreUserTokenAsync(User user, string loginProvider, string name, string value);
    Task<string> GetUserTokenAsync(User user, string loginProvider, string tokenName);
    Task<IdentityResult> ConfirmEmailAsync(User user, string token);
    #endregion

    #region CRUD Users
    Task<IdentityResult> AddUserAsync(User user, string password);
    Task<IdentityResult> UpdateUserAsync(User user);

    #endregion

    #region Roles
    Task CheckRoleAsync(string roleName);
    Task AddUserToRoleAsync(User user, string rolename);
    Task<bool> IsUserInRoleAsync(User user, string rolename);
    Task<string> GetUserRoleAsync(User user);
    #endregion

    #region User Helpers   
    string GenerateRandomPassword(PasswordOptions options = null);
    #endregion
}
