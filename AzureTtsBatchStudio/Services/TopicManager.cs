using System;
using System.Collections.Generic;
using System.Linq;
using AzureTtsBatchStudio.Models;

namespace AzureTtsBatchStudio.Services
{
    public interface ITopicManager
    {
        Topic PickRandomTopic(List<Topic> topics, int? seed = null);
        List<Topic> PickRandomTopics(List<Topic> topics, int count, int? seed = null);
        void ShuffleTopics(List<Topic> topics, int? seed = null);
    }

    public class TopicManager : ITopicManager
    {
        public Topic PickRandomTopic(List<Topic> topics, int? seed = null)
        {
            if (topics == null || topics.Count == 0)
                return new Topic { TopicText = "mystery", Weight = 1.0 };

            var random = seed.HasValue ? new Random(seed.Value) : new Random();
            
            // Calculate total weight
            var totalWeight = topics.Sum(t => t.Weight);
            if (totalWeight <= 0)
                return topics[random.Next(topics.Count)];

            // Weighted random selection
            var randomValue = random.NextDouble() * totalWeight;
            var currentWeight = 0.0;

            foreach (var topic in topics)
            {
                currentWeight += topic.Weight;
                if (randomValue <= currentWeight)
                {
                    return topic;
                }
            }

            // Fallback to last topic
            return topics.Last();
        }

        public List<Topic> PickRandomTopics(List<Topic> topics, int count, int? seed = null)
        {
            if (topics == null || topics.Count == 0 || count <= 0)
                return new List<Topic>();

            var random = seed.HasValue ? new Random(seed.Value) : new Random();
            var result = new List<Topic>();
            var availableTopics = new List<Topic>(topics);

            count = Math.Min(count, availableTopics.Count);

            for (int i = 0; i < count; i++)
            {
                if (availableTopics.Count == 0)
                    break;

                var totalWeight = availableTopics.Sum(t => t.Weight);
                if (totalWeight <= 0)
                {
                    var randomIndex = random.Next(availableTopics.Count);
                    result.Add(availableTopics[randomIndex]);
                    availableTopics.RemoveAt(randomIndex);
                    continue;
                }

                var randomValue = random.NextDouble() * totalWeight;
                var currentWeight = 0.0;
                Topic? selectedTopic = null;

                foreach (var topic in availableTopics)
                {
                    currentWeight += topic.Weight;
                    if (randomValue <= currentWeight)
                    {
                        selectedTopic = topic;
                        break;
                    }
                }

                if (selectedTopic == null)
                    selectedTopic = availableTopics.Last();

                result.Add(selectedTopic);
                availableTopics.Remove(selectedTopic);
            }

            return result;
        }

        public void ShuffleTopics(List<Topic> topics, int? seed = null)
        {
            if (topics == null || topics.Count <= 1)
                return;

            var random = seed.HasValue ? new Random(seed.Value) : new Random();

            // Fisher-Yates shuffle
            for (int i = topics.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (topics[i], topics[j]) = (topics[j], topics[i]);
            }
        }
    }
}