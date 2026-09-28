# Mouse Mover

Piccola app per Windows che tiene il PC sveglio: finché è attiva, Windows non va in standby
e non spegne lo schermo, e il tuo stato nelle app di chat resta "attivo".
Un solo file `.exe` di circa 20 KB: nessuna installazione e nessun permesso di amministratore.

## Come si usa

1. Scarica `MouseMover.exe` dalla pagina [Releases](../../releases) e avvialo con un doppio clic.
2. Compare un'icona verde nella tray, vicino all'orologio. Non si apre nessuna finestra.
3. Doppio clic sull'icona: pausa (grigia) o ripresa (verde).
4. Tasto destro sull'icona: **Metti in pausa**, **Avvia con Windows**, **Esci**.

L'exe non è firmato: al primo avvio Windows potrebbe mostrare "Windows ha protetto il PC".
Clicca "Ulteriori informazioni" e poi "Esegui comunque".

## Come funziona

- Ogni 30 secondi controlla da quanto tempo non usi mouse e tastiera.
- Se sei inattivo da almeno 60 secondi, preme F15 (un tasto che nessun programma usa)
  e sposta il mouse di 1 pixel avanti e indietro. Mentre lavori non fa nulla.
- Finché è attivo, Windows non va in standby e non spegne lo schermo.
- "Avvia con Windows" scrive solo nella chiave utente `HKCU\...\Run`, senza permessi di amministratore.
- Gira con il .NET Framework 4 già incluso in Windows 10 e 11.

## Compilare

Su Windows, senza installare nulla:

```bat
%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /optimize+ /win32icon:src\MouseMover.ico /out:MouseMover.exe src\MouseMover.cs
```

Su Linux o macOS con Mono:

```sh
mcs -target:winexe -sdk:4.5 -optimize+ -win32icon:src/MouseMover.ico -r:System.Windows.Forms.dll -r:System.Drawing.dll -out:MouseMover.exe src/MouseMover.cs
```

Ogni push su `main` compila l'exe con GitHub Actions. Per pubblicare una release con l'exe allegato: Actions → Build → Run workflow, indicando la versione (es. `v1.1.0`). Anche un tag `v*` pubblica una release.
