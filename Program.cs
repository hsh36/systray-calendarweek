using System;
using System.Threading;
using System.Windows.Forms;

namespace SystrayCalendarWeek
{
    internal static class Program
    {
        private const string SingleInstanceMutexName = @"Local\SystrayCalendarWeek.SingleInstance";

        [STAThread]
        private static int Main(string[] args)
        {
            if (args.Length > 0)
            {
                return RunCommand(args[0]);
            }

            // Der Autostart kann die Exe ein zweites Mal starten (z.B. nach manuellem
            // Start). Ohne diese Sperre hingen zwei Icons im Tray.
            using (var instanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out bool isFirstInstance))
            {
                if (!isFirstInstance)
                {
                    return 0;
                }

                ApplicationConfiguration.Initialize();
                MemoryTrimmer.TrimWhenIdle();
                Application.Run(new SystrayApp());

                GC.KeepAlive(instanceMutex);
            }

            return 0;
        }

        private static int RunCommand(string command)
        {
            bool succeeded;

            switch (command)
            {
                case "--register-autostart":
                    succeeded = AutostartHelper.RegisterAutostart();
                    break;

                case "--unregister-autostart":
                    succeeded = AutostartHelper.UnregisterAutostart();
                    break;

                case "--help":
                case "-h":
                case "/?":
                    ConsoleOutput.WriteLine(HelpText);
                    succeeded = true;
                    break;

                default:
                    ConsoleOutput.WriteLine("✗ Unbekanntes Argument: " + command + Environment.NewLine + Environment.NewLine + HelpText);
                    succeeded = false;
                    break;
            }

            ConsoleOutput.Release();
            return succeeded ? 0 : 1;
        }

        private static string HelpText
        {
            get
            {
                return "calendarweek - zeigt die ISO-8601-Kalenderwoche im Systray." + Environment.NewLine +
                       Environment.NewLine +
                       "  calendarweek                     Startet das Tray-Icon" + Environment.NewLine +
                       "  calendarweek --register-autostart    Trägt das Programm in den Autostart ein" + Environment.NewLine +
                       "  calendarweek --unregister-autostart  Entfernt es aus dem Autostart";
            }
        }
    }
}
