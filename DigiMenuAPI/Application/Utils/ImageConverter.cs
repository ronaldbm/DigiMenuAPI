using DigiMenuAPI.Application.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace DigiMenuAPI.Application.Services
{
    public class FileStorageService(IWebHostEnvironment env, IHttpContextAccessor httpContextAccessor) : IFileStorageService
    {
        private readonly string _rootPath = env.WebRootPath;

        public async Task<string> SaveFile(string base64Image, string container)
        {
            if (string.IsNullOrEmpty(base64Image) || !base64Image.Contains(",")) 
                return "";

            // 1. Preparar carpeta
            string folder = Path.Combine(_rootPath, container);

            if (!Directory.Exists(folder)) 
                Directory.CreateDirectory(folder);

            // 2. Procesar imagen con ImageSharp
            string fileName = $"{Guid.NewGuid()}.webp";
            string fullPath = Path.Combine(folder, fileName);

            var base64Data = base64Image.Split(',')[1];
            byte[] bytes = Convert.FromBase64String(base64Data);

            using (var image = Image.Load(bytes))
            {
                // Redimensionar a un tamaño web estándar (máx 800px)
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(800, 0)
                }));

                await image.SaveAsWebpAsync(fullPath, new WebpEncoder { Quality = 85 });
            }

            // 3. Retornar la URL pública
            var request = httpContextAccessor.HttpContext.Request;
            return $"{request.Scheme}://{request.Host}/{container}/{fileName}";
        }

        public void DeleteFile(string route, string container)
        {
            if (string.IsNullOrEmpty(route)) 
                return;

            var fileName = Path.GetFileName(route);
            string fullPath = Path.Combine(_rootPath, container, fileName);

            if (File.Exists(fullPath)) 
                File.Delete(fullPath);
        }
    }
}