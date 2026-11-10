using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Fitz
{
    /// <summary>
    /// The pilothouse interior, laid out after the preserved pilothouse of the
    /// William Clay Ford (1953), a near sister of the Fitzgerald: a row of
    /// free-standing pedestals a pace back from the front windows (radar, console,
    /// wheel stand with the gyro repeater on top, brass binnacle, radar, brass
    /// telegraph), the overhead instrument board above the windows, pale green
    /// enamel everywhere, and the chart table and radios against the back wall.
    /// The player stands behind the wheel and never moves, so everything is kept
    /// within about three metres.
    /// </summary>
    public class Wheelhouse : MonoBehaviour
    {
        public const float Floor = ShipModel.PilothouseFloorY;
        public const float Ceiling = Floor + 2.55f;
        public const float Front = 103.4f, Back = 98.5f, Side = 3.7f;
        public const float PedestalZ = 101.8f;
        public static readonly Vector3 EyePosition = new(0, Floor + 1.72f, 100.45f);
        /// <summary>Where the family snapshot hangs, for the last look.</summary>
        public static Vector3 PhotoPosition;

        const float Sill = Floor + 0.85f, Head = Floor + 2.15f;

        Ship _ship;
        Weather _weather;
        Game _game;

        Transform _wheel, _rudderNeedle, _gyroCard, _helmGyroCard, _binnacleCard, _clinoNeedle, _rpmNeedle, _baroNeedle, _windNeedle,
            _fathomNeedle, _clockHour, _clockMinute, _clockSecond, _telegraphLever, _telegraphAnswer, _whistleHandle, _radioDial;
        Material _pump1, _pump2, _deckLamp, _searchLamp, _radarLamp1, _radarLamp2, _vhfDisplay, _amDial, _nightLight;
        readonly List<Material> _panelGlows = new();
        Material _brass, _dialGlass, _telegraphGlow;
        Light _cabinLight, _redLight;
        RadarScope _radar1, _radar2;
        Texture2D _chartTex;
        Painter _chartBase;
        float _chartTimer;
        float _whistlePull;
        public Texture2D ChartTexture => _chartTex;

        public void Build(Game game, Ship ship, Weather weather, Transform shipRoot)
        {
            _game = game;
            _ship = ship;
            _weather = weather;
            var root = Geo.Node(shipRoot, "Wheelhouse", Vector3.zero);

            var mint = new Color(0.56f, 0.72f, 0.62f);                       // enamel, as on the William Clay Ford
            var cream = new Color(0.88f, 0.9f, 0.86f);
            var paint = Mats.Lit(mint, 0.5f);
            var pedestalPaint = Mats.Lit(Color.white, 0.5f).CloneWith(Surfaces.Paint(0.6f, 1.2f, mint, 31, null, 0.18f, 0.6f));
            var trim = Mats.Lit(cream, 0.45f);                              // cream window frames
            var white = Mats.Lit(new Color(0.85f, 0.85f, 0.82f), 0.35f);    // outside skin
            var steel = Mats.Lit(Color.white, 0.4f, 0.35f).CloneWith(Surfaces.Hammertone(new Color(0.42f, 0.45f, 0.44f), 7));
            var black = Mats.Lit(new Color(0.03f, 0.03f, 0.03f), 0.5f);
            var brass = Mats.Lit(Color.white, 0.6f, 0.9f).CloneWith(Surfaces.Tarnish(new Color(0.62f, 0.48f, 0.21f), 3));
            var wood = Mats.Lit(Color.white, 0.55f).CloneWith(Surfaces.Wood(new Color(0.34f, 0.18f, 0.08f), 5));
            var chrome = Mats.Lit(new Color(0.7f, 0.7f, 0.72f), 0.8f, 1f);
            _brass = brass;
            _dialGlass = DialGlass();
            var iron = Mats.Lit(new Color(0.12f, 0.12f, 0.12f), 0.35f, 0.6f);
            // drop-in models pick these up by material name
            Props.Library["brass"] = brass; Props.Library["wood"] = wood; Props.Library["paint"] = pedestalPaint;
            Props.Library["steel"] = steel; Props.Library["iron"] = iron; Props.Library["chrome"] = chrome; Props.Library["black"] = black;
            Props.Library["cream"] = Mats.Lit(new Color(0.85f, 0.83f, 0.76f), 0.5f); Props.Library["concrete"] = Mats.Lit(new Color(0.8f, 0.8f, 0.76f), 0.2f);
            Props.Library["leather"] = Mats.Lit(new Color(0.25f, 0.1f, 0.06f), 0.5f); Props.Library["coffee"] = Mats.Lit(new Color(0.1f, 0.05f, 0.02f), 0.9f);
            Props.Library["cloth"] = Mats.Lit(new Color(0.35f, 0.3f, 0.22f), 0.1f); Props.Library["orange"] = Mats.Lit(new Color(0.9f, 0.4f, 0.1f), 0.3f);
            Props.Library["oilskin"] = Mats.Lit(new Color(0.72f, 0.5f, 0.1f), 0.7f); Props.Library["default"] = Mats.Lit(new Color(0.5f, 0.5f, 0.5f), 0.4f);

            float cz = (Front + Back) / 2, w = Side * 2, d = Front - Back;
            // floor: linoleum worn pale where the watch stands and walks (texture metres run from the port aft corner)
            Vector2 M(float x, float z) => new(x + Side, z - Back);
            var eye2 = M(EyePosition.x, EyePosition.z);
            var floorTex = Surfaces.Floor(w, d, new Color(0.2f, 0.19f, 0.17f),
                new[] { eye2, M(-2.1f, 99.5f), eye2, M(2.3f, 99.5f), eye2, M(-3.3f, 99.2f), eye2, M(2.9f, 101.2f), M(-3.3f, 99.2f), M(-2.1f, 99.5f) },
                new[] { eye2, M(-2.1f, 99.6f), M(2.3f, 99.6f), M(0.9f, 101.1f) }, 11);
            Geo.Box(root, "FloorSlab", new Vector3(0, Floor - 0.035f, cz), new Vector3(w - 0.02f, 0.1f, d - 0.02f), black);
            Geo.Quad(root, "Floor", new Vector3(0, Floor + 0.016f, cz), new Vector2(w - 0.02f, d - 0.02f), Mats.Lit(Color.white, 0.45f).CloneWith(floorTex), new Vector3(90, 0, 0));
            // kick strip round the foot of the walls
            Geo.Box(root, "Kick", new Vector3(0, Floor + 0.04f, Front - 0.1f), new Vector3(w, 0.08f, 0.03f), black);
            Geo.Box(root, "Kick", new Vector3(0, Floor + 0.04f, Back + 0.1f), new Vector3(w, 0.08f, 0.03f), black);
            Geo.Box(root, "Kick", new Vector3(Side - 0.1f, Floor + 0.04f, cz), new Vector3(0.03f, 0.08f, d), black);
            Geo.Box(root, "Kick", new Vector3(-Side + 0.1f, Floor + 0.04f, cz), new Vector3(0.03f, 0.08f, d), black);
            // deckhead: cream, yellowed around the fixtures (texture metres run from the port forward corner)
            var fixturesAt = new[] { new Vector3(-1.9f, 0, 100.3f), new Vector3(0, 0, 99.4f), new Vector3(1.9f, 0, 100.3f) };
            var ceilTex = Surfaces.Ceiling(w, d, cream, Array.ConvertAll(fixturesAt, f => new Vector2(f.x + Side, Front - f.z)), 13);
            Geo.Box(root, "CeilingSlab", new Vector3(0, Ceiling + 0.06f, cz), new Vector3(w, 0.1f, d), trim);
            Geo.Quad(root, "Ceiling", new Vector3(0, Ceiling + 0.005f, cz), new Vector2(w, d), Mats.Lit(Color.white, 0.3f).CloneWith(ceilTex), new Vector3(-90, 0, 0));
            for (int i = 0; i < 3; i++)
                Geo.Box(root, "Beam", new Vector3(0, Ceiling - 0.05f, Back + 0.6f + i * 1.85f), new Vector3(w, 0.1f, 0.12f), trim);
            // three recessed ceiling fixtures
            var fixtureMat = Mats.Glow(new Color(0.9f, 0.9f, 0.85f), Color.black);
            foreach (var p in fixturesAt)
                Geo.Disc(Geo.Node(root, "Fixture", new Vector3(p.x, Ceiling - 0.005f, p.z), new Vector3(-90, 0, 0)), "Lens", Vector3.zero, 0.26f, fixtureMat);

            var glass = Mats.Custom("Glass");
            var glassLee = Mats.Custom("Glass");
            glassLee.SetFloat("_Facing", 0.3f);

            // ---- walls: a band of windows right round, the door on the port side aft ----
            WindowWall(root, new Vector3(0, 0, Front), 0, w, 7, mint, trim, white, glass, 101);
            WindowWall(root, new Vector3(Side, 0, cz), 90, d, 4, mint, trim, white, glassLee, 102);
            const float doorBay = 1.5f;                                   // solid section of the port wall, aft
            WindowWall(root, new Vector3(-Side, 0, Front - (d - doorBay) / 2), -90, d - doorBay, 3, mint, trim, white, glassLee, 103);
            PortDoorBay(root, doorBay, mint, white, brass, glassLee);
            BackWall(root, mint, trim, white, glassLee);

            // radiator under the front windows
            Geo.Box(root, "Radiator", new Vector3(0, Floor + 0.3f, Front - 0.14f), new Vector3(w - 0.4f, 0.5f, 0.12f), steel);
            for (int i = 0; i < 24; i++)
                Geo.Box(root, "Fin", new Vector3(-w / 2 + 0.45f + i * (w - 0.9f) / 23f, Floor + 0.3f, Front - 0.2f), new Vector3(0.03f, 0.46f, 0.06f), steel);

            // ================= the row of pedestals =================

            // ---- wheel stand: a tall green column, the wheel on its after face, gyro repeater on top ----
            var stand = Geo.Node(root, "WheelStand", new Vector3(0, Floor, PedestalZ));
            Geo.Box(stand, "Column", new Vector3(0, 0.6f, 0), new Vector3(0.5f, 1.2f, 0.42f), pedestalPaint);
            Geo.Box(stand, "Base", new Vector3(0, 0.03f, 0), new Vector3(0.6f, 0.06f, 0.52f), black);
            Geo.Box(stand, "Cap", new Vector3(0, 1.21f, 0), new Vector3(0.54f, 0.03f, 0.46f), black);
            Geo.Cyl(stand, "WheelShaft", new Vector3(0, 0.95f, -0.25f), 0.04f, 0.14f, chrome, new Vector3(90, 0, 0));
            _wheel = Geo.Node(stand, "Wheel", new Vector3(0, 0.95f, -0.32f));
            Geo.MeshObject(_wheel, "Rim", Geo.Torus(0.3f, 0.028f), wood);
            Geo.Cyl(_wheel, "Hub", Vector3.zero, 0.07f, 0.1f, brass, new Vector3(90, 0, 0));
            for (int i = 0; i < 6; i++)
            {
                var spoke = Geo.Node(_wheel, "Spoke", Vector3.zero, new Vector3(0, 0, i * 60f));
                Geo.Cyl(spoke, "Bar", new Vector3(0, 0.18f, 0), 0.014f, 0.34f, brass);
                Geo.Cyl(spoke, "Handle", new Vector3(0, 0.39f, 0), 0.022f, 0.16f, wood);
            }
            Props.Swap(_wheel, "wheel");
            Interactable.Add(Geo.Box(stand, "WheelHit", new Vector3(0, 0.95f, -0.32f), new Vector3(0.95f, 0.95f, 0.2f), null, collider: true).Hide(),
                "", b => _game.OnWheelClicked(b), () => _ship.Wheelsman ? "Wheel  [A/D to take the helm]" : $"Wheel  [A/D]  Rudder {Mathf.Abs(_ship.Rudder):0}° {(_ship.Rudder < -0.5f ? "port" : _ship.Rudder > 0.5f ? "starboard" : "")}  (click: hand to wheelsman)");
            // gyro repeater in a hooded bowl on top, tilted back at the helmsman
            var helmGyro = Geo.Node(stand, "HelmGyro", new Vector3(0, 1.32f, -0.02f), new Vector3(50, 0, 0));
            Geo.Cyl(helmGyro, "Bowl", new Vector3(0, 0, 0.05f), 0.17f, 0.1f, brass, new Vector3(90, 0, 0));
            _helmGyroCard = DialFace(helmGyro, "Card", 0.28f, Dials.GyroCard(), out _, Vector3.zero, 0f, cased: false);
            Geo.Box(helmGyro, "Lubber", new Vector3(0, 0.115f, -0.012f), new Vector3(0.006f, 0.04f, 0.004f), Mats.Glow(Color.red, Color.red * 2));
            Geo.Disc(helmGyro, "Glass", new Vector3(0, 0, -0.022f), 0.27f, _dialGlass);
            Geo.MeshObject(Geo.Node(helmGyro, "Bezel", new Vector3(0, 0, -0.02f)), "Ring", Geo.Torus(0.155f, 0.014f, 48, 8), chrome);
            Geo.Box(helmGyro, "Hood", new Vector3(0, 0.17f, 0.02f), new Vector3(0.36f, 0.04f, 0.18f), black, new Vector3(-50, 0, 0));
            Interactable.Add(Geo.Box(helmGyro, "Hit", Vector3.zero, new Vector3(0.34f, 0.34f, 0.1f), null, collider: true).Hide(), "", null,
                () => $"Gyro repeater  {_ship.Heading:000}°");
            // the generated repeater stands on its own pedestal just port of the wheel, turned to the helmsman;
            // the card, lubber line and glass move into its bezel and the hooded bowl on the stand goes
            var gyroStand = Geo.Node(root, "GyroStand", new Vector3(-0.62f, Floor, PedestalZ - 0.05f), new Vector3(0, -25, 0));
            if (Props.Swap(gyroStand, "gyro"))
            {
                helmGyro.SetParent(gyroStand, false);
                helmGyro.localPosition = new Vector3(0, 0.914f, -0.207f);
                helmGyro.localEulerAngles = Vector3.zero;
                helmGyro.localScale = Vector3.one * 1.154f;
                foreach (var part in new[] { "Bowl", "Bezel", "Hood" })
                    foreach (var r in helmGyro.Find(part).GetComponentsInChildren<Renderer>()) r.enabled = false;
            }

            // ---- port console: sloped face with the depth sounder and the ballast / lighting switches ----
            var con = Geo.Node(root, "PortConsole", new Vector3(-1.3f, Floor, PedestalZ), new Vector3(0, -22, 0));
            Geo.Box(con, "Pedestal", new Vector3(0, 0.47f, 0.02f), new Vector3(0.64f, 0.94f, 0.5f), pedestalPaint);
            Geo.Box(con, "Base", new Vector3(0, 0.03f, 0.02f), new Vector3(0.72f, 0.06f, 0.58f), black);
            var face = Geo.Node(con, "Face", new Vector3(0, 1.08f, -0.02f), new Vector3(35, 0, 0));
            Geo.Box(face, "Slab", new Vector3(0, 0, 0.04f), new Vector3(0.64f, 0.5f, 0.08f), paint);
            Geo.Box(face, "Plate", new Vector3(0, 0, -0.001f), new Vector3(0.6f, 0.44f, 0.01f), black);
            DialFace(face, "Fathometer", 0.3f, Dials.Fathometer(), out _fathomNeedle, new Vector3(-0.14f, 0.0f, -0.008f), 0.12f);
            Interactable.Add(Geo.Box(face, "FathHit", new Vector3(-0.14f, 0, 0), new Vector3(0.3f, 0.3f, 0.1f), null, collider: true).Hide(), "", null,
                () => $"Depth sounder  {Charts.DepthFathoms(_ship.NavNm):0} fathoms");
            var lbl = new Painter(256, new Color(0.08f, 0.08f, 0.08f));
            lbl.Text("PUMP 1", new Vector2(0.27f, 0.84f), 0.07f, Color.white);
            lbl.Text("PUMP 2", new Vector2(0.73f, 0.84f), 0.07f, Color.white);
            lbl.Text("DECK", new Vector2(0.27f, 0.42f), 0.07f, Color.white);
            lbl.Text("SEARCH", new Vector2(0.73f, 0.42f), 0.07f, Color.white);
            lbl.Text("BALLAST", new Vector2(0.5f, 0.12f), 0.06f, new Color(0.8f, 0.8f, 0.7f));
            Geo.Quad(face, "Labels", new Vector3(0.15f, 0, -0.007f), new Vector2(0.26f, 0.36f), Mats.Glow(Color.white, new Color(0.05f, 0.05f, 0.05f), lbl.ToTexture()));
            foreach (var tx in new[] { 0.09f, 0.21f })
                foreach (var ty in new[] { 0.03f, -0.12f })
                {
                    Geo.Cyl(face, "SwitchBoss", new Vector3(tx, ty, -0.006f), 0.012f, 0.008f, chrome, new Vector3(90, 0, 0));
                    Geo.Cyl(face, "Switch", new Vector3(tx, ty + 0.012f, -0.018f), 0.004f, 0.03f, chrome, new Vector3(-30, 0, 0));
                }
            _pump1 = Lamp(face, new Vector3(0.09f, 0.08f, -0.012f), new Color(0.1f, 1f, 0.2f));
            _pump2 = Lamp(face, new Vector3(0.21f, 0.08f, -0.012f), new Color(0.1f, 1f, 0.2f));
            _deckLamp = Lamp(face, new Vector3(0.09f, -0.07f, -0.012f), new Color(1f, 0.7f, 0.2f));
            _searchLamp = Lamp(face, new Vector3(0.21f, -0.07f, -0.012f), new Color(1f, 0.9f, 0.6f));
            Interactable.Add(Geo.Box(face, "PumpHit", new Vector3(0.15f, 0.09f, 0), new Vector3(0.26f, 0.17f, 0.1f), null, collider: true).Hide(), "",
                _ => _game.Story.IssueOrder("pumps"), () => _ship.PumpsOn ? "Ballast pumps: RUNNING" : "Ballast pumps: off  (click: order the pumps started)");
            Interactable.Add(Geo.Box(face, "DeckHit", new Vector3(0.09f, -0.07f, 0), new Vector3(0.12f, 0.14f, 0.1f), null, collider: true).Hide(), "",
                _ => { _ship.DeckLights = !_ship.DeckLights; _game.Audio.Click(); }, () => "Deck lights: " + (_ship.DeckLights ? "ON" : "off"));
            Interactable.Add(Geo.Box(face, "SearchHit", new Vector3(0.21f, -0.07f, 0), new Vector3(0.12f, 0.14f, 0.1f), null, collider: true).Hide(), "",
                _ => { _ship.Searchlight = !_ship.Searchlight; _game.Audio.Click(); }, () => "Searchlight: " + (_ship.Searchlight ? "ON" : "off"));
            // loudhailer / intercom box on top, and a clipboard leaning on it
            Geo.Box(con, "Intercom", new Vector3(0, 1.4f, 0.12f), new Vector3(0.3f, 0.2f, 0.22f), steel);
            Geo.Box(con, "Clipboard", new Vector3(0.22f, 1.26f, 0.14f), new Vector3(0.22f, 0.3f, 0.01f), wood, new Vector3(-20, 0, 0));
            Geo.Quad(con, "Sheet", new Vector3(0.22f, 1.26f, 0.133f), new Vector2(0.2f, 0.27f), Mats.Lit(new Color(0.9f, 0.88f, 0.8f), 0.2f), new Vector3(-20, 0, 0));
            // the generated stand: the working dial, switches and lamps move onto its sloped panel
            if (Props.Swap(con, "console", "Fathometer", "FathHit", "Lamp", "PumpHit", "DeckHit", "SearchHit"))
            {
                face.localPosition = new Vector3(0, 1.119f, 0.067f);
                face.localEulerAngles = new Vector3(46, 0, 0);
                face.localScale = Vector3.one * 0.75f;
                // the depth dial goes over the stand's own gauge, at its size
                foreach (var part in new[] { "Fathometer", "FathHit" })
                {
                    var t = face.Find(part);
                    t.localPosition = new Vector3(-0.293f, -0.113f, t.localPosition.z);
                    t.localScale *= 0.84f;
                }
                // the stand's green jewel shows the pumps, its red one the searchlight; its two toggles
                // are the deck lights and the searchlight (the hints say which)
                face.Find("PumpHit").localPosition = new Vector3(0f, 0.05f, 0);
                face.Find("DeckHit").localPosition = new Vector3(0.01f, -0.1f, 0);
                face.Find("SearchHit").localPosition = new Vector3(0.25f, -0.1f, 0);
                foreach (Transform part in face)
                    if (part.name == "Lamp") part.gameObject.SetActive(false);
                _pump1 = Lamp(face, new Vector3(-0.003f, 0.05f, -0.03f), new Color(0.1f, 1f, 0.2f));
                _pump2 = _pump1;
                _searchLamp = Lamp(face, new Vector3(0.22f, 0.08f, -0.03f), new Color(1f, 0.25f, 0.15f));
                _deckLamp = null;
            }

            // ---- brass binnacle, the magnetic compass, just starboard of the wheel ----
            // A proper binnacle: a tapered brass column, a drum-shaped head with a domed
            // hood, the compass seen through a window in the after side, and the two iron
            // correcting spheres on arms either side.
            var bin = Geo.Node(root, "Binnacle", new Vector3(1.05f, Floor, PedestalZ));
            Geo.Cyl(bin, "Foot", new Vector3(0, 0.03f, 0), 0.22f, 0.06f, brass);
            Geo.Cyl(bin, "Base", new Vector3(0, 0.1f, 0), 0.16f, 0.08f, brass);
            Geo.Cyl(bin, "Column", new Vector3(0, 0.55f, 0), 0.12f, 0.85f, brass);
            Geo.Cyl(bin, "Collar", new Vector3(0, 0.98f, 0), 0.15f, 0.05f, brass);
            Geo.Cyl(bin, "Head", new Vector3(0, 1.12f, 0), 0.19f, 0.26f, brass);
            Geo.MeshObject(Geo.Node(bin, "HeadRim", new Vector3(0, 1.25f, 0), new Vector3(90, 0, 0)), "Ring", Geo.Torus(0.19f, 0.012f, 48, 8), brass);
            Geo.Sphere(bin, "Dome", new Vector3(0, 1.25f, 0), 0.19f, brass).transform.localScale = new Vector3(0.38f, 0.22f, 0.38f);
            Geo.Cyl(bin, "Finial", new Vector3(0, 1.38f, 0), 0.02f, 0.06f, brass);
            Geo.Sphere(bin, "FinialKnob", new Vector3(0, 1.42f, 0), 0.025f, brass);
            // the viewing window on the after side, the card lit from a small lamp inside
            var binFace = Geo.Node(bin, "Window", new Vector3(0, 1.14f, -0.19f), new Vector3(20, 0, 0));
            Geo.Box(binFace, "Frame", new Vector3(0, 0, 0.01f), new Vector3(0.22f, 0.16f, 0.02f), iron);
            Geo.Box(binFace, "Inside", new Vector3(0, 0, 0.06f), new Vector3(0.2f, 0.14f, 0.08f), black);
            _binnacleCard = DialFace(binFace, "Card", 0.17f, Dials.GyroCard(), out _, new Vector3(0, -0.01f, 0.02f), 0f, cased: false);
            Geo.Box(binFace, "Lubber", new Vector3(0, 0.06f, 0.01f), new Vector3(0.005f, 0.03f, 0.004f), Mats.Glow(Color.red, Color.red * 2));
            Geo.Quad(binFace, "Glass", new Vector3(0, 0, -0.001f), new Vector2(0.2f, 0.14f), Mats.Clear(new Color(0.8f, 0.9f, 1f, 0.1f), 0.45f));
            // quadrantal correctors: the two soft-iron spheres that cancel the hull's own magnetism
            foreach (var side in new[] { -1, 1 })
            {
                Geo.Cyl(bin, "Arm", new Vector3(side * 0.2f, 1.1f, 0), 0.02f, 0.2f, brass, new Vector3(0, 0, 90));
                Geo.Cyl(bin, "Cup", new Vector3(side * 0.31f, 1.1f, 0), 0.05f, 0.03f, brass, new Vector3(0, 0, 90));
                Geo.Sphere(bin, "Corrector", new Vector3(side * 0.36f, 1.1f, 0), 0.08f, Mats.Lit(new Color(0.14f, 0.17f, 0.14f), 0.3f, 0.5f));
            }
            // a generated binnacle has its window lower on the head than the built one: the card follows it
            if (Props.Swap(bin, "binnacle", "Card", "Lubber", "Glass", "Hit"))
                binFace.localPosition = new Vector3(0, 0.99f, -0.315f);
            Interactable.Add(Geo.Box(bin, "Hit", new Vector3(0, 1.15f, 0), new Vector3(0.4f, 0.45f, 0.4f), null, collider: true).Hide(), "", null,
                () => $"Magnetic compass  {Mathf.Repeat(_ship.Heading + 4f, 360f):000}°  (4° W variation; the iron balls correct for the hull)");

            // ---- radars: one each side, hooded scopes on grey cabinets ----
            // two sets, as she carried: the 10 cm Sperry for the long picture, a 3 cm set with an amber tube for close work
            _radar1 = BuildRadar(root, new Vector3(1.95f, Floor, PedestalZ), 30, "Radar 1", steel, black, new Color(0.3f, 1.4f, 0.4f), "RADAR 1   10 CM", out _radarLamp1);
            _radar2 = BuildRadar(root, new Vector3(-2.5f, Floor, PedestalZ), -35, "Radar 2", Mats.Lit(Color.white, 0.4f, 0.35f).CloneWith(Surfaces.Hammertone(new Color(0.3f, 0.32f, 0.3f), 9)), black, new Color(1.5f, 0.7f, 0.15f), "RADAR 2   3 CM", out _radarLamp2);
            _radar2.RangeNm = 6;

            // ---- engine order telegraph, brass, far starboard ----
            // turned in toward the helm, so its face reads from behind the wheel
            var tel = Geo.Node(root, "Telegraph", new Vector3(2.9f, Floor, PedestalZ), new Vector3(0, 50, 0));
            Geo.Cyl(tel, "Column", new Vector3(0, 0.5f, 0), 0.1f, 1.0f, brass);
            Geo.Cyl(tel, "Foot", new Vector3(0, 0.03f, 0), 0.19f, 0.06f, brass);
            Geo.Cyl(tel, "Drum", new Vector3(0, 1.18f, 0), 0.24f, 0.22f, brass, new Vector3(90, 0, 0));
            var telFace = DialFace(tel, "Face", 0.42f, Dials.TelegraphFace(), out _telegraphAnswer, new Vector3(0, 1.18f, -0.115f), 0.15f, cased: false);
            // the face is lit from inside the drum, as real ones were, so it reads by day and night
            _telegraphGlow = telFace.GetComponent<Renderer>().sharedMaterial;
            _panelGlows.Remove(_telegraphGlow);
            Geo.MeshObject(Geo.Node(tel, "FaceRim", new Vector3(0, 1.18f, -0.115f)), "Ring", Geo.Torus(0.225f, 0.018f, 48, 8), brass);
            Geo.Disc(tel, "FaceGlass", new Vector3(0, 1.18f, -0.128f), 0.42f, _dialGlass);
            Geo.Cyl(tel, "Bell", new Vector3(0, 1.36f, 0.02f), 0.06f, 0.06f, brass);
            _telegraphLever = Geo.Node(tel, "LeverPivot", new Vector3(0, 1.18f, -0.15f));
            Geo.Box(_telegraphLever, "Lever", new Vector3(0, 0.14f, 0), new Vector3(0.03f, 0.3f, 0.02f), brass);
            Geo.Cyl(_telegraphLever, "Knob", new Vector3(0, 0.3f, -0.02f), 0.035f, 0.08f, wood, new Vector3(90, 0, 0));
            Interactable.Add(Geo.Box(tel, "Hit", new Vector3(0, 1.1f, 0), new Vector3(0.55f, 0.7f, 0.4f), null, collider: true).Hide(), "",
                b => _game.OnTelegraph(b == 0 ? +1 : -1),
                () => $"Engine order telegraph: {Ship.OrderName(_ship.Order)}   [W/S or click ahead / right-click astern]");
            // the model has its own dark dial disc a hair proud of ours: bring the lit face in front of it
            if (Props.Swap(tel, "telegraph", "Face", "LeverPivot", "Hit"))
                telFace.localPosition = new Vector3(0, 1.18f, -0.127f);

            // ---- overhead board above the front windows ----
            var board = Geo.Node(root, "Overhead", new Vector3(0, Ceiling - 0.22f, Front - 0.3f), new Vector3(-30, 0, 0));
            Geo.Box(board, "Board", new Vector3(0, 0, 0.03f), new Vector3(w - 0.3f, 0.42f, 0.05f), Mats.Lit(new Color(0.1f, 0.12f, 0.11f), 0.4f));
            DialFace(board, "RudderAngle", 0.38f, Dials.RudderAngle(), out _rudderNeedle, new Vector3(0, 0, 0), 0.16f);
            DialFace(board, "Gyro", 0.34f, Dials.GyroCard(), out _, new Vector3(-0.9f, 0, 0));
            _gyroCard = board.Find("Gyro");
            Geo.Box(board, "GyroLubber", new Vector3(-0.9f, 0.15f, -0.012f), new Vector3(0.006f, 0.04f, 0.004f), Mats.Glow(Color.red, Color.red * 2));
            DialFace(board, "Clinometer", 0.34f, Dials.Clinometer(), out _clinoNeedle, new Vector3(0.9f, 0, 0), 0.15f, pivotOffset: new Vector2(0, 0.04f));
            var clock = DialFace(board, "Clock", 0.32f, Dials.Clock(), out _clockMinute, new Vector3(-1.75f, 0, 0), 0.125f);
            _clockHour = Needle(clock, 0.085f, 0.012f, Color.black);
            _clockSecond = Needle(clock, 0.135f, 0.004f, new Color(0.6f, 0.05f, 0.05f));
            DialFace(board, "Rpm", 0.32f, Dials.Rpm(), out _rpmNeedle, new Vector3(1.75f, 0, 0), 0.125f);
            // a loudspeaker and the two deck-lamp housings at the ends, as in the photo
            Geo.Box(board, "Speaker", new Vector3(2.6f, 0, 0), new Vector3(0.3f, 0.3f, 0.12f), black);
            Geo.Box(board, "Lamp", new Vector3(-2.6f, 0, 0), new Vector3(0.32f, 0.22f, 0.14f), black);
            Interactable.Add(Geo.Box(board, "Hit", Vector3.zero, new Vector3(0.4f, 0.4f, 0.1f), null, collider: true).Hide(), "", null,
                () => $"Rudder angle  {Mathf.Abs(_ship.Rudder):0}° {(_ship.Rudder < -0.5f ? "port" : _ship.Rudder > 0.5f ? "starboard" : "midships")}");
            Interactable.Add(Geo.Box(board, "Hit", new Vector3(-0.9f, 0, 0), new Vector3(0.36f, 0.36f, 0.1f), null, collider: true).Hide(), "", null,
                () => $"Gyro compass  {_ship.Heading:000}°");
            Interactable.Add(Geo.Box(board, "Hit", new Vector3(0.9f, 0, 0), new Vector3(0.36f, 0.36f, 0.1f), null, collider: true).Hide(), "", null,
                () => $"Clinometer  {Mathf.Abs(_ship.Roll):0}° {(_ship.Roll < 0 ? "to starboard" : "to port")}   standing list {Mathf.Abs(_ship.List):0.0}°");
            Interactable.Add(Geo.Box(board, "Hit", new Vector3(-1.75f, 0, 0), new Vector3(0.34f, 0.34f, 0.1f), null, collider: true).Hide(), "", null,
                () => "Clock  " + GameClock.Format(_game.Clock.Minutes));
            Interactable.Add(Geo.Box(board, "Hit", new Vector3(1.75f, 0, 0), new Vector3(0.34f, 0.34f, 0.1f), null, collider: true).Hide(), "", null,
                () => $"Shaft RPM  {_ship.EngineRpm:0}");

            // ---- whistle pull from the deckhead, port of the wheel ----
            var whistle = Geo.Node(root, "Whistle", new Vector3(-0.6f, Ceiling, 101.2f));
            Geo.Cyl(whistle, "Cord", new Vector3(0, -0.3f, 0), 0.006f, 0.6f, Mats.Lit(new Color(0.6f, 0.55f, 0.45f), 0.1f));
            _whistleHandle = Geo.Node(whistle, "Handle", new Vector3(0, -0.62f, 0));
            Geo.Cyl(_whistleHandle, "Grip", Vector3.zero, 0.025f, 0.12f, wood);
            // a carved oak handle with a brass ferrule hangs from the cord's eye, if the forge made one
            Props.Swap(_whistleHandle, "whistle");
            Interactable.Add(Geo.Box(whistle, "Hit", new Vector3(0, -0.5f, 0), new Vector3(0.15f, 0.62f, 0.15f), null, collider: true).Hide(),
                "Whistle pull  (hold H)", _ => _game.Audio.Whistle(1.6f));

            // ================= the back wall =================

            // ---- radio set on a shelf, starboard ----
            var radio = Geo.Node(root, "Radio", new Vector3(2.3f, Floor + 1.0f, Back + 0.38f), new Vector3(0, 180, 0));
            Geo.Box(radio, "Shelf", new Vector3(0, -0.03f, 0), new Vector3(1.6f, 0.06f, 0.7f), wood);
            Geo.Box(radio, "Cabinet", new Vector3(0, -0.5f, 0.02f), new Vector3(1.55f, 0.9f, 0.65f), pedestalPaint);
            var vhf = Geo.Node(radio, "VHF", new Vector3(-0.4f, 0.17f, 0.05f));
            Geo.Box(vhf, "Body", new Vector3(0, 0, 0.1f), new Vector3(0.58f, 0.28f, 0.42f), Mats.Lit(new Color(0.14f, 0.14f, 0.13f), 0.5f, 0.2f));
            _vhfDisplay = Mats.Glow(Color.black, new Color(1.6f, 0.4f, 0.1f));
            Geo.Quad(vhf, "Display", new Vector3(-0.1f, 0.03f, -0.111f), new Vector2(0.26f, 0.1f), _vhfDisplay);
            var vhfLbl = new Painter(128, new Color(0.14f, 0.14f, 0.13f));
            vhfLbl.Text("VHF FM", new Vector2(0.5f, 0.5f), 0.2f, new Color(0.8f, 0.8f, 0.8f));
            Geo.Quad(vhf, "Label", new Vector3(0.18f, 0.06f, -0.111f), new Vector2(0.14f, 0.07f), Mats.Lit(Color.white).CloneWith(vhfLbl.ToTexture()));
            Geo.Cyl(vhf, "Knob1", new Vector3(0.12f, -0.07f, -0.115f), 0.025f, 0.03f, chrome, new Vector3(90, 0, 0));
            Geo.Cyl(vhf, "Knob2", new Vector3(0.22f, -0.07f, -0.115f), 0.025f, 0.03f, chrome, new Vector3(90, 0, 0));
            Geo.Box(radio, "Handset", new Vector3(-0.72f, 0.3f, -0.12f), new Vector3(0.07f, 0.2f, 0.05f), black);
            Geo.Cyl(radio, "Cord", new Vector3(-0.72f, 0.1f, -0.1f), 0.006f, 0.2f, black);
            var am = Geo.Node(radio, "AM", new Vector3(0.4f, 0.15f, 0.05f));
            Geo.Box(am, "Body", new Vector3(0, 0, 0.1f), new Vector3(0.56f, 0.26f, 0.38f), wood);
            _amDial = Mats.Glow(Color.white, new Color(0.9f, 0.7f, 0.35f), Dials.AmDial());
            Geo.Quad(am, "Dial", new Vector3(0, 0.03f, -0.091f), new Vector2(0.44f, 0.12f), _amDial);
            _radioDial = Geo.Node(am, "Pointer", new Vector3(0, 0.03f, -0.096f));
            Geo.Box(_radioDial, "Line", Vector3.zero, new Vector3(0.004f, 0.11f, 0.002f), Mats.Glow(Color.red, Color.red * 2));
            Interactable.Add(Geo.Box(radio, "Hit", new Vector3(0, 0.15f, 0.05f), new Vector3(1.6f, 0.5f, 0.6f), null, collider: true).Hide(),
                "Radio  (click or R)", _ => _game.UI.OpenRadio());
            Props.Swap(vhf, "vhf", "Display");
            Props.Swap(am, "amradio", "Dial", "Pointer");
            // the radio log on a clipboard hung on the front of the cabinet
            var clip = Geo.Node(root, "RadioLog", new Vector3(2.3f, Floor + 0.62f, Back + 0.715f), new Vector3(0, 180, 0));
            Geo.Box(clip, "Board", new Vector3(0, 0, -0.006f), new Vector3(0.24f, 0.33f, 0.01f), wood);
            Geo.Box(clip, "Clamp", new Vector3(0, 0.15f, -0.016f), new Vector3(0.1f, 0.03f, 0.012f), chrome);
            var page = new Painter(256, new Color(0.92f, 0.9f, 0.82f));
            page.Text("RADIO LOG", new Vector2(0.5f, 0.88f), 0.08f, new Color(0.2f, 0.2f, 0.25f));
            for (int i = 0; i < 9; i++) page.Line(new Vector2(0.1f, 0.78f - i * 0.08f), new Vector2(0.9f, 0.78f - i * 0.08f), 0.004f, new Color(0.6f, 0.65f, 0.75f));
            Geo.Quad(clip, "Page", new Vector3(0, -0.01f, -0.012f), new Vector2(0.21f, 0.29f), Mats.Lit(Color.white, 0.2f).CloneWith(page.ToTexture()));
            Interactable.Add(Geo.Box(clip, "Hit", Vector3.zero, new Vector3(0.26f, 0.35f, 0.06f), null, collider: true).Hide(),
                "Radio and phone log  (click or L)", _ => _game.UI.OpenLog());

            // ---- sound-powered phone to the engine room, above the radio ----
            var phone = Geo.Node(root, "Phone", new Vector3(3.15f, Floor + 1.75f, Back + 0.1f), new Vector3(0, 180, 0));
            Geo.Box(phone, "Box", new Vector3(0, 0, 0.08f), new Vector3(0.26f, 0.36f, 0.14f), Mats.Lit(new Color(0.15f, 0.2f, 0.17f), 0.5f));
            Geo.Box(phone, "Handset", new Vector3(0, 0.05f, -0.01f), new Vector3(0.07f, 0.23f, 0.07f), black);
            Geo.Cyl(phone, "Crank", new Vector3(0.15f, -0.05f, 0.06f), 0.04f, 0.05f, brass, new Vector3(0, 0, 90));
            var phoneLbl = new Painter(128, new Color(0.9f, 0.85f, 0.7f));
            phoneLbl.Text("ENGINE", new Vector2(0.5f, 0.6f), 0.13f, Color.black);
            phoneLbl.Text("ROOM", new Vector2(0.5f, 0.35f), 0.13f, Color.black);
            Geo.Quad(phone, "Label", new Vector3(0, -0.13f, 0.005f), new Vector2(0.14f, 0.07f), Mats.Lit(Color.white, 0.3f).CloneWith(phoneLbl.ToTexture()));
            Interactable.Add(Geo.Box(phone, "Hit", new Vector3(0, 0, 0.05f), new Vector3(0.32f, 0.42f, 0.22f), null, collider: true).Hide(),
                "Sound-powered phone  (click or T: give orders)", _ => _game.UI.OpenOrders());
            Props.Swap(phone, "phone", "Hit");

            // ---- barometer and anemometer on the pillar between the two aft windows ----
            var wallInst = Geo.Node(root, "AftInstruments", new Vector3(0, Floor + 1.55f, Back + 0.1f), new Vector3(0, 180, 0));
            DialFace(wallInst, "Barometer", 0.32f, Dials.Barometer(), out _baroNeedle, new Vector3(0, 0.22f, 0), 0.13f);
            DialFace(wallInst, "Anemometer", 0.32f, Dials.Anemometer(), out _windNeedle, new Vector3(0, -0.22f, 0), 0.13f);
            Interactable.Add(Geo.Box(wallInst, "Hit", new Vector3(0, 0.22f, 0), new Vector3(0.34f, 0.34f, 0.1f), null, collider: true).Hide(), "", null,
                () => $"Barometer  {Barometer():0.00} in.");
            Interactable.Add(Geo.Box(wallInst, "Hit", new Vector3(0, -0.22f, 0), new Vector3(0.34f, 0.34f, 0.1f), null, collider: true).Hide(), "", null,
                () => $"Wind  {_weather.WindKnots:0} knots from the northwest");

            // ---- chart table, port, with the captain's books ----
            var tableX = -2.15f; var tableZ = Back + 0.5f;
            ChartCabinet(root, new Vector3(tableX, Floor, tableZ), wood, brass);
            Geo.Box(root, "TableLip", new Vector3(tableX, Floor + 0.96f, tableZ - 0.47f), new Vector3(1.7f, 0.05f, 0.02f), wood);
            _chartBase = Charts.DrawChartBase(1024);
            _chartTex = _chartBase.ToTexture();
            Geo.Quad(root, "Chart", new Vector3(tableX + 0.1f, Floor + 0.945f, tableZ), new Vector2(1.25f, 0.9f), Mats.Lit(Color.white, 0.15f).CloneWith(_chartTex), new Vector3(90, 0, 0));
            Geo.Cyl(root, "Dividers", new Vector3(tableX + 0.35f, Floor + 0.955f, tableZ - 0.15f), 0.006f, 0.26f, chrome, new Vector3(0, 30, 90));
            Geo.Cyl(root, "Pencil", new Vector3(tableX - 0.1f, Floor + 0.953f, tableZ - 0.3f), 0.004f, 0.17f, Mats.Lit(new Color(0.85f, 0.7f, 0.2f), 0.4f), new Vector3(0, 70, 90));
            Interactable.Add(Geo.Box(root, "ChartHit", new Vector3(tableX, Floor + 0.95f, tableZ), new Vector3(1.6f, 0.2f, 0.95f), null, collider: true).Hide(),
                "Chart  (click or M)", _ => _game.UI.OpenChart());
            Books(root, new Vector3(tableX - 0.62f, Floor + 0.945f, tableZ + 0.12f));
            var mug = Geo.Node(root, "Mug", new Vector3(tableX + 0.55f, Floor + 0.945f, tableZ - 0.25f));
            Geo.Cyl(mug, "Cup", new Vector3(0, 0.055f, 0), 0.045f, 0.11f, Mats.Lit(new Color(0.8f, 0.78f, 0.7f), 0.6f));
            Geo.Cyl(mug, "Coffee", new Vector3(0, 0.11f, 0), 0.04f, 0.005f, Mats.Lit(new Color(0.1f, 0.05f, 0.02f), 0.9f));
            Props.Swap(mug, "mug");
            // chart lamp on a gooseneck, red shade
            Geo.Cyl(root, "Gooseneck", new Vector3(tableX + 0.75f, Floor + 1.2f, tableZ + 0.38f), 0.01f, 0.5f, black);
            Geo.Cyl(root, "ChartLampShade", new Vector3(tableX + 0.65f, Floor + 1.45f, tableZ + 0.28f), 0.06f, 0.08f, black, new Vector3(30, 0, 0));
            _redLight = new GameObject("ChartLamp").AddComponent<Light>();
            _redLight.transform.SetParent(root, false);
            _redLight.transform.localPosition = new Vector3(tableX + 0.45f, Floor + 1.4f, tableZ);
            _redLight.type = LightType.Point;
            _redLight.cullingMask = ~(1 << 4);   // not the lake: leave its light slots for the floods
            _redLight.range = 2.2f;
            _redLight.intensity = 0.8f;
            _redLight.color = new Color(1f, 0.25f, 0.15f);

            // ---- the captain's family, pinned up above the chart table ----
            FamilyPhoto(root, new Vector3(tableX - 0.45f, Floor + 1.72f, Back + 0.1f));

            // ---- stool, life ring, binoculars ----
            var stool = Geo.Node(root, "Stool", new Vector3(2.6f, Floor, 100.2f));
            Geo.Box(stool, "Column", new Vector3(0, 0.4f, 0), new Vector3(0.36f, 0.8f, 0.36f), Mats.Lit(new Color(0.2f, 0.2f, 0.2f), 0.5f, 0.6f));
            Geo.Cyl(stool, "Seat", new Vector3(0, 0.82f, 0), 0.2f, 0.05f, Mats.Lit(new Color(0.25f, 0.1f, 0.06f), 0.5f));
            Props.Swap(stool, "stool");
            var ring = Geo.Node(root, "LifeRing", new Vector3(2.45f, Floor + 2.0f, Back + 0.1f));
            Geo.MeshObject(ring, "Ring", Geo.Torus(0.28f, 0.055f), Mats.Lit(new Color(0.9f, 0.4f, 0.1f), 0.3f));
            Props.Swap(ring, "lifering");
            var bino = Geo.Node(root, "Binoculars", new Vector3(-0.1f, Floor + 0.865f, Front - 0.2f), new Vector3(0, 20, 0));
            Geo.Box(bino, "Body", new Vector3(0, 0.035f, 0), new Vector3(0.18f, 0.07f, 0.14f), black);
            Props.Swap(bino, "binoculars");

            // ---- lights ----
            _cabinLight = new GameObject("CabinLight").AddComponent<Light>();
            _cabinLight.transform.SetParent(root, false);
            _cabinLight.transform.localPosition = new Vector3(0, Ceiling - 0.25f, 100.2f);
            _cabinLight.type = LightType.Point;
            _cabinLight.cullingMask = ~(1 << 4);   // not the lake: leave its light slots for the floods
            _cabinLight.range = 6f;
            _cabinLight.color = new Color(1f, 0.85f, 0.65f);
            _cabinLight.shadows = LightShadows.None;
            _nightLight = fixtureMat;
        }

        // ---------------------------------------------------------------- building helpers

        /// <summary>Inner face of a painted panel: a box for solidity and, just proud of it, a quad carrying the weathered paint.</summary>
        static void Panel(Transform wall, string name, float x0, float x1, float y0, float y1, float t, Color paint, int seed, float[] drips = null, float scuff = 0.12f, float grime = 1f)
        {
            Geo.Box(wall, name, new Vector3((x0 + x1) / 2, (y0 + y1) / 2, -t / 2), new Vector3(x1 - x0, y1 - y0, t), Mats.Lit(paint, 0.5f));
            var tex = Surfaces.Paint(x1 - x0, y1 - y0, paint, seed, drips, scuff, grime, new Vector2(x0, y0 - Floor));
            Geo.Quad(wall, name + "Paint", new Vector3((x0 + x1) / 2, (y0 + y1) / 2, -t - 0.002f), new Vector2(x1 - x0, y1 - y0), Mats.Lit(Color.white, 0.5f).CloneWith(tex));
        }

        void WindowWall(Transform root, Vector3 center, float yaw, float width, int count, Color paint, Material frame, Material outer, Material glass, int seed)
        {
            var wall = Geo.Node(root, "Wall", center, new Vector3(0, yaw, 0));
            float t = 0.08f;
            float mull = 0.11f;
            float paneW = (width - mull * (count + 1)) / count;
            // water gets in at the bottom corners of the panes and runs down the paint
            var corners = new float[count * 2];
            for (int i = 0; i < count; i++)
            {
                float x = -width / 2 + mull + i * (paneW + mull);
                corners[i * 2] = x + width / 2;
                corners[i * 2 + 1] = x + paneW + width / 2;
            }
            Panel(wall, "Lower", -width / 2, width / 2, Floor, Sill, t, paint, seed, corners);
            Geo.Box(wall, "LowerOut", new Vector3(0, (Floor + Sill) / 2, t / 2), new Vector3(width + 0.16f, Sill - Floor, t), outer);
            Panel(wall, "Header", -width / 2, width / 2, Head, Ceiling + 0.1f, t, paint, seed + 1, null, 0f, 0.4f);
            Geo.Box(wall, "HeaderOut", new Vector3(0, (Head + Ceiling + 0.25f) / 2, t / 2), new Vector3(width + 0.16f, Ceiling + 0.25f - Head, t), outer);
            Geo.Box(wall, "Sill", new Vector3(0, Sill, -0.1f), new Vector3(width, 0.04f, 0.2f), frame);
            Geo.Box(wall, "HeadRail", new Vector3(0, Head, -0.07f), new Vector3(width, 0.04f, 0.14f), frame);
            for (int i = 0; i <= count; i++)
            {
                float x = -width / 2 + mull / 2 + i * (paneW + mull);
                Geo.Box(wall, "Mullion", new Vector3(x, (Sill + Head) / 2, -t / 2), new Vector3(mull, Head - Sill, t), frame);
                Geo.Box(wall, "MullionOut", new Vector3(x, (Sill + Head) / 2, t / 2), new Vector3(mull, Head - Sill, t), outer);
            }
            for (int i = 0; i < count; i++)
            {
                float x = -width / 2 + mull + paneW / 2 + i * (paneW + mull);
                Geo.Pane(wall, "Glass", new Vector3(x, (Sill + Head) / 2, 0), new Vector2(paneW, Head - Sill), glass, Vector3.zero);
                PaneTrim(wall, x, paneW, frame);
            }
        }

        Material _gasket;

        /// <summary>
        /// What makes a ship's window read as one: a black rubber gasket round the glass, a
        /// bevelled stop bead inside it, and two brass dogs at the foot that clamp the sash.
        /// </summary>
        void PaneTrim(Transform wall, float x, float paneW, Material frame)
        {
            _gasket ??= Mats.Lit(new Color(0.04f, 0.04f, 0.04f), 0.25f);
            float h = Head - Sill, cy = (Sill + Head) / 2, g = 0.014f, b = 0.03f;
            foreach (var (pos, size) in new[] {
                (new Vector3(x, Sill + 0.02f + g / 2, -0.012f), new Vector3(paneW, g, 0.012f)),
                (new Vector3(x, Head - 0.02f - g / 2, -0.012f), new Vector3(paneW, g, 0.012f)),
                (new Vector3(x - paneW / 2 + g / 2, cy, -0.012f), new Vector3(g, h - 0.04f, 0.012f)),
                (new Vector3(x + paneW / 2 - g / 2, cy, -0.012f), new Vector3(g, h - 0.04f, 0.012f)) })
                Geo.Box(wall, "Gasket", pos, size, _gasket);
            // stop bead, a hair proud of the mullions, chamfered by a second thinner strip
            foreach (var sx in new[] { -1, 1 })
            {
                Geo.Box(wall, "Bead", new Vector3(x + sx * (paneW / 2 - b / 2 + 0.004f), cy, -0.05f), new Vector3(b, h, 0.025f), frame);
                Geo.Box(wall, "Bead", new Vector3(x + sx * (paneW / 2 - b - 0.004f), cy, -0.042f), new Vector3(0.012f, h - 0.02f, 0.012f), frame, new Vector3(0, sx * 35f, 0));
            }
            Geo.Box(wall, "Bead", new Vector3(x, Head - 0.035f, -0.05f), new Vector3(paneW, 0.03f, 0.025f), frame);
            foreach (var dx in new[] { -0.3f, 0.3f })
            {
                var dog = Geo.Node(wall, "Dog", new Vector3(x + dx * paneW, Sill + 0.05f, -0.07f));
                Geo.Cyl(dog, "Boss", Vector3.zero, 0.016f, 0.03f, _brass, new Vector3(90, 0, 0));
                Geo.Box(dog, "Lever", new Vector3(0.025f, 0, -0.018f), new Vector3(0.06f, 0.014f, 0.01f), _brass, new Vector3(0, 0, 12));
            }
        }

        /// <summary>Solid aft end of the port wall: the door out to the bridge wing, with its porthole, and the oilskins on a hook beside it.</summary>
        void PortDoorBay(Transform root, float bay, Color paint, Material outer, Material brass, Material glass)
        {
            var wall = Geo.Node(root, "DoorBay", new Vector3(-Side, 0, Back + bay / 2), new Vector3(0, -90, 0));
            float t = 0.08f, h = Ceiling - Floor;
            // the wettest wall in the house: everyone comes in through this door dripping
            Panel(wall, "Panel", -bay / 2, bay / 2, Floor, Ceiling + 0.1f, t, paint, 104, new[] { 0.3f, 0.55f, 1.1f, 1.3f }, 0.2f, 1.4f);
            Geo.Box(wall, "PanelOut", new Vector3(0, Floor + h / 2 + 0.12f, t / 2), new Vector3(bay + 0.16f, h + 0.25f, t), outer);
            // the door, hinged forward, dogged shut
            var doorMat = Mats.Lit(Color.white, 0.5f).CloneWith(Surfaces.Paint(0.76f, 1.9f, new Color(0.5f, 0.66f, 0.56f), 105, new[] { 0.1f, 0.66f }, 0.25f, 1.6f));
            Geo.Box(wall, "Frame", new Vector3(-0.25f, Floor + 1.0f, -t - 0.02f), new Vector3(0.86f, 2.0f, 0.04f), Mats.Lit(new Color(0.85f, 0.87f, 0.83f), 0.4f));
            Geo.Box(wall, "Door", new Vector3(-0.25f, Floor + 1.0f, -t - 0.05f), new Vector3(0.76f, 1.9f, 0.05f), doorMat);
            var port = Geo.Node(wall, "Porthole", new Vector3(-0.25f, Floor + 1.5f, -t - 0.08f));
            Geo.MeshObject(port, "Rim", Geo.Torus(0.15f, 0.025f, 40, 8), brass);
            Geo.Pane(port, "Glass", Vector3.zero, new Vector2(0.28f, 0.28f), glass, Vector3.zero);
            foreach (var y in new[] { 0.6f, -0.6f })
                Geo.Box(wall, "Dog", new Vector3(0.12f, Floor + 1.0f + y, -t - 0.1f), new Vector3(0.05f, 0.14f, 0.04f), brass, new Vector3(0, 0, 20));
            Geo.Cyl(wall, "Handle", new Vector3(0.05f, Floor + 1.0f, -t - 0.1f), 0.015f, 0.14f, brass, new Vector3(0, 0, 90));
            // the forge's door, frame and all, set into the bulkhead in place of the built one
            var doorProp = Geo.Node(wall, "DoorProp", new Vector3(-0.27f, Floor, -t - 0.085f));
            if (Props.Swap(doorProp, "door"))
                foreach (Transform part in wall)
                    if (part.name is "Frame" or "Door" or "Porthole" or "Dog" or "Handle")
                        foreach (var r in part.GetComponentsInChildren<Renderer>()) r.enabled = false;
            Interactable.Add(Geo.Box(wall, "Hit", new Vector3(-0.25f, Floor + 1.0f, -t - 0.05f), new Vector3(0.76f, 1.9f, 0.1f), null, collider: true).Hide(), "", null,
                () => "Door to the port bridge wing  (dogged shut)");
            // oilskins and sou'wester hanging on a hook forward of the door
            var coat = Geo.Node(wall, "Oilskins", new Vector3(0.5f, Floor + 1.92f, -t - 0.02f));
            var oilskin = Mats.Lit(new Color(0.72f, 0.5f, 0.1f), 0.7f);
            Geo.Cyl(coat, "Hook", new Vector3(0, 0.02f, -0.03f), 0.012f, 0.07f, brass, new Vector3(90, 0, 0));
            // the coat hangs by its collar loop and folds narrower towards the hem
            Geo.Cyl(coat, "Collar", new Vector3(0, -0.06f, -0.09f), 0.08f, 0.07f, oilskin).transform.localScale = new Vector3(0.22f, 0.035f, 0.13f);
            Geo.Cyl(coat, "Shoulders", new Vector3(0, -0.2f, -0.1f), 0.1f, 0.2f, oilskin).transform.localScale = new Vector3(0.44f, 0.1f, 0.19f);
            Geo.Cyl(coat, "Body", new Vector3(0.01f, -0.68f, -0.09f), 0.1f, 0.85f, oilskin, new Vector3(0, 0, 2)).transform.localScale = new Vector3(0.36f, 0.425f, 0.15f);
            Geo.Sphere(coat, "Hem", new Vector3(0.02f, -1.1f, -0.09f), 0.1f, oilskin).transform.localScale = new Vector3(0.34f, 0.08f, 0.14f);
            Geo.Cyl(coat, "SleeveL", new Vector3(-0.22f, -0.55f, -0.09f), 0.06f, 0.6f, oilskin, new Vector3(0, 0, 7)).transform.localScale = new Vector3(0.11f, 0.3f, 0.11f);
            Geo.Cyl(coat, "SleeveR", new Vector3(0.24f, -0.53f, -0.09f), 0.06f, 0.56f, oilskin, new Vector3(0, 0, -9)).transform.localScale = new Vector3(0.11f, 0.28f, 0.11f);
            Geo.Sphere(coat, "Hood", new Vector3(0, -0.08f, -0.14f), 0.1f, oilskin).transform.localScale = new Vector3(0.22f, 0.09f, 0.12f);
            // sou'wester on the same hook: a low dome with a wide brim, longer at the back
            Geo.Sphere(coat, "Hat", new Vector3(0, 0.07f, -0.12f), 0.1f, oilskin).transform.localScale = new Vector3(0.26f, 0.12f, 0.24f);
            Geo.Cyl(coat, "Brim", new Vector3(0, 0.03f, -0.13f), 0.17f, 0.012f, oilskin, new Vector3(18, 0, 0)).transform.localScale = new Vector3(0.38f, 0.006f, 0.34f);
            Props.Swap(coat, "oilskins", "Hit");
            Interactable.Add(Geo.Box(coat, "Hit", new Vector3(0, -0.5f, -0.1f), new Vector3(0.5f, 1.4f, 0.25f), null, collider: true).Hide(), "", null,
                () => "Your oilskins. Still damp from the last trip out to the wing.");
        }

        void BackWall(Transform root, Color paint, Material frame, Material outer, Material glass)
        {
            var wall = Geo.Node(root, "BackWall", new Vector3(0, 0, Back), new Vector3(0, 180, 0));
            float t = 0.08f;
            float w = Side * 2;
            int seed = 110;
            void Solid(float x0, float x1, float y0, float y1, float[] drips = null, float scuff = 0.12f)
            {
                Panel(wall, "Panel", x0, x1, y0, y1, t, paint, seed++, drips, scuff);
                Geo.Box(wall, "PanelOut", new Vector3((x0 + x1) / 2, (y0 + y1) / 2, t / 2), new Vector3(x1 - x0, y1 - y0, t), outer);
            }
            // Two windows either side of the centreline look aft down the spar deck (the
            // wall is turned 180°, so local x is mirrored, which doesn't matter here).
            const float wx = 1.15f, ww = 1.4f;
            Solid(-w / 2, w / 2, Floor, Sill, new[] { w / 2 - wx - ww / 2, w / 2 - wx + ww / 2, w / 2 + wx - ww / 2, w / 2 + wx + ww / 2 });
            Solid(-w / 2, w / 2, Head, Ceiling + 0.1f, null, 0f);
            Solid(-w / 2, -wx - ww / 2, Sill, Head, null, 0f);
            Solid(-wx + ww / 2, wx - ww / 2, Sill, Head, null, 0f);
            Solid(wx + ww / 2, w / 2, Sill, Head, null, 0f);
            // builder's plate above the barometer
            var plate = new Painter(512, new Color(0.55f, 0.42f, 0.18f));
            var engraved = new Color(0.12f, 0.09f, 0.04f);
            plate.Rect(new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.05f), engraved);
            plate.Rect(new Vector2(0.03f, 0.95f), new Vector2(0.97f, 0.97f), engraved);
            plate.Rect(new Vector2(0.03f, 0.03f), new Vector2(0.04f, 0.97f), engraved);
            plate.Rect(new Vector2(0.96f, 0.03f), new Vector2(0.97f, 0.97f), engraved);
            plate.Text("EDMUND FITZGERALD", new Vector2(0.5f, 0.74f), 0.056f, engraved);
            plate.Text("GREAT LAKES ENGINEERING WORKS", new Vector2(0.5f, 0.52f), 0.034f, engraved);
            plate.Text("RIVER ROUGE   MICHIGAN", new Vector2(0.5f, 0.38f), 0.042f, engraved);
            plate.Text("HULL 301        1958", new Vector2(0.5f, 0.22f), 0.042f, engraved);
            var plateMat = Mats.Lit(Color.white, 0.6f, 0.8f).CloneWith(plate.ToTexture());
            Geo.Box(wall, "PlateBack", new Vector3(0, Ceiling - 0.2f, -t - 0.006f), new Vector3(0.56f, 0.24f, 0.012f), Mats.Lit(new Color(0.5f, 0.38f, 0.16f), 0.6f, 0.8f));
            Geo.Quad(wall, "Plate", new Vector3(0, Ceiling - 0.2f, -t - 0.013f), new Vector2(0.54f, 0.22f), plateMat);
            foreach (var sx in new[] { -0.25f, 0.25f })
                foreach (var sy in new[] { -0.09f, 0.09f })
                    Geo.Sphere(wall, "Screw", new Vector3(sx, Ceiling - 0.2f + sy, -t - 0.014f), 0.006f, Mats.Lit(new Color(0.6f, 0.6f, 0.62f), 0.7f, 1f));
            foreach (var x in new[] { -wx, wx })
            {
                Geo.Pane(wall, "Glass", new Vector3(x, (Sill + Head) / 2, 0), new Vector2(ww, Head - Sill), glass, Vector3.zero);
                Geo.Box(wall, "Frame", new Vector3(x, Sill, -0.07f), new Vector3(ww + 0.1f, 0.04f, 0.14f), frame);
                Geo.Box(wall, "Frame", new Vector3(x, Head, -0.06f), new Vector3(ww + 0.1f, 0.04f, 0.12f), frame);
                Geo.Box(wall, "Frame", new Vector3(x - ww / 2 - 0.03f, (Sill + Head) / 2, -0.05f), new Vector3(0.06f, Head - Sill, 0.1f), frame);
                Geo.Box(wall, "Frame", new Vector3(x + ww / 2 + 0.03f, (Sill + Head) / 2, -0.05f), new Vector3(0.06f, Head - Sill, 0.1f), frame);
            }
        }

        /// <summary>A few well-thumbed books stacked at the port end of the chart table, and one lying open.</summary>
        static void Books(Transform root, Vector3 at)
        {
            var stack = Geo.Node(root, "Books", at, new Vector3(0, -12, 0));
            var covers = new[] { new Color(0.12f, 0.16f, 0.3f), new Color(0.45f, 0.12f, 0.1f), new Color(0.2f, 0.3f, 0.2f), new Color(0.55f, 0.5f, 0.4f) };
            var sizes = new[] { new Vector3(0.3f, 0.07f, 0.24f), new Vector3(0.27f, 0.045f, 0.21f), new Vector3(0.24f, 0.035f, 0.2f), new Vector3(0.22f, 0.025f, 0.16f) };
            float y = 0;
            for (int i = 0; i < covers.Length; i++)
            {
                Geo.Box(stack, "Book", new Vector3(i * 0.015f, y + sizes[i].y / 2, i * 0.01f), sizes[i], Mats.Lit(covers[i], 0.25f), new Vector3(0, i * 7f - 8f, 0));
                Geo.Box(stack, "Pages", new Vector3(i * 0.015f + 0.004f, y + sizes[i].y / 2, i * 0.01f), sizes[i] - new Vector3(-0.002f, 0.006f, 0.01f), Mats.Lit(new Color(0.92f, 0.88f, 0.76f), 0.15f), new Vector3(0, i * 7f - 8f, 0));
                y += sizes[i].y;
            }
            // logbook open beside the stack, pencil in the gutter
            var open = Geo.Node(root, "Logbook", at + new Vector3(0.35f, 0, -0.3f), new Vector3(0, 6, 0));
            Geo.Box(open, "Cover", new Vector3(0, 0.01f, 0), new Vector3(0.5f, 0.02f, 0.3f), Mats.Lit(new Color(0.1f, 0.12f, 0.2f), 0.3f));
            Geo.Box(open, "PagesL", new Vector3(-0.12f, 0.028f, 0), new Vector3(0.23f, 0.018f, 0.28f), Mats.Lit(new Color(0.93f, 0.9f, 0.8f), 0.15f), new Vector3(0, 0, 2));
            Geo.Box(open, "PagesR", new Vector3(0.12f, 0.028f, 0), new Vector3(0.23f, 0.018f, 0.28f), Mats.Lit(new Color(0.93f, 0.9f, 0.8f), 0.15f), new Vector3(0, 0, -2));
            var lines = new Painter(256, new Color(0.93f, 0.9f, 0.8f));
            var ink = new Color(0.15f, 0.15f, 0.3f);
            lines.Text("NOV 10 1975", new Vector2(0.5f, 0.9f), 0.05f, ink);
            string[] entries = { "0700 DEP SUPERIOR", "1340 ROLLING SOME", "1352 ANDERSON", "WIND NW 35", "SEAS 10-12 FT", "C 130 FULL AHEAD" };
            for (int i = 0; i < entries.Length; i++) lines.Text(entries[i], new Vector2(0.5f, 0.78f - i * 0.11f), 0.04f, ink);
            Geo.Quad(open, "Writing", new Vector3(-0.12f, 0.038f, 0), new Vector2(0.2f, 0.25f), Mats.Lit(Color.white, 0.15f).CloneWith(lines.ToTexture()), new Vector3(90, 0, 0));
            Interactable.Add(Geo.Box(open, "Hit", new Vector3(0, 0.02f, 0), new Vector3(0.5f, 0.1f, 0.3f), null, collider: true).Hide(), "", null,
                () => "Deck log.  Last entry: " + "rolling some.");
        }

        /// <summary>
        /// The chart table: a varnished oak plan chest, three tiers of wide shallow drawers
        /// with brass cup pulls and card holders, a recessed plinth, a fielded panel on the
        /// open end, and a brass-edged top the chart lies on (top surface at 0.9425 m).
        /// </summary>
        void ChartCabinet(Transform root, Vector3 at, Material wood, Material brass)
        {
            var node = Geo.Node(root, "ChartTable", at);
            var dark = Mats.Lit(Color.white, 0.45f).CloneWith(Surfaces.Wood(new Color(0.2f, 0.1f, 0.045f), 6));
            const float w = 1.7f, d = 0.95f, front = d / 2;
            Geo.Box(node, "Carcass", new Vector3(0, 0.49f, -0.01f), new Vector3(w - 0.02f, 0.86f, d - 0.02f), dark);
            Geo.Box(node, "Plinth", new Vector3(0, 0.045f, -0.03f), new Vector3(w - 0.08f, 0.09f, d - 0.08f), Mats.Lit(new Color(0.06f, 0.04f, 0.03f), 0.3f));
            Geo.Box(node, "Top", new Vector3(0, 0.925f, 0.015f), new Vector3(w + 0.06f, 0.035f, d + 0.03f), wood);
            Geo.Box(node, "TopEdge", new Vector3(0, 0.925f, front + 0.031f), new Vector3(w + 0.06f, 0.035f, 0.004f), brass);
            Geo.Box(node, "Moulding", new Vector3(0, 0.895f, front - 0.005f), new Vector3(w, 0.025f, 0.03f), wood);
            Geo.Box(node, "BaseRail", new Vector3(0, 0.1f, front - 0.01f), new Vector3(w, 0.03f, 0.02f), wood);
            // three tiers, deepest at the bottom, each two drawers wide
            float[] heights = { 0.24f, 0.22f, 0.19f };
            float y = 0.135f;
            var card = Mats.Lit(new Color(0.88f, 0.84f, 0.72f), 0.2f);
            foreach (var h in heights)
            {
                foreach (var side in new[] { -1f, 1f })
                {
                    float cx = side * (w / 4 - 0.005f), dw = w / 2 - 0.05f;
                    var drawer = Geo.Node(node, "Drawer", new Vector3(cx, y + h / 2, front + 0.004f));
                    Geo.Box(drawer, "Front", Vector3.zero, new Vector3(dw, h - 0.02f, 0.02f), wood);
                    // a raised bead round the drawer front
                    Geo.Box(drawer, "BeadT", new Vector3(0, h / 2 - 0.02f, 0.011f), new Vector3(dw - 0.03f, 0.008f, 0.004f), dark);
                    Geo.Box(drawer, "BeadB", new Vector3(0, -h / 2 + 0.02f, 0.011f), new Vector3(dw - 0.03f, 0.008f, 0.004f), dark);
                    foreach (var px in new[] { -0.24f, 0.24f })
                    {
                        Geo.Box(drawer, "PullPlate", new Vector3(px, 0, 0.012f), new Vector3(0.1f, 0.04f, 0.004f), brass);
                        var cup = Geo.Cyl(drawer, "Pull", new Vector3(px, -0.004f, 0.022f), 0.02f, 0.08f, brass, new Vector3(0, 0, 90));
                        cup.transform.localScale = new Vector3(0.036f, 0.04f, 0.022f);
                    }
                    Geo.Box(drawer, "CardFrame", new Vector3(0, h / 2 - 0.05f, 0.012f), new Vector3(0.09f, 0.04f, 0.003f), brass);
                    Geo.Quad(drawer, "Card", new Vector3(0, h / 2 - 0.05f, 0.0141f), new Vector2(0.074f, 0.026f), card, new Vector3(0, 180, 0));
                }
                y += h;
            }
            // the open end, toward the middle of the room, gets a fielded panel
            float endX = w / 2 + 0.002f;
            Geo.Box(node, "EndPanel", new Vector3(endX, 0.5f, -0.01f), new Vector3(0.012f, 0.62f, d - 0.2f), wood);
            Geo.Box(node, "EndField", new Vector3(endX + 0.008f, 0.5f, -0.01f), new Vector3(0.008f, 0.5f, d - 0.32f), dark);
        }

        /// <summary>
        /// A snapshot pinned to the back wall. If StreamingAssets/Props/family.jpg (or .png)
        /// exists it's used; otherwise the forge's snapshot (Resources/Props/family, people who
        /// never existed), and failing that a sepia placeholder so the wall isn't bare.
        /// </summary>
        static void FamilyPhoto(Transform root, Vector3 at)
        {
            Texture2D tex = null;
            foreach (var ext in new[] { "jpg", "jpeg", "png" })
            {
                var path = Path.Combine(Application.streamingAssetsPath, "Props", "family." + ext);
                if (!File.Exists(path)) continue;
                tex = new Texture2D(2, 2, TextureFormat.RGBA32, true);
                if (ImageConversion.LoadImage(tex, File.ReadAllBytes(path))) { tex.wrapMode = TextureWrapMode.Clamp; tex.anisoLevel = 4; break; }
                tex = null;
            }
            float aspect = 1.45f;
            if (tex == null) tex = Resources.Load<Texture2D>("Props/family");
            if (tex == null) tex = PlaceholderPhoto(); else aspect = tex.width / (float)tex.height;
            float h = 0.15f, w = h * aspect;
            PhotoPosition = at;
            var node = Geo.Node(root, "FamilyPhoto", at, new Vector3(0, 180 + 2f, 0));
            Geo.Quad(node, "Border", new Vector3(0, 0, -0.004f), new Vector2(w + 0.012f, h + 0.012f), Mats.Lit(new Color(0.95f, 0.93f, 0.85f), 0.2f));
            Geo.Quad(node, "Print", new Vector3(0, 0, -0.005f), new Vector2(w, h), Mats.Lit(Color.white, 0.35f).CloneWith(tex));
            Geo.Sphere(node, "Tack", new Vector3(0, h / 2 + 0.002f, -0.007f), 0.006f, Mats.Lit(new Color(0.8f, 0.1f, 0.1f), 0.7f));
            Interactable.Add(Geo.Box(node, "Hit", Vector3.zero, new Vector3(w + 0.02f, h + 0.02f, 0.04f), null, collider: true).Hide(), "", null,
                () => "Home. Taken in the yard last fall. You'll be back for Thanksgiving.");
        }

        /// <summary>A sepia snapshot of three figures on a porch, drawn from shapes so no real face is used.</summary>
        static Texture2D PlaceholderPhoto()
        {
            var p = new Painter(256, new Color(0.78f, 0.7f, 0.56f));
            var light = new Color(0.86f, 0.8f, 0.66f);
            var mid = new Color(0.55f, 0.46f, 0.34f);
            var dark = new Color(0.32f, 0.26f, 0.18f);
            // sky, clapboard house behind, porch boards below
            p.Rect(new Vector2(0, 0.55f), new Vector2(1, 1), light);
            p.Rect(new Vector2(0.1f, 0.4f), new Vector2(0.95f, 0.9f), new Color(0.7f, 0.63f, 0.5f));
            for (int i = 0; i < 9; i++) p.Line(new Vector2(0.1f, 0.42f + i * 0.055f), new Vector2(0.95f, 0.42f + i * 0.055f), 0.004f, mid);
            p.Rect(new Vector2(0.62f, 0.55f), new Vector2(0.8f, 0.82f), dark);                 // a window
            p.Rect(new Vector2(0, 0), new Vector2(1, 0.3f), mid);
            for (int i = 0; i < 6; i++) p.Line(new Vector2(0, 0.03f + i * 0.05f), new Vector2(1, 0.03f + i * 0.05f), 0.004f, dark);
            // three figures: a woman and two children, coats and hats, faces left soft
            void Figure(float x, float scale, Color coat)
            {
                float headR = 0.055f * scale, bodyW = 0.14f * scale, bodyH = 0.34f * scale, baseY = 0.22f;
                p.Rect(new Vector2(x - bodyW / 2, baseY), new Vector2(x + bodyW / 2, baseY + bodyH), coat);
                p.Line(new Vector2(x - bodyW / 2, baseY + bodyH), new Vector2(x - bodyW / 2 - 0.03f * scale, baseY + bodyH * 0.45f), 0.045f * scale, coat);
                p.Line(new Vector2(x + bodyW / 2, baseY + bodyH), new Vector2(x + bodyW / 2 + 0.03f * scale, baseY + bodyH * 0.45f), 0.045f * scale, coat);
                p.Disc(new Vector2(x, baseY + bodyH + headR * 0.9f), headR, new Color(0.82f, 0.72f, 0.58f));
                p.Ring(new Vector2(x, baseY + bodyH + headR * 1.25f), headR * 0.75f, headR * 1.1f, dark);  // hair / hat
                p.Rect(new Vector2(x - headR * 1.2f, baseY + bodyH + headR * 1.4f), new Vector2(x + headR * 1.2f, baseY + bodyH + headR * 1.8f), dark);
            }
            Figure(0.36f, 1f, dark);
            Figure(0.56f, 0.72f, new Color(0.62f, 0.55f, 0.42f));
            Figure(0.7f, 0.58f, mid);
            // vignette and a slight warm fade at the edges
            for (int y = 0; y < p.Size; y++)
                for (int x = 0; x < p.Size; x++)
                {
                    float dx = x / (float)p.Size - 0.5f, dy = y / (float)p.Size - 0.5f;
                    float v = Mathf.Clamp01((dx * dx + dy * dy) * 1.6f);
                    Color c = p.Px[y * p.Size + x];
                    p.Px[y * p.Size + x] = Color.Lerp(c, c * 0.65f, v);
                }
            return p.ToTexture();
        }

        /// <summary>
        /// A dial: the printed face, its needle, and unless <paramref name="cased"/> is off a
        /// ship's instrument case round it: a spun brass can behind, a heavy brass bezel, and a
        /// glass with a soft painted sheen (no live highlight, so it never flashes as she rolls).
        /// </summary>
        Transform DialFace(Transform parent, string name, float size, Texture2D tex, out Transform needle, Vector3? pos = null, float needleLen = 0.15f, Vector2? pivotOffset = null, bool cased = true)
        {
            var mat = Mats.Glow(Color.white, new Color(0.05f, 0.05f, 0.05f), tex, 0.6f);
            _panelGlows.Add(mat);
            var q = Geo.Disc(parent, name, pos ?? Vector3.zero, size, mat);
            needle = needleLen > 0 ? Needle(q.transform, needleLen, 0.01f, new Color(0.05f, 0.05f, 0.05f), pivotOffset ?? Vector2.zero) : null;
            if (!cased)
            {
                Geo.MeshObject(Geo.Node(q.transform, "Bezel", new Vector3(0, 0, 0.004f)), "Ring", Geo.Torus(size * 0.5f, 0.011f, 40, 6), Mats.Lit(new Color(0.12f, 0.12f, 0.12f), 0.7f, 0.8f));
                return q.transform;
            }
            float r = size * 0.5f;
            Geo.Cyl(q.transform, "Case", new Vector3(0, 0, 0.026f), r + 0.014f, 0.05f, _brass, new Vector3(90, 0, 0));
            Geo.MeshObject(Geo.Node(q.transform, "Bezel", new Vector3(0, 0, -0.008f)), "Ring", Geo.Torus(r + 0.006f, 0.017f, 48, 10), _brass);
            Geo.MeshObject(Geo.Node(q.transform, "Seat", new Vector3(0, 0, -0.001f)), "Ring", Geo.Torus(r - 0.004f, 0.005f, 48, 6), Mats.Lit(new Color(0.08f, 0.07f, 0.06f), 0.4f));
            Geo.Disc(q.transform, "Glass", new Vector3(0, 0, -0.017f), size + 0.004f, _dialGlass);
            return q.transform;
        }

        /// <summary>Instrument glass: nearly clear, a little grey, with a soft crescent of reflected window light painted in.</summary>
        static Material DialGlass()
        {
            const int n = 128;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = x / (n - 1f) * 2 - 1, v = y / (n - 1f) * 2 - 1;
                    // a band across the upper left, fading at its ends, and a faint rim
                    float band = Mathf.Exp(-Mathf.Pow((u * 0.7f - v * 0.7f + 0.55f) / 0.16f, 2)) * Mathf.Clamp01(1.2f - (u * u + v * v));
                    float rim = Mathf.SmoothStep(0.8f, 1f, Mathf.Sqrt(u * u + v * v)) * 0.25f;
                    float a = 0.05f + band * 0.32f + rim;
                    px[y * n + x] = new Color(0.92f, 0.95f, 1f, Mathf.Clamp01(a));
                }
            t.SetPixels32(px);
            t.Apply(true);
            var m = Mats.Clear(Color.white, 0.15f);
            m.SetTexture("_BaseMap", t);
            return m;
        }

        static Transform Needle(Transform face, float len, float width, Color c, Vector2 pivot = default)
        {
            var p = Geo.Node(face, "Needle", new Vector3(pivot.x, pivot.y, -0.008f));
            Geo.Box(p, "Blade", new Vector3(0, len / 2 - 0.015f, 0), new Vector3(width, len, 0.003f), Mats.Lit(c, 0.6f));
            Geo.Cyl(p, "Cap", Vector3.zero, 0.012f, 0.006f, Mats.Lit(new Color(0.1f, 0.1f, 0.1f), 0.7f, 0.8f), new Vector3(90, 0, 0));
            return p;
        }

        Material Lamp(Transform parent, Vector3 pos, Color c)
        {
            var m = Mats.Glow(c * 0.15f, Color.black);
            Geo.Sphere(parent, "Lamp", pos, 0.025f, m);
            _lampColors[m] = c;
            return m;
        }

        readonly Dictionary<Material, Color> _lampColors = new();

        void SetLamp(Material m, bool on, float bright = 3f)
        {
            if (m == null) return;
            m.SetColor("_EmissionColor", on ? _lampColors[m] * bright : Color.black);
        }

        RadarScope BuildRadar(Transform root, Vector3 basePos, float yaw, string name, Material cabinet, Material black, Color phosphor, string label, out Material lamp)
        {
            var node = Geo.Node(root, name, basePos, new Vector3(0, yaw, 0));
            Geo.Box(node, "Pedestal", new Vector3(0, 0.5f, 0.02f), new Vector3(0.62f, 1.0f, 0.56f), cabinet);
            Geo.Box(node, "Base", new Vector3(0, 0.03f, 0.02f), new Vector3(0.7f, 0.06f, 0.64f), black);
            var head = Geo.Node(node, "Head", new Vector3(0, 1.12f, -0.02f), new Vector3(55, 0, 0));
            Geo.Box(head, "Bezel", new Vector3(0, 0, 0.08f), new Vector3(0.62f, 0.62f, 0.16f), cabinet);
            Geo.Box(head, "Visor", new Vector3(0, 0.26f, -0.1f), new Vector3(0.62f, 0.1f, 0.26f), black, new Vector3(-10, 0, 0));
            Geo.MeshObject(Geo.Node(head, "Hood", new Vector3(0, 0, -0.01f)), "Ring", Geo.Torus(0.23f, 0.028f, 40, 8), black);
            var scope = new RadarScope(256);
            var mat = Mats.Glow(Color.black, phosphor, scope.Texture, 0.9f);
            Geo.Quad(head, "Screen", new Vector3(0, 0, -0.002f), new Vector2(0.42f, 0.42f), mat);
            scope.Material = mat;
            lamp = Lamp(node, new Vector3(0.24f, 1.0f, -0.3f), new Color(0.2f, 1f, 0.3f));
            foreach (var kx in new[] { -0.22f, -0.12f, -0.02f, 0.1f })
                Geo.Cyl(node, "Knob", new Vector3(kx, 0.92f, -0.3f), 0.018f, 0.03f, black, new Vector3(90, 0, 0));
            var plate = new Painter(256, new Color(0.1f, 0.1f, 0.1f));
            plate.Text(label, new Vector2(0.5f, 0.68f), 0.13f, new Color(0.85f, 0.85f, 0.8f));
            plate.Text("RANGE   GAIN   TUNE   STC", new Vector2(0.5f, 0.3f), 0.09f, new Color(0.7f, 0.7f, 0.65f));
            Geo.Quad(node, "Plate", new Vector3(-0.06f, 0.72f, -0.302f), new Vector2(0.4f, 0.1f), Mats.Lit(Color.white, 0.3f).CloneWith(plate.ToTexture()));
            Interactable.Add(Geo.Box(head, "Hit", Vector3.zero, new Vector3(0.62f, 0.62f, 0.2f), null, collider: true).Hide(), "", null,
                () => _ship.RadarsWorking ? $"{name}  range {scope.RangeNm} nm, head-up" : $"{name}  no picture");
            // the generated set has its scope set back on the table, tilted 31 degrees: the picture goes onto it
            if (Props.Swap(node, "radar", "Screen", "Lamp", "Hit"))
            {
                head.localPosition = new Vector3(-0.025f, 1.008f, 0.15f);
                head.localEulerAngles = new Vector3(31, 0, 0);
                head.localScale = Vector3.one * 0.66f;
                node.Find("Lamp").localPosition = new Vector3(0.22f, 0.79f, 0.05f);
                // its viewing hood juts out over the scope and hides it from a standing man: cut the lip back
                Props.Trim(node, "radar", new Bounds(new Vector3(0, 1.45f, -0.3f), new Vector3(1.0f, 0.56f, 0.84f)));
            }
            return scope;
        }

        // ---------------------------------------------------------------- per frame

        public float Barometer()
        {
            // 29.30 in falling to about 28.95 in by evening
            float t = _game.Clock.Progress;
            return Mathf.Lerp(29.32f, 28.95f, t) + Mathf.Sin(Time.time * 0.37f) * 0.005f;
        }

        public void Tick(float dt)
        {
            float wheelDeg = _ship.WheelInput * 540f;
            _wheel.localRotation = Quaternion.Euler(0, 0, -wheelDeg);
            _rudderNeedle.localRotation = Quaternion.Euler(0, 0, -(_ship.Rudder / 40f) * 70f);
            _gyroCard.localRotation = Quaternion.Euler(0, 0, _ship.Heading);
            _helmGyroCard.localRotation = Quaternion.Euler(0, 0, _ship.Heading);
            // the magnetic card lags and swings a little in a seaway
            _binnacleCard.localRotation = Quaternion.Euler(0, 0, _ship.Heading + 4f + Mathf.Sin(Time.time * 0.9f) * 1.5f);
            // Pendulum hangs down: 180 deg is plumb; heel to starboard swings it to port on the dial.
            _clinoNeedle.localRotation = Quaternion.Euler(0, 0, 180f - _ship.Roll * 1.75f);
            _rpmNeedle.localRotation = Quaternion.Euler(0, 0, -Mathf.Lerp(-135f, 135f, _ship.EngineRpm / 120f));
            _baroNeedle.localRotation = Quaternion.Euler(0, 0, -Mathf.Lerp(-135f, 135f, Mathf.InverseLerp(28f, 31f, Barometer())));
            float wind = _ship.Power ? _weather.WindKnots + Mathf.Sin(Time.time * 3.1f) * 2f : 0f;
            _windNeedle.localRotation = Quaternion.Euler(0, 0, -Mathf.Lerp(-135f, 135f, wind / 100f));
            float depth = _ship.Power ? Charts.DepthFathoms(_ship.NavNm) : 0f;
            _fathomNeedle.localRotation = Quaternion.Euler(0, 0, -Mathf.Lerp(-135f, 135f, Mathf.Clamp01(depth / 120f)));

            float mins = _game.Clock.Minutes;
            float hours = mins / 60f;
            _clockHour.localRotation = Quaternion.Euler(0, 0, -(hours % 12f) * 30f);
            _clockMinute.localRotation = Quaternion.Euler(0, 0, -(mins % 60f) * 6f);
            _clockSecond.localRotation = Quaternion.Euler(0, 0, -((mins * 60f) % 60f) * 6f);

            _telegraphLever.localRotation = Quaternion.Euler(0, 0, -TelegraphAngle(_ship.Order));
            _telegraphAnswer.localRotation = Quaternion.Euler(0, 0, -TelegraphAngle(_game.Story.AnsweredOrder));

            _whistlePull = Mathf.MoveTowards(_whistlePull, _game.Audio.WhistleOn ? 1 : 0, dt * 6f);
            _whistleHandle.localPosition = new Vector3(0, -0.62f - _whistlePull * 0.12f, 0);

            // radio dial pointer: 540..1600 kHz across the glass
            _radioDial.localPosition = new Vector3(Mathf.Lerp(-0.2f, 0.2f, Mathf.InverseLerp(540f, 1600f, _game.Radio.AmKhz)), 0.03f, -0.096f);
            _vhfDisplay.SetColor("_EmissionColor", _ship.Power && _game.Radio.VhfOn ? new Color(1.6f, 0.4f, 0.1f) * (_game.Radio.Transmitting ? 1.6f : 0.8f) : Color.black);
            _amDial.SetColor("_EmissionColor", _ship.Power && _game.Radio.AmOn ? new Color(0.9f, 0.65f, 0.3f) : Color.black);

            // lamps
            SetLamp(_pump1, _ship.PumpsOn && _ship.Power);
            SetLamp(_pump2, _ship.PumpsOn && _ship.Power);
            SetLamp(_deckLamp, _ship.DeckLights && _ship.Power);
            SetLamp(_searchLamp, _ship.Searchlight && _ship.Power);
            SetLamp(_radarLamp1, _ship.RadarsWorking && _ship.Power);
            SetLamp(_radarLamp2, _ship.RadarsWorking && _ship.Power);

            // instrument backlighting comes up as daylight goes
            float night = 1f - _weather.Daylight;
            var backlight = _ship.Power ? new Color(0.5f, 0.3f, 0.12f) * (0.22f + night * 0.75f) : Color.black;
            foreach (var m in _panelGlows) m.SetColor("_EmissionColor", backlight);
            _telegraphGlow.SetColor("_EmissionColor", _ship.Power ? new Color(0.95f, 0.82f, 0.55f) * (0.55f + night * 0.25f) : Color.black);
            _cabinLight.intensity = _ship.Power ? Mathf.Lerp(0.35f, 1.6f, night) : 0f;
            _nightLight.SetColor("_EmissionColor", _ship.Power ? new Color(1.6f, 1.3f, 0.9f) * (0.3f + night) : Color.black);
            _redLight.enabled = _ship.Power;

            // ship lights outside
            foreach (var l in ShipModel.DeckLights) l.enabled = _ship.DeckLights && _ship.Power;
            if (ShipModel.Searchlight != null) ShipModel.Searchlight.enabled = _ship.Searchlight && _ship.Power;
            if (ShipModel.NightWindows.Count > 0)
                ShipModel.NightWindows[0].sharedMaterial.SetColor("_EmissionColor", _ship.Power ? new Color(1.2f, 0.85f, 0.45f) * (0.3f + night * 1.2f) : Color.black);

            // radar
            bool radarOn = _ship.RadarsWorking && _ship.Power;
            _radar1.Tick(dt, radarOn, _ship, _game.Story);
            _radar2.Tick(dt, radarOn, _ship, _game.Story);

            _chartTimer -= dt;
            if (_chartTimer <= 0)
            {
                _chartTimer = 5f;
                RedrawChart();
            }
        }

        static float TelegraphAngle(Telegraph t) => ((int)t - 3) * 30f;

        void RedrawChart()
        {
            var p = new Painter(_chartBase.Size, Color.white);
            Array.Copy(_chartBase.Px, p.Px, p.Px.Length);
            var pencil = new Color(0.15f, 0.15f, 0.18f, 0.85f);
            // the course line to Whitefish laid down in the morning
            p.Line(Charts.ChartUv(Charts.StartPosition), Charts.ChartUv(Charts.WhitefishPoint), 0.0015f, new Color(0.3f, 0.3f, 0.6f, 0.6f));
            var track = _ship.Track;
            for (int i = 1; i < track.Count; i += 1)
                p.Line(Charts.ChartUv(track[i - 1]), Charts.ChartUv(track[i]), 0.002f, pencil);
            var here = Charts.ChartUv(_ship.NavNm);
            p.Ring(here, 0.006f, 0.0085f, pencil);
            p.Text(GameClock.Format(_game.Clock.Minutes).Replace(" PM", "").Replace(":", ""), here + new Vector2(0.035f, 0.0f), 0.012f, pencil);
            _chartTex.SetPixels32(p.Px);
            _chartTex.Apply(true);
        }
    }

    static class MatExt
    {
        public static Material CloneWith(this Material m, Texture tex)
        {
            var c = new Material(m);
            c.SetTexture("_BaseMap", tex);
            return c;
        }

        public static GameObject Hide(this GameObject go)
        {
            var r = go.GetComponent<Renderer>();
            if (r != null) r.enabled = false;
            return go;
        }
    }
}
