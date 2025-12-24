using FastBiteGroupMCA.Application.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FastBiteGroupMCA.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UpFilesController : ControllerBase
    {
        private readonly IFileService _fileService;
        public UpFilesController(IFileService fileService)
        {
            _fileService = fileService;
        }
        /// <summary>
        /// (Tiện ích) Tải ảnh đại diện lên khu vực tạm.
        /// </summary>
        /// <remarks>Trả về URL tạm thời để sử dụng</remarks>
        /// <param name="file">Đối tượng chứa file ảnh.</param>
        /// <param name="category"></param>
        [HttpPost("staging/avatar")]
        [Tags("Groups.Utilities")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadStagingAvatar(IFormFile file, [FromForm] string category = "general")
        {
            var result = await _fileService.UploadStagingAvatarAsync(file, category);

            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
