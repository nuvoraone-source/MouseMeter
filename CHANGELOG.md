# Changelog

Alle nennenswerten Änderungen an MouseMeter. Die Versionsnummern folgen dem
Entwicklungsplan aus dem Gesamtkonzept (§36).

## [0.4.0] – 2026-09-05

Geschwindigkeitsmessung und Rekord.

### Neu
- Kachel „Geschwindigkeit“ zeigt die aktuelle Mausgeschwindigkeit, berechnet
  aus der Streckenänderung je Anzeige-Intervall (200 ms).
- Kachel „Durchschnitt“ zeigt die durchschnittliche Geschwindigkeit über die
  gesamte laufende Sitzung (Gesamtstrecke ÷ Gesamtzeit).
- Kachel „Rekord“ merkt sich die höchste in dieser Sitzung gemessene
  Geschwindigkeit.

### Bekannte Einschränkung
Der Rekord gilt nur für die laufende Sitzung und geht beim Beenden von
MouseMeter noch verloren - ein dauerhafter, sitzungsübergreifender Rekord
folgt zusammen mit der Datenspeicherung (v0.7.0).

## [0.3.0] – 2026-09-05

Mausbewegung und Mausstrecke.

### Neu
- Systemweiter Low-Level-Maushook (`GlobalMouseHook`): erfasst die
  Mausposition unabhängig davon, welches Fenster gerade den Fokus hat, damit
  die Erfassung wie im Konzept vorgesehen automatisch im Hintergrund läuft.
- `MouseDistanceTracker` rechnet die Pixelbewegung über die Bildschirm-DPI in
  eine physische Strecke in Metern um und summiert sie laufend auf.
- Die Kachel „Mausstrecke“ zeigt jetzt den echten, live berechneten Wert
  (Aktualisierung alle 200 ms). Der Status wechselt automatisch auf
  „Tracking ● Aktiv“, sobald die Erfassung läuft.
- Kontakt-E-Mail-Adresse korrigiert: `nuvora.one@gmail.com` (vorherige
  Version enthielt versehentlich eine falsche Adresse).

### Bekannte Einschränkung
Geschwindigkeit, Durchschnitt und Rekord bleiben bewusst auf Null - ihre
Berechnung folgt planmäßig mit v0.4.0. Die zurückgelegte Strecke wird nur für
die laufende Sitzung gezählt; dauerhafte Speicherung folgt mit v0.7.0.

## [0.2.0] – 2026-09-05

Dashboard und vollständige Oberfläche.

### Neu
- Live-Dashboard mit den neun Kacheln aus dem Konzept (§7): Mausstrecke,
  Geschwindigkeit, Durchschnitt, Rekord, Links-/Rechts-/Mittelklicks,
  Gesamtklicks und Tastaturanschläge. Zeigt in dieser Version durchgehend
  Nullwerte im endgültigen Zahlenformat (Messwerte mit zwei Nachkommastellen,
  Zähler ohne).
- Zeitraum-Auswahl (Tag/Woche/Monat/Jahr) als Segmentleiste oberhalb des
  Dashboards.
- Platzhalter für die Diagrammfläche, reagiert bereits auf die gewählte
  Zeitraum-Ansicht.
- Einstellungsfenster (bisher nur mit Support-Kontakt gefüllt, Tracking/
  Autostart/Datenverwaltung folgen mit v0.10.0).
- Kontaktweg zu nuvora: kleiner, dezenter „nuvora“-Link mittig in der
  Statusleiste sowie zusätzlich über die Einstellungen erreichbar. Öffnet ein
  Kontaktfenster mit der Support-Adresse (Kopieren oder Standard-Mailprogramm
  öffnen) - MouseMeter versendet dabei nichts selbst.

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
