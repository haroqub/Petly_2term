using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Data;
using Petly.DataAccess;
using Petly.DataAccess.Data;
using Petly.Business.Services;
using Petly.Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddIdentity<ApplicationUser, IdentityRole<int>>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "PetlyAuthCookie";
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
});

builder.Services.AddScoped<PetService>();
builder.Services.AddScoped<NeedService>();
builder.Services.AddScoped<AdoptionService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<SuccessStoryService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<NotificationService>();

builder.Services.AddHttpContextAccessor();
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));


builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.Filters.Add(new AuthorizeFilter(policy));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

//app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
    await EnsureAdoptionApplicationColumnsAsync(dbContext);
    await FullDbInitializer.SeedAsync(services);
}

app.Run();

static async Task EnsureAdoptionApplicationColumnsAsync(ApplicationDbContext dbContext)
{
    await EnsureColumnAsync("applicantName", "ALTER TABLE adoptionapplication ADD COLUMN applicantName varchar(100) NULL");
    await EnsureColumnAsync("applicantSurname", "ALTER TABLE adoptionapplication ADD COLUMN applicantSurname varchar(100) NULL");
    await EnsureColumnAsync("applicantAge", "ALTER TABLE adoptionapplication ADD COLUMN applicantAge int NULL");
    await EnsureColumnAsync("contactInfo", "ALTER TABLE adoptionapplication ADD COLUMN contactInfo varchar(255) NULL");

    async Task EnsureColumnAsync(string columnName, string alterSql)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var existsCommand = connection.CreateCommand();
        existsCommand.CommandText = """
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME = 'adoptionapplication'
              AND COLUMN_NAME = @columnName;
            """;

        var parameter = existsCommand.CreateParameter();
        parameter.ParameterName = "@columnName";
        parameter.Value = columnName;
        existsCommand.Parameters.Add(parameter);

        var exists = Convert.ToInt32(await existsCommand.ExecuteScalarAsync()) > 0;
        if (exists)
        {
            return;
        }

        await dbContext.Database.ExecuteSqlRawAsync(alterSql);
    }
}