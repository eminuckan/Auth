using Auth.Domain.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Auth.Infrastructure.Data;

public static class InitializerExtensions
{
    public static async Task InitialiseDbAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbInitializer>();

        await initialiser.InitialiseAsync();
        await initialiser.SeedAsync();
    }
}

public class ApplicationDbInitializer
{
    private readonly ILogger<ApplicationDbInitializer> _logger;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;
    private readonly IServiceProvider _serviceProvider;

    public ApplicationDbInitializer(
        ILogger<ApplicationDbInitializer> logger,
        ApplicationDbContext context,
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _serviceProvider = serviceProvider;
    }

    public async Task InitialiseAsync()
    {
        try
        {
            await _context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initialising the database.");
            throw;
        }
    }

    public async Task SeedAsync()
    {
        try
        {
            await TrySeedRolesAsync();
            await TrySeedUsersAsync();
            await TrySeedOpeniddictApplicationAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    private async Task TrySeedRolesAsync()
    {
        var roles = new[] { "Admin", "PropertyManager", "Tenant" };

        foreach (var roleName in roles)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new AppRole
                {
                    Name = roleName,
                    TenantId = MockTenants.DefaultTenantId
                });

                _logger.LogInformation($"Role '{roleName}' created.");
            }
        }
    }

    private async Task TrySeedUsersAsync()
    {
        var adminEmail = "admin@propmate.com";
        var adminUser = await _userManager.FindByEmailAsync(adminEmail);

        if (adminUser is null)
        {
            await AddUser(adminEmail, true);
        }

        var tenantEmail = "tenant@gmail.com";
        var tenantUser = await _userManager.FindByEmailAsync(tenantEmail);

        if (tenantUser is null)
        {
           await AddUser(tenantEmail, false);
        }
    }

    private async Task TrySeedOpeniddictApplicationAsync()
    {
        var applicationManager = _serviceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        if (await applicationManager.FindByClientIdAsync("dashboard-client") is null)
        {
            var descriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = "dashboard-client",
                ClientSecret = "dashboard-secret",
                DisplayName = "Dashboard Client",
                RedirectUris = {new Uri("https://oauth.pstmn.io/v1/callback") },
                Permissions =
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.GrantTypes.RefreshToken,
                    Permissions.ResponseTypes.Code
                }
            };

            await applicationManager.CreateAsync(descriptor);
        }

        if (await applicationManager.FindByClientIdAsync("tenant-client") is null)
        {
            var descriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = "tenant-client",
                ClientSecret = "tenant-secret",
                DisplayName = "Tenant Client",
                RedirectUris = { new Uri("https://oauth.pstmn.io/v1/callback") },
                Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.RefreshToken,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.ResponseTypes.Code,
                Permissions.Scopes.Profile,
            }
            };

            await applicationManager.CreateAsync(descriptor);
        }
    }

    private async Task AddUser(string email, bool isAdmin)
    {
        var user = new AppUser {
            Email = email,
            UserName = email,
            EmailConfirmed = true,
            TenantId = MockTenants.DefaultTenantId
        };

        var result = await _userManager.CreateAsync(user, isAdmin ? "Admin123!" : "Tenant123!");

        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, isAdmin ? "Admin" : "Tenant");
            await _userManager.AddClaimAsync(user, new Claim("AllowedClient", isAdmin ? "dashboard-client" : "tenant-client"));
            _logger.LogInformation("User with email {Email} was created and assigned the role {Role}", email ,isAdmin ? "Admin" : "Tenant");
        }
        else
        {
            _logger.LogError("Failed to create user with email {Email}. Errors: {Errors}", email, result.Errors);
        }
    }
}

public static class MockTenants
{
    public static readonly Guid DefaultTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
}