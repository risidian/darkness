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
            System.Console.WriteLine($"[DBService] Target path: {targetPath}");
            string directory = Path.GetDirectoryName(targetPath);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                System.Console.WriteLine($"[DBService] Creating directory: {directory}");
                Directory.CreateDirectory(directory);
            }

            if (!File.Exists(targetPath))
            {
                try
                {
                    System.Console.WriteLine($"[DBService] Copying {filename} from package...");
                    using Stream inputStream = await _fileSystem.OpenAppPackageFileAsync(filename);
                    using FileStream outputStream = File.Create(targetPath);
                    await inputStream.CopyToAsync(outputStream);
                    System.Console.WriteLine("[DBService] Database copy success.");
                }
                catch (FileNotFoundException)
                {
                    System.Console.WriteLine("[DBService] Seed database not found in package. Skipping.");
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"[DBService] ERROR during database copy: {ex.Message}");
                    System.Console.WriteLine(ex.StackTrace);
                }
            }
            else
            {
                System.Console.WriteLine("[DBService] Database already exists in target path.");
            }
        }
    }
}
