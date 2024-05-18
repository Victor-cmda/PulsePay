using Application.Interfaces;
using Application.Interfaces.Application.Interfaces;
using Application.Services;
using Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.Factories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
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
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresDatabase")));

// Registro de servi�os de cache
builder.Services.AddMemoryCache();

// Registro de reposit�rios

// Registro de servi�os
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IAuthenticationPaymentApiService, GetNetAuthenticationService>();

// Registro de factories
builder.Services.AddTransient<IPaymentGatewayFactory, PaymentGatewayFactory>();
builder.Services.AddTransient<IAuthenticationFactory, AuthenticationFactory>();


// Registro de servi�os de integra��o com APIs - Autentica��o
builder.Services.AddHttpClient<GetNetAuthenticationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("OpenCorsPolicy");

app.UseAuthorization();

app.MapControllers();

app.Run();
