# Shipping on Steam (free, Mac + Windows)

Everything here needs your accounts, payment or identity details, so it's yours to do. Rough order:

## 1. Steamworks account and app

1. Sign up at <https://partner.steamgames.com> (Steamworks). You'll need tax and bank details even for a free game.
2. Pay the **Steam Direct fee** (US$100 per app). For a free game it isn't recouped.
3. Create the app. Set the type to **Game**, pricing to **Free To Play** (or just "Free") in the store's pricing section.
4. Note the **App ID** and the two **Depot IDs** Steamworks creates. Make one depot for Windows and one for macOS
   (Steamworks → SteamPipe → Depots), and set each depot's OS in its configuration.

## 2. Builds

```
tools/build_windows.sh            # -> Builds/windows/
tools/build_mac.sh                # -> Builds/mac/  (Apple Silicon)
tools/build_mac.sh -universal     # Intel + Apple Silicon, see below
```

**Universal Mac build:** the Unity 6000.6.6f1 install on this Mac has Burst's `llvm-lipo` without its execute
bit, so the universal build fails with "Access denied". Fix once with
`chmod +x /Applications/Unity/Hub/Editor/6000.6.6f1/Unity.app/Contents/Resources/Burst/Client/bcl/hostmac/llvm-lipo`
and then pass `-universal`. Until then the Mac build is Apple Silicon only.

**Mac signing:** Steam doesn't strictly require notarization, but without it some players will see Gatekeeper
warnings. Notarizing needs an Apple Developer account (US$99/yr), a Developer ID certificate, `codesign` with
hardened runtime, and `xcrun notarytool submit`. Do this before uploading the Mac depot.

Delete `Builds/windows/Edmund Fitzgerald_BackUpThisFolder_ButDontShipItWithYourGame` before uploading; it's debug
symbols and shouldn't ship.

## 3. Upload with SteamPipe

Templates are in `tools/steam/`. Replace `APPID`, `WIN_DEPOT` and `MAC_DEPOT`, then:

```
steamcmd +login <your_steam_account> +run_app_build "$(pwd)/tools/steam/app_build.vdf" +quit
```

Then in Steamworks → SteamPipe → Builds, set the new build live on the `default` branch.

Launch options (Steamworks → Installation → General): Windows `Edmund Fitzgerald.exe`; macOS
`Edmund Fitzgerald.app`.

## 4. Store page

Steam requires these before review:

- Capsule art: header 920×430, small 462×174, main 1232×706, vertical 748×896, library capsule 600×900,
  library hero 3840×1240, library logo 1280×720 (transparent PNG).
- At least 5 screenshots (1920×1080). Set `-screen-width 1920 -screen-height 1080` on `tools/shot.sh` runs,
  and use `-fitzStart HH:MM` and `-fitzLook yaw,pitch` to frame them.
- A short description, a long description, and system requirements.
- **Content survey:** mark that it depicts a real tragedy with loss of life. No gore, no violence.
- A trailer is optional but helps.

Then submit the store page and the build for review (a few business days each). Steam also asks for a
"Coming Soon" page to be live for at least two weeks before release.

## 5. Optional

- **Steamworks SDK** (achievements, overlay hooks): not needed for a free game without achievements.
  If wanted later, add [Steamworks.NET](https://steamworks.github.io/) and a `steam_appid.txt`.
- **Sensitivity:** consider sending the build to the Great Lakes Shipwreck Historical Society before launch.
  The game deliberately names no crew member. The radio exchanges with the Anderson and the Avafors paraphrase
  the recorded traffic; everything else spoken aboard is invented, and written to stay plain and respectful.
