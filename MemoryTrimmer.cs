using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SystrayCalendarWeek
{
    /// <summary>
    /// Gibt den Arbeitssatz frei, sobald die Anwendung nach dem Start in den Leerlauf geht.
    /// Der Start zieht einmalig ~45 MB an Runtime- und WinForms-Seiten in den Speicher,
    /// die eine Tray-Anwendung danach nicht mehr anfasst. Windows lagert sie aus und
    /// holt sie bei Bedarf (Kontextmenü, Info-Dialog) zurück.
    /// </summary>
    internal static class MemoryTrimmer
    {
        [DllImport("psapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EmptyWorkingSet(IntPtr processHandle);

        /// <summary>
        /// Hängt sich einmalig an das Idle-Ereignis der Anwendung. Vorher zu trimmen
        /// bringt nichts, weil der Start die Seiten sofort wieder anfordert.
        /// </summary>
        public static void TrimWhenIdle()
        {
            Application.Idle += OnIdle;
        }

        private static void OnIdle(object? sender, EventArgs e)
        {
            Application.Idle -= OnIdle;
            Trim();
        }

        private static void Trim()
        {
            try
            {
                using (Process process = Process.GetCurrentProcess())
                {
                    EmptyWorkingSet(process.Handle);
                }
            }
            catch (Exception)
            {
                // Rein optional - schlägt der Aufruf fehl, läuft die Anwendung normal weiter.
            }
        }
    }
}
