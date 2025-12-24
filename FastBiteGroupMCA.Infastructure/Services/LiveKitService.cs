using FastBiteGroupMCA.Infastructure.DependencyInjection.Options;
using Livekit.Server.Sdk.Dotnet;
using Microsoft.Extensions.Options;

namespace FastBiteGroupMCA.Infastructure.Services;

public class LiveKitService : ILiveKitService
{
    private readonly LiveKitSettings _settings;

    public LiveKitService(IOptions<LiveKitSettings> settings)
    {
        _settings = settings.Value;
    }

    public string GenerateToken(AppUser user, VideoCallSessions session, EnumGroupRole? userRole)
    {
        var grants = new VideoGrants
        {
            RoomJoin = true,
            Room = session.VideoCallSessionID.ToString(),
            CanPublish = true,
            CanSubscribe = true,
            CanPublishData = true
        };

        // Áp dụng Logic Phân quyền Hybrid
        bool isInitiator = (user.Id == session.InitiatorUserID);
        bool isGroupAdminOrMod = (userRole == EnumGroupRole.Admin || userRole == EnumGroupRole.Moderator);

        // Nếu là người tạo hoặc là Admin/Mod của nhóm, cấp quyền quản lý phòng
        if (isInitiator || isGroupAdminOrMod)
        {
            grants.RoomAdmin = true;
        }

        var accessToken = new AccessToken(_settings.ApiKey, _settings.ApiSecret)
            .WithIdentity(user.Id.ToString())
            .WithName(user.FullName)
            .WithTtl(TimeSpan.FromHours(1))
            .WithGrants(grants); 

        return accessToken.ToJwt();
    }
}
