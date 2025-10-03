using LuisCorreiaOsteopata.WEB.Data.Entities;
using LuisCorreiaOsteopata.WEB.Data;
using LuisCorreiaOsteopata.WEB.Helpers;
using LuisCorreiaOsteopata.WEB.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace LuisCorreiaOsteopata.WEB.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserHelper _userHelper;
        private readonly IPatientRepository _patientRepository;
        private readonly IStaffRepository _staffRepository;
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IEmailSender _emailSender;

        public AccountController(IUserHelper userHelper,
            IPatientRepository patientRepository,
            IStaffRepository staffRepository,
            IAppointmentRepository appointmentRepository,
            IEmailSender emailSender)
        {
            _userHelper = userHelper;
            _patientRepository = patientRepository;
            _staffRepository = staffRepository;
            _appointmentRepository = appointmentRepository;
            _emailSender = emailSender;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                var user = await _userHelper.GetCurrentUserAsync();

                var appointments = await _appointmentRepository.GetAppointmentsByUserAsync(user);

                var events = appointments.Select(a => new
                {
                    id = a.Id,
                    title = $"Consulta com {a.Staff.FullName}",
                    start = a.StartTime.ToString("s"),  
                    end = a.EndTime.ToString("s"),      
                    allDay = false
                }).ToList();

                ViewBag.Appointments = events; 

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
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userHelper.GetUserByEmailAsync(model.Email);
            if (user != null)
            {
                ModelState.AddModelError(string.Empty, "Já existe um utilizador com esse email.");
                return View(model);
            }

            user = new User
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                UserName = model.Email,
                PhoneNumber = model.PhoneNumber,
            };

            var result = await _userHelper.AddUserAsync(user, model.Password);
            if (result != IdentityResult.Success)
            {
                ModelState.AddModelError(string.Empty, "Houve um erro ao criar o utilizador.");
                return View(model);
            }

            await _userHelper.AddUserToRoleAsync(user, "Utente");

            var patient = await _patientRepository.CreatePatientAsync(user, "Utente");
            if (patient != null)
            {
                await _patientRepository.CreateAsync(patient);
            }

            var token = await _userHelper.GenerateEmailConfirmationTokenAsync(user);
            var confirmationLink = Url.Action("ConfirmEmail", "Account", new
            {
                userId = user.Id,
                token = token
            }, protocol: HttpContext.Request.Scheme);

            var mailMessage = $"Viva, {user.FirstName}!<br/><br/>Bem-vindo à plataforma Luis Correia, Osteopata. Aqui é o lugar ideal para gestionares todas as tuas consultas bem como estar a par de todas as novidades acerca do meu trabalho.<br/>" +
                $"Para aceder à plataforma, primeiro necessitas de confirmar a tua conta, clicando aqui <a href='{confirmationLink}'>Confirmar Conta</a>";

            await _emailSender.SendEmailAsync(user.Email, "Ativação de Conta - Luis Correia, Osteopata", mailMessage);

            TempData["SuccessMessage"] = "Registo realizado! Por favor, verifica o teu email para confirmar a conta.";
            return RedirectToAction("Login");

            //if (ModelState.IsValid)
            //{
            //    var user = await _userHelper.GetUserByEmailAsync(model.Email);

            //    if (user == null)
            //    {
            //        user = new User
            //        {
            //            FirstName = model.FirstName,
            //            LastName = model.LastName,
            //            Email = model.Email,
            //            UserName = model.Email,
            //            PhoneNumber = model.PhoneNumber,
            //        };

            //        var result = await _userHelper.AddUserAsync(user, model.Password);

            //        await _userHelper.AddUserToRoleAsync(user, "Utente");

            //        var role = await _userHelper.IsUserInRoleAsync(user, "Utente");
            //        if (!role)
            //        {
            //            await _userHelper.AddUserToRoleAsync(user, "Utente");
            //        }

            //        var patient = await _patientRepository.CreatePatientAsync(user, "Utente");
            //        if (patient != null)
            //        {
            //            await _patientRepository.CreateAsync(patient);
            //        }

            //        if (result != IdentityResult.Success)
            //        {
            //            ModelState.AddModelError(string.Empty, "Houve um erro ao criar o utilizador.");
            //            return View(model);
            //        }

            //        var result2 = await _userHelper.LoginAsync(model.Email, model.Password, false);
            //        if (result2.Succeeded)
            //        {
            //            return RedirectToAction("Index", "Account");
            //        }
            //    }
            //}

            //ModelState.AddModelError(string.Empty, "Já existe um utilizador com esse email.");
            //return View(model);
        }

        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await _userHelper.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userHelper.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Email confirmado com sucesso! Podes usar a conta.";
                return RedirectToAction("Login");
            }

            TempData["ErrorMessage"] = "Erro ao confirmar email.";
            return RedirectToAction("Index", "Home");
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
                    var user = await _userHelper.GetUserByEmailAsync(model.Username);
                    if (user != null && !await _userHelper.IsEmailConfirmedAsync(user))
                    {
                        ModelState.AddModelError(string.Empty, "Por favor confirma o teu email antes de fazer login.");
                        return View(model);                      
                    }

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
                var password = _userHelper.GenerateRandomPassword();

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
