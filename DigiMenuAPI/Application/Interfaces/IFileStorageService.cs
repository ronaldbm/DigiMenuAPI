namespace DigiMenuAPI.Application.Interfaces
{
    public interface IFileStorageService
    {
        Task<string> SaveFile(string base64Image, string container);
        void DeleteFile(string route, string container);
    }
}
