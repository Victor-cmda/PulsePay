using Application.DTOs.BankSlip;
using Application.DTOs.Pix;
using Application.Interfaces;
using Application.Mappers;
using Application.Mappers.GetNet.Pix;
using Application.Mappers.K8Pay;
using Application.Mappers.K8Pay.BankSlip;
using Application.Services;
using Application.Validators;
using Domain.Entities.GetNet.Pix;
using Domain.Entities.K8Pay.BankSlip;
using Domain.Interfaces;
using FluentValidation;
using Infrastructure.Adapters.PaymentGateway;
using Infrastructure.Data;
using Infrastructure.Factories;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;

namespace Infrastructure.DI
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtConfig = configuration.GetSection("Jwt");
            var key = Encoding.ASCII.GetBytes(jwtConfig["Key"]);

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
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtConfig["Issuer"],
                    ValidAudience = jwtConfig["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    NameClaimType = ClaimTypes.Name,
                    RoleClaimType = ClaimTypes.Role
                };
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("UserPolicy", policy =>
                    policy.RequireClaim("TokenType", "User"));

                options.AddPolicy("ClientPolicy", policy =>
                    policy.RequireClaim("TokenType", "Client"));

                options.AddPolicy("AdminPolicy", policy =>
                    policy.RequireRole("Admin"));
            });

            return services;
        }

        public static IServiceCollection AddApplicationCors(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("OpenCorsPolicy", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            return services;
        }

        public static IServiceCollection AddApplicationSwagger(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "PulsePay API", Version = "v1" });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            },
                            Scheme = "oauth2",
                            Name = "Bearer",
                            In = ParameterLocation.Header
                        },
                        Array.Empty<string>()
                    }
                });
            });

            return services;
        }

        public static IServiceCollection AddApplicationDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("PostgresDatabase")));

            return services;
        }

        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Services
            services.AddTransient<FileService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<IWalletService, WalletService>();
            services.AddScoped<IBankAccountService, BankAccountService>();
            services.AddScoped<IWalletTransactionService, WalletTransactionService>();
            services.AddTransient<ITransactionService, TransactionService>();
            services.AddScoped<IWithdrawService, WithdrawService>();
            services.AddScoped<IDepositService, DepositService>();
            services.AddScoped<ICustomerPayoutService, CustomerPayoutService>();
            services.AddScoped<IPixService, PixService>();
            services.AddScoped<IListenerService, ListenerService>();
            services.AddScoped<IRefundService, RefundService>();

            services.AddSingleton<IPixValidationCacheService, PixValidationCacheService>();

            return services;
        }

        public static IServiceCollection AddApplicationRepositories(this IServiceCollection services)
        {
            // Repositories
            services.AddTransient<ITransactionRepository, TransactionRepository>();
            services.AddScoped<IWalletRepository, WalletRepository>();
            services.AddScoped<IBankAccountRepository, BankAccountRepository>();
            services.AddScoped<IWalletTransactionRepository, WalletTransactionRepository>();
            services.AddScoped<IWithdrawRepository, WithdrawRepository>();
            services.AddScoped<IDepositRepository, DepositRepository>();
            services.AddScoped<ICustomerPayoutRepository, CustomerPayoutRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IRefundRepository, RefundRepository>();

            return services;
        }

        public static IServiceCollection AddPaymentGatewayServices(this IServiceCollection services)
        {
            // Authentication Services
            services.AddScoped<IAuthenticationPaymentApiService, GetNetAuthenticationService>();
            services.AddScoped<IAuthenticationPaymentApiService, K8PayAuthenticationService>();
            services.AddHttpClient<GetNetAuthenticationService>();
            services.AddHttpClient<K8PayAuthenticationService>();

            // Adapters
            services.AddTransient<GetNetAdapter>();
            services.AddTransient<K8PayAdapter>();

            // Factories
            services.AddTransient<IPaymentGatewayFactory, PaymentGatewayFactory>();
            services.AddTransient<IAuthenticationFactory, AuthenticationFactory>();

            return services;
        }

        public static IServiceCollection AddMappers(this IServiceCollection services)
        {
            // GetNet Mappers
            services.AddTransient<IResponseMapper<GetNetPixResponse, PaymentPixResponseDto>, GetNetPixResponseMapper>();
            services.AddTransient<IResponseMapper<PaymentPixRequestDto, GetNetPixRequest>, GetNetPixRequestMapper>();

            // K8Pay Mappers
            services.AddTransient<IResponseMapper<K8PayBankSlipResponse, PaymentBankSlipResponseDto>, K8PayBankSlipResponseMapper>();
            services.AddTransient<IResponseMapper<PaymentBankSlipRequestDto, K8PayBankSlipRequest>, K8PayBankSlipRequestMapper>();

            // Mapper Factory
            services.AddSingleton<IResponseMapperFactory, ResponseMapperFactory>();

            return services;
        }

        public static IServiceCollection AddValidators(this IServiceCollection services)
        {
            services.AddScoped<IDocumentValidator, DocumentValidator>();
            services.AddScoped<IPixKeyValidator, PixKeyValidator>();

            // Registrar todos os validadores do FluentValidation
            // Nota: Este método requer o pacote FluentValidation.DependencyInjectionExtensions
            services.AddValidatorsFromAssemblyContaining<BankAccountCreateDtoValidator>();

            return services;
        }
    }
}