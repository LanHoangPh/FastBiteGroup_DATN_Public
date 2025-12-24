using FastBiteGroupMCA.Persistentce.SeedData;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace FastBiteGroupMCA.Persistentce.DependencyInjection.Extensions;

public static class DataSeedingExtensions
{
    public static async Task<IApplicationBuilder> SeedDatabaseAsync(this IApplicationBuilder app)
    {
        await ApplyDatabaseMigrations(app.ApplicationServices); 

        var environment = app.ApplicationServices.GetService<IWebHostEnvironment>();
        if (environment != null && !environment.IsDevelopment())
        {
            return app;
        }

        using (var scope = app.ApplicationServices.CreateScope())
        {
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<IApplicationBuilder>>();
            try
            {
                var dbContext = services.GetRequiredService<ApplicationDbContext>();
                var userManager = services.GetRequiredService<UserManager<AppUser>>();
                var roleManager = services.GetRequiredService<RoleManager<AppRole>>();
                var mongoDatabase = services.GetRequiredService<IMongoDatabase>();

                if (!dbContext.Groups.Any())
                {
                    logger.LogInformation("Database contains no groups. Seeding all data...");

                    var adminRole = await roleManager.FindByNameAsync("Admin");
                    var customerRole = await roleManager.FindByNameAsync("Customer");

                    if (adminRole == null || customerRole == null)
                    {
                        logger.LogError("CRITICAL: Admin or Customer role not found in the database. Please seed roles first.");
                        return app;
                    }

                    var generatedData = DataGenerator.Seed();

                    logger.LogInformation("Creating {Count} fake users...", generatedData.Users.Count);
                    foreach (var user in generatedData.Users)
                    {
                        if (await userManager.FindByEmailAsync(user.Email) == null)
                        {
                            var result = await userManager.CreateAsync(user, "User@123");
                            if (result.Succeeded)
                            {
                                await userManager.AddToRoleAsync(user, customerRole.Name!);
                            }
                            else
                            {
                                logger.LogError("Error creating user {Email}: {Errors}", user.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                            }
                        }
                    }

                    var firstUser = generatedData.Users.FirstOrDefault();
                    if (firstUser != null && !await userManager.IsInRoleAsync(firstUser, adminRole.Name!))
                    {
                        await userManager.AddToRoleAsync(firstUser, adminRole.Name!);
                    }

 
                    logger.LogInformation("Seeding other SQL & MongoDB data...");

                    await dbContext.Groups.AddRangeAsync(generatedData.Groups);
                    await dbContext.GroupMembers.AddRangeAsync(generatedData.GroupMembers);
                    await dbContext.Conversations.AddRangeAsync(generatedData.Conversations);
                    await dbContext.ConversationParticipants.AddRangeAsync(generatedData.ConversationParticipants);
                    await dbContext.Posts.AddRangeAsync(generatedData.Posts);
                    await dbContext.PostComments.AddRangeAsync(generatedData.Comments);
                    await dbContext.ContentReports.AddRangeAsync(generatedData.Reports);

                    await dbContext.SaveChangesAsync();

                    if (generatedData.Messages.Any())
                    {
                        var messageCollection = mongoDatabase.GetCollection<Messages>("messages");
                        await messageCollection.DeleteManyAsync(FilterDefinition<Messages>.Empty);
                        await messageCollection.InsertManyAsync(generatedData.Messages);
                    }

                    logger.LogInformation("Data seeding completed successfully!");
                }
                else
                {
                    logger.LogInformation("Database already contains data. Skipping seeding.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during data seeding.");
            }
        }
        return app;
    }
    private static async Task ApplyDatabaseMigrations(IServiceProvider serviceProvider)
    {
        using (var scope = serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<IApplicationBuilder>>();

            try
            {
                logger.LogInformation("Applying database migrations...");
                await dbContext.Database.MigrateAsync();
                logger.LogInformation("Database migrations applied successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while applying database migrations.");
            }
        }
    }
}
