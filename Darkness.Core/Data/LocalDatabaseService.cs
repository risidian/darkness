using Darkness.Core.Interfaces;
using System.IO;

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
            string directory = _fileSystem.AppDataDirectory;
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            return Path.Combine(directory, filename);
        }
    }
}
