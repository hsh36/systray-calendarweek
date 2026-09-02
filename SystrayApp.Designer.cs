using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace SystrayCalendarWeek
{
    partial class SystrayApp
    {
        /// <summary>Container für Designer-Komponenten.</summary>
        private readonly IContainer components = new Container();

        /// <summary>
        /// Grundeinstellungen der Form. Sie wird nie angezeigt und dient nur als
        /// Message-Pump und Besitzer des NotifyIcon.
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(0, 0);
            this.FormBorderStyle = FormBorderStyle.None;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.Name = "SystrayApp";
            this.Opacity = 0d;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(-32000, -32000);
            this.Text = "Kalenderwoche";
            this.WindowState = FormWindowState.Minimized;

            this.ResumeLayout(false);
        }
    }
}
