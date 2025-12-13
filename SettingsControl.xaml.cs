// SettingsControl.xaml.cs
using System.Windows;
using System.Windows.Controls;

namespace maorc287.RBRDataExtPlugin
{
    public partial class SettingsControl : UserControl
    {
        private readonly RBRSettings _settings;

        public SettingsControl(RBRSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            DataContext = _settings;
        }

        private void SaveNow()
        {
            SettingsStorage.Save(_settings);
        }

        private void Toggle_Changed(object sender, RoutedEventArgs e)
        {
            SaveNow();
        }
        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {

        }
    }
}
