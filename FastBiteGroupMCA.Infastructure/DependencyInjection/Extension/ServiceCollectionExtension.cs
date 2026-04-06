using Azure;
using Azure.AI.ContentSafety;
using FastBiteGroupMCA.Application.IServices.Auth;
using FastBiteGroupMCA.Application.IServices.BackgroundJob;
using FastBiteGroupMCA.Application.IServices.FileStorage;
using FastBiteGroupMCA.Infastructure.Caching;
using FastBiteGroupMCA.Infastructure.DependencyInjection.Options;
using FastBiteGroupMCA.Infastructure.Filters;
using FastBiteGroupMCA.Infastructure.Messaging;
using FastBiteGroupMCA.Infastructure.Redis;
using FastBiteGroupMCA.Infastructure.Services;
using FastBiteGroupMCA.Infastructure.Services.BackgroundJob;
using FastBiteGroupMCA.Infastructure.Services.FileStorage;
using FastBiteGroupMCA.Infastructure.Services.Token;
using Ganss.Xss;
using Hangfire;
using Livekit.Server.Sdk.Dotnet;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SendGrid;
using StackExchange.Redis;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;

namespace FastBiteGroupMCA.Infastructure.DependencyInjection.Extension
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection UseInfastructureService(this IServiceCollection services, IConfiguration config)
        {
            // Add your infrastructure services here

            // Azure Content Safety Client
            services.AddSingleton(sp => {
                var config = sp.GetRequiredService<IConfiguration>();
                var endpoint = config["AzureContentSafety:Endpoint"];
                var key = config["AzureContentSafety:Key"];

                if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(key))
                {
                    throw new InvalidOperationException("Azure Content Safety Endpoint and Key must be configured.");
                }

                return new ContentSafetyClient(new Uri(endpoint), new AzureKeyCredential(key));
            });

            // Redis
            var redisConnectionString = config.GetConnectionString("RedisCloud"); 

            var options = ConfigurationOptions.Parse(redisConnectionString!);
            options.Ssl = false;
            options.SslProtocols = SslProtocols.Tls13; 
            options.ConnectTimeout = 5000; 
            options.AbortOnConnectFail = false;

            options.CertificateValidation += (sender, certificate, chain, sslPolicyErrors) => true;

            services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(options));

            services.AddScoped<ICacheService, RedisService>();
            services.AddSingleton<IRedisKeyManager, RedisKeyManager>();
            services.AddSingleton<IPubSubService, RedisPubSubService>();

            // Hangfire
            var connectionString = config.GetConnectionString("DefaultConnection");

            services.AddHangfire(config => config
                    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseSqlServerStorage(connectionString));

            // Background job service
            services.AddHangfireServer();
            services.AddScoped<IDataRetentionService, DataRetentionService>();
            services.AddScoped<IReadReceiptProcessor, ReadReceiptProcessor>();

            // JWT Authentication
            services.AddAuthentication(options =>
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
                    ValidIssuer = config["Jwt:Issuer"],
                    ValidAudience = config["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!)),
                    ClockSkew = TimeSpan.Zero
                };
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();

                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";
                        var apiResponse = ApiResponse<string>.Fail(
                            "AUTH_UNAUTHORIZED",
                            "Bạn cần đăng nhập để thực hiện hành động này."
                        );

                        await context.Response.WriteAsync(JsonSerializer.Serialize(apiResponse));
                    },
                    OnForbidden = async context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/json";

                        var apiResponse = ApiResponse<string>.Fail(
                            "AUTH_FORBIDDEN",
                            "Bạn không có quyền thực hiện hành động này."
                        );

                        await context.Response.WriteAsync(JsonSerializer.Serialize(apiResponse));
                    },
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };

            });
            services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
                options.AddPolicy("VIPAccess", policy => policy.RequireRole("VIP", "Admin"));
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser() // Chỉ yêu cầu đăng nhập
                        .Build();
            });

            // SendGrid Email Service
            services.AddSingleton<ISendGridClient>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                return new SendGridClient(config["SendGrid:ApiKey"]);
            });

            // LiveKit
            services.Configure<LiveKitSettings>(config.GetSection("LiveKit"));

            var liveKitSettings = config.GetSection("LiveKit").Get<LiveKitSettings>();
            if (liveKitSettings == null || string.IsNullOrEmpty(liveKitSettings.Url) ||
                    string.IsNullOrEmpty(liveKitSettings.ApiKey) || string.IsNullOrEmpty(liveKitSettings.ApiSecret))
            {
                throw new ArgumentException("LiveKit settings are not properly configured in appsettings.json");
            }
            services.AddSingleton<RoomServiceClient>(sp =>
                new RoomServiceClient(liveKitSettings.Url.Replace("wss://", "https://"), liveKitSettings.ApiKey, liveKitSettings.ApiSecret)
            );

            //OneSignalService
            var oneSignalSettings = config.GetSection("OneSignal").Get<OneSignalSettings>();
            if (oneSignalSettings == null || string.IsNullOrEmpty(oneSignalSettings.AppId) || string.IsNullOrEmpty(oneSignalSettings.RestApiKey))
            {
                throw new ArgumentException("OneSignal settings are not properly configured in appsettings.json");
            }
            services.AddHttpClient<IOneSignalService, OneSignalService>(client =>
                    {
                        client.BaseAddress = new Uri("https://onesignal.com/api/v1/");
                        client.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue("Basic", oneSignalSettings.RestApiKey);
                    });

            // HTML Sanitizer
            services.AddSingleton<IHtmlSanitizer>(new HtmlSanitizer());

            ////Storage Options
            //services.Configure<FirebaseStorageOptions>(
            //    config.GetSection(FirebaseStorageOptions.SectionName)
            //);
            services.Configure<CloudinaryOptions>(config.GetSection("Cloudinary"));
            services.Configure<AmazonS3Options>(config.GetSection("AmazonS3Settings"));
            services.Configure<AzureStorageOptions>(config.GetSection(AzureStorageOptions.SectionName));

            services.AddScoped<CloudinaryStorageService>();
            //services.AddScoped<FirebaseStorageService>();
            services.AddScoped<AmazonS3StorageService>();
            services.AddScoped<AzureBlobStorageService>();
            

            // Đăng ký các dịch vụ lưu trữ tệp
            services.AddSingleton<IFileStorageService, CloudinaryStorageService>();
            //services.AddSingleton<IFileStorageService, FirebaseStorageService>();pi
            services.AddSingleton<IFileStorageService, AmazonS3StorageService>();
            services.AddSingleton<IFileStorageService, AzureBlobStorageService>();

            // Đăng ký DashboardService
            services.AddScoped<IAdminDashboardService, AdminDashboardService>();
            services.AddScoped<StorageStrategy>();
            services.AddSingleton<PresenceTracker>();
            
            services.AddTransient<HangfireJobFailureNotifierFilter>();

            //
            services.AddScoped<IVideoCallService, VideoCallService>();
            services.AddScoped<IMessageService, MessageService>();
            services.AddScoped<IChatService, ChatService>();
            services.AddScoped<IPollService, PollService>();
            services.AddScoped<IConversationService, ConversationService>();
            services.AddScoped<IGroupService, GroupService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IOtpService, OtpService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IInvitationService, InvitationService>();
            services.AddScoped<IFileService, FileService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IPostService, PostService>();
            services.AddScoped<IGroupModerationService, GroupModerationService>();
            services.AddScoped<IContentReportService, ContentReportService>();
            services.AddScoped<IAdminNotificationService, AdminNotificationService>();
            services.AddScoped<IAdminGroupService, AdminGroupService>();
            services.AddScoped<IAdminUserService, AdminUserService>();
            services.AddScoped<ILiveKitService, LiveKitService>();
            services.AddScoped<IAdminAuditLogService, AdminAuditLogService>();
            services.AddScoped<IAdminExportService, AdminExportService>();
            services.AddScoped<IUserPresenceService, RedisUserPresenceService>();
            services.AddScoped<IContentModerationService, ContentModerationService>();
            services.AddScoped<IContentRendererService, ContentRendererService>();
            services.AddScoped<ICommentService, CommentService>();
            services.AddScoped<IDashboardService, DashboardService>();
            // Đăng ký dịch vụ cài đặt
            services.AddSingleton<ISettingsService, SettingsService>();
            return services;
        }
        public static IApplicationBuilder UsenfastructureService(this IApplicationBuilder app)
        {
            // Job để dọn dẹp các nhóm đã xóa mềm
            var serviceProvider = app.ApplicationServices;

            app.UseHangfireDashboard("/admin/hangfire");

            var failureNotifierFilter = serviceProvider.GetRequiredService<HangfireJobFailureNotifierFilter>();
            GlobalJobFilters.Filters.Add(failureNotifierFilter);

            using (var scope = serviceProvider.CreateScope())
            {
                var dataRetentionService = scope.ServiceProvider.GetRequiredService<IDataRetentionService>();

                RecurringJob.AddOrUpdate(
                    "cleanup-recalled-messages-job",
                    () => dataRetentionService.PermanentlyDeleteOldRecalledMessagesAsync(),
                    Cron.Daily(3)
                );

                RecurringJob.AddOrUpdate(
                    "cleanup-login-history-job",
                    () => dataRetentionService.DeleteOldLoginHistoryAsync(),
                    Cron.Daily(1)
                );

                RecurringJob.AddOrUpdate(
                    "purge-soft-deleted-groups",
                    () => dataRetentionService.PurgeSoftDeletedGroupsAsync(),
                    Cron.Weekly()
                );
                RecurringJob.AddOrUpdate(
                    "cleanup-orphaned-files-job",
                    () => dataRetentionService.CleanUpOrphanedPostFilesAsync(),
                    Cron.Daily(2) // Chạy vào 2:00 AM UTC mỗi ngày
                );
                RecurringJob.AddOrUpdate(
                    "purge-soft-deleted-users",
                    () => dataRetentionService.PurgeSoftDeletedUsersAsync(),
                    Cron.Weekly()
                );

                RecurringJob.AddOrUpdate(
                    "purge-old-audit-logs",
                    () => dataRetentionService.PurgeOldAuditLogsAsync(),
                    Cron.Monthly()
                );
                RecurringJob.AddOrUpdate(
                    "purge-revoked-refresh-tokens",
                    () => dataRetentionService.PurgeRevokedRefreshTokensAsync(),
                    Cron.Daily()
                );
            }
            return app;
        }
    }
}
