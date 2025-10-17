using CareNirvana.DataAccess;
using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Application.Services;
using CareNirvana.Service.Application.UseCases;
using CareNirvana.Service.Infrastructure.Repository;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.Filters;
using System.Text;

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
builder.Services.AddScoped<IAuthActivityRepository, AuthActivityRepository>();
builder.Services.AddScoped<ICodesetsRepository, CodesetsRepository>();
builder.Services.AddScoped<IRolePermissionConfigRepository, RolePermissionConfigRepository>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IMemberEnrollmentRepository, MemberEnrollmentRepository>();
builder.Services.AddScoped<IMemberNotes, MemberNotesRepository>();
builder.Services.AddScoped<IMemberDocument, MemberDocumentRepository>();
builder.Services.AddScoped<IMemberCareGiverRepository, MemberCareGiverRepository>();
builder.Services.AddScoped<IMemberCareTeamRepository, MemberCareTeamRepository>();
builder.Services.AddScoped<IMemberProgramRepository, MemberProgramRepository>();
builder.Services.AddScoped<IMemberAlertRepository, MemberAlertsRepository>();
builder.Services.AddScoped<IMemberJourney, MemberJourneyRepository>();
builder.Services.AddScoped<IRecentlyAccessed, RecentlyAccessedRepository>();

var allowedOrigins = new[] {
    "http://localhost:4200",
    "https://proud-field-09c04620f.5.azurestaticapps.net",
    "https://proud-coast-0237bd90f.6.azurestaticapps.net"
};

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

app.Logger.LogInformation("CORS policy 'AllowAngularApp' applied.");

app.Use((context, next) =>
{
    Console.WriteLine($"Request received: {context.Request.Method} {context.Request.Path}");
    if (context.Request.Method == "OPTIONS")
    {
        context.Response.Headers.Add("Access-Control-Allow-Origin", "https://proud-coast-0237bd90f.6.azurestaticapps.net/");
        context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
        context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
        context.Response.Headers.Add("Access-Control-Max-Age", "86400");
        context.Response.StatusCode = 200;
        Console.WriteLine("OPTIONS response sent with status 200");
        return Task.CompletedTask;
    }
    return next();
});

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.Use(async (context, next) =>
{
    context.Response.ContentType = "application/json";
    await next();
});

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        var error = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        if (error != null)
        {
            await context.Response.WriteAsync($"{{ \"error\": \"{error.Error.Message}\" }}");
        }
    });
});


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();