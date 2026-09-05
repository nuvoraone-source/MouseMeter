# Changelog

Alle nennenswerten Änderungen an MouseMeter. Die Versionsnummern folgen dem
Entwicklungsplan aus dem Gesamtkonzept (§36), mit einer Planänderung ab
v0.7.0: auf Nutzerwunsch wurden Ablage-Minimieren und Scrollrad-Erfassung
vorgezogen. Die ursprünglich für v0.7.0 vorgesehene Datenspeicherung rückt
dadurch eine Version weiter nach hinten.

## [0.7.1] – 2026-09-05

Scrollstrecke in Metern.

### Neu
- Die Scrollrad-Kachel zeigt jetzt zusätzlich eine „Scrollstrecke“ in Metern
  (Format wie die Mausstrecke), die Zählung nach vorwärts/rückwärts bleibt
  darunter erhalten.
- `ScrollTracker.DistanceMeters`: rechnet Scrollrad-Ausschläge in eine
  geschätzte Strecke um (typischer Radumfang ÷ übliche Raststufen pro
  Umdrehung, ≈ 2,5 mm je Ausschlag).

### Wichtiger Hinweis
Anders als die Mausstrecke (die über die tatsächliche Bildschirm-DPI exakt
berechnet wird) ist die Scrollstrecke eine **Schätzung** - Scrollräder haben
kein genormtes Maß pro Raststufe. Deshalb steht in der Kachel ein kleines
„ca.“, und der Tooltip weist ausdrücklich darauf hin.

## [0.7.0] – 2026-09-05

Minimieren in die Ablage und Scrollrad-Erfassung.

### Neu
- MouseMeter verschwindet beim Minimieren aus der Taskleiste und legt
  stattdessen ein Symbol in die Windows-Ablage neben der Uhr (systemnaher
  Hintergrundbetrieb, Gesamtkonzept Abschnitt 31). Ein Linksklick auf das
  Symbol stellt das Fenster wieder her, ein Rechtsklick öffnet ein Menü zum
  Öffnen oder Beenden. Implementiert direkt über die native Shell-API
  (`Shell_NotifyIcon`), ohne zusätzliche Windows-Forms-Abhängigkeit.
- Neue Kachel „Scrollrad“ (halbe Breite, neben der ebenfalls auf halbe
  Breite verkleinerten Kachel „Tastaturanschläge“): zählt Scrollrad-Ausschläge
  getrennt nach vorwärts (▲) und rückwärts (▼).
- `GlobalMouseHook` erkennt zusätzlich Scrollrad-Ereignisse; `ScrollTracker`
  zählt sie getrennt nach Richtung.

### Bekannte Einschränkung
Auch die Scrollrad-Zähler gelten nur für die laufende Sitzung; dauerhafte
Speicherung folgt mit der nächsten Version.

## [0.6.0] – 2026-09-05

Tastaturanschläge.

### Neu
- Systemweiter Low-Level-Tastaturhook (`GlobalKeyboardHook`): erkennt
  Tastendruck und Loslassen anhand des virtuellen Tastencodes - niemals das
  resultierende Zeichen oder den eingegebenen Text.
- `KeystrokeTracker` zählt echte Anschläge und rechnet dabei die
  Windows-eigene Wiederholung beim Gedrückthalten einer Taste heraus: ein
  Tastencode zählt erst nach dem Loslassen erneut als neuer Anschlag.
- Die Kachel „Tastaturanschläge“ zeigt jetzt den echten, live gezählten Wert.

Damit zeigen alle neun Dashboard-Kacheln echte, live erfasste Werte -
das Grundgerüst aus v0.1.0/v0.2.0 ist vollständig mit Leben gefüllt.

### Bekannte Einschränkung
Auch dieser Zähler gilt nur für die laufende Sitzung; dauerhafte Speicherung
folgt mit v0.7.0.

## [0.5.0] – 2026-09-05

Mausklick-Erfassung.

### Neu
- Der systemweite Maushook (`GlobalMouseHook`) erkennt jetzt zusätzlich
  Linksklicks, Rechtsklicks und Mittelklicks, unabhängig vom Fensterfokus.
- `MouseClickTracker` zählt die drei Klickarten getrennt.
- Die Kacheln „Linksklicks“, „Rechtsklicks“, „Mittelklicks“ und
  „Gesamtklicks“ (Summe der drei) zeigen jetzt die echten, live gezählten
  Werte als ganze Zahlen ohne Nachkommastellen (Abschnitt 33 des Konzepts).

### Bekannte Einschränkung
Die Klickzähler gelten nur für die laufende Sitzung und beginnen bei jedem
Programmstart wieder bei Null - dauerhafte Speicherung folgt mit v0.7.0.

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
