using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace SystrayCalendarWeek
{
    /// <summary>
    /// Unsichtbare Form, die das Tray-Icon hält. Die Kalenderwoche wird einmal
    /// beim Start berechnet - das Programm läuft im Autostart und wird täglich neu gestartet.
    /// </summary>
    public partial class SystrayApp : Form
    {
        private const string AppTitle = "Kalenderwoche";

        private NotifyIcon trayIcon = null!;
        private ContextMenuStrip trayMenu = null!;
        private ToolStripMenuItem autostartMenuItem = null!;
        private Icon? weekIcon;

        private int currentWeek;
        private int currentWeekYear;

        public SystrayApp()
        {
            InitializeComponent();

            currentWeek = CalendarWeekHelper.GetCalendarWeek(DateTime.Now);
            currentWeekYear = CalendarWeekHelper.GetCalendarWeekYear(DateTime.Now);

            InitializeSystemTray();
            ShowCalendarWeek();
        }

        /// <summary>Die Form soll beim Start keinen Fokus stehlen.</summary>
        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        /// <summary>
        /// Verhindert, dass die Form jemals sichtbar wird - zuverlässiger als
        /// ein Hide() im OnLoad, da Application.Run() sie sonst kurz aufblitzen lässt.
        /// </summary>
        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(false);
        }

        private void InitializeSystemTray()
        {
            trayMenu = new ContextMenuStrip(components);

            var headerMenuItem = new ToolStripMenuItem(BuildTooltipText()) { Enabled = false };

            autostartMenuItem = new ToolStripMenuItem("Mit Windows starten")
            {
                CheckOnClick = true,
                Checked = AutostartHelper.IsAutostartEnabled()
            };
            autostartMenuItem.Click += ToggleAutostart;

            var infoMenuItem = new ToolStripMenuItem("Info...");
            infoMenuItem.Click += ShowInfo;

            var exitMenuItem = new ToolStripMenuItem("Beenden");
            exitMenuItem.Click += ExitApplication;

            trayMenu.Items.AddRange(new ToolStripItem[]
            {
                headerMenuItem,
                new ToolStripSeparator(),
                autostartMenuItem,
                infoMenuItem,
                new ToolStripSeparator(),
                exitMenuItem
            });

            weekIcon = TrayIconFactory.CreateWeekIcon(currentWeek);

            trayIcon = new NotifyIcon(components)
            {
                Icon = weekIcon,
                // ContextMenuStrip statt manuellem Show(Cursor.Position): Windows
                // positioniert und schließt das Menü dann korrekt.
                ContextMenuStrip = trayMenu,
                Visible = true
            };
            trayIcon.DoubleClick += ShowInfo;

            // Fenster-Icon für Alt+Tab / Dialoge.
            this.Icon = weekIcon;
        }

        /// <summary>Setzt den Tooltip auf die aktuelle Woche (Limit: 127 Zeichen).</summary>
        private void ShowCalendarWeek()
        {
            trayIcon.Text = BuildTooltipText();
        }

        private string BuildTooltipText()
        {
            return string.Format(CultureInfo.CurrentCulture, "Kalenderwoche {0}", currentWeek);
        }

        private void ToggleAutostart(object? sender, EventArgs e)
        {
            bool shouldEnable = autostartMenuItem.Checked;
            bool succeeded = shouldEnable
                ? AutostartHelper.RegisterAutostart()
                : AutostartHelper.UnregisterAutostart();

            // Bei einem Registry-Fehler den Haken zurücksetzen, statt einen
            // Zustand anzuzeigen, der so nicht gespeichert wurde.
            autostartMenuItem.Checked = succeeded ? shouldEnable : AutostartHelper.IsAutostartEnabled();
        }

        private void ShowInfo(object? sender, EventArgs e)
        {
            DateTime monday = CalendarWeekHelper.GetFirstDayOfWeek(DateTime.Now);
            DateTime sunday = monday.AddDays(6);

            string message = string.Format(
                CultureInfo.CurrentCulture,
                "Kalenderwoche {0} ({1}){2}{3:d} - {4:d}{2}{2}ISO 8601, Woche beginnt am Montag.",
                currentWeek,
                currentWeekYear,
                Environment.NewLine,
                monday,
                sunday);

            MessageBox.Show(message, AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ExitApplication(object? sender, EventArgs e)
        {
            trayIcon.Visible = false;
            Application.Exit();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Icon vor dem NotifyIcon abhängen, damit kein Geister-Icon zurückbleibt.
                if (trayIcon != null)
                {
                    trayIcon.Visible = false;
                }

                components.Dispose();

                if (weekIcon != null)
                {
                    this.Icon = null;
                    weekIcon.Dispose();
                    weekIcon = null;
                }
            }

            base.Dispose(disposing);
        }
    }
}
