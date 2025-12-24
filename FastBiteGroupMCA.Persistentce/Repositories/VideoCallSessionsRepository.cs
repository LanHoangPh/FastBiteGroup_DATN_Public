using FastBiteGroupMCA.Persistentce.Repositories.Efcore;

namespace FastBiteGroupMCA.Persistentce.Repositories;

public class VideoCallSessionsRepository : GenericRepository<VideoCallSessions>, IVideoCallSessionsRepository
{
    public VideoCallSessionsRepository(ApplicationDbContext context) : base(context)
    {
    }
}
