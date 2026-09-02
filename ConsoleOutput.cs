using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace SystrayCalendarWeek
{
    /// <summary>
    /// Eine WinExe hat keine eigene Konsole. Für die Autostart-Flags wird die Ausgabe
    /// deshalb an das aufrufende Terminal weitergereicht. Gibt es keines
    /// (Start per Doppelklick), fällt die Ausgabe auf eine MessageBox zurück.
    /// </summary>
    internal static class ConsoleOutput
    {
        private const int AttachParentProcess = -1;
        private const int StdOutputHandle = -11;

        private static readonly IntPtr InvalidHandleValue = new IntPtr(-1);

        private static bool hasOutput;
        private static bool attachedConsole;
        private static bool initialized;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int stdHandle);

        public static void WriteLine(string message)
        {
            EnsureInitialized();

            if (hasOutput)
            {
                Console.WriteLine(message);
            }
            else
            {
                MessageBox.Show(message, "Kalenderwoche", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Gibt eine angehängte Konsole wieder frei und leert den Puffer.
        /// </summary>
        public static void Release()
        {
            if (!hasOutput)
            {
                return;
            }

            Console.Out.Flush();

            if (attachedConsole)
            {
                FreeConsole();
            }

            hasOutput = false;
            attachedConsole = false;
        }

        private static void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;

            // Bei Umleitung (> datei oder | pipe) erbt auch eine GUI-Anwendung ein
            // gültiges stdout-Handle. Dann direkt dorthin schreiben - ein AttachConsole
            // würde die Std-Handles auf die Konsole umbiegen und die Umleitung ins Leere
            // laufen lassen.
            IntPtr standardOutput = GetStdHandle(StdOutputHandle);

            if (standardOutput != IntPtr.Zero && standardOutput != InvalidHandleValue)
            {
                hasOutput = true;

                // Console.OutputEncoding greift bei umgeleitetem stdout nicht, deshalb
                // den Writer selbst auf UTF-8 setzen - sonst landen Umlaute und ✓/✗
                // in der ANSI-Codepage.
                var writer = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false))
                {
                    AutoFlush = true
                };
                Console.SetOut(writer);
            }
            else if (AttachConsole(AttachParentProcess))
            {
                // Kein umgeleitetes stdout: an die Konsole des Aufrufers hängen.
                hasOutput = true;
                attachedConsole = true;

                try
                {
                    // Setzt die Codepage der Konsole - nötig für die ✓/✗ Zeichen.
                    Console.OutputEncoding = Encoding.UTF8;
                }
                catch (Exception)
                {
                    // Lässt sich nicht überall setzen - die Ausgabe bleibt trotzdem lesbar.
                }
            }
        }
    }
}
