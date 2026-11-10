using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace Fitz
{
    public enum GameState { Title, Intro, Playing, Memorial }

    public class Game : MonoBehaviour
    {
        public static Game I;
        public GameClock Clock = new();
        public Ship Ship;
        public Ocean Ocean;
        public Weather Weather;
        public Wheelhouse Wheelhouse;
        public GameAudio Audio;
        public Dialogue Dialogue;
        public Radio Radio;
        public Story Story;
        public GameUI UI;
        public Camera Cam;
        public GameState State = GameState.Title;

        // settings
        public Land Land;
        public float MasterVolume = 1f, MusicVolume = 1f, Sensitivity = 1f;
        public bool InvertY, Subtitles = true, Hints = true, Readouts;
        /// <summary>0 = fullscreen at the monitor's resolution, otherwise an index into WindowSizes + 1.</summary>
        public int DisplayMode;
        public static readonly Vector2Int[] WindowSizes = { new(1280, 720), new(1600, 900), new(1920, 1080), new(2560, 1440) };

        float _yaw, _pitch;
        float _titleYaw;
        Transform _camRig;
        Interactable _hover;
        public Interactable Hover => _hover;

        void Awake()
        {
            I = this;
            Application.targetFrameRate = 120;
            LoadSettings();
            // Unity remembers the last -screen-* command line in the player prefs, so the
            // game applies its own display setting unless a test run is sizing the window.
            if (!Args.Has("-screen-width") && !Args.Has("-screen-fullscreen")) ApplyDisplay();

            var shipGo = new GameObject("Ship");
            Ship = shipGo.AddComponent<Ship>();
            Ocean = new GameObject("Lake").AddComponent<Ocean>();
            Ocean.Build();
            foreach (var t in Ocean.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 4;   // Water
            Ship.Ocean = Ocean;
            Land = new GameObject("Land").AddComponent<Land>();
            Land.Build();
            ShipModel.Build(shipGo.transform);

            Weather = new GameObject("Weather").AddComponent<Weather>();
            Weather.Build();

            Audio = new GameObject("Audio").AddComponent<GameAudio>();
            Audio.Build();
            Dialogue = Audio.gameObject.AddComponent<Dialogue>();
            Dialogue.Init(Audio);
            Radio = gameObject.AddComponent<Radio>();
            Story = gameObject.AddComponent<Story>();
            UI = gameObject.AddComponent<GameUI>();

            Wheelhouse = shipGo.AddComponent<Wheelhouse>();
            Wheelhouse.Build(this, Ship, Weather, shipGo.transform);

            // camera: your eyes, standing behind the wheel
            _camRig = Geo.Node(shipGo.transform, "Eyes", Wheelhouse.EyePosition);
            var camGo = new GameObject("Camera");
            camGo.transform.SetParent(_camRig, false);
            Cam = camGo.AddComponent<Camera>();
            Cam.nearClipPlane = 0.04f;
            Cam.farClipPlane = 6000f;
            Cam.fieldOfView = 68f;
            camGo.AddComponent<AudioListener>();
            var data = camGo.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            camGo.tag = "MainCamera";

            Ship.Init(Charts.StartPosition, 130f);
            Radio.Init(this);
            Story.Init(this);
            UI.Init(this);

            // test and screenshot switches
            Clock.Scale = Args.Float("-fitzTimeScale", 1f);
            float start = Args.ClockMinutes("-fitzStart", GameClock.StartMinutes);
            if (start > GameClock.StartMinutes)
            {
                Clock.Minutes = start;
                Story.FastForwardTo(start);
            }
            if (Args.Has("-fitzSkipTitle")) BeginPlaying(skipIntro: true);
            var camPos = Args.Get("-fitzCamPos");
            if (camPos != null)
            {
                var c = camPos.Split(',');
                _camRig.localPosition = new Vector3(float.Parse(c[0], System.Globalization.CultureInfo.InvariantCulture), float.Parse(c[1], System.Globalization.CultureInfo.InvariantCulture), float.Parse(c[2], System.Globalization.CultureInfo.InvariantCulture));
            }
            var look = Args.Get("-fitzLook");
            if (look != null)
            {
                var p = look.Split(',');
                _yaw = float.Parse(p[0], System.Globalization.CultureInfo.InvariantCulture);
                _pitch = p.Length > 1 ? float.Parse(p[1], System.Globalization.CultureInfo.InvariantCulture) : 0;
            }
            switch (Args.Get("-fitzPanel"))
            {
                case "radio": UI.OpenRadio(); Radio.AmOn = true; break;
                case "orders": UI.OpenOrders(); break;
                case "chart": UI.OpenChart(); break;
                case "settings": UI.OpenSettings(); break;
                case "log": UI.OpenLog(); break;
            }
            if (Args.Has("-fitzLights")) { Ship.Searchlight = true; Ship.DeckLights = true; }
            if (Args.Get("-fitzShot") != null) Application.runInBackground = true;
            if (Args.Get("-fitzShot") != null) StartCoroutine(Screenshot(Args.Get("-fitzShot"), Args.Float("-fitzShotDelay", 6f)));
            if (Args.Has("-fitzMemorial")) StartCoroutine(MemorialSoon());
        }

        IEnumerator MemorialSoon()
        {
            yield return new WaitForSeconds(1f);
            ShowMemorial();
        }

        IEnumerator Screenshot(string path, float delay)
        {
            yield return new WaitForSeconds(delay);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"[Fitz] Screenshot {path}  avg fps {Time.frameCount / Mathf.Max(0.1f, Time.realtimeSinceStartup):0}  audio blocks {ProcSource.Blocks}  voice files {Dialogue.VoiceFilesFound}");
            yield return new WaitForSeconds(1.5f);
            if (Args.Has("-fitzQuit")) Application.Quit();
        }

        public void BeginPlaying(bool skipIntro = false)
        {
            if (skipIntro)
            {
                State = GameState.Playing;
                Clock.Running = true;
            }
            else
            {
                State = GameState.Intro;
                UI.ShowIntro(() =>
                {
                    State = GameState.Playing;
                    Clock.Running = true;
                    UI.Hint("Steer with A/D, engine orders with W/S. Look around with the mouse and click on things. [T] orders, [R] radio, [M] chart, [H] whistle.", 30);
                });
            }
            _yaw = 0;
            _pitch = 0;
        }

        public void ShowMemorial()
        {
            State = GameState.Memorial;
            Clock.Running = false;
            UI.ShowMemorial();
        }

        public void ReturnToTitle()
        {
            Time.timeScale = 1f;
            ReleaseLook();
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            var kb = Keyboard.current;
            var mouse = Mouse.current;

            bool playing = State == GameState.Playing && !UI.Paused;
            if (UI.Paused) { Clock.Running = false; }
            else if (State == GameState.Playing && !Story.Ending) Clock.Running = true;

            float before = Clock.Minutes;
            Clock.Tick(dt);
            float gameDt = Clock.Minutes - before;

            // helm
            float steer = 0;
            if (playing && !UI.TextFocus && kb != null && !Story.Ending)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) steer -= 1;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) steer += 1;
                if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) OnTelegraph(+1);
                if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) OnTelegraph(-1);
            }
            Ship.Step(dt, gameDt, steer);
            Land.Place(Ship.NavNm);

            Story.Tick(dt);
            Radio.Tick(dt);
            Weather.Tick(Clock.Minutes, Cam, Ship, Ocean, dt);
            Wheelhouse.Tick(dt);
            UpdateCamera(dt, playing, mouse);
            UpdateInteraction(playing, mouse);

            bool holdWhistle = playing && kb != null && kb.hKey.isPressed && Ship.Power;
            Audio.Tick(holdWhistle);
            var amb = Audio.Ambience;
            amb.Wind = Mathf.Clamp01(Weather.WindKnots / 70f);
            amb.Gust = Weather.Gust;
            amb.Rain = Mathf.Clamp01(Weather.Snow * 0.6f + Weather.Rain + Weather.SprayOnGlass);
            amb.Engine = Ship.Power ? Mathf.Clamp01(Ship.EngineRpm / 112f) * 0.8f + 0.2f : 0f;

            // where the floating origin and wheelhouse live, for the precipitation shader
            Shader.SetGlobalMatrix("_FitzShipW2L", Ship.transform.worldToLocalMatrix);
            Shader.SetGlobalVector("_FitzCabinMin", new Vector4(-Wheelhouse.Side - 0.1f, Wheelhouse.Floor - 0.2f, Wheelhouse.Back - 0.1f, 0));
            Shader.SetGlobalVector("_FitzCabinMax", new Vector4(Wheelhouse.Side + 0.1f, Wheelhouse.Ceiling + 0.3f, Wheelhouse.Front + 0.1f, 0));
        }

        void UpdateCamera(float dt, bool playing, Mouse mouse)
        {
            bool look = playing && !UI.CursorNeeded && mouse != null;
            Cursor.lockState = look ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !look;
            if (look)
            {
                var d = mouse.delta.ReadValue() * 0.08f * Sensitivity;
                _yaw += d.x;
                _pitch += InvertY ? d.y : -d.y;
                _yaw = Mathf.Clamp(_yaw, -175f, 175f);
                _pitch = Mathf.Clamp(_pitch, -70f, 75f);
            }
            if (State == GameState.Title)
            {
                _titleYaw = Mathf.Sin(Time.time * 0.05f) * 25f;
                _camRig.localRotation = Quaternion.Euler(-2f, _titleYaw, 0);
                Cam.transform.localRotation = Quaternion.identity;
                return;
            }
            if (_lookActive)
            {
                // the story turns your head for you (the last look at the photograph)
                _lookBlend = Mathf.MoveTowards(_lookBlend, 1f, Time.unscaledDeltaTime / _lookSeconds);
                var d = _lookTarget - _camRig.localPosition;
                float wantYaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
                float wantPitch = -Mathf.Asin(Mathf.Clamp(d.y / d.magnitude, -1f, 1f)) * Mathf.Rad2Deg;
                float k = Mathf.SmoothStep(0, 1, _lookBlend);
                _yaw = Mathf.LerpAngle(_lookFromYaw, wantYaw, k);
                _pitch = Mathf.Lerp(_lookFromPitch, wantPitch, k);
            }
            // You brace against the roll: the head stays a little steadier than the ship.
            _camRig.localRotation = Quaternion.Euler(Ship.Pitch * -0.2f, 0, -Ship.Roll * 0.25f);
            Cam.transform.localRotation = Quaternion.Euler(_pitch, _yaw, 0);
        }

        void UpdateInteraction(bool playing, Mouse mouse)
        {
            _hover = null;
            if (!playing || UI.CursorNeeded || Story.Ending) return;
            var ray = new Ray(Cam.transform.position, Cam.transform.forward);
            if (Physics.Raycast(ray, out var hit, 9f)) _hover = hit.collider.GetComponent<Interactable>();
            if (_hover != null && mouse != null)
            {
                if (mouse.leftButton.wasPressedThisFrame) _hover.OnUse?.Invoke(0);
                if (mouse.rightButton.wasPressedThisFrame) _hover.OnUse?.Invoke(1);
            }
        }

        bool _lookActive;
        Vector3 _lookTarget;
        float _lookBlend, _lookSeconds, _lookFromYaw, _lookFromPitch;

        /// <summary>Turns the view to a point in ship space over a few seconds and holds it.</summary>
        public void LookToward(Vector3 shipSpacePoint, float seconds)
        {
            _lookActive = true;
            _lookTarget = shipSpacePoint;
            _lookSeconds = Mathf.Max(0.1f, seconds);
            _lookBlend = 0;
            _lookFromYaw = _yaw;
            _lookFromPitch = _pitch;
        }

        public void ReleaseLook() => _lookActive = false;

        public void OnTelegraph(int dir)
        {
            if (Story.Ending || !Ship.Power) return;
            var next = (Telegraph)Mathf.Clamp((int)Ship.Order + dir, 0, 6);
            if (next == Ship.Order) return;
            Ship.Order = next;
            Audio.Play(Audio.TelegraphClip, 0.9f);
        }

        public void OnWheelClicked(int button)
        {
            if (Story.Dialogue_Free()) Story.IssueOrder("wheel");
        }

        // ---------------------------------------------------------------- settings

        void LoadSettings()
        {
            MasterVolume = PlayerPrefs.GetFloat("master", 1f);
            MusicVolume = PlayerPrefs.GetFloat("music", 1f);
            Sensitivity = PlayerPrefs.GetFloat("sens", 1f);
            InvertY = PlayerPrefs.GetInt("invert", 0) == 1;
            Subtitles = PlayerPrefs.GetInt("subs", 1) == 1;
            Hints = PlayerPrefs.GetInt("hints", 1) == 1;
            Readouts = PlayerPrefs.GetInt("readouts", 0) == 1;
            DisplayMode = Mathf.Clamp(PlayerPrefs.GetInt("display", 0), 0, WindowSizes.Length);
            AudioListener.volume = MasterVolume;
        }

        public void ApplyDisplay()
        {
            if (DisplayMode == 0)
                Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, FullScreenMode.FullScreenWindow);
            else
            {
                var s = WindowSizes[DisplayMode - 1];
                Screen.SetResolution(s.x, s.y, FullScreenMode.Windowed);
            }
        }

        public void SaveSettings()
        {
            PlayerPrefs.SetFloat("master", MasterVolume);
            PlayerPrefs.SetFloat("music", MusicVolume);
            PlayerPrefs.SetFloat("sens", Sensitivity);
            PlayerPrefs.SetInt("invert", InvertY ? 1 : 0);
            PlayerPrefs.SetInt("subs", Subtitles ? 1 : 0);
            PlayerPrefs.SetInt("hints", Hints ? 1 : 0);
            PlayerPrefs.SetInt("readouts", Readouts ? 1 : 0);
            PlayerPrefs.SetInt("display", DisplayMode);
            PlayerPrefs.Save();
            AudioListener.volume = MasterVolume;
        }
    }

    public static class StoryExt
    {
        public static bool Dialogue_Free(this Story s) => !Game.I.Dialogue.Busy && !s.Ending;
    }
}
