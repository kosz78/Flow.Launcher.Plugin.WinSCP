using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using Microsoft.Win32;

namespace Flow.Launcher.Plugin.WinSCP
{
    internal class SessionEntry
    {
        /// <summary>
        /// The identifier with path
        /// </summary>
        public string Identifier { get; set; }

        /// <summary>
        /// Decoded title from indentifier
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// The string version of protocol (scp, sftp, s3, etc.)
        /// </summary>
        public string Protocol { get; set; }

        /// <summary>
        /// The optional Username
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// The Hostname
        /// </summary>
        public string Hostname { get; set; }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            if (string.IsNullOrEmpty(Username))
            {
                return string.Format("{0}://{1}", Protocol, Hostname);
            }

            return string.Format("{0}://{1}@{2}", Protocol, Username, Hostname);
        }
    }

    internal class WinSCPService
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly PluginInitContext _context;
        private readonly Settings _settings;
        private readonly List<SessionEntry> _entries;

        private DateTime _lastCheck = DateTime.MinValue;
        private DateTime _lastConfigChange = DateTime.MinValue;

        public WinSCPService(PluginInitContext context, Settings settings)
        {
            _context = context;
            _settings = settings;
            _entries = new List<SessionEntry>();
        }

        private void ReloadFromFile(string file)
        {
            SessionEntry entry = null;
            bool isSession = false;
            foreach (var line in File.ReadAllLines(file))
            {
                if (line.StartsWith('[') && line.EndsWith(']'))
                {
                    if (entry is not null)
                    {
                        _entries.Add(entry);
                        entry = null;
                    }
                    if (line.StartsWith("[Sessions\\"))
                    {
                        isSession = true;
                        entry = new() { Identifier = line[10..^1], Protocol = "sftp", };
                        entry.Title = HttpUtility.UrlDecode(entry.Identifier);
                    }
                    else
                    {
                        isSession = false;
                    }
                }
                else if (isSession && entry is not null)
                {
                    if (line.StartsWith("HostName="))
                    {
                        entry.Hostname = line[9..];
                    }
                    else if (line.StartsWith("UserName="))
                    {
                        entry.Username = line[9..];
                    }
                    else if (line.StartsWith("FSProtocol="))
                    {
                        entry.Protocol = line[11..] switch
                        {
                            "0" => "scp",
                            "7" => "s3",
                            _ => "sftp",
                        };
                    }
                }
            }
            if (entry is not null)
            {
                _entries.Add(entry);
            }
        }

        private void ReloadFromRegistry()
        {
            using var root = Registry.CurrentUser.OpenSubKey(
                "Software\\Martin Prikryl\\WinSCP 2\\Sessions"
            );
            if (root is null)
            {
                return;
            }
            foreach (var subKey in root.GetSubKeyNames())
            {
                using var SessionSubKey = root.OpenSubKey(subKey);
                if (SessionSubKey == null)
                {
                    continue;
                }

                try
                {
                    SessionEntry entry = new()
                    {
                        Identifier = subKey,
                        Title = HttpUtility.UrlDecode(subKey),
                        Hostname = SessionSubKey.GetValue("HostName").ToString(),
                        Protocol = "sftp",
                    };

                    Object protocol = SessionSubKey.GetValue("FSProtocol");
                    if(protocol != null)
                    {
                        entry.Protocol = protocol.ToString() switch
                        {
                            "0" => "scp",
                            "7" => "s3",
                            _ => "sftp"
                        };
                    }

                    Object username = SessionSubKey.GetValue("UserName");
                    if (username != null)
                    {
                        entry.Username = username.ToString();
                    }

                    _entries.Add(entry);
                }
                catch (Exception)
                {
                    // ignore
                }
            }
        }

        private void CheckAndReload()
        {
            if (_entries.Count == 0 || _lastCheck.AddMinutes(5) < DateTime.Now)
            {
                _entries.Clear();
                try
                {
                    _lastCheck = DateTime.Now;
                    Logger.Info("Reloading WinSCP config");
                    string configPath = Path.Combine(
                        Path.GetDirectoryName(_settings.WinSCPExePath),
                        "WinSCP.ini"
                    );
                    if (!Path.Exists(configPath))
                    {
                        configPath = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                            "WinSCP.ini"
                        );
                    }
                    if (Path.Exists(configPath))
                    {
                        if (_lastConfigChange != File.GetLastWriteTime(configPath)) {
                            ReloadFromFile(configPath);
                        }
                        _lastConfigChange = File.GetLastWriteTime(configPath);
                    }
                    else
                    {
                        ReloadFromRegistry();
                    }
                }
                catch (System.Exception e)
                {
                    string errorTitle = _context.API.GetTranslation(
                        "flowlauncher_plugin_winscp_error"
                    );
                    _context.API.ShowMsg(errorTitle, e.ToString(), "");
                }
            }
        }

        internal List<SessionEntry> GetAll()
        {
            CheckAndReload();
            return _entries;
        }
    }
}
