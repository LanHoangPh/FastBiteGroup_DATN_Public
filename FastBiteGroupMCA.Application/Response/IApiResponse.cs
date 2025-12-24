namespace FastBiteGroupMCA.Application.Response
{
    public interface IApiResponse
    {
        bool Success { get; }
        string? Message { get; set; }
        List<ApiError>? Errors { get; set; }
    }
}
