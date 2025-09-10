using Xunit;
using AzureTtsBatchStudio.Services;
using AzureTtsBatchStudio.Models;
using System.Collections.Generic;

namespace AzureTtsBatchStudio.Tests
{
    public class DirectiveServiceTests
    {
        [Fact]
        public void ShouldEnforceDirective_AtCorrectPart_ReturnsTrue()
        {
            // Arrange
            var directiveService = new DirectiveService();
            var directive = new Directive
            {
                Trigger = new DirectiveTrigger { AtPart = 3 },
                DirectiveText = "Add tension",
                Strict = false
            };

            // Act & Assert
            Assert.True(directiveService.ShouldEnforceDirective(directive, 3, 100));
            Assert.False(directiveService.ShouldEnforceDirective(directive, 2, 100));
        }

        [Fact]
        public void ShouldEnforceDirective_StrictMode_EnforcesFromTriggerOnwards()
        {
            // Arrange
            var directiveService = new DirectiveService();
            var directive = new Directive
            {
                Trigger = new DirectiveTrigger { AtPart = 3 },
                DirectiveText = "Add tension",
                Strict = true
            };

            // Act & Assert
            Assert.True(directiveService.ShouldEnforceDirective(directive, 3, 100));
            Assert.True(directiveService.ShouldEnforceDirective(directive, 4, 100));
            Assert.True(directiveService.ShouldEnforceDirective(directive, 5, 100));
            Assert.False(directiveService.ShouldEnforceDirective(directive, 2, 100));
        }

        [Fact]
        public void BuildPromptWithDirectives_AddsDirectivesToPrompt()
        {
            // Arrange
            var directiveService = new DirectiveService();
            var basePrompt = "Write about a hero";
            var directives = new List<Directive>
            {
                new() { DirectiveText = "Add tension" },
                new() { DirectiveText = "Reveal secret" }
            };

            // Act
            var result = directiveService.BuildPromptWithDirectives(basePrompt, directives);

            // Assert
            Assert.Contains("Add tension", result);
            Assert.Contains("Reveal secret", result);
            Assert.Contains("Write about a hero", result);
        }

        [Fact]
        public void GetNextDirective_ReturnsClosestUpcomingDirective()
        {
            // Arrange
            var directiveService = new DirectiveService();
            var directives = new List<Directive>
            {
                new() { Trigger = new DirectiveTrigger { AtPart = 5 }, DirectiveText = "Far directive" },
                new() { Trigger = new DirectiveTrigger { AtPart = 3 }, DirectiveText = "Near directive" },
                new() { Trigger = new DirectiveTrigger { AtPart = 1 }, DirectiveText = "Past directive" }
            };

            // Act
            var next = directiveService.GetNextDirective(directives, 2, 100);

            // Assert
            Assert.NotNull(next);
            Assert.Equal("Near directive", next.DirectiveText);
        }
    }
}