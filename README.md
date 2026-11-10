# Edmund Fitzgerald

A short, free memorial game for Mac and Windows. You are the captain of the SS *Edmund Fitzgerald* on
Lake Superior on November 10, 1975, from 2:40 PM as the storm builds until about 7:15 PM. A session runs
about thirty minutes, and it always ends the same way.

**This is a personal learning project, not a commercial game.** I made it to learn how to build game art with
AI tools that run entirely on my own computer: turning a written description into a picture, the picture into a
textured 3D model, and the model into a working prop in Unity. The Fitzgerald gave the project a subject I care
about. It's shared for free, as is, for anyone who wants to play it or see how it was made.

## Download and play

Get the Mac or Windows zip from the [Releases](../../releases) page, unzip it and run it.

- **Windows:** run `Edmund Fitzgerald.exe`. If SmartScreen warns about an unknown publisher, choose
  *More info → Run anyway*.
- **Mac (Apple Silicon):** open `Edmund Fitzgerald.app`. It is signed and notarized by Apple.

Play with headphones. There are no recorded voices yet; every line is shown as a subtitle.

## How the art was made

Everything ran offline on one Mac, with no paid services and no cloud AI:

1. **Pictures:** Qwen-Image (Apache-2.0) makes a reference picture of each prop from a written description,
   for example "a brass gyrocompass repeater on a pedestal, 1958 ship, worn."
2. **3D models:** Pixal3D (MIT) turns the chosen picture into a textured 3D model.
3. **Into the game:** the scripts in `tools/` convert each model for Unity, square it up, clean small defects out of
   its texture, and bake picture details into the wall and floor textures. `Props.cs` fits each model into its slot
   in the pilothouse, and the game draws its working dials, screens and switches on top.

The AI-made parts are the wheel, binnacle, gyro repeater, port console, radars, telephone, stool, life ring,
oilskins, door, whistle handle, the wall and floor detail, and the family photo above the chart table (of people
who never existed). The ship, sea, sky, weather, instruments, chart, sound and story are written in code.
`docs/ASSETS.md` has the details, prompts and sizes. Some of these models have flaws a hand-built model wouldn't;
that's part of what I was learning.

## The song

*The Wreck of the Edmund Fitzgerald* by Gordon Lightfoot is not in the game, because it isn't licensed. The title
screen has a link to listen to it on YouTube instead. Inside the game, try the AM radio at 1360 kc.
`docs/EASTER_EGG.md` explains why.

## Playing

| Key | Does |
|---|---|
| Mouse | Look around the pilothouse |
| Click | Use what you're looking at (wheel, telegraph, phone, radio, chart, switches, whistle) |
| A / D, ← / → | Turn the wheel |
| W / S, ↑ / ↓ | Engine order telegraph, one notch ahead or astern |
| T | Orders to the crew (pumps, soundings, damage reports, the wheelsman, and so on) |
| R | Radio: VHF calls on channel 16, including Mayday, and the AM broadcast set |
| M | Chart, with your track pencilled in |
| L | Radio and phone log: everything said so far, on a clipboard by the radio |
| H | Ship's whistle (hold) |
| F1 | On-screen instrument readouts |
| F11 | Toggle fullscreen (window sizes are in Settings) |
| Esc | Pause and settings |

Things to find: the AM set has three stations (850, 1230 and 1360 kc). Calling Mayday changes the ending text
but not the ending.

## What's modelled

- **The ship:** 729-foot laker, pilothouse forward, 21 hatches, fence rail, stern house and stack, deck floodlights, searchlight.
- **The pilothouse,** laid out after the preserved pilothouse of the *William Clay Ford* (1953) at the Dossin Great Lakes
  Museum: a row of free-standing pedestals a pace back from the windows, mint enamel, the overhead instrument board.
  Fitted with what a 1958 laker carried by 1975: wheel, rudder angle indicator, two gyro repeaters, brass binnacle,
  engine order telegraph with engine-room answer pointer, two radar PPIs (head-up, with land echoes and the
  *Anderson* astern until they fail at 4:10 PM), clinometer, barometer, anemometer, depth sounder, ship's clock,
  VHF radiotelephone, AM receiver, sound-powered phone to the engine room, whistle pull, ballast pump and lighting panel, chart table.
- **The lake:** Gerstner-wave sea that the ship actually rides (the same maths runs on the GPU and CPU), building from
  about 4.5 m to 9 m significant wave height. It also has whitecaps and wind streaks, snow squalls, rain and spray on the
  glass, and sunset a little after 5 PM, then full dark.
- **Caribou Island** off the starboard bow around three o'clock; steer for it and the wheelsman takes the wheel until she's clear.
  Wander off the line to Whitefish and the *Anderson* calls to ask what you're doing.
