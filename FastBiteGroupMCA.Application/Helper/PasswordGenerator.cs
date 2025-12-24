using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;

namespace FastBiteGroupMCA.Application.Helper;

public static class PasswordGenerator
{
    public static string GenerateRandomPassword(PasswordOptions opts)
    {
        // Lấy các quy tắc từ cấu hình Identity
        int length = opts.RequiredLength;
        bool requireDigit = opts.RequireDigit;
        bool requireLowercase = opts.RequireLowercase;
        bool requireUppercase = opts.RequireUppercase;
        bool requireNonAlphanumeric = opts.RequireNonAlphanumeric;

        // Định nghĩa các bộ ký tự
        const string lower = "abcdefghijklmnopqrstuvwxyz";
        const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string digits = "0123456789";
        const string nonAlpha = "!@#$%^&*()_-+=[{]};:<>|./?";

        var charSets = new List<string>();
        if (requireLowercase) charSets.Add(lower);
        if (requireUppercase) charSets.Add(upper);
        if (requireDigit) charSets.Add(digits);
        if (requireNonAlphanumeric) charSets.Add(nonAlpha);

        // Nếu không có quy tắc nào, dùng mặc định
        if (charSets.Count == 0) charSets.Add(lower + upper + digits);

        var password = new char[length];
        var allChars = string.Concat(charSets);

        // Đảm bảo có ít nhất một ký tự từ mỗi bộ ký tự bắt buộc
        int nextCharIdx = 0;
        foreach (var charSet in charSets)
        {
            password[nextCharIdx++] = GetRandomChar(charSet);
        }

        // Điền phần còn lại của mật khẩu bằng các ký tự ngẫu nhiên
        for (int i = nextCharIdx; i < length; i++)
        {
            password[i] = GetRandomChar(allChars);
        }

        // Xáo trộn mảng ký tự để các ký tự bắt buộc không luôn ở đầu
        return Shuffle(password);
    }
    private static char GetRandomChar(string characterSet)
    {
        int index = RandomNumberGenerator.GetInt32(characterSet.Length);
        return characterSet[index];
    }

    private static string Shuffle(char[] array)
    {
        int n = array.Length;
        while (n > 1)
        {
            n--;
            int k = RandomNumberGenerator.GetInt32(n + 1);
            (array[k], array[n]) = (array[n], array[k]);
        }
        return new string(array);
    }
}
