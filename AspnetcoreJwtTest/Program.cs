using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using AspnetcoreJwtTest;
using Microsoft.AspNetCore.Identity;
using AspnetcoreJwtTest.Interfaces;
using AspnetcoreJwtTest.Services;
using AspnetcoreJwtTest.Extensions;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using AspnetcoreJwtTest.Entities;

var builder = WebApplication.CreateBuilder(args);

// **1. Add Controllers**
builder.Services.AddControllers();

// **2. Add Scalar for API documentation**
builder.Services.AddOpenApi();

// **3. Configure Database Context**
builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10); // Set lockout duration to 3 minutes
    options.Lockout.MaxFailedAccessAttempts = 3; // Max failed attempts before lockout
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@"; // Username validation
    options.User.RequireUniqueEmail = true; // Ensure unique email addresses
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();
//// **4. Configure Identity**
//builder.Services.AddIdentity<IdentityUser, IdentityRole>()
//    .AddEntityFrameworkStores<ApplicationDbContext>()
//    .AddDefaultTokenProviders();

// **5. Configure JWT Authentication**
var jwtSettings = builder.Configuration.GetSection("Jwt");

// Ensure JWT key exists
string secretKey = jwtSettings["Key"];
if (string.IsNullOrEmpty(secretKey) || secretKey.Length < 16)
{
    throw new Exception("JWT Key is missing or too short. It must be at least 16 characters.");
}

var key = Encoding.UTF8.GetBytes(secretKey);

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
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.FromMinutes(5) // Ensures token expiration is exact
    };

    options.Events = new JwtBearerEvents
    {
        OnChallenge = async context =>
        {
            if (!context.Response.HasStarted) // Ensure response is not already sent
            {
                context.HandleResponse(); // Prevent default 401 response

                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";

                await context.Response.WriteAsync("{\"error\": \"Unauthorized\", \"message\": \"The token has expired.\"}");
            }
        }
    };
});

// **6. Register Services**
builder.Services.AddScoped<ITokenBuilder, TokenBuilder>();
// Add CORS policy
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowAll", builder =>
//    {
//        builder.AllowAnyOrigin()
//               .AllowAnyMethod()
//               .AllowAnyHeader();
//    });
//});

// **7. Build App**
var app = builder.Build();

// **8. Configure Scalar API Docs**
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // Enable OpenAPI in development
    app.MapScalarApiReference("/docs/scalar"); // Scalar API documentation
}

// **9. Middleware Setup**
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// **10. Handle Expired Token Errors**
app.Use(async (context, next) =>
{
    await next();

    if (context.Response.StatusCode == 401)
    {
        //context.Response.ContentType = "application/json";
        await context.Response.WriteAsync("{\"error\": \"Unauthorized\", \"message\": \"The token has expired.\"}");
    }
});

// **11. Map Controllers**
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    var roles = new[] { "Admin", "User" };

    // Create roles if they don't exist
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    // Create admin user if it doesn't exist
    string adminEmail = "admin@admin.com";
    string adminPassword = "Password@123";

    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new IdentityUser { UserName = adminEmail, Email = adminEmail };
        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}
    //string email = "admin@admin.com";
    //string password = "Password@123";

    //if (await UserManager.FindbyEmailAsync(email) == null)
    //    var user = newIdentityUSer();
    //User.Name = email;
    //User.Email = email;

    //UserManager.CreateAsync(User,password)
//}

// **12. Run Application**
app.Run();
