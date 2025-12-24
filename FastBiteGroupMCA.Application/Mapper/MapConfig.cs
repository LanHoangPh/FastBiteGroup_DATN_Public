using AutoMapper;
using FastBiteGroupMCA.Application.DTOs.Auth;
using FastBiteGroupMCA.Application.DTOs.Comment;
using FastBiteGroupMCA.Application.DTOs.Common;
using FastBiteGroupMCA.Application.DTOs.Conversation;
using FastBiteGroupMCA.Application.DTOs.Group;
using FastBiteGroupMCA.Application.DTOs.Hubs;
using FastBiteGroupMCA.Application.DTOs.Message;
using FastBiteGroupMCA.Application.DTOs.Notification;
using FastBiteGroupMCA.Application.DTOs.Post;
using FastBiteGroupMCA.Application.DTOs.SharedFile;
using FastBiteGroupMCA.Application.DTOs.User;
using FastBiteGroupMCA.Application.Response.IFileStorage;
using FastBiteGroupMCA.Domain.Entities;
using FastBiteGroupMCA.Domain.Entities.Identity;
using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.Mapper;

public class MapConfig : Profile
{
    public MapConfig()
    {
        // thường dùng cho các việc lấy dữ liuẹ sẽ chuyển đổi từ Entity sang DTO
        // Auth
        CreateMap<AppUser, UserDto>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName ?? $"{src.FisrtName} {src.LastName}"))
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FisrtName))
            .ForMember(dest => dest.Roles, opt => opt.Ignore()); // Mapped separately in service

        CreateMap<AppUser, UserProfileDto>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName ?? $"{src.FisrtName} {src.LastName}"))
            .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => src.AvatarUrl));

        CreateMap<RegisterDto, AppUser>()
            .ForMember(dest => dest.FisrtName, opt => opt.MapFrom(src => src.FirstName))
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => false));

        CreateMap<CreateUserDto, AppUser>()
            .ForMember(dest => dest.FisrtName, opt => opt.MapFrom(src => src.FirstName))
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => false));

        CreateMap<UpdateUserADDto, AppUser>()
            .ForMember(dest => dest.FisrtName, opt => opt.MapFrom(src => src.FirstName))
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.DateOfBirth, opt => opt.Condition(src => src.DateOfBirth != default))
            .ForMember(dest => dest.Bio, opt => opt.Condition(src => src.Bio != null))
            .ForMember(dest => dest.TwoFactorEnabled, opt => opt.Condition(src => src.TwoFactorEnabled != default));

        CreateMap<AppUser, UserSearchResultDTO>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => src.FullName ?? $"{src.FisrtName} {src.LastName}"))
            .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => src.AvatarUrl));

        CreateMap<CreateChatGroupDto, Group>();

        CreateMap<CreateCommunityGroupDto, Group>();

        CreateMap<ReadReceiptInfo, ReadReceiptDto>();

        CreateMap<Group, UserGroupDTO>()
            .ForMember(dest => dest.GroupType, opt => opt.MapFrom(src =>
                src.GroupType == EnumGroupType.Community
                    ? GroupTypeApiDto.Community
                    : GroupTypeApiDto.Chat))
            .ForMember(dest => dest.Privacy, opt => opt.MapFrom(src =>
                (GroupPrivacyApiDto)src.Privacy)).
            ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => src.GroupAvatarUrl));

        CreateMap<Group, PublicGroupDto>()
            .ForMember(dest => dest.MemberCount,
                       opt => opt.MapFrom(src => src.Members.Count()))
            .ForMember(dest => dest.GroupType, opt => opt.MapFrom(src =>
                src.GroupType == EnumGroupType.Community
                    ? GroupTypeApiDto.Community
                    : GroupTypeApiDto.Chat));
        //
        CreateMap<Conversation, ConversationSummaryDTO>();
        CreateMap<Domain.Entities.Notifications, NotificationDTO>()
             .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()));

        CreateMap<GroupMember, GroupMemberDetailDTO>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserID))
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User!.FullName))
            .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => src.User!.AvatarUrl));
        ///
        CreateMap<AppUser, PostAuthorDTO>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id));

        // phần create post bài viết
        CreateMap<CreatePostDTO, Posts>()
            .ForMember(dest => dest.ContentJson, opt => opt.MapFrom(src => src.ContentJson))
            .ForMember(dest => dest.Attachments, opt => opt.Ignore());

        // Map từ Posts entity sang DTO chi tiết để trả về
        CreateMap<Posts, PostDetailDTO>()
            .ForMember(dest => dest.ContentJson, opt => opt.MapFrom(src => src.ContentJson))
            .ForMember(dest => dest.LikeCount, opt => opt.MapFrom(src => src.Likes.Count))
            .ForMember(dest => dest.CommentCount, opt => opt.MapFrom(src => src.Comments.Count(c => !c.IsDeleted)))
            .ForMember(dest => dest.Attachments, opt => opt.MapFrom(src => src.Attachments));

        // Map từ liên kết đính kèm sang DTO đính kèm
        CreateMap<PostAttachment, PostAttachmentDTO>()
            .ForMember(dest => dest.FileId, opt => opt.MapFrom(src => src.SharedFile!.FileID))
            .ForMember(dest => dest.FileName, opt => opt.MapFrom(src => src.SharedFile!.FileName))
            .ForMember(dest => dest.StorageUrl, opt => opt.MapFrom(src => src.SharedFile!.StorageUrl))
            .ForMember(dest => dest.FileType, opt => opt.MapFrom(src => src.SharedFile!.FileType))
            .ForMember(dest => dest.FileSize, opt => opt.MapFrom(src => src.SharedFile!.FileSize));

        CreateMap<CreateCommentDTO, PostComments>();

        CreateMap<PostComments, PostCommentDTO>()
            .ForMember(dest => dest.CommentId, opt => opt.MapFrom(src => src.CommentID))
            .ForMember(dest => dest.Author, opt => opt.MapFrom(src => src.User));

        CreateMap<AppUser, PostAuthorDTO>()
        .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id));

        CreateMap<AppUser, ConversationPartnerDto>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id));
        // Các trường FullName, AvatarUrl, PresenceStatus sẽ được AutoMapper tự động map
        // vì có tên giống nhau.

        // Message
        CreateMap<SenderInfo, MessageSenderDTO>();
        CreateMap<Messages, MessageDTO>()
           // Báo cho AutoMapper bỏ qua việc map Reactions, chúng ta sẽ xử lý riêng
           .ForMember(dest => dest.Reactions, opt => opt.Ignore());
        CreateMap<AppUser, MessageSenderDTO>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => src.FullName));

        CreateMap<SharedFiles, AttachmentInfo>()
            // Cần map tường minh vì tên thuộc tính khác nhau (FileID vs FileId)
            .ForMember(dest => dest.FileId, opt => opt.MapFrom(src => src.FileID));

        CreateMap<FileUploadResult, SharedFiles>()
            // Map các thuộc tính có tên khác nhau một cách tường minh
            .ForMember(dest => dest.StorageUrl, opt => opt.MapFrom(src => src.Url))
            .ForMember(dest => dest.FileType, opt => opt.MapFrom(src => src.ContentType))

            // Tự động gán thời gian upload là thời điểm hiện tại khi map
            .ForMember(dest => dest.UploadedAt, opt => opt.MapFrom(src => DateTime.UtcNow))

            // Bỏ qua các trường không cần map hoặc sẽ được gán thủ công trong service
            .ForMember(dest => dest.FileID, opt => opt.Ignore())
            .ForMember(dest => dest.UploadedByUserID, opt => opt.Ignore())
            .ForMember(dest => dest.UploadedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

        CreateMap<SharedFiles, FileUploadResponseDto>()
            // Cần map tường minh vì tên thuộc tính khác nhau (FileID vs FileId)
            .ForMember(dest => dest.FileId, opt => opt.MapFrom(src => src.FileID));

       
    }
}
