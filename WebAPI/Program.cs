using System.Text;
using Application.Interfaces;
using Application.Services;
using Application.Services.Jobs;
using Domain.Abstractions;
using Hangfire;
using Hangfire.PostgreSql;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WebAPIDocker.Middlewares;

var builder = WebApplication.CreateBuilder(args);

        var rpcUrl = "https://sepolia.infura.io/v3/13c2c1b2df36470898405a574a72ac65";

        var contractAddress = "0x4b5DC8DC92492c52E23De4975e30FCD5f550222F";

        var senderPrivateKey = "2a1a4a534cceafbe4eef95d3b9ea502730edac8bc629ec3a9c8194e1ff0a561b";
        // Đăng ký BlockchainService vào DI container
        builder.Services.AddSingleton<IBlockchainService>(provider =>
            new BlockchainService(rpcUrl, contractAddress, senderPrivateKey));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy => policy
            .WithOrigins("http://localhost:5175","http://localhost:5173") // domain frontend
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
    );
});



// Add services

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = 
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddScoped<InvoiceSnapshotService>();

builder.Services.AddScoped<IUserService, UserAccountService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();

builder.Services.AddScoped<ICustomerService, CustomerService>();

builder.Services.AddScoped<IGeminiService, GeminiService>();
builder.Services.AddScoped<IUserService, UserAccountService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IPaymentGateway, PaymentGatewayService>();
builder.Services.AddScoped<IInvoiceRequestService, InvoiceRequestService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IServicePackageService, ServicePackageService>();
builder.Services.AddScoped<IServicePackageRepository, ServicePackageRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddHttpClient<IGeminiService, GeminiService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Đăng ký DbContext với PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Hangfire
builder.Services.AddHangfire(config => 
    config.UsePostgreSqlStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHangfireServer();

// Đăng ký job
builder.Services.AddScoped<InvoiceVerificationJob>();


var app = builder.Build();


// Áp dụng migration tự động khi app khởi động
using (var scope = app.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    recurringJobManager.AddOrUpdate<InvoiceVerificationJob>(
        "invoice-verification-job",           // job id
        job => job.VerifyInvoicesAsync(),     // phương thức
        Cron.MinuteInterval(5)                // cron expression
    );
}

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseMiddleware<ExceptionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
