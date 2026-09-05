# Changelog

Alle nennenswerten Änderungen an MouseMeter. Die Versionsnummern folgen dem
Entwicklungsplan aus dem Gesamtkonzept (§36).

## [0.1.0] – 2026-09-05

Grundprojekt, Programmstart und Startbildschirm.

### Neu
- WPF-Projekt auf .NET 10 (`net10.0-windows`) mit Solution `src/MouseMeter.sln`.
- Startbildschirm mit Logo, Wortmarke „MouseMeter“, Claim „Every Move Counts“,
  Ladebalken und Versionsangabe. Sichtbare Dauer 1,7 s, Ein- und Ausblenden animiert.
- Hauptfenster mit Kopfbereich (Logo, Wortmarke, Claim, Zahnrad), Inhaltsfläche
  und Statusleiste („Tracking ● Inaktiv“, Versionsnummer).
- Dunkles Farb- und Typografie-Thema in `Styles/Theme.xaml`, Vektorlogo in
  `Assets/Logo.xaml`, Anwendungssymbol als Multi-Size-`.ico`.
- Dunkle native Titelleiste über `DwmSetWindowAttribute`.
- Einzelinstanz-Sperre: MouseMeter läuft nie doppelt und erfasst damit später
  keine Aktivität zweifach.
- Portable Veröffentlichung als einzelne EXE über das Publish-Profil `Portable`.
- Werkzeuge: `tools/make-icon.ps1` (Symbol erzeugen), `tools/make-release.ps1`
  (Versionsarchiv erzeugen).

### Noch nicht enthalten
Erfassung von Maus, Klicks und Tastatur, Dashboard-Kacheln, Datenspeicherung,
Zeiträume, Diagramme und Einstellungen. Diese folgen ab v0.2.0.
