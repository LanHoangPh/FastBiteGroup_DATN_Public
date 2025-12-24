using FastBiteGroupMCA.Application.Authorization;
using FastBiteGroupMCA.Application.Authorization.Handlers;
using FastBiteGroupMCA.Application.CurrentUserClaim;
using FastBiteGroupMCA.Application.DependencyInjection.Options;
using FastBiteGroupMCA.Application.Mapper;
using FastBiteGroupMCA.Application.Notifications.Abstractions;
using FastBiteGroupMCA.Application.Response;
using FastBiteGroupMCA.Application.Validator;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FastBiteGroupMCA.Application.DependencyInjection.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddApplicationService(this IServiceCollection services, IConfiguration configuration)
        {
            // Add your application services here
            
            services.AddAutoMapper(typeof(MapConfig));

                
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUser, CurrentUser>();
            services.AddScoped<IAuthorizationHandler, ContentManagementHandler>();
            services.AddScoped<IAuthorizationHandler, GroupManagementHandler>();
            services.AddScoped<IAuthorizationHandler, IsConversationParticipantHandler>();
            services.AddScoped<IAuthorizationHandler, CanAccessMessageHandler>();

            // C. Đăng ký tất cả các Policies
            services.AddAuthorization(options =>
            {
                options.AddPolicy("IsConversationMember", policy => policy.AddRequirements(new IsConversationParticipantRequirement()));
                options.AddPolicy("CanAccessMessage", policy => policy.AddRequirements(new CanAccessMessageRequirement()));
                options.AddPolicy("IsGroupMember", policy => policy.AddRequirements(GroupOperations.View));
                options.AddPolicy("CanViewGroupLayout", policy => policy.AddRequirements(GroupOperations.View));
                options.AddPolicy("CanCreateContentInGroup", policy => policy.AddRequirements(GroupOperations.CreateContent));
                options.AddPolicy("CanModerateContent", policy => policy.AddRequirements(GroupOperations.ModerateContent));
                options.AddPolicy("CanRemoveMember", policy => policy.AddRequirements(GroupOperations.RemoveMember));
                options.AddPolicy("CanManageGroupRoles", policy => policy.AddRequirements(GroupOperations.ManageRoles));
                options.AddPolicy("CanDeleteGroup", policy => policy.AddRequirements(GroupOperations.DeleteGroup));

                options.AddPolicy("CanEditOwnContent", policy => policy.AddRequirements(ContentOperations.Edit));
                options.AddPolicy("CanDeleteOwnContent", policy => policy.AddRequirements(ContentOperations.Delete));
            });

            services.Configure<FileUploadSettings>(
                configuration.GetSection(FileUploadSettings.SectionName)
            );
            services.AddFluentValidationAutoValidation();
            services.AddValidatorsFromAssemblyContaining<RegisterDtoValidate>();
            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState
                        .Where(e => e.Value!.Errors.Count > 0)
                        .SelectMany(x => x.Value!.Errors.Select(e => new ApiError(
                            errorCode: x.Key,
                            message: e.ErrorMessage
                        ))).ToList();
                    var errorResponse = ApiResponse<object>.Fail(errors);

                    return new BadRequestObjectResult(errorResponse);
                };
            });

            services.Scan(scan => scan
                    .FromAssemblyOf<INotificationTemplate>() 
                    .AddClasses(classes => classes.AssignableTo<INotificationTemplate>())
                    .AsSelf()
                    .WithTransientLifetime());
            return services;
        }
    }
}
