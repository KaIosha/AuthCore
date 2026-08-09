using EventHub.Application.Interfaces;
using EventHub.Domain.Entities;
using EventHub.Infrastructure.Data;
using EventHub.Infrastructure.Data.Seed;
using EventHub.Infrastructure.Repositories;
using EventHub.Infrastructure.UnitOfWork;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

var app = builder.Build();

//using (var scope = app.Services.CreateScope())
//{
//    await DbSeeder.SeedAsync(
//        scope.ServiceProvider,
//        app.Configuration["SuperAdmin:Email"],
//        app.Configuration["SuperAdmin:Password"],
//        app.Configuration["SuperAdmin:FirstName"],
//        app.Configuration["SuperAdmin:LastName"]);
//}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(c => { c.SwaggerEndpoint("/openapi/v1.json", "EventHub v1"); });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
