# Props to model

Everything in the pilothouse is built from primitives. Each item below has a drop-in slot: save the model as
`Assets/Resources/Props/<name>.fbx` (or `.obj`), and the next time the game starts the primitive version is hidden and
your model sits in its place. Nothing else to wire up. If a file is missing the primitive stays.

## Rules for every model

- **Units:** metres, real size. Unity reads FBX scale; set the exporter to metres or check the import scale is 1.
- **Up:** Y up. **Front:** model the side the player looks at as the front in Blender's own terms (−Y, the "Front"
  view). Export FBX with Blender's default axes. The game turns the model so that side faces the player; if a model
  from another tool comes in backwards, say so and I'll add a per-model turn.
- **Pivot:** as listed per item (usually the base, centred). The game positions the pivot, so a wrong pivot puts the
  model in the floor or the ceiling.
- **Budget:** under 5,000 triangles each, 10,000 for the wheel stand or telegraph is fine. One material, one 1024 or
  2048 PNG texture (colour; a roughness map is a bonus). Keep the materials standard so Unity's URP Lit picks them up.
- **Finish:** 1958 ship, 1975 condition. Brass dulled, paint chipped at the edges, grey hammertone cabinets, nothing
  shiny-new. Mint-green enamel is `#8FB89E` if you want paint to match the walls.
- Prompts below are written for a text-to-3D tool (Meshy, Tripo, Rodin, Luma) or as a brief for Asset Forge. The
  sizes matter more than the words.
- **Current state:** every slot already has a Blender-built placeholder from `tools/blender/build_props.py`
  (run it with `/Applications/Blender.app/Contents/MacOS/Blender -b -P tools/blender/build_props.py`). A better model
  dropped in with the same name replaces it.

## The list, most valuable first

### 1. `oilskins`  (biggest win)
*Prompt:* "Yellow PVC oilskin raincoat hanging from a wall hook by its collar loop, with a yellow sou'wester hat hung on
the same hook above it. 1970s fisherman's foul-weather gear, slightly stiff, folds and creases, a little dirty at the
hem. Hanging limp, not worn. Game-ready, low poly, single texture."
*Size:* about 1.3 m from hook to hem, 0.5 m wide. *Pivot:* at the hook, the coat hangs down from it (model extends
in −Y). *Faces:* −Z.

### 2. `wheel`
*Prompt:* "Ship's steering wheel, 0.6 m diameter, six spokes with turned wooden handles, varnished oak rim, brass hub.
Great Lakes freighter, 1950s. Low poly, game asset."
*Size:* 0.6 m across. *Pivot:* at the hub centre, wheel lying in the XY plane facing −Z (the player). The game spins it
about Z. No stand: the green column is separate.

### 3. `telegraph`
*Prompt:* "Brass engine order telegraph on a pedestal, 1950s Great Lakes freighter. Round brass column 1 m tall on a
flared foot, a horizontal brass drum head 0.45 m diameter with a glass dial face reading FULL HALF SLOW STOP SLOW HALF
FULL and a single handle lever on the near face, a small brass bell on top. Worn brass, fingerprints."
*Size:* 1.4 m tall. *Pivot:* centre of the foot on the floor. Drum centre at y = 1.18 m; the dial face flat on the −Z
side at z = −0.115. (The game draws its own answer pointer and lever on that face; if yours has a lever, leave it off
or pointing straight up.)

### 4. `binnacle`
*Prompt:* "Ship's magnetic compass binnacle, brass, 1950s. Tapered round column 1 m tall on a flared base, a drum-shaped
head with a low domed hood and finial, a small rectangular viewing window on one side, two soft-iron correcting spheres
on short arms either side of the head. Dulled brass, dark iron balls."
*Size:* 1.45 m tall, 0.75 m across the spheres. *Pivot:* centre of the base on the floor. Viewing window on the −Z
side, its centre at y = 1.14 m. (The game draws the compass card inside the window.)

### 5. `radar`
*Prompt:* "1960s marine radar display console, grey hammertone steel cabinet 0.62 m wide, 1.0 m tall, 0.56 m deep, with
a round hooded PPI scope on top tilted back 35 degrees, a black rubber viewing hood, a row of four black knobs and a
small label plate on the front. Sperry or Raytheon style. Scratched, worn."
*Size:* as above, 1.35 m overall. *Pivot:* centre of the base. The scope face is a 0.42 m disc centred at y = 1.12 m,
tilted back 55° from vertical, facing −Z; the game draws the picture on its own disc there, so leave the scope face
plain black.

