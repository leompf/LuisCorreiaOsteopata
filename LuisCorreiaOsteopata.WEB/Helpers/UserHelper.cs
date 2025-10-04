using LuisCorreiaOsteopata.WEB.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace LuisCorreiaOsteopata.WEB.Helpers;

public class UserHelper : IUserHelper
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserHelper(UserManager<User> userManager,
        SignInManager<User> signInManager,
        RoleManager<IdentityRole> roleManager,
        IHttpContextAccessor httpContextAccessor)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _httpContextAccessor = httpContextAccessor;
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

    public async Task<IdentityResult> ConfirmEmailAsync(User user, string token)
    {
        return await _userManager.ConfirmEmailAsync(user, token);
    }

    public async Task<string> GenerateEmailConfirmationTokenAsync(User user)
    {
        return await _userManager.GenerateEmailConfirmationTokenAsync(user);
    }

    public string GenerateRandomPassword(PasswordOptions options = null)
    {
        if (options == null) options = new PasswordOptions()
        {
            RequiredLength = 6,
            RequiredUniqueChars = 4,
            RequireDigit = true,
            RequireLowercase = true,
            RequireNonAlphanumeric = true,
            RequireUppercase = true
        };

        string[] randomChars = new[] {
            "ABCDEFGHJKLMNOPQRSTUVWXYZ",    
            "abcdefghijkmnopqrstuvwxyz",    
            "0123456789",                   
            "!@$?_-"                        
        };

        Random rand = new Random(Environment.TickCount);
        List<char> chars = new List<char>();

        if (options.RequireUppercase)
            chars.Insert(rand.Next(0, chars.Count),
                randomChars[0][rand.Next(0, randomChars[0].Length)]);

        if (options.RequireLowercase)
            chars.Insert(rand.Next(0, chars.Count),
                randomChars[1][rand.Next(0, randomChars[1].Length)]);

        if (options.RequireDigit)
            chars.Insert(rand.Next(0, chars.Count),
                randomChars[2][rand.Next(0, randomChars[2].Length)]);

        if (options.RequireNonAlphanumeric)
            chars.Insert(rand.Next(0, chars.Count),
                randomChars[3][rand.Next(0, randomChars[3].Length)]);

        for (int i = chars.Count; i < options.RequiredLength
            || chars.Distinct().Count() < options.RequiredUniqueChars; i++)
        {
            string rcs = randomChars[rand.Next(0, randomChars.Length)];
            chars.Insert(rand.Next(0, chars.Count),
                rcs[rand.Next(0, rcs.Length)]);
        }

        return new string(chars.ToArray());
    }

    public async Task<User> GetCurrentUserAsync()
    {
        var email = _httpContextAccessor.HttpContext.User.Identity.Name;

        if (string.IsNullOrEmpty(email))
        {
            return null;
        }

        return await GetUserByEmailAsync(email);
    }

    public async Task<User> GetUserByEmailAsync(string email)
    {
        return await _userManager.FindByEmailAsync(email);
    }

    public async Task<User> GetUserByIdAsync(string id)
    {
        return await _userManager.FindByIdAsync(id);
    }

    public async Task<User> GetUserByNifAsync(string nif)
    {
        var user = _userManager.Users.FirstOrDefault(u => u.Nif == nif);
        return user;
    }

    public async Task<string> GetUserRoleAsync(User user)
    {
        var role = await _userManager.GetRolesAsync(user);
        return role.FirstOrDefault();
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
            isPersistent: rememberMe,
            lockoutOnFailure: false
        );
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
