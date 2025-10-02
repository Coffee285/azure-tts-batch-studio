using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AzureTtsBatchStudio.Features.StoryBuilderV2.Models;
using AzureTtsBatchStudio.Infrastructure.Common;

namespace AzureTtsBatchStudio.Features.StoryBuilderV2.Services
{
    /// <summary>
    /// Interface for story project persistence
    /// </summary>
    public interface IProjectStore
    {
        /// <summary>
        /// Save a project with automatic backup
        /// </summary>
        Task<Result> SaveAsync(StoryProject project, string projectPath);

        /// <summary>
        /// Load a project from disk
        /// </summary>
        Task<Result<StoryProject>> LoadAsync(string projectPath);

        /// <summary>
        /// Create a new project directory structure
        /// </summary>
        Task<Result<string>> CreateProjectAsync(string projectsRoot, string projectName);

        /// <summary>
        /// List all projects in the root directory
        /// </summary>
        Task<Result<List<string>>> ListProjectsAsync(string projectsRoot);

        /// <summary>
        /// Get backups for a project
        /// </summary>
        List<string> GetBackups(string projectPath);

        /// <summary>
        /// Restore from backup
        /// </summary>
        Task<Result> RestoreBackupAsync(string projectPath, string backupPath);
    }

    /// <summary>
    /// File-based project store with JSON serialization and rolling backups
    /// </summary>
    public class FileProjectStore : IProjectStore
    {
        private const string ProjectFileName = "project.json";
        private const int MaxBackups = 10;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public async Task<Result> SaveAsync(StoryProject project, string projectPath)
        {
            try
            {
                Guard.AgainstNull(project, nameof(project));
                Guard.AgainstNullOrWhiteSpace(projectPath, nameof(projectPath));

                if (!Directory.Exists(projectPath))
                {
                    return Result.Failure($"Project path does not exist: {projectPath}");
                }

                var projectFile = Path.Combine(projectPath, ProjectFileName);

                // Create backup if file exists
                if (File.Exists(projectFile))
                {
                    await CreateBackupAsync(projectFile);
                }

                // Update timestamp
                var updatedProject = project with { UpdatedUtc = DateTimeOffset.UtcNow };

                // Write to temp file first (atomic write)
                var tempFile = Path.Combine(projectPath, $"{ProjectFileName}.tmp");
                var json = JsonSerializer.Serialize(updatedProject, JsonOptions);
                await File.WriteAllTextAsync(tempFile, json);

                // Replace original file
                File.Replace(tempFile, projectFile, backupFileName: null);

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure($"Failed to save project: {ex.Message}");
            }
        }

        public async Task<Result<StoryProject>> LoadAsync(string projectPath)
        {
            try
            {
                Guard.AgainstNullOrWhiteSpace(projectPath, nameof(projectPath));

                var projectFile = Path.Combine(projectPath, ProjectFileName);
                
                if (!File.Exists(projectFile))
                {
                    return Result<StoryProject>.Failure($"Project file not found: {projectFile}");
                }

                var json = await File.ReadAllTextAsync(projectFile);
                var project = JsonSerializer.Deserialize<StoryProject>(json, JsonOptions);

                if (project == null)
                {
                    return Result<StoryProject>.Failure("Failed to deserialize project");
                }

                return Result<StoryProject>.Success(project);
            }
            catch (Exception ex)
            {
                return Result<StoryProject>.Failure($"Failed to load project: {ex.Message}");
            }
        }

