namespace FastBiteGroupMCA.Application.Helper;

public static class MimeTypes
{
    public static string GetMimeType(string fileName)
    {
        var extension = System.IO.Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".pdf" => "application/pdf",
            ".doc" or ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" or ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
    }
    public const string ImageJpeg = "image/jpeg";
    public const string ImagePng = "image/png";
    public const string ImageGif = "image/gif";
    public const string ImageWebp = "image/webp";

    public const string ApplicationPdf = "application/pdf";
    public const string ApplicationDocx = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    public const string ApplicationPptx = "application/vnd.openxmlformats-officedocument.presentationml.presentation";

    public const string VideoMp4 = "video/mp4";
}