- **The timeline:** the *Anderson* checking in, a possible touch on Six Fathom Shoal, the fence rail, vents and list,
  both radars lost, Whitefish Point light and beacon dark, the *Avafors*, the Coast Guard broadcast, the two big seas,
  and the last exchange: "We are holding our own."
- **Wear:** every surface is painted at start-up from noise: mottled enamel, rust from the window corners, the scuffed
  kick band, the track worn pale in the linoleum, smoke-yellowed deckhead, tarnished brass, hammertone steel (`Surfaces.cs`).
- **Audio:** all synthesised at run time: wind, sea, engine, whistle, telegraph bell, phone, bell, AM static and two original
  fingerpicked guitar stations.

Personal touches: drop a photo at `Assets/StreamingAssets/Props/family.jpg` and it's pinned up above the chart table
(image files there are gitignored).

## Drop-in models

Each prop has a slot: put `Assets/Resources/Props/<name>.fbx` in place and it replaces the primitive version.
`docs/ASSETS.md` lists the names, sizes, pivots and a prompt for each, and the picture-to-3D route described above.
The slots without an AI model (telegraph, radios, mug, binoculars) use models built in Blender from
`tools/blender/build_props.py`; rebuild them with
`/Applications/Blender.app/Contents/MacOS/Blender -b -P tools/blender/build_props.py`.

## Voices

Every spoken line is in `Assets/StreamingAssets/Dialogue/lines.tsv`. **`VOICE_SCRIPT.md`** is the recording script
(regenerate it with `python3 tools/export_script.py` after editing the TSV). Save each take as
`Assets/StreamingAssets/Voice/<line id>.wav` and the game plays it instead of the subtitle-only fallback.

## Building

Unity 6000.6.6f1, URP. The whole world is built in code from one boot object, so the only scene is `Assets/Scenes/Main.unity`.

```
tools/build_mac.sh        # Builds/mac/Edmund Fitzgerald.app (Apple Silicon; -universal for Intel too, see docs/STEAM.md)
tools/build_windows.sh    # Builds/windows/Edmund Fitzgerald.exe
```

Both run `Fitz.EditorTools.FitzBuild.Setup` first, which (re)creates the material templates and the boot scene.
Opening the project in the Unity editor works too: menu **Fitz → Setup Project**, then press Play.

### Test switches

```
tools/shot.sh out.png -fitzSkipTitle -fitzStart 18:40 -fitzLook 180,10 -fitzShotDelay 8
```

| Switch | Meaning |
|---|---|
| `-fitzSkipTitle` | Straight into play |
| `-fitzStart HH:MM` | Start at a later time (earlier events are marked done) |
| `-fitzTimeScale N` | Run the clock N times faster |
| `-fitzLook yaw,pitch` | Initial view direction |
| `-fitzCamPos x,y,z` | Put the camera somewhere else on the ship (ship space, metres) |
| `-fitzPanel radio\|orders\|chart\|log\|settings` | Open a panel at start |
| `-fitzLights` | Searchlight and deck lights on at start |
| `-fitzShot file.png` / `-fitzShotDelay s` / `-fitzQuit` | Screenshot and quit |
| `-fitzMemorial` | Jump to the ending screen |
| `-fitzNoGlass` | Hide window glass (debugging) |

## Layout

```
Assets/Scripts/           Game.cs (boot + loop), Ship.cs (helm, engine, flooding, motion), Ocean.cs,
                          Weather.cs, ShipModel.cs (exterior), Wheelhouse.cs (interior + instruments),
                          Dials.cs (faces + radar), Charts.cs (coastlines, chart), Story.cs (timeline, orders,
                          radio calls, ending), Dialogue.cs, Radio.cs, Audio.cs, UI.cs
Assets/Scripts/Editor/    FitzBuild.cs (setup and builds)
Assets/Resources/Shaders/ Ocean, Sky, Glass (rain on windows), Precip (snow, rain, spray)
Assets/StreamingAssets/   Dialogue/lines.tsv, Voice/, Radio/
docs/                     ASSETS.md (props and the AI art route), STEAM.md (shipping),
                          EASTER_EGG.md (the 1360 station and the Lightfoot song)
tools/                    build and screenshot scripts, AI-model import and clean-up scripts,
                          voice-script exporter, SteamPipe templates
```

No Asset Store content is used, so the repository is complete on its own.

## History and respect

The radio exchanges follow the public record of that afternoon, paraphrased. No crew member is named; the captain is
"you" and the others are known by their jobs. The ending screen states what happened and closes with the bell, rung
twenty-nine times and once more, as it is every November 10th.

## Licence

The code and everything else in this repository are under the MIT licence (`LICENSE`), except the Patrick Hand
font, which is under the SIL Open Font License. Gordon Lightfoot's song is not part of this project.
