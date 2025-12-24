using FastBiteGroupMCA.Persistentce.Repositories.Efcore;

namespace FastBiteGroupMCA.Persistentce.Repositories;

public class VideoCallParticipantsRepository : GenericRepository<VideoCallParticipants>, IVideoCallParticipantsRepository
{
    public VideoCallParticipantsRepository(ApplicationDbContext context) : base(context)
    {
    }
}
