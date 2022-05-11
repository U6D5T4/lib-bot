using LibBot.Services.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;

namespace LibBot.Services;

public class FileService : IFileService
{
    public async Task<string[]> GetBookPathsFromFileAsync(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException();
        }

        using(var streamReader = new StreamReader(path))
        {
            var booksPaths = await streamReader.ReadToEndAsync();
            streamReader.Close();
            var separator = booksPaths.Contains("\r\n") ? "\r\n" : "\n";
            return booksPaths.Split(separator, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
