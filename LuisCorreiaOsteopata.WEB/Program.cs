using Ganss.Xss;
using LuisCorreiaOsteopata.WEB.Data;
using LuisCorreiaOsteopata.WEB.Data.Entities;
using LuisCorreiaOsteopata.WEB.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;


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
            cfg.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
        });

        builder.Services.AddTransient<SeedDB>();
        builder.Services.AddTransient<IEmailSender, EmailSender>();
        builder.Services.AddScoped<IUserHelper, UserHelper>();
        builder.Services.AddScoped<IConverterHelper, ConverterHelper>();
        builder.Services.AddScoped<HtmlSanitizer>();

        builder.Services.AddScoped<IPatientRepository, PatientRepository>();
        builder.Services.AddScoped<IStaffRepository, StaffRepository>();
        builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();

        builder.Services.Configure<AuthMessageSenderOptions>(builder.Configuration);

        builder.Services.AddAuthentication()
            .AddGoogle(options =>
            {
                options.ClientId = builder.Configuration["GoogleSettings:ClientId"];
                options.ClientSecret = builder.Configuration["GoogleSettings:ClientSecret"];
                options.CallbackPath = "/signin-google";

                options.Scope.Add("https://www.googleapis.com/auth/calendar.events");
                options.Scope.Add("email");
                options.Scope.Add("profile");

                options.SaveTokens = true;
            });

        var app = builder.Build();

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
