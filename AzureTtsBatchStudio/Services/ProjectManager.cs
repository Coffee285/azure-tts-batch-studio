using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AzureTtsBatchStudio.Models;

namespace AzureTtsBatchStudio.Services
{
    public interface IProjectManager
    {
        Task<StoryProject> CreateProjectAsync(string projectsRoot, string projectName);
        Task<StoryProject> LoadProjectAsync(string projectPath);
        Task SaveProjectAsync(string projectPath, StoryProject project);
        Task<List<string>> GetProjectsAsync(string projectsRoot);
        Task<List<StoryPart>> GetStoryPartsAsync(string projectPath);
        Task SaveStoryPartAsync(string projectPath, string partContent, int? index = null);
        Task<string> LoadInstructionsAsync(string projectPath);
        Task SaveInstructionsAsync(string projectPath, string instructions);
        Task<List<Topic>> LoadTopicsAsync(string projectPath);
        Task SaveTopicsAsync(string projectPath, List<Topic> topics);
        Task<List<Directive>> LoadDirectivesAsync(string projectPath);
        Task SaveDirectivesAsync(string projectPath, List<Directive> directives);
        Task SaveSessionAsync(string projectPath, GenerationSession session);
        string GetProjectsRootPath();
        void SetProjectsRootPath(string path);
    }

    public class ProjectManager : IProjectManager
    {
        private string _projectsRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "StoryBuilder Projects");

        public string GetProjectsRootPath() => _projectsRoot;

        public void SetProjectsRootPath(string path)
        {
            _projectsRoot = path;
        }

        public async Task<StoryProject> CreateProjectAsync(string projectsRoot, string projectName)
        {
            var projectPath = Path.Combine(projectsRoot, projectName);
            
            if (Directory.Exists(projectPath))
                throw new InvalidOperationException($"Project '{projectName}' already exists.");

            // Create project structure
            Directory.CreateDirectory(projectPath);
            Directory.CreateDirectory(Path.Combine(projectPath, "story_parts"));
            Directory.CreateDirectory(Path.Combine(projectPath, "sessions"));
            Directory.CreateDirectory(Path.Combine(projectPath, "scratch"));
            Directory.CreateDirectory(Path.Combine(projectPath, "exports"));

            var project = new StoryProject
            {
                Name = projectName,
                CreatedAt = DateTime.UtcNow,
                LastOpened = DateTime.UtcNow
            };

            // Create initial files
            await SaveProjectAsync(projectPath, project);
            await SaveInstructionsAsync(projectPath, "Write engaging, creative stories with vivid descriptions and compelling characters.");
            await SaveTopicsAsync(projectPath, new List<Topic>
            {
                new() { TopicText = "adventure", Weight = 1.0 },
                new() { TopicText = "mystery", Weight = 1.0 },
                new() { TopicText = "friendship", Weight = 1.0 }
            });
            await SaveDirectivesAsync(projectPath, new List<Directive>());

            return project;
        }

        public async Task<StoryProject> LoadProjectAsync(string projectPath)
        {
            var projectFile = Path.Combine(projectPath, ".project.json");
            if (!File.Exists(projectFile))
                throw new FileNotFoundException($"Project file not found: {projectFile}");

            var json = await File.ReadAllTextAsync(projectFile);
            var project = JsonSerializer.Deserialize<StoryProject>(json) ?? new StoryProject();
            project.LastOpened = DateTime.UtcNow;
            
            return project;
        }

        public async Task SaveProjectAsync(string projectPath, StoryProject project)
        {
            var projectFile = Path.Combine(projectPath, ".project.json");
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            
            var json = JsonSerializer.Serialize(project, options);
            await File.WriteAllTextAsync(projectFile, json);
        }

        public async Task<List<string>> GetProjectsAsync(string projectsRoot)
        {
            if (!Directory.Exists(projectsRoot))
                return new List<string>();

            var projects = new List<string>();
            var directories = Directory.GetDirectories(projectsRoot);

            foreach (var dir in directories)
            {
                var projectFile = Path.Combine(dir, ".project.json");
                if (File.Exists(projectFile))
                {
                    projects.Add(Path.GetFileName(dir));
                }
            }

            return projects;
        }

