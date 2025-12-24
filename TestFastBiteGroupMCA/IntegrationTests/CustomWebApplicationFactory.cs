using FastBiteGroupMCA.API;
using FastBiteGroupMCA.Persistentce;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore.InMemory; 
using System.Linq;

namespace TestFastBiteGroupMCA.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType ==
                    typeof(DbContextOptions<ApplicationDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryDbForTesting");
            });

            var sp = services.BuildServiceProvider();

            using (var scope = sp.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<ApplicationDbContext>();
                var logger =
                    scopedServices.GetRequiredService<ILogger<CustomWebApplicationFactory>>();

                db.Database.EnsureCreated();

                try
                {
                    // Seed the database with test data if needed
                }
                catch (System.Exception ex)
                {
                    logger.LogError(ex, "An error occurred seeding the database. Error: {Message}", ex.Message);
                }
            }
        });
    }
}
