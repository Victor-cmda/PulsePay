using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services
{
    public class FileService
    {
        private readonly string? _basePath;

        public FileService(IConfiguration configuration)
        {
            _basePath = configuration["FileStorage:BasePath"];
        }

        public async Task<string> SaveBase64AsPdfAsync(string base64String, string fileName)
        {
            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
            }

            byte[] pdfBytes = Convert.FromBase64String(base64String);
            string filePath = Path.Combine(_basePath, fileName);

            await File.WriteAllBytesAsync(filePath, pdfBytes);

            return filePath;
        }
    }

}
