using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AzureTtsBatchStudio.Models;

namespace AzureTtsBatchStudio.Services
{
    public interface ITokenBudgeter
    {
        BudgetResult CalculateBudget(
            string instructions, 
            string userPrompt, 
            List<StoryPart> recentParts, 
            ModelParameters parameters);
        int EstimateTokens(string text);
        string TruncateToTokenBudget(string text, int maxTokens);
    }

    public class BudgetResult
    {
        public List<StoryPart> IncludedParts { get; set; } = new();
        public string TruncatedInstructions { get; set; } = string.Empty;
        public string TruncatedPrompt { get; set; } = string.Empty;
        public int EstimatedTokens { get; set; }
        public bool IsTruncated { get; set; }
        public string TruncationReason { get; set; } = string.Empty;
        public bool HasWarning { get; set; }
        public string WarningMessage { get; set; } = string.Empty;
        public double BudgetUtilization { get; set; }
    }

    public class TokenBudgeter : ITokenBudgeter
    {
        // Rough estimation: 1 token ≈ 4 characters for English text
        private const double CharsPerToken = 4.0;

        public BudgetResult CalculateBudget(
            string instructions, 
            string userPrompt, 
            List<StoryPart> recentParts, 
            ModelParameters parameters)
        {
            var result = new BudgetResult();
            var availableBudget = parameters.ContextBudgetTokens - parameters.MaxOutputTokens;
            
            // Reserve tokens for system message overhead and formatting
            var systemOverhead = 100;
            availableBudget -= systemOverhead;

            // Start with essential components
            var promptTokens = EstimateTokens(userPrompt);
            var instructionsTokens = EstimateTokens(instructions);
            
            var usedTokens = promptTokens + instructionsTokens;
            result.TruncatedPrompt = userPrompt;
            result.TruncatedInstructions = instructions;

            // If basic prompt + instructions exceeds budget, truncate instructions first
            if (usedTokens > availableBudget)
            {
                var instructionsBudget = availableBudget - promptTokens;
                if (instructionsBudget > 0)
                {
                    result.TruncatedInstructions = TruncateToTokenBudget(instructions, instructionsBudget);
                    result.IsTruncated = true;
                    result.TruncationReason = "Instructions truncated to fit budget";
                }
                else
                {
                    // Even the prompt is too large, truncate it
                    result.TruncatedPrompt = TruncateToTokenBudget(userPrompt, availableBudget * 3 / 4);
                    result.TruncatedInstructions = TruncateToTokenBudget(instructions, availableBudget / 4);
                    result.IsTruncated = true;
                    result.TruncationReason = "Both prompt and instructions truncated severely";
                }
                
                usedTokens = EstimateTokens(result.TruncatedPrompt) + EstimateTokens(result.TruncatedInstructions);
            }

            // Add recent parts in reverse order (newest first) up to K limit and budget
            var remainingBudget = availableBudget - usedTokens;
            var partsToInclude = new List<StoryPart>();
            var maxParts = Math.Min(parameters.KRecentParts, recentParts.Count);

            for (int i = 0; i < maxParts && remainingBudget > 0; i++)
            {
                var part = recentParts[recentParts.Count - 1 - i]; // Get from newest
                var partTokens = EstimateTokens(part.Content);
                
                if (partTokens <= remainingBudget)
                {
                    partsToInclude.Insert(0, part); // Insert at beginning to maintain chronological order
                    remainingBudget -= partTokens;
                    usedTokens += partTokens;
                }
                else if (remainingBudget > 100) // If we have some budget left, try to include partial content
                {
                    var truncatedContent = TruncateToTokenBudget(part.Content, remainingBudget);
                    var truncatedPart = new StoryPart
                    {
                        FileName = part.FileName,
                        Content = truncatedContent + "\n[...truncated for context budget...]",
                        Index = part.Index,
                        CreatedAt = part.CreatedAt,
                        ModifiedAt = part.ModifiedAt,
                        WordCount = CountWords(truncatedContent)
                    };
                    
                    partsToInclude.Insert(0, truncatedPart);
                    usedTokens += EstimateTokens(truncatedPart.Content);
                    result.IsTruncated = true;
                    result.TruncationReason = $"Part {part.Index} truncated to fit budget";
                    break;
                }
                else
                {
                    break; // No room for any more parts
                }
            }

            result.IncludedParts = partsToInclude;
            result.EstimatedTokens = usedTokens;

            // Calculate budget utilization and warnings
            result.BudgetUtilization = (double)usedTokens / availableBudget;
            
            if (result.BudgetUtilization > 0.9)
            {
                result.HasWarning = true;
                result.WarningMessage = "Token budget is nearly exhausted (>90%)";
            }
            else if (result.BudgetUtilization > 0.8)
            {
                result.HasWarning = true;
                result.WarningMessage = "Token budget is high (>80%)";
            }

            return result;
        }

        public int EstimateTokens(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            // Simple estimation based on character count
            return (int)Math.Ceiling(text.Length / CharsPerToken);
        }

        public string TruncateToTokenBudget(string text, int maxTokens)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var estimatedTokens = EstimateTokens(text);
            if (estimatedTokens <= maxTokens)
                return text;

            // Calculate approximate character limit
            var maxChars = (int)(maxTokens * CharsPerToken);
            
            // Try to truncate at word boundaries
            if (text.Length <= maxChars)
                return text;

            var truncated = text.Substring(0, maxChars);
            var lastSpace = truncated.LastIndexOf(' ');
            
            if (lastSpace > maxChars / 2) // If we can find a reasonable word boundary
            {
                return truncated.Substring(0, lastSpace);
            }
            
            return truncated; // Fallback to character truncation
        }

        private static int CountWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            return text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }
    }
}