        public async Task<List<StoryPart>> GetStoryPartsAsync(string projectPath)
        {
            var partsPath = Path.Combine(projectPath, "story_parts");
            if (!Directory.Exists(partsPath))
                return new List<StoryPart>();

            var parts = new List<StoryPart>();
            var files = Directory.GetFiles(partsPath, "*.txt")
                .OrderBy(f => f)
                .ToArray();

            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                var content = await File.ReadAllTextAsync(file);
                var fileInfo = new FileInfo(file);
                
                parts.Add(new StoryPart
                {
                    FileName = Path.GetFileName(file),
                    Content = content,
                    CreatedAt = fileInfo.CreationTimeUtc,
                    ModifiedAt = fileInfo.LastWriteTimeUtc,
                    WordCount = CountWords(content),
                    Index = i + 1
                });
            }

            return parts;
        }

        public async Task SaveStoryPartAsync(string projectPath, string partContent, int? index = null)
        {
            var partsPath = Path.Combine(projectPath, "story_parts");
            Directory.CreateDirectory(partsPath);

            if (index == null)
            {
                // Find next available index
                var existingFiles = Directory.GetFiles(partsPath, "*.txt");
                var maxIndex = 0;
                
                foreach (var file in existingFiles)
                {
                    var existingFileName = Path.GetFileNameWithoutExtension(file);
                    if (int.TryParse(existingFileName, out var fileIndex))
                    {
                        maxIndex = Math.Max(maxIndex, fileIndex);
                    }
                }
                
                index = maxIndex + 1;
            }

            var fileName = $"{index:D3}.txt";
            var filePath = Path.Combine(partsPath, fileName);
            
            await File.WriteAllTextAsync(filePath, partContent);
        }

        public async Task<string> LoadInstructionsAsync(string projectPath)
        {
            var instructionsFile = Path.Combine(projectPath, "instructions.txt");
            if (!File.Exists(instructionsFile))
                return string.Empty;

            return await File.ReadAllTextAsync(instructionsFile);
        }

        public async Task SaveInstructionsAsync(string projectPath, string instructions)
        {
            var instructionsFile = Path.Combine(projectPath, "instructions.txt");
            await File.WriteAllTextAsync(instructionsFile, instructions);
        }

        public async Task<List<Topic>> LoadTopicsAsync(string projectPath)
        {
            var topicsFile = Path.Combine(projectPath, "scratch", "topics.json");
            if (!File.Exists(topicsFile))
                return new List<Topic>();

            var json = await File.ReadAllTextAsync(topicsFile);
            return JsonSerializer.Deserialize<List<Topic>>(json) ?? new List<Topic>();
        }

        public async Task SaveTopicsAsync(string projectPath, List<Topic> topics)
        {
            var scratchPath = Path.Combine(projectPath, "scratch");
            Directory.CreateDirectory(scratchPath);
            
            var topicsFile = Path.Combine(scratchPath, "topics.json");
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(topics, options);
            await File.WriteAllTextAsync(topicsFile, json);
        }

        public async Task<List<Directive>> LoadDirectivesAsync(string projectPath)
        {
            var directivesFile = Path.Combine(projectPath, "scratch", "directives.json");
            if (!File.Exists(directivesFile))
                return new List<Directive>();

            var json = await File.ReadAllTextAsync(directivesFile);
            return JsonSerializer.Deserialize<List<Directive>>(json) ?? new List<Directive>();
        }

        public async Task SaveDirectivesAsync(string projectPath, List<Directive> directives)
        {
            var scratchPath = Path.Combine(projectPath, "scratch");
            Directory.CreateDirectory(scratchPath);
            
            var directivesFile = Path.Combine(scratchPath, "directives.json");
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(directives, options);
            await File.WriteAllTextAsync(directivesFile, json);
        }

        public async Task SaveSessionAsync(string projectPath, GenerationSession session)
        {
            var sessionsPath = Path.Combine(projectPath, "sessions");
            Directory.CreateDirectory(sessionsPath);
            
            var sessionFile = Path.Combine(sessionsPath, $"{session.Timestamp:yyyyMMdd_HHmmss}_{session.Id[..8]}.json");
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(session, options);
            await File.WriteAllTextAsync(sessionFile, json);
        }

        private static int CountWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            return text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }
    }
}