using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.IServices;

public interface ISettingsService
{
    /// <summary>
    /// Lấy một giá trị cài đặt đã được ép kiểu.
    /// </summary>
    /// <typeparam name="T">Kiểu dữ liệu mong muốn (string, bool, int...).</typeparam>
    /// <param name="key">Khóa của cài đặt.</param>
    /// <param name="defaultValue">Giá trị mặc định nếu không tìm thấy.</param>
    /// <returns>Giá trị của cài đặt.</returns>
    T Get<T>(SettingKeys key, T defaultValue);

    /// <summary>
    /// Lấy tất cả các cài đặt hiện tại.
    /// </summary>
    Dictionary<string, string> GetAllSettings();

    /// <summary>
    /// Cập nhật một hoặc nhiều cài đặt.
    /// </summary>
    Task UpdateSettingsAsync(Dictionary<string, string> settingsToUpdate);
}
