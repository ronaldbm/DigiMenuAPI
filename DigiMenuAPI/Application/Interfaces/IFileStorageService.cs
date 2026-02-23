namespace DigiMenuAPI.Application.Interfaces
{
    public interface IFileStorageService
    {
        Task<string> SaveFile(IFormFile file, string container);
        void DeleteFile(string route, string container);
    }
}
