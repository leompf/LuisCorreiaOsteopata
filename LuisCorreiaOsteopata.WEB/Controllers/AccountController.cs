using LuisCorreiaOsteopata.WEB.Data;
using LuisCorreiaOsteopata.WEB.Data.Entities;
using LuisCorreiaOsteopata.WEB.Helpers;
using LuisCorreiaOsteopata.WEB.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LuisCorreiaOsteopata.WEB.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserHelper _userHelper;
        private readonly SignInManager<User> _signInManager;
        private readonly IPatientRepository _patientRepository;
        private readonly IStaffRepository _staffRepository;
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IEmailSender _emailSender;

        public AccountController(IUserHelper userHelper,
            SignInManager<User> signInManager,
            IPatientRepository patientRepository,
            IStaffRepository staffRepository,
            IAppointmentRepository appointmentRepository,
            IEmailSender emailSender)
        {
            _userHelper = userHelper;
            _signInManager = signInManager;
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
                Names = model.Names,
                LastName = model.LastName,
                Email = model.Email,
                UserName = model.Email,
                PhoneNumber = model.PhoneNumber,
                Birthdate = model.Birthdate,
                Nif = model.Nif,
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

            var mailMessage = $"Viva, {user.Names.Split(' ')[0]}!<br/><br/>Bem-vindo à plataforma Luis Correia, Osteopata. Aqui é o lugar ideal para gestionares todas as tuas consultas bem como estar a par de todas as novidades acerca do meu trabalho.<br/>" +
                $"Para aceder à plataforma, primeiro necessitas de confirmar a tua conta, clicando aqui <a href='{confirmationLink}'>Confirmar Conta</a>";

            await _emailSender.SendEmailAsync(user.Email, "Ativação de Conta - Luis Correia, Osteopata", mailMessage);

            ViewBag.SignUpMessage = "Registo realizado! Por favor, verifica o teu email para confirmares a conta.";

            return View();
        }

        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null)
            {
                return View("ConfirmEmailFailure");
            }

            var user = await _userHelper.GetUserByIdAsync(userId);
            if (user == null)
            {
                return View("ConfirmEmailFailure");
            }

            var result = await _userHelper.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                return View("ConfirmEmailSuccess");
            }

            return View("ConfirmEmailFailure");
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
                else if (result.IsNotAllowed)
                {
                    ModelState.AddModelError(string.Empty, "Por favor confirma o teu email antes de fazer login.");
                    return View(model);
                }
            }

            this.ModelState.AddModelError(string.Empty, "Erro ao fazer login");
            return View(model);
        }


        public IActionResult LoginGoogle()
        {
            var redirectUrl = Url.Action("Response", "Account");
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(GoogleDefaults.AuthenticationScheme, redirectUrl);
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }


        public async Task<IActionResult> Response()
        {
            var loginInfo = await _signInManager.GetExternalLoginInfoAsync();
            if (loginInfo == null)
                return RedirectToAction("Login");

            var result = await _signInManager.ExternalLoginSignInAsync(
                loginInfo.LoginProvider,
                loginInfo.ProviderKey,
                isPersistent: true,
                bypassTwoFactor: true
            );

            if (result.Succeeded)
                return RedirectToAction("Index", "Account");

            var claims = loginInfo.Principal.Identities.FirstOrDefault()?.Claims;
            var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            var googleId = claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            var user = await _userHelper.GetUserByEmailAsync(email);
            if (user == null)
            {
                user = new User
                {
                    Email = email,
                    UserName = email,
                    Names = name.Split(' ')[0],
                    LastName = name.Contains(" ") ? name.Substring(name.IndexOf(" ") + 1) : "",
                    EmailConfirmed = true
                };

                await _userHelper.AddUserAsync(user, Guid.NewGuid().ToString());
                await _userHelper.AddUserToRoleAsync(user, "Utente");

                var patient = await _patientRepository.CreatePatientAsync(user, "Utente");
                if (patient != null)
                {
                    await _patientRepository.CreateAsync(patient);
                }
            }

            // get the tokens
            var accessToken = loginInfo.AuthenticationTokens.FirstOrDefault(t => t.Name == "access_token")?.Value;
            var refreshToken = loginInfo.AuthenticationTokens.FirstOrDefault(t => t.Name == "refresh_token")?.Value;
            var expiresAt = loginInfo.AuthenticationTokens.FirstOrDefault(t => t.Name == "expires_at")?.Value;

            if (!string.IsNullOrEmpty(accessToken))
                await _userHelper.StoreUserTokenAsync(user, "Google", "access_token", accessToken);
            if (!string.IsNullOrEmpty(refreshToken))
                await _userHelper.StoreUserTokenAsync(user, "Google", "refresh_token", refreshToken);
            if (!string.IsNullOrEmpty(expiresAt))
                await _userHelper.StoreUserTokenAsync(user, "Google", "expires_at", expiresAt);

            await _signInManager.SignInAsync(user, isPersistent: true);

            return RedirectToAction("Index", "Account");
        }

        public async Task<IActionResult> Logout()
        {
            await _userHelper.LogoutAsync();
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

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
                        Names = model.Names,
                        LastName = model.LastName,
                        Email = model.Email,
                        UserName = model.Email,
                        PhoneNumber = model.PhoneNumber,
                        Nif = model.Nif,
                        Birthdate = model.Birthdate
                    };

                    var result = await _userHelper.AddUserAsync(user, password);

                    await _userHelper.AddUserToRoleAsync(user, "Colaborador");

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

                    var token = await _userHelper.GenerateEmailConfirmationTokenAsync(user);
                    var confirmationLink = Url.Action("ConfirmEmail", "Account",
                        new { userId = user.Id, token = token }, protocol: HttpContext.Request.Scheme);

                    var mailMessage = $@"Bem-vindo à equipa, {user.Names.Split(' ')[0]}!<br/><br/><p>" +
                        $"Foi criado um utilizador para ti no sistema.</p>" +
                        $"<p><strong>Email:</strong> {user.Email}<br/>" +
                        $"<strong>Password temporária:</strong> {password}</p>" +
                        $"<p>Antes de iniciar sessão, tens de confirmar a tua conta clicando no seguinte link <a href='{confirmationLink}'>Confirmar conta</a></p>" +
                        $"Após confirmares, podes iniciar sessão e alterar a tua password.</p>";

                    await _emailSender.SendEmailAsync(user.Email, "Bem-vindo à equipa!", mailMessage);

                    ViewBag.AddStaffMessage = "Colaborador adicionado com sucesso! Foi enviado um email de confirmação de conta para o email estipulado.";

                    return View();
                }
            }

            ModelState.AddModelError(string.Empty, "Já existe um utilizador com esse email.");
            return View(model);
        }
    }
}
