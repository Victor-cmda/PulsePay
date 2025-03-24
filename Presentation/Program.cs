using Application.BackgroundService;
using Infrastructure.DI;
using Infrastructure.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
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

// Configure Kestrel
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Configure(builder.Configuration.GetSection("Kestrel"));
});

builder.Services.AddHttpClient();

// Register services with extension methods
builder.Services
    .AddApplicationAuthentication(builder.Configuration)
    .AddApplicationCors()
    .AddControllers();

builder.Services
    .AddApplicationSwagger()
    .AddApplicationDatabase(builder.Configuration)
    .AddMemoryCache()
    .AddApplicationServices()
    .AddApplicationRepositories()
    .AddPaymentGatewayServices()
    .AddMappers()
    .AddValidators()
    .AddHostedService<NotificationRetryService>()
    .AddEndpointsApiExplorer();

var app = builder.Build();

// Configure middleware
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PulsePay API v1");
});

app.UseHttpsRedirection();
app.UseCors("OpenCorsPolicy");
app.UseGlobalExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();