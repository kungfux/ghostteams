using CefSharp;
using CefSharp.WinForms;
using GhostTeams.Properties;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace GhostTeams
{
    public partial class Form1 : Form
    {
        public ChromiumWebBrowser ChromeBrowser { get; set; }
        public NotifyIcon NotifyIcon { get; set; }

        private const string CAPTION = "Ghost Teams";

        private const string SETTING_URL = "TeamsURL";
        private const string SETTING_WIDTH = "WindowWidth";
        private const string SETTING_HEIGHT = "WindowHeight";

        private const string EXIT = "Exit";

        private const int DEFAULT_HEIGHT = 600;
        private const int DEFAULT_WIDTH = 800;

        public Form1()
        {
            InitializeComponent();

            Text = CAPTION;
            Icon = Resources.iconfinder_10_avatar_2754575;
            StartPosition = FormStartPosition.CenterScreen;

            int.TryParse(ConfigurationManager.AppSettings.Get(SETTING_WIDTH), out var width);
            int.TryParse(ConfigurationManager.AppSettings.Get(SETTING_HEIGHT), out var height);
            Height = height > 0 ? height : DEFAULT_HEIGHT;
            Width = width > 0 ? width : DEFAULT_WIDTH;

            FormClosing += Form1_FormClosing;

            InitializeChromium();
            InitializeTrayIcon();
        }

        public void InitializeChromium()
        {
            var settings = new CefSettings()
            {
                //CefCommandLineArgs = { new KeyValuePair<string, string>("enable-media-stream", "1") },
                CefCommandLineArgs =
                {
                    "enable-media-stream",
                    "enable-smooth-scrolling",
                    "enable-overlay-scrollbar",
                    "high-dpi-support"
                },
                CachePath = Path.Combine(Application.StartupPath, "Cache"),
            };

            Cef.Initialize(settings);

            ChromeBrowser = new ChromiumWebBrowser(ConfigurationManager.AppSettings.Get(SETTING_URL))
            {
                Dock = DockStyle.Fill,
            };

            ChromeBrowser.TitleChanged += ChromeBrowser_TitleChanged;

            Controls.Add(ChromeBrowser);
        }

        private void ChromeBrowser_TitleChanged(object sender, TitleChangedEventArgs e)
        {
            var newTitle = $"{e.Title} - {CAPTION}";

            if (Text == newTitle)
            {
                return;
            }

            Text = newTitle;

            var notifications = Regex.IsMatch(e.Title, @"\A([()0-9]+)");
            if (notifications && !Focused)
            {
                NotifyIcon.ShowBalloonTip(TimeSpan.FromSeconds(3).Milliseconds, "You have new messages.", CAPTION, ToolTipIcon.Info);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason != CloseReason.UserClosing)
            {
                return;
            }

            e.Cancel = true;
            Hide();
        }

        public void InitializeTrayIcon()
        {
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.AddRange(
                new ToolStripItem[] {
                    new ToolStripMenuItem(EXIT, null, NotifyIcon_Exit)
                });

            NotifyIcon = new NotifyIcon
            {
                Visible = true,
                Icon = Icon,
                Text = CAPTION,
                ContextMenuStrip = contextMenu
            };
            NotifyIcon.MouseClick += NotifyIcon_MouseClick;
            NotifyIcon.BalloonTipClicked += NotifyIcon_BalloonTipClicked;
        }

        private void NotifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            RestoreWindow();
        }

        private void NotifyIcon_BalloonTipClicked(object sender, EventArgs e)
        {
            RestoreWindow();
        }

        private void NotifyIcon_Exit(object sender, EventArgs e)
        {
            NotifyIcon.Visible = false;
            Cef.Shutdown();
            Application.Exit();
        }

        private void RestoreWindow()
        {
            if (WindowState == FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Normal;
                return;
            }

            if (!Visible)
            {
                Show();
                Visible = true;

                NotifyIcon.Visible = true;
            }
        }
    }
}
