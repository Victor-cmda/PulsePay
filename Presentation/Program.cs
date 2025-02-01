using Application.Interfaces;
using Application.Services;
using Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.Factories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Presentation.Configuration;
using System.Text;
using Serilog;
using Application.DTOs.Pix;
using Domain.Entities.GetNet.Pix;
using Application.Mappers;
using Infrastructure.Adapters.PaymentGateway;
using Infrastructure.Repositories;
using Domain.Entities.K8Pay.BankSlip;
using Application.DTOs.BankSlip;
using Application.Mappers.K8Pay.BankSlip;
using Application.Mappers.K8Pay;
using Application.Mappers.GetNet.Pix;
using Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: null,
        fileSizeLimitBytes: null,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Configure(builder.Configuration.GetSection("Kestrel"));
});

var jwtConfig = builder.Configuration.GetSection("Jwt").Get<JwtConfig>();
var key = Encoding.ASCII.GetBytes(jwtConfig.Key);

builder.Services.AddAuthentication(options =>
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
        ValidIssuer = jwtConfig.Issuer,
        ValidAudience = jwtConfig.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("UserPolicy", policy =>
        policy.RequireClaim("TokenType", "User"));

    options.AddPolicy("ClientPolicy", policy =>
        policy.RequireClaim("TokenType", "Client"));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("OpenCorsPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
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
            new string[] {}
        }
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresDatabase")));

builder.Services.AddMemoryCache();

builder.Services.AddTransient<FileService>();

builder.Services.AddTransient<ITransactionRepository, TransactionRepository>();

builder.Services.AddTransient<ITransactionService, TransactionService>();

//Services
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<IWithdrawService, WithdrawService>();

//Repository
builder.Services.AddScoped<IWalletRepository, WalletRepository>();
builder.Services.AddScoped<IWithdrawRepository, WithdrawRepository>();

builder.Services.AddScoped<IAuthenticationPaymentApiService, GetNetAuthenticationService>();
builder.Services.AddScoped<IAuthenticationPaymentApiService, K8PayAuthenticationService>();


builder.Services.AddHttpClient<GetNetAuthenticationService>();
builder.Services.AddHttpClient<K8PayAuthenticationService>();

// Register mappers
//GetNet
builder.Services.AddTransient<IResponseMapper<GetNetPixResponse, PaymentPixResponseDto>, GetNetPixResponseMapper>();
builder.Services.AddTransient<IResponseMapper<PaymentPixRequestDto, GetNetPixRequest>, GetNetPixRequestMapper>();
//K8Pay
builder.Services.AddTransient<IResponseMapper<K8PayBankSlipResponse, PaymentBankSlipResponseDto>, K8PayBankSlipResponseMapper>();
builder.Services.AddTransient<IResponseMapper<PaymentBankSlipRequestDto, K8PayBankSlipRequest>, K8PayBankSlipRequestMapper>();


builder.Services.AddSingleton<IResponseMapperFactory, ResponseMapperFactory>();

// Register adapters
builder.Services.AddTransient<GetNetAdapter>();
builder.Services.AddTransient<K8PayAdapter>();

// Register Factories
builder.Services.AddTransient<IPaymentGatewayFactory, PaymentGatewayFactory>();
builder.Services.AddTransient<IAuthenticationFactory, AuthenticationFactory>();


var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PulsePay API v1");
});

app.UseHttpsRedirection();

app.UseCors("OpenCorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
