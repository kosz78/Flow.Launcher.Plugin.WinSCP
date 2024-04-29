using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;
using Flow.Launcher.Plugin;
using Flow.Launcher.Plugin.WinSCP.ViewModels;

namespace Flow.Launcher.Plugin.WinSCP
{
    /// <summary>
    /// Plugin for WinSCP
    /// </summary>
    public class WinSCP : IPlugin, ISettingProvider, IPluginI18n, IContextMenu
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private PluginInitContext _context;
        private Settings _settings;
        private WinSCPService _service;

        /// <inheritdoc/>
        public void Init(PluginInitContext context)
        {
            _context = context;
            _settings = context.API.LoadSettingJsonStorage<Settings>();
            _service = new WinSCPService(_context, _settings);
        }

        /// <inheritdoc/>
        public List<Result> Query(Query query)
        {
            if (string.IsNullOrEmpty(_settings.WinSCPExePath))
            {
                string errorTitle = _context.API.GetTranslation(
                    "flowlauncher_plugin_winscp_pathNotSet"
                );
                string errorMsg = _context.API.GetTranslation(
                    "flowlauncher_plugin_winscp_configPlugin"
                );
                _context.API.ShowMsg(errorTitle, errorMsg, "");
                return new();
            }

            var querySearch = query.Search;
            if (string.IsNullOrEmpty(querySearch))
            {
                return _service.GetAll().ConvertAll(e => MakeResult(e));
            }

            return _service
                .GetAll()
                .Where(entry =>
                    entry.Title.ToLowerInvariant().Contains(querySearch.ToLowerInvariant())
                )
                .ToList()
                .ConvertAll(entry => MakeResult(entry));
        }

        /// <inheritdoc/>
        public Control CreateSettingPanel()
        {
            return new PluginSettings(_context, _settings, new SettingsViewModel(_context));
        }

        /// <inheritdoc/>
        public string GetTranslatedPluginTitle()
        {
            return _context.API.GetTranslation("flowlauncher_plugin_winscp_plugin_title");
        }

        /// <inheritdoc/>
        public string GetTranslatedPluginDescription()
        {
            return _context.API.GetTranslation("flowlauncher_plugin_winscp_plugin_description");
        }

        /// <inheritdoc/>
        public List<Result> LoadContextMenus(Result selectedResult)
        {
            return new()
            {
                MakeActionResult((SessionEntry)selectedResult.ContextData, "Run"),
                MakeActionResult(
                    (SessionEntry)selectedResult.ContextData,
                    "Run new instance",
                    "/newinstance"
                )
            };
        }

        private Result MakeResult(SessionEntry sessionEntry)
        {
            return new()
            {
                Title = sessionEntry.Title,
                IcoPath = "icon.png",
                Score = 50,
                SubTitle = sessionEntry.ToString(),
                ContextData = sessionEntry,
                Action = _context => LaunchWinSCPSession(sessionEntry),
            };
        }

        private Result MakeActionResult(
            SessionEntry sessionEntry,
            string title,
            string additionalArgs = null,
            string subTitle = null
        )
        {
            return new()
            {
                Title = title,
                SubTitle = subTitle,
                IcoPath = "icon.png",
                Score = 50,
                Action = _context => LaunchWinSCPSession(sessionEntry, additionalArgs),
            };
        }

        private bool LaunchWinSCPSession(SessionEntry sessionEntry, string additionalArgs = null)
        {
            try
            {
                var winScpPath = _settings.WinSCPExePath;
                var p = new Process { StartInfo = { FileName = winScpPath } };
                p.StartInfo.Arguments = '"' + sessionEntry.Identifier + '"';
                if (additionalArgs != null)
                {
                    p.StartInfo.Arguments += " " + additionalArgs;
                }
                p.Start();
                return true;
            }
            catch (Exception ex)
            {
                string trError = _context.API.GetTranslation("flowlauncher_plugin_winscp_error");
                _context.API.ShowMsg(
                    trError
                        + ": "
                        + sessionEntry?.Identifier
                        + " ("
                        + _settings.WinSCPExePath
                        + ") ",
                    ex.Message,
                    ""
                );
                return false;
            }
        }
    }
}
