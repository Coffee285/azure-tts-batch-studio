using Avalonia.Controls;
using AzureTtsBatchStudio.ViewModels;

namespace AzureTtsBatchStudio.Views
{
    public partial class StoryBuilderView : UserControl
    {
        public StoryBuilderView()
        {
            InitializeComponent();
            DataContext = new StoryBuilderViewModel();
        }
    }
}