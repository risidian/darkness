using System.IO;
using System.Threading.Tasks;

namespace Darkness.Core.Interfaces
{
    public interface IFileSystemService
    {
        string AppDataDirectory { get; }
        Task<Stream> OpenAppPackageFileAsync(string filename);
    }
}
