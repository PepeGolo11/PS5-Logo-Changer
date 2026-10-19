# PS5 Logo Changer

[![Release Stage](https://img.shields.io/badge/stage-Alpha-red.svg)](https://github.com/)
[![Target Framework](https://img.shields.io/badge/.NET-7.0--windows-blue.svg)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/platform-PS5%20Jailbreak-black.svg)](https://github.com/)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

A Windows desktop utility designed for jailbroken PlayStation 5 consoles to manage and customize home-screen assets, game logos, and concept metadata directly through an active FTP connection.

---

> [!CAUTION]
> ### ⚠️ Alpha Stage Disclaimer — Use at Your Own Risk
> **PS5 Logo Changer is currently in active ALPHA development.**
> - Expect bugs, incomplete features, and potential unexpected behavior.
> - This software modifies and overwrites `/system_data/priv/mms/app.db` on your console.
> - An interrupted write, corrupted database, or conflict can cause database rebuilds or kernel panics on the console.
> - **Always make a manual backup of `/system_data/priv/mms/app.db` before using this tool.**
> - Make sure **all games and applications are closed** on your PS5 before uploading or updating data.

---

## 🎮 What It Does

- **Direct PS5 FTP Integration**: Connects seamlessly to your console's FTP server (default port `2121`).
- **Database Inspection**: Automatically downloads and reads `/system_data/priv/mms/app.db` to parse installed games and their metadata.
- **Custom Game Logos**:
  - Automatically loads and previews existing logos stored on the console when clicking any game entry.
  - Browse local `.png` files and upload them directly to `/user/data/GameLogos/<GameName>.png`.
  - Automatically sanitizes special characters from game titles to ensure clean filesystem paths.
  - Updates `tbl_conceptmetadata.logoImageInfo` in `app.db` and safely uploads the modified database back to the PS5.
- **Game Description Editor**:
  - View and edit the `description` column for any selected game.
  - Dedicated **Update Description** button with a 150-character limit (allows empty/null values).
  - Updates the SQLite database and writes changes back over FTP.
- **Search & Quick Filters**:
  - Real-time search box to find titles quickly.
  - One-click **Missing Logo** filter to display only games without an assigned logo.
- **Persistent Settings**:
  - Configure and save your PS5 FTP IP Address, FTP Port, and Logo save destination directory.
- **Modern Dark Glass UI**:
  - Built with a sleek dark theme featuring subtle borders and smooth visual feedback.

---

## 📋 Requirements

1. **PC (Windows)**:
   - Windows 10 or 11 (64-bit)
   - [.NET 7.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/7.0) (or [.NET 7.0 SDK](https://dotnet.microsoft.com/download/dotnet/7.0) if building from source)
2. **PlayStation 5 Console**:
   - Jailbroken PS5 with homebrew/FTP server running (e.g., etaHEN, ftps5, or similar background FTP payload).
   - Write access to `/system_data/priv/mms/app.db` and `/user/data/`.
   - Both your PC and PS5 must be connected to the same local network (LAN / Wi-Fi).

---

## 🚀 How to Use

### 1. Configure Connection Settings
1. Launch **PS5 Logo Changer**.
2. Click the **Settings** button in the top-right corner.
3. Enter your PS5's local IP address (e.g., `192.168.1.100` or `10.0.0.186`).
4. Set the FTP Port (default is `2121`).
5. (Optional) Adjust the target directory on PS5 for logos (default: `/user/data/GameLogos/`).
6. Click **Save**, then click **Go Back** to return to the main screen.

### 2. Connect to Your PS5
1. On the **Logo Change** screen, click **Connect**.
2. The status indicator will turn green (`● Connected`), and the utility will download and parse `app.db`.
3. The table on the left will populate with all installed games sorted alphabetically (A–Z).

### 3. Change a Game Logo
1. Select a game from the table.
2. If the game already has a logo assigned, it will download and display in the **Logo Preview** box.
3. Click **Browse PNG...** and choose your replacement `.png` image from your computer.
4. Once selected, a local preview will appear and the **Upload Logo** button will become enabled.
5. Click **Upload Logo**.
6. Review and confirm the warning dialog.
7. The utility will:
   - Upload the PNG image to `/user/data/GameLogos/<SanitizedGameName>.png`.
   - Download the current `app.db`.
   - Update `tbl_conceptmetadata.logoImageInfo`.
   - Upload the updated `app.db` back to `/system_data/priv/mms/app.db`.
   - Refresh the table view automatically.

> [!NOTE]
> - **Instant Change**: If applying a logo to a game that did *not* previously have one, the change usually appears immediately on the PS5 home screen.
> - **Restart Required**: If replacing a logo that *already existed*, the PS5 may cache the asset; a console reboot is often required to display the new logo.

### 4. Edit Game Description
1. Select a game from the table.
2. In the **Description** box on the right, edit or enter new text (maximum 150 characters, null/empty allowed).
3. Click the **Update Description** button located directly beneath the text box.
4. Confirm the warning dialog to write the change to `app.db` on your console.

---

## 🛠️ Building from Source

To compile the application yourself:

```bash
# Clone the repository
git clone https://github.com/your-username/PS5-Logo-Changer.git

# Navigate into the project folder
cd PS5-Logo-Changer

# Restore dependencies and build release binary
dotnet build -c Release
```

The compiled binary (`PS5 Logo Changer.exe`) will be located in `bin/Release/net7.0-windows/`.

---

## 🗺️ Roadmap & Upcoming Features

- [ ] **API Scraper Integration** *(Work in Progress)*:
  - ScreenScraper.fr metadata & asset search.
  - Launchbox database search.
  - SteamGridDB API key support.
- [ ] **Icon Change Module**: Customizing application icons and square tiles.
- [ ] **Bulk Management**: Batch download and batch upload logos.
- [ ] **Automatic Backup Manager**: One-click local backup and restore for `app.db`.

---

## ⚖️ License & Disclaimer

This project is open-source software provided under the [MIT License](LICENSE).

This tool is not affiliated with, endorsed by, or associated with Sony Interactive Entertainment Inc. PlayStation and PS5 are registered trademarks of Sony Interactive Entertainment Inc. All trademarks belong to their respective owners. Use of this utility on modified console software is undertaken at your own discretion and risk.
