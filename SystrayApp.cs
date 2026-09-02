using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Microsoft.Win32;

namespace SystrayCalendarWeek
{
    /// <summary>
    /// Unsichtbare Form, die das Tray-Icon hält. Die Kalenderwoche wird beim Start
    /// berechnet und danach um Mitternacht, bei Zeitumstellung und nach dem
    /// Aufwachen aus dem Energiesparmodus aktualisiert.
    /// </summary>
    public partial class SystrayApp : Form
    {
        private const string AppTitle = "Kalenderwoche";

        private NotifyIcon trayIcon = null!;
        private ContextMenuStrip trayMenu = null!;
        private ToolStripMenuItem headerMenuItem = null!;
        private ToolStripMenuItem autostartMenuItem = null!;
        private System.Windows.Forms.Timer midnightTimer = null!;
        private Icon? weekIcon;

        private int currentWeek;
        private int currentWeekYear;

        public SystrayApp()
        {
            InitializeComponent();

            // Handle erzwingen: die Form wird nie sichtbar, wird aber als Ziel für
            // BeginInvoke aus den SystemEvents-Rückrufen gebraucht. Ohne Anzeige
            // entsteht das Fensterhandle sonst nie.
            _ = this.Handle;

            currentWeek = CalendarWeekHelper.GetCalendarWeek(DateTime.Now);
            currentWeekYear = CalendarWeekHelper.GetCalendarWeekYear(DateTime.Now);

            InitializeSystemTray();
            ShowCalendarWeek();
            InitializeScheduledRefresh();
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

            headerMenuItem = new ToolStripMenuItem(BuildTooltipText()) { Enabled = false };

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

        /// <summary>
        /// Richtet die Aktualisierung um Mitternacht ein. Zusätzlich zum Timer werden
        /// Zeitumstellung und Aufwachen abgefangen: schläft der Rechner über Mitternacht,
        /// feuert der Timer verspätet, und eine manuelle Zeitänderung sieht er gar nicht.
        /// </summary>
        private void InitializeScheduledRefresh()
        {
            midnightTimer = new System.Windows.Forms.Timer(components);
            midnightTimer.Tick += OnMidnightTick;
            ScheduleNextMidnight();

            SystemEvents.TimeChanged += OnSystemTimeChanged;
            SystemEvents.PowerModeChanged += OnPowerModeChanged;
        }

        private void ScheduleNextMidnight()
        {
            midnightTimer.Stop();
            midnightTimer.Interval = RefreshSchedule.MillisecondsUntilNextMidnight(DateTime.Now);
            midnightTimer.Start();
        }

        private void OnMidnightTick(object? sender, EventArgs e)
        {
            RefreshCalendarWeek();

            // Immer neu planen, auch wenn sich die Woche nicht geändert hat: bei einer
            // Zeitumstellung kann der Timer vor Mitternacht feuern.
            ScheduleNextMidnight();
        }

        private void OnSystemTimeChanged(object? sender, EventArgs e)
        {
            RunOnUiThread(() =>
            {
                RefreshCalendarWeek();
                ScheduleNextMidnight();
            });
        }

        private void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode != PowerModes.Resume)
            {
                return;
            }

            RunOnUiThread(() =>
            {
                RefreshCalendarWeek();
                ScheduleNextMidnight();
            });
        }

        /// <summary>
        /// Die SystemEvents-Rückrufe kommen auf einem fremden Thread; NotifyIcon und
        /// Timer dürfen aber nur vom UI-Thread angefasst werden.
        /// </summary>
        private void RunOnUiThread(Action action)
        {
            if (IsDisposed || !IsHandleCreated)
            {
                return;
            }

            try
            {
                if (InvokeRequired)
                {
                    BeginInvoke(action);
                }
                else
                {
                    action();
                }
            }
            catch (ObjectDisposedException)
            {
                // Anwendung wird gerade beendet - nichts mehr zu tun.
            }
        }

        /// <summary>
        /// Berechnet die Woche neu und aktualisiert Icon, Tooltip und Menükopf,
        /// falls sie sich geändert hat.
        /// </summary>
        private void RefreshCalendarWeek()
        {
            DateTime now = DateTime.Now;
            int week = CalendarWeekHelper.GetCalendarWeek(now);
            int weekYear = CalendarWeekHelper.GetCalendarWeekYear(now);

            if (week == currentWeek && weekYear == currentWeekYear)
            {
                return;
            }

            currentWeek = week;
            currentWeekYear = weekYear;

            // Neues Icon erst zuweisen, dann das alte freigeben - sonst zeigt der Tray
            // kurzzeitig auf ein zerstörtes Handle.
            Icon? previousIcon = weekIcon;
            weekIcon = TrayIconFactory.CreateWeekIcon(currentWeek);
            trayIcon.Icon = weekIcon;
            this.Icon = weekIcon;
            previousIcon?.Dispose();

            ShowCalendarWeek();
            headerMenuItem.Text = BuildTooltipText();

            // Das Neuzeichnen holt Seiten zurück in den Arbeitssatz.
            MemoryTrimmer.TrimWhenIdle();
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
                // SystemEvents hält statische Referenzen - ohne Abmelden bleibt die
                // Form am Leben und die Rückrufe laufen ins Leere.
                SystemEvents.TimeChanged -= OnSystemTimeChanged;
                SystemEvents.PowerModeChanged -= OnPowerModeChanged;

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
