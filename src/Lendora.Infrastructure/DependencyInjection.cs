using System.Text;
using Amazon.Extensions.NETCore.Setup;
using Amazon.S3;
using Amazon.Textract;
using Lendora.Application.Abstractions.Admin;
using Lendora.Application.Abstractions.Affordability;
using Lendora.Application.Abstractions.Audit;
using Lendora.Application.Abstractions.Authentication;
using Lendora.Application.Abstractions.BankChecks;
using Lendora.Application.Abstractions.Documents;
using Lendora.Application.Abstractions.Loans;
using Lendora.Application.Abstractions.Persistence;
using Lendora.Application.Abstractions.Repayments;
using Lendora.Application.Abstractions.Risk;
using Lendora.Infrastructure.Authentication;
using Lendora.Infrastructure.Affordability;
using Lendora.Infrastructure.Admin;
using Lendora.Infrastructure.Audit;
using Lendora.Infrastructure.BankChecks;
using Lendora.Infrastructure.Data;
using Lendora.Infrastructure.Documents;
using Lendora.Infrastructure.Identity;
using Lendora.Infrastructure.Loans;
using Lendora.Infrastructure.Repayments;
using Lendora.Infrastructure.Risk;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace Lendora.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured.");

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Key), "JWT signing key must be configured.")
            .ValidateOnStart();

        services.Configure<AdminSeedOptions>(configuration.GetSection(AdminSeedOptions.SectionName));
        services.Configure<DocumentStorageOptions>(configuration.GetSection(DocumentStorageOptions.SectionName));
        services.Configure<DocumentOcrOptions>(configuration.GetSection(DocumentOcrOptions.SectionName));

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                npgsql.EnableRetryOnFailure(5);
            });
        });

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key));

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = signingKey,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IAdminLoanProductManagementService, AdminLoanProductManagementService>();
        services.AddScoped<IAdminUserManagementService, AdminUserManagementService>();
        services.AddScoped<IApplicationAuditService, ApplicationAuditService>();
        services.AddScoped<IAffordabilityAssessmentService, AffordabilityAssessmentService>();
        services.AddScoped<IAutomatedBankCheckService, AutomatedBankCheckService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDocumentMetadataService, DocumentMetadataService>();
        services.AddScoped<IDocumentOcrService, DocumentOcrService>();
        services.AddAWSService<IAmazonS3>();
        services.AddAWSService<IAmazonTextract>();
        services.AddScoped<LocalDocumentStorageService>();
        services.AddScoped<S3DocumentStorageService>();
        services.AddScoped<DisabledDocumentTextExtractor>();
        services.AddScoped<TextractDocumentTextExtractor>();
        services.AddScoped<IDocumentStorageService>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<DocumentStorageOptions>>().Value;
            return string.Equals(options.Provider, "S3", StringComparison.OrdinalIgnoreCase)
                ? sp.GetRequiredService<S3DocumentStorageService>()
                : sp.GetRequiredService<LocalDocumentStorageService>();
        });
        services.AddScoped<IDocumentTextExtractor>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<DocumentOcrOptions>>().Value;
            return string.Equals(options.Provider, "Textract", StringComparison.OrdinalIgnoreCase)
                ? sp.GetRequiredService<TextractDocumentTextExtractor>()
                : sp.GetRequiredService<DisabledDocumentTextExtractor>();
        });
        services.AddScoped<ILoanApplicationService, LoanApplicationService>();
        services.AddScoped<IRepaymentScheduleService, RepaymentScheduleService>();
        services.AddScoped<IRiskAssessmentService, RiskAssessmentService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IdentitySeeder>();
        services.AddScoped<LoanProductSeeder>();

        return services;
    }

    public static void ConfigureSerilog(IConfiguration configuration)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .CreateLogger();
    }
}
