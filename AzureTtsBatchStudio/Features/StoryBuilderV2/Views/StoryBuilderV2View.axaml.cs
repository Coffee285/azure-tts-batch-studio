using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AzureTtsBatchStudio.Features.StoryBuilderV2.Views
{
    public partial class StoryBuilderV2View : UserControl
    {
        public StoryBuilderV2View()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
