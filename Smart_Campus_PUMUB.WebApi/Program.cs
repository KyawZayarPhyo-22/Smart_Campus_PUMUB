using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Smart_Campus_PUMUB.Database.AppDbContext;
using Smart_Campus_PUMUB.WebApi.Models;
using Smart_Campus_PUMUB.WebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Smart_Campus_PUMUB.WebApi.Filters;
var builder = WebApplication.CreateBuilder(args);


builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 100_000_000;
});
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 100_000_000;
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Ignore cyclic references in JSON serializer
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Smart Campus Web API", Version = "v1" });
    
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Input format: Bearer {your_jwt_token}",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header
            },
            new List<string>()
        }
    });
});
builder.Services.AddDbContext<SmartCampusDbContext>(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Mail Service
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IFacultyDataScopeService, FacultyDataScopeService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<IGradeService, GradeService>();

var jwtSettings = new JwtSettings();
builder.Configuration.GetSection("Jwt").Bind(jwtSettings);
builder.Services.AddSingleton(jwtSettings);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtSettings.Secret))
    };
});
// Dynamic Policy Provider
builder.Services.AddSingleton<IAuthorizationPolicyProvider, DynamicPolicyProvider>();
builder.Services.AddAuthorization();

var app = builder.Build();

// Ensure Email, Faculty_Id and RoleHierarchy exist in DB
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<SmartCampusDbContext>();

        // Step 1: Add Email column if missing
        db.Database.ExecuteSqlRaw(@"
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[User]') AND name = 'Email')
            BEGIN
                ALTER TABLE [dbo].[User] ADD [Email] NVARCHAR(150) NULL;
            END
        ");

        // Step 2: Add Faculty_Id column if missing (column only, no FK inline)
        db.Database.ExecuteSqlRaw(@"
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[User]') AND name = 'Faculty_Id')
            BEGIN
                ALTER TABLE [dbo].[User] ADD [Faculty_Id] INT NULL;
            END
        ");

        // Step 3: Add FK constraint separately if it doesn't exist yet
        db.Database.ExecuteSqlRaw(@"
            IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_User_Faculty')
            BEGIN
                ALTER TABLE [dbo].[User]
                    ADD CONSTRAINT [FK_User_Faculty] FOREIGN KEY ([Faculty_Id]) REFERENCES [dbo].[Faculty] ([Faculty_Id]);
            END
        ");

        // Step 4: Add RoleHierarchy table if missing
        db.Database.ExecuteSqlRaw(@"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RoleHierarchy')
            BEGIN
                CREATE TABLE [dbo].[RoleHierarchy] (
                    [Id] INT IDENTITY(1,1) PRIMARY KEY,
                    [Parent_Role_Id] INT NOT NULL,
                    [Child_Role_Id] INT NOT NULL,
                    [CanAccessAllFaculties] BIT NOT NULL DEFAULT 0
                );

                IF EXISTS (SELECT * FROM [dbo].[Role] WHERE Role_Id = 4)
                BEGIN
                    INSERT INTO [dbo].[RoleHierarchy] ([Parent_Role_Id], [Child_Role_Id], [CanAccessAllFaculties])
                    VALUES (4, 1, 1);
                END
            END
        ");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"DB Migration error: {ex.Message}");
    }
}


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();

app.UseStaticFiles(); 

app.UseRouting();


app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
