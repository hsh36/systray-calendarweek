# 📅 Systray Calendar Week

Kleines, schlankes Windows-11-Programm, das die aktuelle ISO-8601-Kalenderwoche im Systemtray anzeigt.

Das Tray-Icon zeigt die **Wochennummer direkt als Zahl** – kein Hovern nötig. Der Tooltip nennt zusätzlich „Kalenderwoche XX".

## Features

- ✅ **Wochennummer im Icon:** Die KW wird zur Laufzeit ins Icon gezeichnet und passt sich der DPI-Skalierung an
- ✅ **ISO 8601:** Korrekte Berechnung (KW 1–53), Woche beginnt am Montag
- ✅ **Autostart:** Ein- und Austragen per Kommandozeile oder Kontextmenü
- ✅ **Single Instance:** Ein zweiter Start bringt kein zweites Icon in den Tray
- ✅ **Schlank:** 188 KB EXE, ~7 MB RAM, Start in ~0,4 s
- ✅ **Aktualisiert sich selbst:** Um Mitternacht, nach dem Aufwachen aus dem Energiesparmodus und bei Zeitumstellung

## Voraussetzungen

- Windows 10/11
- **.NET 8 Desktop Runtime** (für den Build: .NET 8 SDK)

Prüfen mit:

```powershell
dotnet --list-runtimes    # Microsoft.WindowsDesktop.App 8.x muss dabei sein
```

## Build

```powershell
cd systray-calendarweek
dotnet publish -c Release
```

Das Ergebnis ist eine einzelne Datei – alle Einstellungen (Single-File, win-x64, framework-abhängig) stehen bereits in der `.csproj`:

```
bin\Release\net8.0-windows\win-x64\publish\calendarweek.exe
```

Diese EXE ist frei kopierbar, z. B. nach `C:\Tools\calendarweek.exe`.

## Installation

### Schritt 1: EXE ablegen

Kopiere `calendarweek.exe` an einen festen Ort. **Wichtig:** Der Autostart-Eintrag zeigt auf genau diesen Pfad – wird die EXE später verschoben, muss der Autostart neu registriert werden.

### Schritt 2: In den Autostart legen

**Variante A – das Programm trägt sich selbst ein:**

```powershell
.\calendarweek.exe --register-autostart
# Ausgabe: ✓ Autostart registriert: "C:\Tools\calendarweek.exe"
```

Geschrieben wird nach `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run`. **Admin-Rechte sind nicht nötig**, es wird nur der User-Hive angefasst.

**Variante B – über das Kontextmenü:** Rechtsklick auf das Tray-Icon → „Mit Windows starten"

**Variante C – manuell:** `Win + R` → `shell:startup` → Verknüpfung zur EXE hineinlegen

### Schritt 3: Testen

```powershell
.\calendarweek.exe
```

Das Icon mit der Wochennummer erscheint im Systemtray.

> **Icon nicht sichtbar?** Windows 11 versteckt neue Tray-Icons standardmäßig im Überlauf-Menü (Pfeil nach oben, links neben den Systemsymbolen). Zum dauerhaften Anheften: *Einstellungen → Personalisierung → Taskleiste → Andere Symbole in der Taskleiste* → `calendarweek` einschalten. Alternativ das Icon aus dem Überlauf-Menü einfach auf die Taskleiste ziehen.

## Verwendung

Das Programm läuft ohne Fenster im Hintergrund.

| Aktion | Ergebnis |
|---|---|
| **Hover** | Tooltip „Kalenderwoche 36" |
| **Doppelklick** | Info-Dialog mit KW, ISO-Wochenjahr und Datumsbereich der Woche |
| **Rechtsklick** | Kontextmenü |

Das Kontextmenü enthält:

- **Kalenderwoche 36** – aktuelle Woche (nur Anzeige)
- **Mit Windows starten** – Autostart an/aus, der Haken zeigt den aktuellen Zustand
- **Info…** – z. B. „Kalenderwoche 36 (2026) / 31.08.2026 – 06.09.2026"
- **Beenden** – schließt das Programm und entfernt das Icon

## Kommandozeile

```powershell
calendarweek.exe                        # Startet das Tray-Icon
calendarweek.exe --register-autostart   # Trägt das Programm in den Autostart ein
calendarweek.exe --unregister-autostart # Entfernt es aus dem Autostart
calendarweek.exe --help                 # Hilfe
```

Exit-Code `0` bei Erfolg, `1` bei Fehler oder unbekanntem Argument. Die Ausgabe erscheint im aufrufenden Terminal und funktioniert auch mit Umleitung (`> log.txt`) und in Pipes. Wird die EXE mit einem Flag per Doppelklick gestartet – also ohne Terminal –, erscheint die Meldung als Dialogfenster.

## Deinstallation

1. Autostart entfernen:

   ```powershell
   calendarweek.exe --unregister-autostart
   ```

2. Rechtsklick auf das Icon → **Beenden**
3. `calendarweek.exe` löschen

Es werden keine weiteren Dateien, Ordner oder Registry-Schlüssel angelegt.

## Technisches

| | |
|---|---|
| **Sprache** | C# / .NET 8 (`net8.0-windows`) |
| **UI** | WinForms + NotifyIcon |
| **Größe** | 188 KB (Single-File, framework-abhängig) |
| **RAM** | ~7 MB Arbeitssatz im Leerlauf |
| **Startzeit** | ~0,4 s bis das Icon steht |
| **Abhängigkeiten** | nur die .NET 8 Desktop Runtime |

