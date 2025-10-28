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
using QuestPDF.Fluent;
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
        private readonly IOrderRepository _orderRepository;
        private readonly IGoogleHelper _googleHelper;
        private readonly IEmailSender _emailSender;
        private readonly IConverterHelper _converterHelper;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IUserHelper userHelper,
            SignInManager<User> signInManager,
            IPatientRepository patientRepository,
            IStaffRepository staffRepository,
            IAppointmentRepository appointmentRepository,
            IOrderRepository orderRepository,
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
            _orderRepository = orderRepository;
            _googleHelper = googleHelper;
            _emailSender = emailSender;
            _converterHelper = converterHelper;
            _logger = logger;
        }

        #region Homepage
        public async Task<IActionResult> Index()
        {            
            _logger.LogInformation("Homepage accessed by user {User}", User.Identity?.Name ?? "Anonymous");

            if (User.IsInRole("Administrador"))
            {
                return RedirectToAction("Admin", "Account");
            }

            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userHelper.GetCurrentUserAsync();
                if (user == null)
                {
                    _logger.LogWarning("Authenticated user could not be retrieved from UserHelper.");
                    return RedirectToAction("Login", "Account");
                }

                var accessToken = await _userHelper.GetUserTokenAsync(user, "Google", "access_token");
                ViewBag.Token = accessToken;

                ViewBag.GoogleCalendars = new List<SelectListItem>();
                string? calendarName = null;

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

                        if (!string.IsNullOrWhiteSpace(user.CalendarId))
                        {
                            var selected = ((List<SelectListItem>)ViewBag.GoogleCalendars)
                                .FirstOrDefault(c => c.Value == user.CalendarId);
                            calendarName = selected?.Text;
                        }
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

                var credits = await _orderRepository.GetRemainingCreditsAsync(user.Id);

                var appointments = await _appointmentRepository.GetAppointmentsByUserAsync(user);
                _logger.LogInformation("Fetched {AppointmentCount} appointments for user {UserId}", appointments.Count, user.Id);

                var events = appointments.Select(a => new
                {
                    id = a.Id,
                    title = $"Consulta com {a.Staff.FullName}",
                    start = a.AppointmentDate.Add(a.StartTime.ToTimeSpan()).ToString("s"),
                    end = a.AppointmentDate.Add(a.EndTime.ToTimeSpan()).ToString("s"),
                    allDay = false
                }).ToList();

                _logger.LogDebug("Mapped {EventCount} events for calendar display for user {UserId}", events.Count, user.Id);

                ViewBag.Appointments = events;
                ViewBag.CalendarId = user.CalendarId;
                ViewBag.CalendarName = calendarName;
                ViewBag.RemainingCredits = credits;

                return View();
            }

            _logger.LogInformation("User is not authenticated, redirecting to Login page.");
            return RedirectToAction("Login", "Account");
        }

        public IActionResult Admin()
        {
            return View();
        }
        #endregion

        #region Account Creation
        [HttpGet]
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
            var nif = await _userHelper.GetUserByNifAsync(model.Nif);

            if (user != null && nif != null)
            {
                ModelState.AddModelError(string.Empty, "Já existe um utilizador com esse Email e NIF.");
                return View(model);
            }
            else if (user != null)
            {
                ModelState.AddModelError(string.Empty, "Já existe um utilizador com esse Email.");
                return View(model);
            }
            else if (nif != null)
            {
                ModelState.AddModelError(string.Empty, "Já existe um utilizador com esse NIF.");
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

        [HttpGet]
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

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> ResendEmailConfirmation(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest("Por favor insere o email.");

            var user = await _userHelper.GetUserByEmailAsync(email);
            if (user == null || user.EmailConfirmed)
                return Ok("Se este email estiver registado, receberá um novo link de confirmação.");

            var token = await _userHelper.GenerateEmailConfirmationTokenAsync(user);
            var confirmationLink = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, token }, Request.Scheme);

            var message = $@"<p>Olá {user.Names.Split(' ')[0]},</p>
            <p>Foi requisitado o reenvio do email de confirmação para a tua conta, na qual podes confirmar
            clicando <a href='{confirmationLink}'>aqui</a>.</p>
            <p>Se não foste tu a efetuar este pedido ou não tens conta na plataforma, por favor contacta-nos.</p>
            <p>Obrigado e cumprimentos,<br />
            Luís Correia, Osteopata</p>";


            await _emailSender.SendEmailAsync(user.Email, "Confirmação de Conta", message);

            return Ok("Foi enviado um novo email de confirmação.");
        }

        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public IActionResult AddStaff()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> AddStaff(AddNewStaffViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userHelper.GetUserByEmailAsync(model.Email);
                var password = _userHelper.GenerateRandomPassword(new PasswordOptions()
                {
                    RequiredLength = 6,
                    RequiredUniqueChars = 4,
                    RequireDigit = true,
                    RequireLowercase = true,
                    RequireNonAlphanumeric = true,
                    RequireUppercase = true
                });


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
        #endregion

        #region Login & Logout
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userHelper.GetUserByEmailAsync(model.Username);
                if (user == null)
                {
                    _logger.LogWarning("Login failed: user not found ({Username})", model.Username);
                    ModelState.AddModelError(string.Empty, "Nome de utilizador ou palavra-passe incorretos.");
                    return View(model);
                }

                var isPasswordValid = await _userHelper.CheckPasswordAsync(user, model.Password);
                if (!isPasswordValid)
                {
                    _logger.LogWarning("Login failed: invalid password for {Username}", model.Username);
                    ModelState.AddModelError(string.Empty, "Nome de utilizador ou palavra-passe incorretos.");
                    return View(model);
                }

                var result = await _userHelper.LoginAsync(model.Username, model.Password, model.RememberMe);

                if (result.Succeeded)
                {
                    if (Request.Query.TryGetValue("ReturnUrl", out var returnUrl) && !string.IsNullOrEmpty(returnUrl))
                    {
                        _logger.LogInformation("Redirecting user {Username} to ReturnUrl {ReturnUrl}", model.Username, returnUrl);
                        return Redirect(returnUrl!);
                    }
                    if (User.IsInRole("Administrador"))
                    {
                        return RedirectToAction("Admin", "Account");
                    }

                    return this.RedirectToAction("Index", "Account");
                }
                else if (result.IsNotAllowed)
                {
                    ModelState.AddModelError(string.Empty, "Por favor confirma o teu email antes de fazer login.");
                    ViewBag.ShowResendConfirmation = true;
                    return View(model);
                }
                else if (result.RequiresTwoFactor)
                {
                    return RedirectToAction(nameof(LoginWith2fa), new { RememberMe = model.RememberMe });
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Erro ao fazer login");
                }
            }
            else
            {
                _logger.LogWarning("Login attempt failed due to invalid model state for user {Username}", model.Username);
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult LoginGoogle()
        {
            _logger.LogInformation("Initiating Google login flow.");

            var redirectUrl = Url.Action("GoogleResponse", "Account");
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(GoogleDefaults.AuthenticationScheme, redirectUrl);

            _logger.LogInformation("Redirecting user to Google for authentication.");
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public async Task<IActionResult> GoogleResponse()
        {
            _logger.LogInformation("Received Google authentication response.");

            var loginInfo = await _signInManager.GetExternalLoginInfoAsync();
            if (loginInfo == null)
            {
                _logger.LogWarning("Google login info is null. Redirecting to login page.");
                return RedirectToAction("Login");
            }

            var signInResult = await _signInManager.ExternalLoginSignInAsync(
                loginInfo.LoginProvider,
                loginInfo.ProviderKey,
                isPersistent: true,
                bypassTwoFactor: false);

            if (signInResult.Succeeded)
            {
                _logger.LogInformation("User successfully signed in with Google provider {Provider}.", loginInfo.LoginProvider);
                return RedirectToAction("Index", "Account");
            }

            if (signInResult.RequiresTwoFactor)
            {
                _logger.LogInformation("User requires 2FA. Redirecting to 2FA page.");
                return RedirectToAction(nameof(LoginWith2fa), new { rememberMe = true });
            }

            var claims = loginInfo.Principal.Identities.FirstOrDefault()?.Claims;
            var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("Google login failed: email claim not found.");
                return RedirectToAction("Login");
            }

            var user = await _userHelper.GetUserByEmailAsync(email) ?? await CreateUserFromGoogleAsync(email, name);

            var existingLogin = await _userHelper.GetExternalLoginAsync(user, loginInfo.LoginProvider);
            if (existingLogin == null)
            {
                await _userHelper.AddExternalLoginAsync(user, loginInfo);
            }

            await StoreGoogleTokensAsync(user, loginInfo);

            _logger.LogInformation("Signing in user {UserId} after Google login.", user.Id);
            await _signInManager.SignInAsync(user, isPersistent: true);

            return RedirectToAction("Index", "Account");
        }

        [HttpGet]
        public async Task<IActionResult> LoginWith2fa(bool rememberMe, string? returnUrl = null)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                _logger.LogError("Unable to load 2FA user for request");
                throw new InvalidOperationException("Unable to load 2FA user.");
            }

            _logger.LogInformation("Displaying 2FA login page for user {UserId} ({Email})", user.Id, user.Email);

            var model = new LoginWith2faViewModel { RememberMe = rememberMe };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginWith2fa(LoginWith2faViewModel model)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("2FA login attempt with invalid model state.");
                return View(model);
            }

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                _logger.LogError("Unable to load 2FA user during POST login attempt.");
                throw new InvalidOperationException("Unable to load two-factor authentication user.");
            }

            var authenticatorCode = model.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);
            _logger.LogInformation("User {UserId} attempting 2FA login.", user.Id);

            var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(
                authenticatorCode, model.RememberMe, model.RememberDevice);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {UserId} successfully logged in via 2FA.", user.Id);

                if (this.Request.Query.Keys.Contains("ReturnUrl"))
                {
                    var returnUrl = this.Request.Query["ReturnUrl"].First();
                    _logger.LogInformation("Redirecting 2FA user {UserId} to ReturnUrl {ReturnUrl}.", user.Id, returnUrl);
                    return Redirect(returnUrl!);
                }

                return this.RedirectToAction("Index", "Account");
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("2FA login attempt failed: user {UserId} is locked out.", user.Id);
                ModelState.AddModelError(string.Empty, "Conta bloqueada.");
                return View(model);
            }

            _logger.LogWarning("Invalid 2FA code entered by user {UserId}.", user.Id);
            ModelState.AddModelError(string.Empty, "Código inválido.");
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await _userHelper.LogoutAsync();
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userHelper.GetUserByEmailAsync(model.Email);
            if (user == null)
            {
                return NotFound();
            }

            var token = await _userHelper.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.Action(
                nameof(ResetPassword),
                "Account",
                new { token, email = user.Email },
                protocol: Request.Scheme);

            var mail = $@"
            <p>Olá {user.Names.Split(' ')[0]},</p>
            <p>Foi emitido um pedido de reposição de password para a tua conta, podes prosseguir com o mesmo clicando <a href='{callbackUrl}'>aqui</a></p>
            <p>Caso não tenhas requisitado o mesmo, por favor ignora este mail e certifica-te que a tua conta está protegida habilitando a Autenticação de 2 Fatores na tua Área Pessoal</p>
            <p>Obrigado e cumprimentos<br />
            Luís Correia, Osteopata</p>";

            await _emailSender.SendEmailAsync(model.Email, "Repôr Password", mail);
            ViewBag.ForgotPasswordMessage = "Se a conta existir, um mail foi enviado.";
            ViewBag.IsError = false;

            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            if (token == null || email == null)
                return BadRequest("A valid password reset token must be supplied.");

            var model = new ResetPasswordViewModel { Token = token, Email = email };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userHelper.GetUserByEmailAsync(model.Email);
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userHelper.ResetPasswordAsync(user, model.Token, model.NewPassword);

            if (result.Succeeded)
            {
                ViewBag.PasswordNotification = "Password alterada com sucesso!";
            }
            else
            {
                ViewBag.ToastMessage = string.Join(" ", result.Errors.Select(e => e.Description));
            }

            return View(model);
        }

        #endregion

        #region CRUD
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var user = await _userHelper.GetCurrentUserAsync();
            if (user == null)
                return NotFound();

            var model = new EditAccountViewModel
            {
                Names = user.Names,
                LastName = user.LastName,
                Email = user.Email,
                Birthdate = user.Birthdate
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditAccountViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userHelper.GetUserByEmailAsync(model.Email);
            if (user == null)
                return NotFound();

            user.Names = model.Names;
            user.LastName = model.LastName;
            user.Email = model.Email;
            user.UserName = model.Email;
            user.Birthdate = model.Birthdate;

            var result = await _userHelper.UpdateUserAsync(user);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
                return View(model);
            }

            if (!string.IsNullOrEmpty(model.CurrentPassword) && !string.IsNullOrEmpty(model.NewPassword))
            {
                var passwordChangeResult = await _userHelper.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
                if (!passwordChangeResult.Succeeded)
                {
                    foreach (var error in passwordChangeResult.Errors)
                        ModelState.AddModelError("", error.Description);
                    return View(model);
                }

                ViewBag.PasswordMessage = "A tua palavra-passe foi alterada com sucesso!";
            }

            return RedirectToAction(nameof(Edit));
        }

        [HttpGet]
        public async Task<IActionResult> Profile(string? id)
        {
            var currentUser = await _userHelper.GetCurrentUserAsync();
            if (currentUser == null)
                return RedirectToAction("Login", "Account");

            User? user;
            if (string.IsNullOrEmpty(id))
            {
                user = currentUser;
            }
            else
            {
                user = await _userHelper.GetUserByIdAsync(id);
                if (user == null)
                    return NotFound();

                if (user.Id != currentUser.Id && !User.IsInRole("Administrador") && !User.IsInRole("Colaborador"))
                    return Forbid();
            }

            var role = await _userHelper.GetUserRoleAsync(user);

            bool isOwnProfile = user.Id == currentUser.Id;
            bool isAdmin = User.IsInRole("Administrador");
            bool isColaborador = User.IsInRole("Colaborador");
            bool isUtente = User.IsInRole("Utente");

            var model = new ProfileViewModel
            {
                Id = user.Id,
                Name = $"{user.Names} {user.LastName}",
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                BirthDate = user.Birthdate,
                NIF = user.Nif,
                Role = role,
                IsEditable = isOwnProfile && (isUtente || isAdmin) || isAdmin, 
                ShowPatientFields = (isUtente && isOwnProfile) || isAdmin || (isColaborador && role == "Utente"),
                ArePatientFieldsReadonly = (isColaborador && role == "Utente") || (!isUtente && !isAdmin),
            };

            if (role == "Utente")
            {
                var patient = await _patientRepository.GetPatientByUserEmailAsync(user.Email!);
                if (patient != null)
                {
                    model.Gender = patient.Gender;
                    model.Height = patient.Height;
                    model.Weight = patient.Weight;
                    model.MedicalHistory = patient.MedicalHistory;
                }
            }

            return View(model);
        }

        [HttpGet]
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
        public async Task<IActionResult> UpdateUser(string id, ProfileViewModel model)
        {
            if (!ModelState.IsValid)
                return View("Profile", model);

            var userToUpdate = await _userHelper.GetUserByIdAsync(id);
            if (userToUpdate == null)
                return NotFound();

            var currentUser = await _userHelper.GetCurrentUserAsync();
            var currentRole = await _userHelper.GetUserRoleAsync(currentUser);
            var targetRole = await _userHelper.GetUserRoleAsync(userToUpdate);

            bool canEdit =
                currentUser.Id == userToUpdate.Id ||
                currentRole == "Administrador" ||
                (currentRole == "Colaborador" && targetRole == "Utente");

            if (!canEdit)
                return Forbid();

            userToUpdate.PhoneNumber = model.PhoneNumber;
            userToUpdate.Email = model.Email;
            if (model.BirthDate.HasValue)
                userToUpdate.Birthdate = model.BirthDate.Value;

            if ((currentRole == "Administrador" || string.IsNullOrEmpty(userToUpdate.Nif)) && !string.IsNullOrEmpty(model.NIF))
                userToUpdate.Nif = model.NIF;

            await _userHelper.UpdateUserAsync(userToUpdate);

            if (targetRole == "Utente")
            {
                var patient = await _patientRepository.GetPatientByUserEmailAsync(userToUpdate.Email!);
                if (patient != null)
                {
                    patient.Gender = model.Gender;
                    patient.Height = model.Height;
                    patient.Weight = model.Weight;
                    patient.MedicalHistory = model.MedicalHistory;

                    await _patientRepository.UpdateAsync(patient);
                }
            }

            return RedirectToAction("Profile", new { id });
        }

        [HttpGet]
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
        #endregion

        #region 2FA Authentication
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
        #endregion

        #region Helper Methods
        private string FormatKey(string key)
        {
            return Regex.Replace(key.ToUpperInvariant(), ".{4}", "$0 ").Trim();
        }

        private async Task<User> CreateUserFromGoogleAsync(string email, string? name)
        {
            _logger.LogInformation("Creating new user for Google login: {Email}", email);

            var user = new User
            {
                Email = email,
                UserName = email,
                Names = name?.Split(' ')[0] ?? email,
                LastName = name?.Contains(" ") == true ? name.Substring(name.IndexOf(" ") + 1) : "",
                EmailConfirmed = true
            };

            await _userHelper.AddUserAsync(user, Guid.NewGuid().ToString());
            await _userHelper.AddUserToRoleAsync(user, "Utente");

            var patient = await _patientRepository.CreatePatientAsync(user, "Utente");
            if (patient != null)
                await _patientRepository.CreateAsync(patient);

            return user;
        }

        private async Task StoreGoogleTokensAsync(User user, ExternalLoginInfo loginInfo)
        {
            var tokens = loginInfo.AuthenticationTokens?.Where(t => t.Value != null)
                .ToDictionary(t => t.Name, t => t.Value!) ?? new Dictionary<string, string>();

            foreach (var token in tokens)
            {
                await _userHelper.StoreUserTokenAsync(user, loginInfo.LoginProvider, token.Key, token.Value);
            }
        }
        #endregion
    }
}
