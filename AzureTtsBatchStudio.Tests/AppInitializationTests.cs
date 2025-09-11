using Xunit;
using System;
using Avalonia;
using Avalonia.Headless;
using AzureTtsBatchStudio;

namespace AzureTtsBatchStudio.Tests
{
    public class AppInitializationTests
    {
        [Fact]
        public void App_CanBeCreated_WithoutExceptions()
        {
            // Arrange & Act & Assert
            // This test verifies that the App class can be instantiated without XAML errors
            Exception? exception = null;
            
            try
            {
                var app = new App();
                
                // Try to initialize the app - this is where XAML resource errors would occur
                app.Initialize();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            
            // Assert that no "Static resource not found" errors occurred
            Assert.Null(exception);
        }
        
        [Fact]
        public void App_Initialize_DoesNotThrowStaticResourceException()
        {
            // Arrange
            var app = new App();
            
            // Act & Assert
            // This should not throw "Static resource 'FontSizeSmall' not found" exception
            var exception = Record.Exception(() => app.Initialize());
            
            Assert.Null(exception);
        }
    }
}