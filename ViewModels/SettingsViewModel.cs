using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.WinSCP.ViewModels
{
    /// <summary>
    /// Settings view model
    /// </summary>
    public class SettingsViewModel
    {
        private readonly PluginInitContext context;

        /// <summary>
        /// Settings view model
        /// </summary>
        /// <param name="context">context</param>
        public SettingsViewModel(PluginInitContext context)
        {
            this.context = context;
        }
    }
}