### Quellcode-Struktur

```
systray-calendarweek/
├── Program.cs                   # Entry Point, Argumente, Single-Instance-Mutex
├── SystrayApp.cs                # Unsichtbare Form, NotifyIcon, Kontextmenü
├── SystrayApp.Designer.cs       # Form-Grundeinstellungen
├── CalendarWeekHelper.cs        # ISO-8601-Wochenberechnung
├── RefreshSchedule.cs           # Terminrechnung bis zur nächsten Mitternacht
├── TrayIconFactory.cs           # Zeichnet die Wochennummer ins Icon
├── AutostartHelper.cs           # Registry-Integration (HKCU)
├── ConsoleOutput.cs             # Konsolenausgabe für eine WinExe
├── MemoryTrimmer.cs             # Gibt den Arbeitssatz im Leerlauf frei
├── app.manifest                 # Windows-11-Kompatibilität
├── Resources/
│   └── calendar-icon.ico        # Exe-Icon (16–256 px)
└── systray-calendarweek.csproj
```

### Designentscheidungen

**Wochennummer im Icon statt statischem Symbol.** Ein Kalender-Symbol mit der KW nur im Tooltip würde bedeuten, dass man für die eigentliche Information erst hovern muss. Das Icon wird deshalb bei jedem Start neu gezeichnet – in der Größe, die `SystemInformation.SmallIconSize` für die aktuelle DPI meldet. Das statische Kalender-Symbol dient als Exe-Icon.

**Eigene ISO-Berechnung statt `CultureInfo.GetWeekOfYear()`.** Dessen `CalendarWeekRule.FirstFourDayWeek` ist nicht ISO-konform, weil es den Wochenbeginn aus der Kultur zieht und das Wochenjahr nicht kennt. `CalendarWeekHelper` implementiert die Standardformel direkt.

**Single-Instance-Mutex.** Ohne ihn hängen zwei Icons im Tray, sobald man das Programm manuell startet, während die Autostart-Instanz schon läuft.

**Arbeitssatz-Trim im Leerlauf.** Der Start zieht einmalig ~45 MB Runtime- und WinForms-Seiten in den Speicher, die eine Tray-Anwendung danach nicht mehr anfasst. `MemoryTrimmer` gibt sie frei, sobald die Anwendung idle ist – danach bleiben ~7 MB. Windows holt die Seiten bei Bedarf (Kontextmenü, Dialog) zurück.

**Aktualisierung über drei Wege statt nur einem Timer.** Ein Timer allein reicht nicht: Schläft der Rechner über Mitternacht, feuert er verspätet, und eine manuelle Zeitänderung sieht er gar nicht. Deshalb hängt die Anwendung zusätzlich an `SystemEvents.TimeChanged` und `SystemEvents.PowerModeChanged`. Diese Rückrufe kommen auf einem fremden Thread und werden über `BeginInvoke` auf den UI-Thread geholt, weil `NotifyIcon` und `Timer` sonst nicht angefasst werden dürfen.

**Der Timer wird nach jedem Feuern neu gestellt,** statt einmal auf 24 Stunden. Bei einer Zeitumstellung ist die Spanne bis zur nächsten Mitternacht 23 oder 25 Stunden; durch das Neustellen korrigiert sich der Termin selbst, und ein zu frühes Feuern bleibt folgenlos, weil `RefreshCalendarWeek()` bei unveränderter Woche nichts tut.

## Tests

Die ISO-Berechnung wurde über **109.938 Tage (1900–2200)** gegen `System.Globalization.ISOWeek` abgeglichen – keine Abweichung. Zusätzlich geprüfte Grenzfälle:

| Datum | Wochentag | Erwartet |
|---|---|---|
| 01.01.2026 | Donnerstag | KW 1 / 2026 |
| 31.12.2026 | Donnerstag | KW 53 / 2026 |
| 01.01.2027 | Freitag | KW 53 / **2026** |
| 30.12.2019 | Montag | KW 1 / **2020** |
| 01.01.2021 | Freitag | KW 53 / **2020** |
| 01.01.2000 | Samstag | KW 52 / **1999** |

Die Beispiele mit abweichendem Jahr zeigen, warum `GetCalendarWeekYear()` nötig ist: Am Jahreswechsel unterscheidet sich das ISO-Wochenjahr vom Kalenderjahr.

## Fehlerbehandlung

**Programm startet nicht:** Von der Kommandozeile starten und auf Meldungen achten. Meist fehlt die .NET 8 Desktop Runtime (`dotnet --list-runtimes`).

**Icon erscheint nicht:** Siehe Hinweis zum Überlauf-Menü oben. Läuft der Prozess? `Get-Process calendarweek`

**Autostart greift nicht:** Eintrag prüfen mit

```powershell
Get-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name CalendarWeek
```

Zeigt er auf einen alten Pfad, `--register-autostart` am neuen Ort erneut ausführen.

**Falsche Woche:** Die Anzeige wird um Mitternacht nachgezogen. Stimmt sie trotzdem nicht, prüfe Datum und Zeitzone von Windows – die Berechnung nutzt die lokale Systemzeit.

## Mögliche Erweiterungen

- Sprachumschaltung DE/EN
- Konfigurierbare Icon-Farbe

## Lizenz

MIT

---

**Version:** 1.0
**Letzte Aktualisierung:** 2026-09-02
**Technologie:** C# + .NET 8 + WinForms
