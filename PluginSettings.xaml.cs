using Flow.Launcher.Plugin.WinSCP.ViewModels;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Flow.Launcher.Plugin.WinSCP
{
    /// <summary>
    /// Interaction logic for PluginSettings.xaml
    /// </summary>
    public partial class PluginSettings : UserControl
    {
        private readonly PluginInitContext context;
        private readonly Settings settings;
        private readonly SettingsViewModel vm;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="settings"></param>
        /// <param name="vm"></param>
        public PluginSettings(PluginInitContext context, Settings settings, SettingsViewModel vm)
        {
            this.context = context;
            this.settings = settings;
            this.vm = vm;
            DataContext = vm;

            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            SettingsWinSCPExecutablePath.Text = settings.WinSCPExePath;
        }

        private void SettingsOpenWinSCPPath_Click(object sender, RoutedEventArgs e)
        {
            string trExeFile = context.API.GetTranslation("flowlauncher_plugin_winscp_settings_winSCPOpenExePath");
            OpenFileDialog openFileDialog = new()
            {
                Filter = trExeFile + " (*.exe)| *.exe"
            };
            if (!string.IsNullOrEmpty(settings.WinSCPExePath))
                openFileDialog.InitialDirectory = System.IO.Path.GetDirectoryName(settings.WinSCPExePath);

            if (openFileDialog.ShowDialog() == true)
            {
                settings.WinSCPExePath = openFileDialog.FileName;
            }

            SettingsWinSCPExecutablePath.Text = settings.WinSCPExePath;
        }
    }
}
