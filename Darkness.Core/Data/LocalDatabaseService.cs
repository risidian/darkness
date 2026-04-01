using Darkness.Core.Interfaces;
using System;
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
            string directory = Path.GetDirectoryName(targetPath);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!File.Exists(targetPath))
            {
                try
                {
                    using Stream inputStream = await _fileSystem.OpenAppPackageFileAsync(filename);
                    using FileStream outputStream = File.Create(targetPath);
                    await inputStream.CopyToAsync(outputStream);
                }
                catch (FileNotFoundException)
                {
                    // If the seed database is not in the app package, we just skip copying.
                }
                catch (Exception ex)
                {
                    // Log error to console if possible
                    System.Diagnostics.Debug.WriteLine($"DB COPY ERROR: {ex.Message}");
                }
            }
        }
    }
}
