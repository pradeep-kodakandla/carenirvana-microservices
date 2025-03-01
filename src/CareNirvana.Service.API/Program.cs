using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Swashbuckle.AspNetCore.Filters;
using CareNirvana.Service.Application.Services;
using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Infrastructure.Repository;
using CareNirvana.DataAccess;
using CareNirvana.Service.Application.UseCases;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
});

var key = Encoding.ASCII.GetBytes("bP3!x5$G8@r9ZyL2WqT4!bN7eK1sD#uV");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer"
    });
    options.OperationFilter<SecurityRequirementsOperationFilter>();
});

builder.Services.AddScoped<IAbstractDataLayer, AbstractDataLayer>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthTemplateRepository, AuthTemplateRepository>();
builder.Services.AddScoped<IAuthDetailRepository, AuthDetailRepository>();
builder.Services.AddTransient<GetAuthTemplatesQuery>();
builder.Services.AddTransient<SaveAuthDetailCommand>();
builder.Services.AddScoped<IConfigAdminService, ConfigAdminService>();
builder.Services.AddScoped<IConfigAdminRepository, ConfigAdminRepository>();

var allowedOrigins = new[] { "http://localhost:4200", "https://proud-field-09c04620f.5.azurestaticapps.net", "https://prod.angular-app.com" };
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp",
        policy => policy.WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials());
});

var app = builder.Build();

app.UseCors("AllowAngularApp");

// Explicit OPTIONS handling
app.Use(async (context, next) =>
{
    if (context.Request.Method == "OPTIONS")
    {
        var origin = context.Request.Headers["Origin"].ToString();
        // Optionally, validate that 'origin' is in your allowed origins list
        context.Response.Headers.Add("Access-Control-Allow-Origin", origin);
        context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
        context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
        context.Response.Headers.Add("Access-Control-Allow-Credentials", "true");
        context.Response.Headers.Add("Access-Control-Max-Age", "86400");
        context.Response.StatusCode = 200;
        return;
    }
    await next();
});


app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.Use(async (context, next) =>
{
    context.Response.ContentType = "application/json";
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();