### 6. `phone`
*Prompt:* "Sound-powered ship's telephone in a grey-green steel wall box, 1950s, with a black bakelite handset in a
cradle on the front, a small brass crank handle on the side, and an engraved label plate. Chipped paint."
*Size:* 0.26 m wide, 0.36 m tall, 0.14 m deep. *Pivot:* centre of the back face (it mounts on the wall), front faces −Z.

### 7. `vhf`
*Prompt:* "1970s marine VHF radiotelephone, Raytheon Ray-Tel or Motorola style, grey-black steel case with a channel
selector knob, squelch and volume knobs, a small channel window, and a black telephone-style handset on a coiled
cord resting on a hook on the left side."
*Size:* 0.58 m wide, 0.28 m tall, 0.42 m deep. *Pivot:* centre of the base. Front faces −Z. Leave the channel window
plain; the game lights it.

### 8. `amradio`
*Prompt:* "1960s tabletop AM radio in a walnut veneer wooden case, a long horizontal dial glass across the front
with a tuning knob and a volume knob below it, cloth speaker grille, slightly scuffed."
*Size:* 0.56 m wide, 0.26 m tall, 0.38 m deep. *Pivot:* centre of the base. Front faces −Z, dial glass centred
0.03 m above the middle of the front face (the game draws the dial scale and pointer on it).

### 9. `binoculars`
*Prompt:* "7x50 marine binoculars, black leatherette and rubber, 1970s, lying flat, neck strap coiled."
*Size:* 0.18 × 0.07 × 0.14 m. *Pivot:* centre of the underside (they lie on a sill).

### 10. `mug`
*Prompt:* "Heavy white ceramic diner coffee mug, chipped rim, a dark ring stain, half full of black coffee."
*Size:* 0.09 m across, 0.11 m tall. *Pivot:* centre of the base.

### 11. `stool`
*Prompt:* "Steel swivel stool with a round brown leatherette seat, grey painted column and base, 1950s workshop or
ship style, worn."
*Size:* 0.4 m across, 0.85 m tall. *Pivot:* centre of the base.

### 12. `lifering`
*Prompt:* "Orange cork ship's life ring, 0.7 m across, with four white rope grab lines and faded black stencil
lettering EDMUND FITZGERALD, a little grubby."
*Size:* 0.7 m outer diameter. *Pivot:* centre of the ring, ring in the XY plane facing −Z (it hangs on the back wall).

## The picture-to-3D route (asset-forge)

The same pipeline as Bulwark's art (`~/Developer/asset-forge/WORKFLOW.md`), with a `marine` look added to
`forge.py` and this game's manifest at `art/asset-manifest.json`:

```
cd ~/Developer/asset-forge
~/image-to-3dlab/.venv/bin/python forge.py pictures ~/Developer/edmund-fitzgerald/art/asset-manifest.json --ids oilskins
#   pictures land in runs/fitz/pictures/<id>/takeN.png, about 6.5 minutes each
#   pick a winner: runs/fitz/picks.json  {"oilskins": {"winner": "take2.png"}}  (or forge.py review in a browser)
~/image-to-3dlab/.venv/bin/python forge.py build ~/Developer/edmund-fitzgerald/art/asset-manifest.json --ids oilskins
cd ~/Developer/edmund-fitzgerald && tools/forge_to_props.sh oilskins      # GLB -> Props/oilskins.fbx + colour map
```

Generated models come in at an arbitrary size with the pivot in the middle; the game measures each one and fits
it to its slot (`Props.Slots`: largest dimension and pivot), so no hand placement is needed. The log line
`[Fitz] Prop 'x' size ... scaled ...` says what it did.

## After dropping files in

Run `tools/build_mac.sh` (or open the project in Unity and press Play). The log says `[Fitz] Prop 'wheel' loaded`
for each one it found. If a model comes in sideways or the wrong size, fix it in the exporter rather than in Unity, so
the file stays the single source.

The slots are defined by `Props.Swap(...)` calls in `Assets/Scripts/Wheelhouse.cs` if you want to add more.
