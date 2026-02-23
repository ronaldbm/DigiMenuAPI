using DigiMenuAPI.Application.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace DigiMenuAPI.Application.Services
{
    public class FileStorageService(IWebHostEnvironment env, IHttpContextAccessor httpContextAccessor) : IFileStorageService
    {
        // Usa ContentRootPath como base para construir la ruta, 
        // así funciona aunque wwwroot no exista todavía
        private readonly string _rootPath = string.IsNullOrEmpty(env.WebRootPath)
            ? Path.Combine(env.ContentRootPath, "wwwroot")
            : env.WebRootPath;

        public async Task<string> SaveFile(IFormFile file, string container)
        {
            if (file == null || file.Length == 0)
                return "";

            // 1. Preparar carpeta física en wwwroot
            string folder = Path.Combine(_rootPath, container);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            // 2. Generar nombre de archivo único con extensión .webp
            string fileName = $"{Guid.NewGuid()}.webp";
            string fullPath = Path.Combine(folder, fileName);

            // 3. Procesar imagen con ImageSharp
            using (var stream = file.OpenReadStream())
            using (var image = await Image.LoadAsync(stream))
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(800, 0)
                }));

                await image.SaveAsWebpAsync(fullPath, new WebpEncoder
                {
                    Quality = 80,
                    Method = WebpEncodingMethod.BestQuality
                });
            }

            // 4. Retornar URL pública
            var request = httpContextAccessor.HttpContext?.Request;
            if (request == null)
                return $"/{container}/{fileName}";

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