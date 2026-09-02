using System;

namespace SystrayCalendarWeek
{
    /// <summary>
    /// Terminrechnung für die nächtliche Aktualisierung. Bewusst als reine Funktion
    /// getrennt vom Timer, damit sie ohne laufende UI prüfbar ist.
    /// </summary>
    internal static class RefreshSchedule
    {
        /// <summary>Obergrenze: der Timer soll nie länger als einen Tag schlafen.</summary>
        public const int OneDayInMilliseconds = 24 * 60 * 60 * 1000;

        /// <summary>
        /// Untergrenze. Timer.Interval muss mindestens 1 sein; eine Sekunde vermeidet
        /// zusätzlich, dass der Timer direkt an der Tagesgrenze mehrfach kurz hintereinander feuert.
        /// </summary>
        public const int MinimumIntervalInMilliseconds = 1000;

        /// <summary>
        /// Millisekunden bis eine Sekunde nach der nächsten Mitternacht.
        /// Der Puffer stellt sicher, dass DateTime.Now beim Feuern wirklich schon
        /// im neuen Tag liegt und nicht auf der Grenze steht.
        /// </summary>
        /// <remarks>
        /// Das Ergebnis ist auf [1 s, 24 h] geklemmt. Die Obergrenze greift bei einer
        /// Zeitumstellung, die die Wandzeitspanne über 24 Stunden streckt; feuert der
        /// Timer dadurch zu früh, plant der Aufrufer einfach neu.
        /// </remarks>
        public static int MillisecondsUntilNextMidnight(DateTime now)
        {
            DateTime next = now.Date.AddDays(1).AddSeconds(1);
            double milliseconds = (next - now).TotalMilliseconds;

            return (int)Math.Clamp(milliseconds, MinimumIntervalInMilliseconds, OneDayInMilliseconds);
        }
    }
}
