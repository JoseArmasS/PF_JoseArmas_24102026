using Microsoft.EntityFrameworkCore;
using TransMetroUMG.Infrastructure;
using TransMetroUMG.Application;

using Microsoft.AspNetCore.Identity;
using TransMetroUMG.Domain;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IAutenticacionService, AutenticacionService>();
builder.Services.AddScoped<IOcupacionEstacionesService, OcupacionEstacionesService>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("InicioSesion", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "local",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0
        }));
    options.OnRejected = async (context, cancellationToken) =>
        await context.HttpContext.Response.WriteAsync("Demasiados intentos. Espera un minuto e intenta nuevamente.", cancellationToken);
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Cuenta/IniciarSesion";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Events.OnValidatePrincipal = async context =>
        {
            var servicio = context.HttpContext.RequestServices.GetRequiredService<IAutenticacionService>();
            if (!int.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var codigo)
                || !await servicio.SigueVigenteAsync(codigo, context.HttpContext.RequestAborted))
                context.RejectPrincipal();
        };
    });
builder.Services.AddScoped<ICatalogosUsuariosService, CatalogosUsuariosService>();
builder.Services.AddScoped<IMunicipalidadesService, MunicipalidadesService>();
builder.Services.AddScoped<IRolesService, RolesService>();
builder.Services.AddScoped<IBusesService, BusesService>();
builder.Services.AddScoped<IPilotosService, PilotosService>();
builder.Services.AddScoped<IAsignacionesService, AsignacionesService>();
builder.Services.AddScoped<IParqueosBaseService, ParqueosBaseService>();
builder.Services.AddScoped<IOperacionBusesService, OperacionBusesService>();
builder.Services.AddScoped<IRegistroOperacionBusService, RegistroOperacionBusService>();
builder.Services.AddScoped<ICiclosBusesService, CiclosBusesService>();
builder.Services.AddScoped<IEsperaBusesService, EsperaBusesService>();
builder.Services.AddScoped<IHoraConsultaService, HoraConsultaService>();
builder.Services.AddScoped<IConsultaBusesService, ConsultaBusesService>();
builder.Services.AddScoped<IConsultasAsignacionesService, ConsultasAsignacionesService>();
builder.Services.AddScoped<IOcupacionBusesService, OcupacionBusesService>();
builder.Services.AddScoped<IEstacionesService, EstacionesService>();
builder.Services.AddScoped<IParqueosService, ParqueosService>();
builder.Services.AddScoped<ILineasService, LineasService>();
builder.Services.AddScoped<IRecorridosService, RecorridosService>();
builder.Services.AddScoped<IParametrosOperacionService, ParametrosOperacionService>();
builder.Services.AddScoped<IUsuariosService, UsuariosService>();
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();

builder.Services.AddDbContext<MunitransDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Munitrans")
        ?? throw new InvalidOperationException("Falta configurar ConnectionStrings:Munitrans.")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
