using System;

namespace SystrayCalendarWeek
{
    /// <summary>
    /// ISO 8601 Kalenderwochen-Berechnung.
    /// Woche 1 ist die Woche, die den 4. Januar (bzw. den ersten Donnerstag) enthält.
    /// Die Woche beginnt am Montag.
    /// Bewusst eigenständig implementiert statt über CultureInfo.GetWeekOfYear(),
    /// da dessen CalendarWeekRule.FirstFourDayWeek nicht ISO-konform ist.
    /// </summary>
    public static class CalendarWeekHelper
    {
        /// <summary>
        /// Berechnet die ISO 8601 Kalenderwoche (1-53).
        /// </summary>
        /// <param name="date">Datum, z.B. DateTime.Now.</param>
        /// <returns>Wochennummer 1-53.</returns>
        public static int GetCalendarWeek(DateTime date)
        {
            GetIsoWeekAndYear(date, out int week, out _);
            return week;
        }

        /// <summary>
        /// Gibt das ISO-Wochenjahr zurück. Es kann von DateTime.Year abweichen,
        /// z.B. gehört der 1. Januar 2027 zur Woche 53 des Wochenjahres 2026.
        /// </summary>
        public static int GetCalendarWeekYear(DateTime date)
        {
            GetIsoWeekAndYear(date, out _, out int year);
            return year;
        }

        /// <summary>
        /// Montag der Woche, in der das Datum liegt (Uhrzeit auf 00:00 gesetzt).
        /// </summary>
        public static DateTime GetFirstDayOfWeek(DateTime date)
        {
            return date.Date.AddDays(1 - IsoDayOfWeek(date));
        }

        /// <summary>
        /// Anzahl der ISO-Wochen eines Wochenjahres: 52 oder 53.
        /// Ein Jahr hat 53 Wochen, wenn es mit einem Donnerstag beginnt
        /// oder ein Schaltjahr ist, das mit einem Mittwoch beginnt.
        /// </summary>
        public static int GetWeeksInYear(int year)
        {
            return 52 + ((JanuaryFirstWeekday(year) == 4 || JanuaryFirstWeekday(year - 1) == 3) ? 1 : 0);
        }

        private static void GetIsoWeekAndYear(DateTime date, out int week, out int year)
        {
            year = date.Year;

            // Standard-ISO-Formel: week = floor((Tag im Jahr - Wochentag + 10) / 7)
            week = (date.DayOfYear - IsoDayOfWeek(date) + 10) / 7;

            if (week < 1)
            {
                // Datum gehört noch zur letzten Woche des Vorjahres.
                year--;
                week = GetWeeksInYear(year);
            }
            else if (week > GetWeeksInYear(year))
            {
                // Datum gehört bereits zur Woche 1 des Folgejahres.
                year++;
                week = 1;
            }
        }

        /// <summary>Wochentag nach ISO: Montag = 1 ... Sonntag = 7.</summary>
        private static int IsoDayOfWeek(DateTime date)
        {
            int day = (int)date.DayOfWeek; // Sonntag = 0 ... Samstag = 6
            return day == 0 ? 7 : day;
        }

        /// <summary>Wochentag des 1. Januar nach ISO (Montag = 1 ... Sonntag = 7).</summary>
        private static int JanuaryFirstWeekday(int year)
        {
            int p = (year + (year / 4) - (year / 100) + (year / 400)) % 7;
            return p == 0 ? 7 : p;
        }
    }
}
