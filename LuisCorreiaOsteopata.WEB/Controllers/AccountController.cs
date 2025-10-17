using LuisCorreiaOsteopata.WEB.Data;
using LuisCorreiaOsteopata.WEB.Data.Entities;
using LuisCorreiaOsteopata.WEB.Helpers;
using LuisCorreiaOsteopata.WEB.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using QRCoder;
using System.Security.Claims;
using System.Text.RegularExpressions;


namespace LuisCorreiaOsteopata.WEB.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserHelper _userHelper;
        private readonly SignInManager<User> _signInManager;
        private readonly IPatientRepository _patientRepository;
        private readonly IStaffRepository _staffRepository;
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IGoogleHelper _googleHelper;
        private readonly IEmailSender _emailSender;
        private readonly IConverterHelper _converterHelper;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IUserHelper userHelper,
            SignInManager<User> signInManager,
            IPatientRepository patientRepository,
            IStaffRepository staffRepository,
            IAppointmentRepository appointmentRepository,
            IGoogleHelper googleHelper,
            IEmailSender emailSender,
            IConverterHelper converterHelper,
            ILogger<AccountController> logger)
        {
            _userHelper = userHelper;
            _signInManager = signInManager;
            _patientRepository = patientRepository;
            _staffRepository = staffRepository;
            _appointmentRepository = appointmentRepository;
            _googleHelper = googleHelper;
            _emailSender = emailSender;
            _converterHelper = converterHelper;
            _logger = logger;
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
                else if (result.RequiresTwoFactor)
                {
                    return RedirectToAction(nameof(LoginWith2fa), new { RememberMe = model.RememberMe });
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


        public async Task<IActionResult> Profile(string? id)
        {
            User user;

            if (string.IsNullOrEmpty(id))
            {
                user = await _userHelper.GetCurrentUserAsync();
                if (user == null)
                    return RedirectToAction("Login", "Account");
            }
            else
            {
                if (User.IsInRole("Utente"))
                    return Forbid();

                user = await _userHelper.GetUserByIdAsync(id);
                if (user == null)
                    return NotFound();
            }

            var role = await _userHelper.GetUserRoleAsync(user);

            var accessToken = await _userHelper.GetUserTokenAsync(user, "Google", "access_token");
            ViewBag.Token = accessToken;

            ViewBag.GoogleCalendars = new List<SelectListItem>();

            if (!string.IsNullOrEmpty(accessToken))
            {
                try
                {
                    var calendars = await _googleHelper.GetUserCalendarsAsync(user, CancellationToken.None);

                    ViewBag.GoogleCalendars = calendars
                        .Select(c => new SelectListItem
                        {
                            Value = c.Id,
                            Text = c.Summary
                        })
                        .ToList();
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("Google account not connected"))
                {
                    _logger.LogInformation("User {Email} has no Google account connected.", user.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to retrieve Google calendars for {Email}.", user.Email);
                }
            }

            var model = new ProfileViewModel
            {
                Name = $"{user.Names} {user.LastName}",
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                BirthDate = user.Birthdate,
                NIF = user.Nif,
                Role = role,
                CalendarId = user.CalendarId,
                CalendarName = null,
                IsEditable = string.IsNullOrEmpty(id) || User.IsInRole("Administrador") || User.IsInRole("Colaborador")
            };

            if (!string.IsNullOrWhiteSpace(user.CalendarId) && ViewBag.GoogleCalendars != null)
            {
                var selected = ((List<SelectListItem>)ViewBag.GoogleCalendars)
                                    .FirstOrDefault(c => c.Value == user.CalendarId);
                if (selected != null)
                {
                    model.CalendarName = selected.Text;
                }
            }

            return View(model);
        }

        public async Task<IActionResult> Security()
        {
            var user = await _userHelper.GetCurrentUserAsync();
            if (user == null)
                return RedirectToAction("Login");

            var is2faEnabled = user.TwoFactorEnabled;

            var model = new SecurityViewModel
            {
                Is2faEnabled = is2faEnabled
            };

            return View(model);
        }


        [HttpPost]
        public async Task<IActionResult> UpdateUser(ProfileViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userHelper.GetCurrentUserAsync();
                if (user != null)
                {
                    user.Email = model.Email;
                    user.PhoneNumber = model.PhoneNumber;
                    user.Nif = model.NIF;
                    if (model.BirthDate.HasValue)
                    {
                        user.Birthdate = model.BirthDate.Value;
                    }

                    await _userHelper.UpdateUserAsync(user);
                }
            }

            return RedirectToAction("Profile");
        }

        public async Task<IActionResult> ViewAllUsers(string? name, string? email, string? phone, string? nif, string? role, string? sortBy, bool sortDescending = true)
        {
            var users = await _userHelper.GetAllUsersAsync();
            var userList = new List<UserViewModel>();

            foreach (var user in users)
            {
                var userRole = await _userHelper.GetUserRoleAsync(user);
                userList.Add(_converterHelper.ToUserViewModel(user, userRole));
            }

            userList = _userHelper.FilterUsers(userList, name, email, phone, nif);

            userList = _userHelper.SortUsers(userList, sortBy, sortDescending);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var partialModel = new UserListViewModel
                {
                    Users = userList
                };
                return PartialView("_ViewAllUsersTable", partialModel.Users);
            }

            var model = new UserListViewModel
            {
                NameFilter = name,
                EmailFilter = email,
                PhoneFilter = phone,
                NifFilter = nif,
                Users = userList,
                Roles = _userHelper.GetAllRolesAsync()
            };

            ViewBag.DefaultSortColumn = sortBy ?? "Name";
            ViewBag.DefaultSortDescending = sortDescending;

            return View(model);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Enable2fa()
        {
            var user = await _userHelper.GetCurrentUserAsync();
            var key = await _userHelper.GetAuthenticatorKeyAsync(user);

            if (string.IsNullOrEmpty(key))
            {
                await _userHelper.ResetAuthenticatorKeyAsync(user);
                key = await _userHelper.GetAuthenticatorKeyAsync(user);
            }

            var authenticatorUri = $"otpauth://totp/{Uri.EscapeDataString("LuisCorreiaOsteopata")}:{Uri.EscapeDataString(user.Email)}?secret={key}&issuer={Uri.EscapeDataString("LuisCorreiaOsteopata")}&digits=6";

            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(authenticatorUri, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeImage = Convert.ToBase64String(qrCode.GetGraphic(20));

            var model = new Enable2faViewModel
            {
                SharedKey = FormatKey(key),
                QrCodeImage = $"data:image/png;base64,{qrCodeImage}"
            };

            return View(model);
        }


        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enable2fa(Enable2faViewModel model)
        {
            var user = await _userHelper.GetCurrentUserAsync();
            if (user == null)
                return RedirectToAction("Login");

            var isValid = await _userHelper.VerifyTwoFactorTokenAsync(user, model.Code);
            if (!isValid)
            {
                ModelState.AddModelError("Code", "Código inválido. Tenta novamente.");
                return View(model);
            }

            await _userHelper.SetTwoFactorEnabledAsync(user, true);
            return RedirectToAction("Security");
        }

        [Authorize]
        public async Task<IActionResult> Disable2fa()
        {
            var user = await _userHelper.GetCurrentUserAsync();
            if (user == null)
                return RedirectToAction("Login");

            await _userHelper.SetTwoFactorEnabledAsync(user, false);
            await _signInManager.ForgetTwoFactorClientAsync();
            await _userHelper.ResetAuthenticatorKeyAsync(user);

            return RedirectToAction("Security");
        }

        [HttpGet]
        public async Task<IActionResult> LoginWith2fa(bool rememberMe, string? returnUrl = null)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
                throw new InvalidOperationException("Unable to load 2FA user.");

            var model = new LoginWith2faViewModel { RememberMe = rememberMe };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginWith2fa(LoginWith2faViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
                throw new InvalidOperationException("Unable to load two-factor authentication user.");

            var authenticatorCode = model.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);

            var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(
                authenticatorCode, model.RememberMe, model.RememberDevice);

            if (result.Succeeded)
            {
                if (this.Request.Query.Keys.Contains("ReturnUrl"))
                {
                    return Redirect(this.Request.Query["ReturnUrl"].First());
                }

                return this.RedirectToAction("Index", "Account");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Conta bloqueada.");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Código inválido.");
            return View(model);
        }

        private string FormatKey(string key)
        {
            return Regex.Replace(key.ToUpperInvariant(), ".{4}", "$0 ").Trim();
        }
    }
}
