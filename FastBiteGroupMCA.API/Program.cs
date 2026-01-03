using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using FastBiteGroupMCA.API.Converter;
using FastBiteGroupMCA.API.Hubs;
using Microsoft.Azure.SignalR;
using FastBiteGroupMCA.API.Midleware;
using FastBiteGroupMCA.Application.DependencyInjection.Extensions;
using FastBiteGroupMCA.Infastructure.DependencyInjection.Extension;
using FastBiteGroupMCA.Infastructure.Hubs;
using FastBiteGroupMCA.Persistentce.DependencyInjection.Extensions;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using Serilog;
using Serilog.Events;
using System.Reflection;
using System.Text.Json.Serialization;

namespace FastBiteGroupMCA.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateBootstrapLogger();
            try
            {
                Log.Information("Starting FastBiteGroupMCA API application...");

                var builder = WebApplication.CreateBuilder(args);

                var configuration = builder.Configuration;

                builder.Host.UseSerilog((context, services, config) =>
                {
                    config
                        .ReadFrom.Configuration(context.Configuration)
                        .Enrich.FromLogContext();
                });

                builder.Services.AddLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddSerilog();
                });

                builder.Services.AddControllers()
                    .AddJsonOptions(options =>
                    {
                        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                        options.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());
                    });
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen();
                // add tầng service Persistence
                builder.Services.AddPersistentceService(builder.Configuration, builder.Environment);
                // add tầng repository
                builder.Services.AddPersistentceRepository();
                // add tầng service Infastructure
                builder.Services.UseInfastructureService(builder.Configuration);
                // add tầng service Application
                builder.Services.AddApplicationService(builder.Configuration);
                var connectionStringSignalR = configuration.GetConnectionString("AzureSignalR");

                builder.Services.AddSignalR();
                    //.AddAzureSignalR(connectionStringSignalR);

                builder.Services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("Public-v1", new OpenApiInfo
                    {
                        Title = "FastBiteGroup API - Public", 
                        Version = "v1",
                        Description = "APIs for general users and collaborative features.",
                        TermsOfService = new Uri("https://example.com/terms"),
                        Contact = new OpenApiContact
                        {
                            Name = "Your Team Name",
                            Url = new Uri("https://example.com/contact")
                        },
                        License = new OpenApiLicense
                        {
                            Name = "Your License",
                            Url = new Uri("https://example.com/license")
                        }
                    });

                    c.SwaggerDoc("Admin-v1", new OpenApiInfo
                    {
                        Title = "FastBiteGroup API - Admin",
                        Version = "v1",
                        Description = "APIs for system administration."
                    });
                    c.SwaggerDoc("Dev-v1", new OpenApiInfo { Title = "FastBiteGroup API - Dev/Test", Version = "v1" });

                    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                    {
                        BearerFormat = "JWT",
                        Description = "JWT Authorization header using the Bearer scheme (Example: 'Bearer 12345abcdef')",
                        Name = "Authorization",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.Http,
                        Scheme = "Bearer"
                    });

                    c.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                }
                            },
                            Array.Empty<string>()
                        }
                    });
                    c.CustomSchemaIds(type => type.FullName);
                    c.UseOneOfForPolymorphism();
                    c.UseAllOfForInheritance();
                    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));

                });
                builder.Services.AddCors(options =>
                {
                    options.AddPolicy("AllowOrigins", policy =>
                    {
                        policy.WithOrigins("http://localhost:3000", "https://localhost:3000")
                              .AllowAnyMethod()
                              .AllowAnyHeader()
                              .AllowCredentials();
                    });
                });
                builder.Services.ConfigureApplicationCookie(options =>
                {
                    options.Events.OnRedirectToLogin = context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    };
                });

                var app = builder.Build();

                app.UseSerilogRequestLogging(options =>
                {
                    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                    {
                        diagnosticContext.Set("RequestMethod", httpContext.Request.Method);
                        diagnosticContext.Set("RequestPath", httpContext.Request.Path);
                        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                        diagnosticContext.Set("UserId", httpContext.User?.FindFirst("sub")?.Value ?? "Anonymous");
                    };
                    options.GetLevel = (httpContext, elapsed, ex) =>
                    {
                        if (ex != null) return LogEventLevel.Error;
                        if (httpContext.Response.StatusCode >= 500) return LogEventLevel.Error;
                        if (httpContext.Response.StatusCode >= 400) return LogEventLevel.Warning;
                        return LogEventLevel.Information;
                    };
                });

                // Configure the HTTP request pipeline.
                if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI(c =>
                    {
                        c.SwaggerEndpoint("/swagger/Public-v1/swagger.json", "Public API v1");
                        c.SwaggerEndpoint("/swagger/Admin-v1/swagger.json", "Admin API v1");
                        c.SwaggerEndpoint("/swagger/Dev-v1/swagger.json", "Dev/Test API v1");
                    });
                }
                await app.SeedDatabaseAsync();

                using (var scope = app.Services.CreateScope())
                {
                    var mongoDatabase = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                    try
                    {
                        logger.LogInformation("Creating MongoDB indexes...");
                        await mongoDatabase.CreateIndexesAsync();
                        logger.LogInformation("MongoDB indexes created successfully.");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "An error occurred while creating MongoDB indexes.");
                    }
                }

                app.UseHttpsRedirection();

                app.UseCors("AllowOrigins");

                app.UseWebSockets();


                app.UseRouting();

                
                // add middleware cho tầng service Infastructure
                app.UsenfastructureService();

                app.UseAuthentication();
                app.UseAuthorization();

                // thêm cấc hub SignalR
                app.MapHub<ChatHub>("hubs/chatHub");
                app.MapHub<VideoCallHub>("/hubs/videocall");
                app.MapHub<PresenceHub>("/hubs/presence");
                app.MapHub<NotificationsHub>("/hubs/notifications");
                app.MapHub<AdminHub>("/hubs/admin");
                app.MapHub<PostsHub>("/hubs/posts");

                app.UseMiddleware<MaintenanceMiddleware>();
                app.UseMiddleware<ExceptionMiddleware>();

                app.MapControllers();

                await app.RunAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application start-up failed");
                throw;
            }
            finally
            {
                await Log.CloseAndFlushAsync();
            }
            
        }
    }
}
