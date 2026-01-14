using System.Text.Json.Serialization;
using PaymentGateway.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Serialize enums as strings in JSON responses
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
    
// Configure Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure HttpClient for acquirer integration
builder.Services.AddHttpClient("AcquirerClient", client =>
{
    var acquirerConfig = builder.Configuration.GetSection("AcquirerService");
    client.BaseAddress = new Uri(acquirerConfig["BaseUrl"] ?? "http://localhost:8080");
    client.Timeout = TimeSpan.FromSeconds(int.Parse(acquirerConfig["TimeoutSeconds"] ?? "10"));
});

// Register services
builder.Services.AddSingleton<PaymentsRepository>();
builder.Services.AddScoped<IAcquirerService, AcquirerService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

// Required for integration tests
public partial class Program { }
