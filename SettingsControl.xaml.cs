// SettingsControl.xaml.cs
using System.Windows.Controls;

namespace maorc287.RBRDataExtPlugin
{
    public partial class SettingsControl : UserControl
    {
        // Take the settings instance from the plugin and bind to it.
        public SettingsControl(RBRSettings settings)
        {
            InitializeComponent();
            DataContext = settings;
        }
    }
}
