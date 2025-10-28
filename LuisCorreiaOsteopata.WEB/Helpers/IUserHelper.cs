using LuisCorreiaOsteopata.WEB.Data.Entities;
using LuisCorreiaOsteopata.WEB.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LuisCorreiaOsteopata.WEB.Helpers;

public interface IUserHelper
{
    #region CRUD Users
    Task<User> GetUserByEmailAsync(string email);
    Task<User> GetUserByNifAsync(string nif);
    Task<User> GetUserByIdAsync(string id);
    Task<User> GetCurrentUserAsync();
    Task<List<User>> GetAllUsersAsync();
    Task<IdentityResult> AddUserAsync(User user, string password);
    Task<IdentityResult> UpdateUserAsync(User user);
    Task<IdentityResult> ChangePasswordAsync(User user, string currentPassword, string newPassword);
    Task<IdentityResult> ResetPasswordAsync(User user, string token, string newPassword);
    #endregion

    #region CRUD Roles
    Task CheckRoleAsync(string roleName);
    Task AddUserToRoleAsync(User user, string rolename);
    Task<bool> IsUserInRoleAsync(User user, string rolename);
    Task<string> GetUserRoleAsync(User user);
    IEnumerable<SelectListItem> GetAllRolesAsync();
    #endregion

    #region 2FA Authentication
    Task<string?> GetAuthenticatorKeyAsync(User user);
    Task ResetAuthenticatorKeyAsync(User user);
    Task<bool> VerifyTwoFactorTokenAsync(User user, string code);
    Task SetTwoFactorEnabledAsync(User user, bool enabled);
    #endregion

    #region Login
    Task<bool> CheckPasswordAsync(User user, string password);
    Task<SignInResult> LoginAsync(string username, string password, bool rememberMe);
    Task LogoutAsync();
    #endregion

    #region External
    Task StoreUserTokenAsync(User user, string loginProvider, string name, string value);
    Task<string?> GetUserTokenAsync(User user, string loginProvider, string tokenName);
    Task<IdentityResult> AddExternalLoginAsync(User user, ExternalLoginInfo info);
    Task<UserLoginInfo?> GetExternalLoginAsync(User user, string loginProvider);
    #endregion

    #region Helper Methods
    string GenerateRandomPassword(PasswordOptions options);
    List<UserViewModel> SortUsers(IEnumerable<UserViewModel> users, string? sortBy, bool sortDescending);
    Task<string> GenerateEmailConfirmationTokenAsync(User user);
    Task<string> GeneratePasswordResetTokenAsync(User user);
    Task<IdentityResult> ConfirmEmailAsync(User user, string token);
    List<UserViewModel> FilterUsers(IEnumerable<UserViewModel> users, string? nameFilter, string? emailFilter, string? phoneFilter, string? nifFilter);
    #endregion
}
