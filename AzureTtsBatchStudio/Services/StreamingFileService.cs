using System;
using System.IO;
using System.Threading.Tasks;

namespace AzureTtsBatchStudio.Services
{
    public interface IStreamingFileService
    {
        Task<string> CreateTempFileAsync(string prefix = "story_output");
        Task AppendToFileAsync(string filePath, string content);
        Task<string> FinalizeFileAsync(string tempFilePath, string finalFileName);
        Task CleanupTempFileAsync(string tempFilePath);
    }

    public class StreamingFileService : IStreamingFileService
    {
        public async Task<string> CreateTempFileAsync(string prefix = "story_output")
        {
            var tempPath = Path.GetTempPath();
            var fileName = $"{prefix}_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.partial";
            var filePath = Path.Combine(tempPath, fileName);
            
            // Create empty file
            await File.WriteAllTextAsync(filePath, string.Empty);
            return filePath;
        }

        public async Task AppendToFileAsync(string filePath, string content)
        {
            if (string.IsNullOrEmpty(content))
                return;

            try
            {
                await File.AppendAllTextAsync(filePath, content);
            }
            catch (Exception)
            {
                // Ignore append errors during streaming - content is still in memory
            }
        }

        public async Task<string> FinalizeFileAsync(string tempFilePath, string finalFileName)
        {
            if (!File.Exists(tempFilePath))
                throw new FileNotFoundException($"Temp file not found: {tempFilePath}");

            var directory = Path.GetDirectoryName(tempFilePath) ?? Path.GetTempPath();
            var finalPath = Path.Combine(directory, finalFileName);

            // Atomic rename
            File.Move(tempFilePath, finalPath);
            
            return finalPath;
        }

        public async Task CleanupTempFileAsync(string tempFilePath)
        {
            try
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
            catch (Exception)
            {
                // Ignore cleanup errors
            }
        }
    }
}