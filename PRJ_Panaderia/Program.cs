using System;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using PRJ_Panaderia.Data;
using PRJ_Panaderia.Models;
using PRJ_Panaderia.Services;
using System.IO;

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddControllersWithViews();

    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.LoginPath = "/Login";
            options.LogoutPath = "/Login/Logout";
            options.AccessDeniedPath = "/Login";
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
            options.Cookie.Name = "Auth_Panaderia";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
        });

    builder.Services.AddScoped<CargoRepository>();
    builder.Services.AddScoped<EmpleadoRepository>();
    builder.Services.AddScoped<CategoriaRepository>();
    builder.Services.AddScoped<ClienteRepository>();
    builder.Services.AddScoped<MesaRepository>();
    builder.Services.AddScoped<ProductoRepository>();
    builder.Services.AddScoped<ConfiguracionSistemaRepository>();
    builder.Services.AddScoped<TurnoCajaRepository>();
    builder.Services.AddScoped<PedidoRepository>();
    builder.Services.AddScoped<DetallePedidoRepository>();
    builder.Services.AddScoped<PagoRepository>();
    builder.Services.AddScoped<AuditoriaRepository>();
    builder.Services.AddScoped<DashboardRepository>();

    builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
    builder.Services.AddScoped<EmailService>(sp =>
    {
        var settings = new SmtpSettings();
        builder.Configuration.GetSection("SmtpSettings").Bind(settings);
        var logger = sp.GetRequiredService<ILogger<EmailService>>();
        return new EmailService(settings, logger);
    });

    var app = builder.Build();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    string imagesFolder = Path.Combine(app.Environment.ContentRootPath, "Images", "Productos");
    if (!Directory.Exists(imagesFolder))
    {
        Directory.CreateDirectory(imagesFolder);
    }

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(imagesFolder),
        RequestPath = "/Images/Productos"
    });

    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Login}/{action=Index}/{id?}");

    Console.WriteLine("Aplicacion iniciada correctamente");
    app.Run();
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("\nError durante el arranque:");
    Console.ResetColor();
    Console.WriteLine(ex.Message);
    if (ex.InnerException != null)
        Console.WriteLine(ex.InnerException.Message);
}