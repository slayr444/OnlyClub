using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using OnlyClub.Adapters;
using OnlyClub.Adapters.Security;
using OnlyClub.API.Endpoints;
using OnlyClub.Application;
using OnlyClub.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddAdapters(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services
    .AddAuthentication(TmaAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, TmaAuthenticationHandler>(
        TmaAuthenticationHandler.SchemeName, _ => { });

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OnlyClubDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Applying database migrations...");
        await db.Database.MigrateAsync();

        logger.LogInformation("Seeding database...");
        await DbSeeder.SeedAsync(db);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
        throw;
    }
}

app.UseAuthentication();
app.UseAuthorization();

var distPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "dist");
if (Directory.Exists(distPath))
{
    var fileProvider = new PhysicalFileProvider(distPath);

    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = fileProvider });

    app.UseStaticFiles(new StaticFileOptions { FileProvider = fileProvider });
}
else
{
    app.Logger.LogWarning("Папка wwwroot/dist не найдена. Фронтенд не будет загружен. Выполнили ли вы 'npm run build'?");
}

app.MapOnlyClubEndpoints();

app.Run();