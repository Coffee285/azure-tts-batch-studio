using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AzureTtsBatchStudio.Tts
{
    public sealed class FfmpegMerger
    {
        public async Task<string> MergeAsync(IEnumerable<string> partPaths, string outputPath, CancellationToken ct = default)
        {
            var pathList = partPaths.ToList();
            
            // If only one part, just copy/rename to final
            if (pathList.Count == 1)
            {
                File.Copy(pathList[0], outputPath, overwrite: true);
                return outputPath;
            }

            if (pathList.Count == 0)
            {
                throw new ArgumentException("No input paths provided for merging");
            }

            var outputDir = Path.GetDirectoryName(outputPath) ?? throw new ArgumentException("Invalid output path");
            var tmpList = Path.Combine(outputDir, "concat_list.txt");

            try
            {
                // Create concat list file with escaped paths
                var listContent = pathList.Select(p => $"file '{EscapeForConcat(Path.GetFullPath(p))}'");
                await File.WriteAllLinesAsync(tmpList, listContent, ct);

                var ffmpegPath = ResolveFfmpegPath();
                
                // Try concat demuxer first (fastest, lossless)
                var psi = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = $"-hide_banner -loglevel error -f concat -safe 0 -i \"{tmpList}\" -c copy \"{outputPath}\"",
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start ffmpeg process");
                
                var stderr = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync(ct);

                if (process.ExitCode == 0)
                {
                    return outputPath;
                }

                // Fallback: re-encode if concat failed
                var fallbackPath = Path.Combine(outputDir, "temp_reencoded.mp3");
                var reencodeArgs = $"-hide_banner -loglevel error -f concat -safe 0 -i \"{tmpList}\" -ar 48000 -ac 1 -b:a 192k \"{fallbackPath}\"";
                
                var fallbackPsi = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = reencodeArgs,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using var fallbackProcess = Process.Start(fallbackPsi) ?? throw new InvalidOperationException("Failed to start ffmpeg fallback process");
                
                var stderr2 = await fallbackProcess.StandardError.ReadToEndAsync();
                await fallbackProcess.WaitForExitAsync(ct);

                if (fallbackProcess.ExitCode != 0)
                {
                    throw new InvalidOperationException($"FFmpeg merge failed:\nFirst attempt: {stderr}\nFallback: {stderr2}");
                }

                // Move temp file to final location
                File.Move(fallbackPath, outputPath, overwrite: true);
                return outputPath;
            }
            finally
            {
                // Clean up temp files
                try
                {
                    if (File.Exists(tmpList))
                        File.Delete(tmpList);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        private static string ResolveFfmpegPath()
        {
            // Try common locations and PATH
            var candidates = new[]
            {
                "ffmpeg",
                "ffmpeg.exe",
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "ffmpeg.exe"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe"),
                @"C:\ffmpeg\bin\ffmpeg.exe",
                "/usr/bin/ffmpeg",
                "/usr/local/bin/ffmpeg"
            };

            foreach (var candidate in candidates)
            {
                try
                {
                    using var process = Process.Start(new ProcessStartInfo
                    {
                        FileName = candidate,
                        Arguments = "-version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    });

                    if (process?.WaitForExit(5000) == true && process.ExitCode == 0)
                    {
                        return candidate;
                    }
                }
                catch
                {
                    // Continue trying other candidates
                }
            }

            throw new FileNotFoundException(
                "FFmpeg not found. Please install FFmpeg and ensure it's available in PATH, " +
                "or place ffmpeg.exe in the application directory or tools subfolder.");
        }

        private static string EscapeForConcat(string path)
        {
            // Escape single quotes by replacing ' with '\''
            return path.Replace("'", @"'\''");
        }
    }
}