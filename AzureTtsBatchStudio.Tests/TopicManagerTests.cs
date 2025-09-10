using Xunit;
using AzureTtsBatchStudio.Services;
using AzureTtsBatchStudio.Models;
using System.Collections.Generic;

namespace AzureTtsBatchStudio.Tests
{
    public class TopicManagerTests
    {
        [Fact]
        public void PickRandomTopic_WithWeightedTopics_ReturnsWeightedResult()
        {
            // Arrange
            var topicManager = new TopicManager();
            var topics = new List<Topic>
            {
                new() { TopicText = "heavy", Weight = 10.0 },
                new() { TopicText = "light", Weight = 1.0 }
            };

            // Act - use fixed seed for reproducible results
            var results = new List<string>();
            for (int i = 0; i < 100; i++)
            {
                var selected = topicManager.PickRandomTopic(topics, 42 + i);
                results.Add(selected.TopicText);
            }

            // Assert - heavy topic should appear more frequently
            var heavyCount = results.Count(t => t == "heavy");
            var lightCount = results.Count(t => t == "light");
            
            Assert.True(heavyCount > lightCount, $"Heavy topic ({heavyCount}) should appear more than light topic ({lightCount})");
        }

        [Fact]
        public void PickRandomTopics_WithCount_ReturnsCorrectNumber()
        {
            // Arrange
            var topicManager = new TopicManager();
            var topics = new List<Topic>
            {
                new() { TopicText = "topic1", Weight = 1.0 },
                new() { TopicText = "topic2", Weight = 1.0 },
                new() { TopicText = "topic3", Weight = 1.0 }
            };

            // Act
            var result = topicManager.PickRandomTopics(topics, 2, 42);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal(2, result.Select(t => t.TopicText).Distinct().Count()); // No duplicates
        }
    }
}