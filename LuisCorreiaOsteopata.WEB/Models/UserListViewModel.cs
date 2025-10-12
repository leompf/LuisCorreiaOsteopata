using Microsoft.AspNetCore.Mvc.Rendering;

namespace LuisCorreiaOsteopata.WEB.Models;

public class UserListViewModel
{
    public string? NameFilter { get; set; }

    public string? NifFilter { get; set; }

    public string? EmailFilter { get; set; }

    public string? PhoneFilter { get; set; }

    public IEnumerable<UserViewModel> Users { get; set; }
    public IEnumerable<SelectListItem> Roles { get; set; }
}
