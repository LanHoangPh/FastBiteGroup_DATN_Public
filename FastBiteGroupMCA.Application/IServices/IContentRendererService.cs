namespace FastBiteGroupMCA.Application.IServices;

public interface IContentRendererService
{
    /// <summary>
    /// Render một chuỗi JSON của Tiptap/ProseMirror thành một chuỗi HTML đã được làm sạch.
    /// </summary>
    /// <param name="jsonContent">Nội dung dạng JSON từ editor.</param>
    /// <returns>Một chuỗi HTML an toàn.</returns>
    string RenderAndSanitize(string jsonContent);
}
