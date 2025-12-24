using FastBiteGroupMCA.Application.IServices.FileStorage;
using Google.Apis.Storage.v1;

namespace FastBiteGroupMCA.Infastructure.Services.FileStorage;

public class StorageStrategy
{
    private readonly IEnumerable<IFileStorageService> _storageServices;

    public StorageStrategy(IEnumerable<IFileStorageService> storageServices)
    {
        _storageServices = storageServices;
    }

    public IFileStorageService GetStorageService(string contentType)
    {
        var specificService = _storageServices.FirstOrDefault(s => s.CanHandle(contentType));

        if (specificService != null)
        {
            return specificService;
        }
        return _storageServices.First(s => s is AzureBlobStorageService);
    }
}
