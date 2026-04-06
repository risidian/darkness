using Darkness.Core.Interfaces;
using LiteDB;
using System.IO;

namespace Darkness.Core.Data
{
    public class LocalDatabaseService
    {
        private readonly IFileSystemService _fileSystem;
        private readonly string _dbPath;

        public LocalDatabaseService(IFileSystemService fileSystem)
        {
            _fileSystem = fileSystem;
            string directory = _fileSystem.AppDataDirectory;
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            _dbPath = Path.Combine(directory, "Darkness.db");
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

        public LiteDatabase OpenDatabase() 
        {
            var connectionString = $"Filename={_dbPath};Connection=shared";
            return new LiteDatabase(connectionString);
        }
    }
}
