using Darkness.Core.Interfaces;
using System.IO;
using System.Threading.Tasks;

namespace Darkness.Core.Data
{
    public class LocalDatabaseService
    {
        private readonly IFileSystemService _fileSystem;

        public LocalDatabaseService(IFileSystemService fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public string GetLocalFilePath(string filename)
        {
            return Path.Combine(_fileSystem.AppDataDirectory, filename);
        }

        public async Task CopyDatabaseIfNotExistsAsync(string filename)
        {
            string targetPath = GetLocalFilePath(filename);

            if (!File.Exists(targetPath))
            {
                using Stream inputStream = await _fileSystem.OpenAppPackageFileAsync(filename);
                using FileStream outputStream = File.Create(targetPath);
                await inputStream.CopyToAsync(outputStream);
            }
        }
    }
}
