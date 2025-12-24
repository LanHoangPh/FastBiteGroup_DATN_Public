using FastBiteGroupMCA.Domain.Abstractions.Repository.EFCore;
using FastBiteGroupMCA.Persistentce.Repositories;
using FastBiteGroupMCA.Persistentce.Repositories.Efcore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

namespace FastBiteGroupMCA.Persistentce.DependencyInjection.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddPersistentceService(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            // Chỉ bật các log chi tiết khi ở môi trường Development
            if (environment.IsDevelopment())
            {
                options.EnableDetailedErrors(true);
                options.EnableSensitiveDataLogging(true);
            }
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlServerOptionsAction: sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                });
        });
        services.AddSingleton<IMongoClient>(sp =>
                new MongoClient(sp.GetRequiredService<IConfiguration>()["MongoDbSettings:ConnectionString"]!));

        services.AddScoped<IMongoDatabase>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var client = sp.GetRequiredService<IMongoClient>();
            var dbName = config["MongoDbSettings:DatabaseName"]!;
            return client.GetDatabase(dbName);
        });
        services.AddIdentity<AppUser, AppRole>(
            options =>
            {
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireDigit = true;

                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;

                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
        return services;
    }
    public static IServiceCollection AddPersistentceRepository(this IServiceCollection services) 
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IMessagesRepository, MessagesRepository>();
        services.AddScoped<INotificationsRepository, NotificationsRepository>();
        services.AddScoped<IContentReportsRepository, ContentReportsRepository>();
        services.AddScoped<IVideoCallSessionsRepository, VideoCallSessionsRepository>();
        services.AddScoped<IVideoCallParticipantsRepository, VideoCallParticipantsRepository>();
        return services;
    }
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app;
    }
}
