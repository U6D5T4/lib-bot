using System.Threading.Tasks;

namespace LibBot.Services.Interfaces;

public interface IFileService
{
    Task<string[]> GetBookPathsFromFileAsync(string path);
}
