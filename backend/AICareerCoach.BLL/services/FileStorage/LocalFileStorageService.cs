using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.services.FileStorage
{
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly string _uploadPath;

        public LocalFileStorageService()
        {
            _uploadPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Uploads",
                "CVs");

            if (!Directory.Exists(_uploadPath))
                Directory.CreateDirectory(_uploadPath);
        }

        public async Task<string> SaveFileAsync(
            Stream fileStream,
            string fileName)
        {
            var uniqueFileName =
                $"{Guid.NewGuid()}_{fileName}";

            var filePath =
                Path.Combine(_uploadPath, uniqueFileName);

            using var stream =
                new FileStream(filePath, FileMode.Create);

            await fileStream.CopyToAsync(stream);

            return filePath;
        }

        public void DeleteFile(string filePath)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }
}
