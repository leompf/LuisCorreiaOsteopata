using LuisCorreiaOsteopata.Library.Data;
using LuisCorreiaOsteopata.Library.Data.Entities;
using LuisCorreiaOsteopata.Library.Helpers;
using LuisCorreiaOsteopata.WEB.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LuisCorreiaOsteopata.WEB.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserHelper _userHelper;
        private readonly IPatientRepository _patientRepository;
        private readonly IStaffRepository _staffRepository;

        public AccountController(IUserHelper userHelper,
            IPatientRepository patientRepository,
            IStaffRepository staffRepository)
        {
            _userHelper = userHelper;
            _patientRepository = patientRepository;
            _staffRepository = staffRepository;
        }

        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                return View();
            }

            return RedirectToAction("Login", "Account");
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
                        PhoneNumber = model.PhoneNumber,
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
                        return RedirectToAction("Index", "Account");
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

                    return this.RedirectToAction("Index", "Account");
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

        [Authorize(Roles = "Administrador")]
        public IActionResult AddStaff()
        {
            return View();
        }

        [Authorize(Roles = "Administrador")]
        [HttpPost]
        public async Task<IActionResult> AddStaff(AddNewStaffViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userHelper.GetUserByEmailAsync(model.Email);
                var password =  _userHelper.GenerateRandomPassword();

                if (user == null)
                {
                    user = new User
                    {
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        Email = model.Email,
                        UserName = model.Email,
                        PhoneNumber = model.PhoneNumber,
                        Nif = model.Nif,
                    };

                    var result = await _userHelper.AddUserAsync(user, password);

                    await _userHelper.AddUserToRoleAsync(user, "Colaborador");

                    var role = await _userHelper.IsUserInRoleAsync(user, "Colaborador");
                    if (!role)
                    {
                        await _userHelper.AddUserToRoleAsync(user, "Colaborador");
                    }

                    var staff = await _staffRepository.CreatStaffAsync(user, "Colaborador");
                    if (staff != null)
                    {
                        await _staffRepository.CreateAsync(staff);
                    }

                    if (result != IdentityResult.Success)
                    {
                        ModelState.AddModelError(string.Empty, "Houve um erro ao criar o utilizador.");
                        return View(model);
                    }

                    TempData["SuccessMessage"] = "Colaborador criado com sucesso!";
                    return RedirectToAction("Index", "Account");
                }
            }

            ModelState.AddModelError(string.Empty, "Já existe um utilizador com esse email.");
            return View(model);
        }
    }
}
