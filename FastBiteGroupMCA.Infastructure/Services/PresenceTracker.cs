using System.Collections.Concurrent;

namespace FastBiteGroupMCA.Infastructure.Services;

public class PresenceTracker
{
    // Key: userId (string), Value: danh sách các connectionId
    private static readonly ConcurrentDictionary<string, List<string>> OnlineUsers = new();

    public Task<bool> UserConnected(string userId, string connectionId)
    {
        bool isOnline = false;
        lock (OnlineUsers)
        {
            if (OnlineUsers.ContainsKey(userId))
            {
                OnlineUsers[userId].Add(connectionId);
            }
            else
            {
                OnlineUsers.TryAdd(userId, new List<string> { connectionId });
                isOnline = true; // Đây là kết nối ĐẦU TIÊN
            }
        }
        return Task.FromResult(isOnline);
    }

    public Task<bool> UserDisconnected(string userId, string connectionId)
    {
        bool isOffline = false;
        lock (OnlineUsers)
        {
            if (!OnlineUsers.ContainsKey(userId)) return Task.FromResult(isOffline);

            OnlineUsers[userId].Remove(connectionId);

            if (OnlineUsers[userId].Count == 0)
            {
                OnlineUsers.TryRemove(userId, out _);
                isOffline = true; // Đây là kết nối CUỐI CÙNG
            }
        }
        return Task.FromResult(isOffline);
    }
}
