using System.IO;
using System.Threading.Tasks;

namespace Darkness.Core.Interfaces
{
    public interface IFileSystemService
    {
        string AppDataDirectory { get; }
        Task<Stream> OpenAppPackageFileAsync(string filename);
        string ReadAllText(string filename);
        bool FileExists(string path);
        bool DirectoryExists(string path);
        string[] GetFiles(string path, string searchPattern, bool recursive = false);
    }
}
