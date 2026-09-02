using System;
using Microsoft.Win32;

namespace SystrayCalendarWeek
{
    /// <summary>
    /// Ein-/Austragen im Windows-Autostart über den User-Hive der Registry.
    /// Benötigt keine Admin-Rechte, da nur HKEY_CURRENT_USER beschrieben wird.
    /// </summary>
    public static class AutostartHelper
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "CalendarWeek";

        /// <summary>
        /// Registriert das Programm im Windows-Autostart.
        /// </summary>
        /// <returns>true bei Erfolg.</returns>
        public static bool RegisterAutostart()
        {
            try
            {
                // Pfad in Anführungszeichen, sonst bricht der Run-Key bei Leerzeichen im Pfad.
                string command = "\"" + ExecutablePath + "\"";

                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath))
                {
                    key.SetValue(AppName, command, RegistryValueKind.String);
                }

                ConsoleOutput.WriteLine("✓ Autostart registriert: " + command);
                return true;
            }
            catch (Exception ex)
            {
                ConsoleOutput.WriteLine("✗ Fehler beim Registrieren: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Entfernt das Programm aus dem Windows-Autostart.
        /// </summary>
        /// <returns>true bei Erfolg.</returns>
        public static bool UnregisterAutostart()
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true))
                {
                    key?.DeleteValue(AppName, throwOnMissingValue: false);
                }

                ConsoleOutput.WriteLine("✓ Autostart deregistriert");
                return true;
            }
            catch (Exception ex)
            {
                ConsoleOutput.WriteLine("✗ Fehler beim Deregistrieren: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Prüft, ob das Programm im Autostart eingetragen ist.
        /// </summary>
        public static bool IsAutostartEnabled()
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath))
                {
                    return key?.GetValue(AppName) != null;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Pfad der laufenden Exe. Funktioniert auch für PublishSingleFile,
        /// weil der Prozesspfad (nicht das Assembly-Location) gelesen wird.
        /// </summary>
        private static string ExecutablePath
        {
            get
            {
                return Environment.ProcessPath ?? System.Windows.Forms.Application.ExecutablePath;
            }
        }
    }
}
