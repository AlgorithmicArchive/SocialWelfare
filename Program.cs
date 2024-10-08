using System.Text.Json.Serialization;
using Encryption;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using SendEmails;
using SocialWelfare.Models.Entities;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();
builder.Services.AddSignalR(); // Add SignalR service
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options => options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

builder.Services.AddDbContext<SocialWelfareDepartmentContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    );
});

builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
});


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost",
        builder =>
        {
            builder.AllowAnyHeader()
                   .AllowAnyMethod()
                   .AllowCredentials()
                   .WithOrigins("http://localhost:8081"); // Change to your frontend URL if necessary
        });
});


builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.LoginPath = "/Home/Authentication";
    options.AccessDeniedPath = "/Home/Unauthorized";
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
});

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("CitizenPolicy", policy => policy.RequireRole("Citizen"))
    .AddPolicy("OfficerPolicy", policy => policy.RequireRole("Officer"));

builder.Services.AddSession(option =>
{
    option.IdleTimeout = TimeSpan.FromMinutes(30);
});

builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<OtpStore>();
builder.Services.AddScoped<EmailSender>();
builder.Services.AddScoped<UserHelperFunctions>();
builder.Services.AddTransient<PdfService>();
builder.Services.AddSingleton<IEncryptionService, EncryptionHelper>();
builder.Services.AddCors();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseCors("AllowLocalhost");


app.UseAuthentication();  // Ensure authentication is used
app.UseAuthorization();

app.UseSession();

// Map SignalR hub route
app.MapHub<ProgressHub>("/progressHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