        public async Task<Result<string>> CreateProjectAsync(string projectsRoot, string projectName)
        {
            try
            {
                Guard.AgainstNullOrWhiteSpace(projectsRoot, nameof(projectsRoot));
                Guard.AgainstNullOrWhiteSpace(projectName, nameof(projectName));

                // Sanitize project name
                var safeName = SanitizeFileName(projectName);
                var projectPath = Path.Combine(projectsRoot, safeName);

                if (Directory.Exists(projectPath))
                {
                    return Result<string>.Failure($"Project '{safeName}' already exists");
                }

                // Create directory structure
                Directory.CreateDirectory(projectPath);
                Directory.CreateDirectory(Path.Combine(projectPath, "beats"));
                Directory.CreateDirectory(Path.Combine(projectPath, "audio"));
                Directory.CreateDirectory(Path.Combine(projectPath, "exports"));
                Directory.CreateDirectory(Path.Combine(projectPath, "sfx"));
                Directory.CreateDirectory(Path.Combine(projectPath, ".backups"));

                // Create initial project
                var project = new StoryProject
                {
                    Title = projectName,
                    CreatedUtc = DateTimeOffset.UtcNow,
                    UpdatedUtc = DateTimeOffset.UtcNow
                };

                var saveResult = await SaveAsync(project, projectPath);
                if (saveResult.IsFailure)
                {
                    return Result<string>.Failure(saveResult.Error);
                }

                return Result<string>.Success(projectPath);
            }
            catch (Exception ex)
            {
                return Result<string>.Failure($"Failed to create project: {ex.Message}");
            }
        }

        public async Task<Result<List<string>>> ListProjectsAsync(string projectsRoot)
        {
            try
            {
                Guard.AgainstNullOrWhiteSpace(projectsRoot, nameof(projectsRoot));

                if (!Directory.Exists(projectsRoot))
                {
                    Directory.CreateDirectory(projectsRoot);
                    return Result<List<string>>.Success(new List<string>());
                }

                var projects = new List<string>();
                var directories = Directory.GetDirectories(projectsRoot);

                foreach (var dir in directories)
                {
                    var projectFile = Path.Combine(dir, ProjectFileName);
                    if (File.Exists(projectFile))
                    {
                        projects.Add(dir);
                    }
                }

                await Task.CompletedTask;
                return Result<List<string>>.Success(projects);
            }
            catch (Exception ex)
            {
                return Result<List<string>>.Failure($"Failed to list projects: {ex.Message}");
            }
        }

        public List<string> GetBackups(string projectPath)
        {
            Guard.AgainstNullOrWhiteSpace(projectPath, nameof(projectPath));

            var backupDir = Path.Combine(projectPath, ".backups");
            if (!Directory.Exists(backupDir))
            {
                return new List<string>();
            }

            return Directory.GetFiles(backupDir, "*.json")
                .OrderByDescending(f => File.GetCreationTimeUtc(f))
                .ToList();
        }

        public async Task<Result> RestoreBackupAsync(string projectPath, string backupPath)
        {
            try
            {
                Guard.AgainstNullOrWhiteSpace(projectPath, nameof(projectPath));
                Guard.AgainstNullOrWhiteSpace(backupPath, nameof(backupPath));

                if (!File.Exists(backupPath))
                {
                    return Result.Failure($"Backup file not found: {backupPath}");
                }

                var projectFile = Path.Combine(projectPath, ProjectFileName);
                
                // Create backup of current file before restoring
                if (File.Exists(projectFile))
                {
                    await CreateBackupAsync(projectFile);
                }

                // Copy backup to project file
                File.Copy(backupPath, projectFile, overwrite: true);

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure($"Failed to restore backup: {ex.Message}");
            }
        }

        private async Task CreateBackupAsync(string projectFile)
        {
            var projectDir = Path.GetDirectoryName(projectFile);
            if (projectDir == null) return;

            var backupDir = Path.Combine(projectDir, ".backups");
            Directory.CreateDirectory(backupDir);

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var backupFile = Path.Combine(backupDir, $"project_{timestamp}.json");

            File.Copy(projectFile, backupFile);

            // Clean up old backups (keep only MaxBackups)
            var backups = Directory.GetFiles(backupDir, "*.json")
                .OrderByDescending(f => File.GetCreationTimeUtc(f))
                .ToList();

            if (backups.Count > MaxBackups)
            {
                foreach (var oldBackup in backups.Skip(MaxBackups))
                {
                    File.Delete(oldBackup);
                }
            }

            await Task.CompletedTask;
        }

        private static string SanitizeFileName(string fileName)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("_", fileName.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
            return sanitized.Trim();
        }
    }
}
