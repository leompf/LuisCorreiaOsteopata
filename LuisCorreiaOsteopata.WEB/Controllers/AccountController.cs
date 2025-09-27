using LuisCorreiaOsteopata.Library.Data;
using LuisCorreiaOsteopata.Library.Data.Entities;
using LuisCorreiaOsteopata.Library.Helpers;
using LuisCorreiaOsteopata.WEB.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace LuisCorreiaOsteopata.WEB.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserHelper _userHelper;
        private readonly IPatientRepository _patientRepository;

        public AccountController(IUserHelper userHelper,
            IPatientRepository patientRepository)
        {
            _userHelper = userHelper;
            _patientRepository = patientRepository;
        }

        public IActionResult SignUp()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> SignUp(SignUpViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userHelper.GetUserByEmailAsync(model.Email);

                if (user == null)
                {
                    user = new User
                    {
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        Email = model.Email,
                        UserName = model.Email,
                    };

                    var result = await _userHelper.AddUserAsync(user, model.Password);

                    await _userHelper.AddUserToRoleAsync(user, "Utente");

                    var role = await _userHelper.IsUserInRoleAsync(user, "Utente");
                    if (!role)
                    {
                        await _userHelper.AddUserToRoleAsync(user, "Utente");
                    }

                    var patient = await _patientRepository.CreatePatientAsync(user, "Utente");
                    if (patient != null)
                    {
                        await _patientRepository.CreateAsync(patient);
                    }

                    if (result != IdentityResult.Success)
                    {
                        ModelState.AddModelError(string.Empty, "Houve um erro ao criar o utilizador.");
                        return View(model);
                    }

                    var result2 = await _userHelper.LoginAsync(model.Email, model.Password, false);
                    if (result2.Succeeded)
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }
            }

            ModelState.AddModelError(string.Empty, "Já existe um utilizador com esse email.");
            return View(model);
        }

        public IActionResult Login()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _userHelper.LoginAsync(model.Username, model.Password, model.RememberMe);
                if (result.Succeeded)
                {
                    if (this.Request.Query.Keys.Contains("ReturnUrl"))
                    {
                        return Redirect(this.Request.Query["ReturnUrl"].First());
                    }

                    return this.RedirectToAction("Index", "Home");
                }
            }

            this.ModelState.AddModelError(string.Empty, "Erro ao fazer login");
            return View(model);
        }


        public async Task<IActionResult> Logout()
        {
            await _userHelper.LogoutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
