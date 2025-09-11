using Avalonia.Controls;

namespace AzureTtsBatchStudio.Preflight
{
    public partial class PreflightView : UserControl
    {
        public PreflightView()
        {
            InitializeComponent();
            DataContext = new PreflightViewModel();
        }
    }
}