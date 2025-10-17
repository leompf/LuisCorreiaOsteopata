using Ganss.Xss;
using Hangfire;
using LuisCorreiaOsteopata.WEB.Data;
using LuisCorreiaOsteopata.WEB.Data.Entities;
using LuisCorreiaOsteopata.WEB.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using System.Globalization;


namespace LuisCorreiaOsteopata.WEB;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllersWithViews();

        builder.Services.AddIdentity<User, IdentityRole>(cfg =>
        {
            cfg.User.RequireUniqueEmail = true;
            cfg.SignIn.RequireConfirmedAccount = true;
            cfg.Password.RequireDigit = false;
            cfg.Password.RequiredUniqueChars = 0;
            cfg.Password.RequireLowercase = false;
            cfg.Password.RequireUppercase = false;
            cfg.Password.RequireNonAlphanumeric = false;
            cfg.Password.RequiredLength = 6;
        })
          .AddEntityFrameworkStores<DataContext>()
          .AddDefaultTokenProviders();

        builder.Services.AddDbContext<DataContext>(cfg =>
        {
            cfg.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
            .EnableSensitiveDataLogging();
        });

        builder.Services.AddHangfire(cfg =>
            cfg.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

        builder.Services.AddHangfireServer();

        builder.Services.AddTransient<SeedDB>();
        builder.Services.AddTransient<IEmailSender, EmailSender>();
        builder.Services.AddScoped<IReminderHelper, ReminderHelper>();
        builder.Services.AddScoped<IUserHelper, UserHelper>();
        builder.Services.AddScoped<IConverterHelper, ConverterHelper>();
        builder.Services.AddScoped<HtmlSanitizer>();
        builder.Services.AddScoped<IGoogleHelper, GoogleHelper>();
        builder.Services.AddScoped<IImageHelper, ImageHelper>();


        builder.Services.AddScoped<IPatientRepository, PatientRepository>();
        builder.Services.AddScoped<IStaffRepository, StaffRepository>();
        builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        builder.Services.AddScoped<IProductRepository, ProductRepository>();
        builder.Services.AddScoped<IOrderRepository, OrderRepository>();

        builder.Services.Configure<AuthMessageSenderOptions>(builder.Configuration);
        builder.Services.Configure<GoogleSettings>(builder.Configuration.GetSection("GoogleSettings"));
        builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));
        Stripe.StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

        builder.Services.AddAuthentication()
            .AddGoogle(options =>
            {
                options.ClientId = builder.Configuration["GoogleSettings:ClientId"];
                options.ClientSecret = builder.Configuration["GoogleSettings:ClientSecret"];
                options.CallbackPath = "/signin-google";

                options.Scope.Add("https://www.googleapis.com/auth/calendar");
                options.Scope.Add("https://www.googleapis.com/auth/calendar.events");
                options.Scope.Add("email");
                options.Scope.Add("profile");

                options.AccessType = "offline";
                options.SaveTokens = true;                

                options.Events.OnRedirectToAuthorizationEndpoint = context =>
                {
                    context.Response.Redirect(context.RedirectUri + "&prompt=consent");
                    return Task.CompletedTask;
                };
            });

        var app = builder.Build();

        var defaultCulture = new CultureInfo("pt-PT");
        var localizationOptions = new RequestLocalizationOptions
        {
            DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(defaultCulture),
            SupportedCultures = new List<CultureInfo> { defaultCulture },
            SupportedUICultures = new List<CultureInfo> { defaultCulture }
        };

        app.UseRequestLocalization(localizationOptions);

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseHangfireDashboard("/hangfire");
        using (var scope = app.Services.CreateScope())
        {
            var reminderService = scope.ServiceProvider.GetRequiredService<IReminderHelper>();

            RecurringJob.AddOrUpdate(
                "appointment-reminders",
                () => reminderService.SendAppointmentReminderAsync(),
                Cron.Hourly 
            );
        }

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        using (var scope = app.Services.CreateScope())
        {
            var seeder = scope.ServiceProvider.GetRequiredService<SeedDB>();
            seeder.SeedAsync().Wait();
        }

        app.Run();
    }
}
