using System.Collections.Generic;
using AzureTtsBatchStudio.Models;

namespace AzureTtsBatchStudio.Services
{
    public interface IQuickActionService
    {
        List<QuickAction> GetQuickActions();
        string ApplyQuickAction(QuickAction action, string currentText, string prompt);
    }

    public class QuickActionService : IQuickActionService
    {
        public List<QuickAction> GetQuickActions()
        {
            return new List<QuickAction>
            {
                new()
                {
                    Name = "Continue",
                    Label = "Continue",
                    Prompt = "Continue the story from where it left off:",
                    Description = "Continue writing from the current point"
                },
                new()
                {
                    Name = "Rewrite",
                    Label = "Rewrite",
                    Prompt = "Rewrite this text to be shorter, tighter, and clearer:\n\n{text}",
                    Description = "Make the text more concise and clear"
                },
                new()
                {
                    Name = "Expand",
                    Label = "Expand",
                    Prompt = "Expand this text by adding more detail and depth (+200 words):\n\n{text}",
                    Description = "Add more detail and expand the content"
                },
                new()
                {
                    Name = "ToneWarmer",
                    Label = "Warmer Tone",
                    Prompt = "Rewrite this text with a warmer, more emotional tone:\n\n{text}",
                    Description = "Make the tone more warm and emotional"
                },
                new()
                {
                    Name = "ToneDarker",
                    Label = "Darker Tone",
                    Prompt = "Rewrite this text with a darker, more intense tone:\n\n{text}",
                    Description = "Make the tone more dark and intense"
                },
                new()
                {
                    Name = "DeRepeat",
                    Label = "De-repeat",
                    Prompt = "Rewrite this text to reduce repetitive phrases and improve flow:\n\n{text}",
                    Description = "Remove repetitive phrases and improve flow"
                }
            };
        }

        public string ApplyQuickAction(QuickAction action, string currentText, string prompt)
        {
            if (action.Name == "Continue")
            {
                // For continue, we use the current text as context and the original prompt
                return $"{action.Prompt}\n\n{currentText}";
            }
            else
            {
                // For other actions, replace {text} placeholder with current text
                return action.Prompt.Replace("{text}", currentText);
            }
        }
    }
}