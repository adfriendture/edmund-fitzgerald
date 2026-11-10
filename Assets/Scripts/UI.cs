using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Fitz
{
    /// <summary>All 2D interface, in IMGUI, laid out for 1920x1080 and scaled.</summary>
    public class GameUI : MonoBehaviour
    {
        Game _g;
        enum Panel { None, Radio, Orders, Chart, Pause, Settings, Log }
        Panel _lastPanel;
        float _panelSince;   // the screen buttons ignore the click that may have opened the panel
        Panel _panel;
        Panel _settingsReturn;
        public bool Paused => _panel == Panel.Pause || _panel == Panel.Settings;
        public bool TextFocus => false;
        public bool CursorNeeded => _panel != Panel.None || _g.State != GameState.Playing;

        string _hint = "";
        float _hintUntil;
        float _introStart = -1;
        Action _introDone;
        float _memorialStart = -1;
        int _bells;
        bool _memorialSkipped;

        Font _serif, _sans, _hand;
        GUIStyle _ink, _inkTitle, _inkSmall, _inkOption, _inkOptionOff, _inkTime;
        Texture2D _paper;
        int _logScroll;
        GUIStyle _toggle, _title, _subtitle, _body, _small, _button, _box, _sub, _speaker, _hover, _memorial, _option, _optionOff, _label, _screenOn, _screenOff, _link;
        Texture2D _black, _panelBg, _white;
        bool _stylesReady;
        const float W = 1920, H = 1080;

        public void Init(Game g)
        {
            _g = g;
            _black = Solid(Color.black);
            _panelBg = Solid(new Color(0.03f, 0.04f, 0.045f, 0.86f));
            _white = Solid(Color.white);
        }

        static Texture2D Solid(Color c)
        {
            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }

        /// <summary>The OS lists faces, not families ("Bradley Hand Bold"), so match by prefix.</summary>
        Font HandFont()
        {
            var installed = Font.GetOSInstalledFontNames();
            foreach (var want in new[] { "Bradley Hand", "Noteworthy", "Segoe Print", "Ink Free", "Segoe Script", "Chalkboard SE", "Chalkboard", "Marker Felt" })
                foreach (var name in installed)
                    if (name.StartsWith(want, System.StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.Log("[Fitz] Notebook font: " + name);
                        return Font.CreateDynamicFontFromOSFont(name, 26);
                    }
            Debug.Log("[Fitz] No handwriting font found; using the serif");
            return Font.CreateDynamicFontFromOSFont(new[] { "Georgia", "Times New Roman", "Times" }, 26);
        }

        void BuildStyles()
        {
            _serif = Font.CreateDynamicFontFromOSFont(new[] { "Georgia", "Times New Roman", "Times" }, 32);
            _sans = Font.CreateDynamicFontFromOSFont(new[] { "Helvetica Neue", "Segoe UI", "Arial" }, 22);
            // pencil on the notebook pages: whatever handwriting face the machine has
            // Patrick Hand (SIL OFL), shipped in Resources/Fonts so Mac and Windows match
            _hand = Resources.Load<Font>("Fonts/PatrickHand-Regular") ?? HandFont();
            _paper = Surfaces.Paper(21);
            GUIStyle S(Font f, int size, Color c, TextAnchor a = TextAnchor.UpperLeft, FontStyle fs = FontStyle.Normal) =>
                new GUIStyle { font = f, fontSize = size, alignment = a, wordWrap = true, richText = true, fontStyle = fs, normal = { textColor = c } };
            var paper = new Color(0.9f, 0.88f, 0.82f);
            _title = S(_serif, 96, paper, TextAnchor.MiddleLeft);
            _subtitle = S(_serif, 30, new Color(0.75f, 0.74f, 0.7f), TextAnchor.MiddleLeft, FontStyle.Italic);
            _body = S(_sans, 22, paper);
            _small = S(_sans, 18, new Color(0.7f, 0.7f, 0.68f));
            _label = S(_sans, 20, paper, TextAnchor.MiddleLeft);
            _button = S(_serif, 34, paper, TextAnchor.MiddleLeft);
            _button.hover.textColor = new Color(1f, 0.75f, 0.45f);
            _link = new GUIStyle(_button) { fontSize = 22 };
            _box = new GUIStyle { normal = { background = _panelBg }, padding = new RectOffset(24, 24, 20, 20) };
            _sub = S(_sans, 28, Color.white, TextAnchor.MiddleCenter);
            _speaker = S(_sans, 20, new Color(1f, 0.78f, 0.45f), TextAnchor.MiddleCenter, FontStyle.Bold);
            _hover = S(_sans, 21, new Color(0.95f, 0.95f, 0.9f), TextAnchor.UpperCenter);
            _memorial = S(_serif, 30, paper, TextAnchor.UpperCenter);
            _option = S(_sans, 24, paper, TextAnchor.MiddleLeft);
            _option.hover.textColor = new Color(1f, 0.75f, 0.45f);
            _optionOff = S(_sans, 24, new Color(0.45f, 0.45f, 0.45f), TextAnchor.MiddleLeft);
            _screenOn = new GUIStyle(_option) { fontSize = 21, wordWrap = false };
            _screenOff = new GUIStyle(_optionOff) { fontSize = 21, wordWrap = false };
            _screenOff.hover.textColor = new Color(0.7f, 0.7f, 0.7f);
            var pencil = new Color(0.22f, 0.21f, 0.24f);
            _ink = S(_hand, 25, pencil, TextAnchor.MiddleLeft);
            _inkTitle = S(_hand, 40, pencil, TextAnchor.MiddleLeft);
            _inkSmall = S(_hand, 20, new Color(0.4f, 0.38f, 0.4f), TextAnchor.MiddleLeft);
            _inkOption = S(_hand, 26, pencil, TextAnchor.MiddleLeft);
            _inkOption.hover.textColor = new Color(0.55f, 0.12f, 0.1f);
            _inkOptionOff = S(_hand, 26, new Color(0.6f, 0.58f, 0.58f), TextAnchor.MiddleLeft);
            _inkTime = S(_hand, 21, new Color(0.45f, 0.42f, 0.45f), TextAnchor.UpperLeft);
            _toggle = new GUIStyle(GUI.skin.toggle) { font = _sans, fontSize = 20 };
            _toggle.normal.textColor = paper; _toggle.onNormal.textColor = paper; _toggle.hover.textColor = paper; _toggle.onHover.textColor = paper;
            _stylesReady = true;
        }

        public void Hint(string text, float seconds)
        {
            _hint = text;
            _hintUntil = Time.time + seconds;
        }

        public void OpenRadio() { if (_g.State == GameState.Playing && !_g.Story.Ending) _panel = Panel.Radio; }
        public void CloseRadio() { if (_panel == Panel.Radio) _panel = Panel.None; }
        public void OpenOrders() { if (_g.State == GameState.Playing && !_g.Story.Ending) _panel = Panel.Orders; }
        public void OpenChart() { if (_g.State == GameState.Playing) _panel = Panel.Chart; }
        public void OpenLog() { if (_g.State == GameState.Playing) { _panel = Panel.Log; _logScroll = 0; } }
        public void OpenSettings() { _settingsReturn = _g.State == GameState.Playing ? Panel.Pause : Panel.None; _panel = Panel.Settings; }
        public void CloseAll() { if (!Paused) _panel = Panel.None; }

        public void ShowIntro(Action done)
        {
            _introStart = Time.time;
            _introDone = done;
        }

        public void ShowMemorial()
        {
            _panel = Panel.None;
            _memorialStart = Time.time;
            _bells = 0;
            StartCoroutine(Bells());
        }

        IEnumerator Bells()
        {
            yield return new WaitForSeconds(9f);
            while (_bells < 30 && !_memorialSkipped)
            {
                _g.Audio.Play(_g.Audio.BellClip, _bells == 29 ? 0.8f : 1f);
                _bells++;
                yield return new WaitForSeconds(_bells == 29 ? 4.5f : 2.6f);
            }
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (_g.State == GameState.Intro && _introStart > 0 && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || Time.time - _introStart > 24f))
                FinishIntro();

            if (_g.State == GameState.Memorial)
            {
                float t = Time.time - _memorialStart;
                if (t > 4f && (kb.escapeKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
                {
                    if (_bells >= 30 || _memorialSkipped) _g.ReturnToTitle();
                    else _memorialSkipped = true;
                }
                return;
            }

            if (_g.State != GameState.Playing) return;

            if (kb.escapeKey.wasPressedThisFrame)
            {
                if (_panel == Panel.Settings) _panel = _settingsReturn;
                else if (_panel == Panel.None) _panel = Panel.Pause;
                else _panel = Panel.None;
            }
            if (Paused) return;
            if (kb.rKey.wasPressedThisFrame) _panel = _panel == Panel.Radio ? Panel.None : (_g.Story.Ending ? Panel.None : Panel.Radio);
            if (kb.tKey.wasPressedThisFrame) _panel = _panel == Panel.Orders ? Panel.None : (_g.Story.Ending ? Panel.None : Panel.Orders);
            if (kb.mKey.wasPressedThisFrame) _panel = _panel == Panel.Chart ? Panel.None : Panel.Chart;
            if (kb.lKey.wasPressedThisFrame) { if (_panel == Panel.Log) _panel = Panel.None; else OpenLog(); }
            if (_panel == Panel.Log)
            {
                if (kb.pageUpKey.wasPressedThisFrame) _logScroll += 8;
                if (kb.pageDownKey.wasPressedThisFrame) _logScroll -= 8;
            }
            if (kb.f1Key.wasPressedThisFrame) { _g.Readouts = !_g.Readouts; _g.SaveSettings(); }
            if (kb.f11Key.wasPressedThisFrame) { _g.DisplayMode = _g.DisplayMode == 0 ? 3 : 0; _g.ApplyDisplay(); _g.SaveSettings(); }

            if (_panel == Panel.Radio || _panel == Panel.Orders)
            {
                var opts = _panel == Panel.Radio ? _g.Story.RadioCalls() : _g.Story.Orders();
                for (int i = 0; i < Mathf.Min(opts.Count, 10); i++)
                {
                    var key = i == 9 ? kb.digit0Key : kb[(Key)((int)Key.Digit1 + i)];
                    if (key.wasPressedThisFrame && opts[i].Enabled)
                    {
                        _panel = Panel.None;
                        opts[i].Do();
                        break;
                    }
                }
            }
            if (_panel == Panel.Radio)
            {
                float step = kb.shiftKey.isPressed ? 50 : 10;
                if (kb.qKey.wasPressedThisFrame || kb.leftBracketKey.wasPressedThisFrame) _g.Radio.Nudge(-step);
                if (kb.eKey.wasPressedThisFrame || kb.rightBracketKey.wasPressedThisFrame) _g.Radio.Nudge(step);
                if (kb.oKey.wasPressedThisFrame) ToggleAm();
            }
        }

        void ToggleAm()
        {
            _g.Radio.AmOn = !_g.Radio.AmOn;
            _g.Audio.Click();
        }

        void FinishIntro()
        {
            _introStart = -1;
            var d = _introDone;
            _introDone = null;
            d?.Invoke();
        }

        // ------------------------------------------------------------------ drawing

        void OnGUI()
        {
            if (!_stylesReady) BuildStyles();
            if (_panel != _lastPanel) { _lastPanel = _panel; _panelSince = Time.unscaledTime; }
            float scale = Screen.height / H;
            float width = Screen.width / scale;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));

            switch (_g.State)
            {
                case GameState.Title: DrawTitle(width); break;
                case GameState.Intro: DrawIntro(width); break;
                case GameState.Playing: DrawPlaying(width); break;
                case GameState.Memorial: DrawMemorial(width); break;
            }
        }

        bool Button(Rect r, string text, GUIStyle style = null)
        {
            return GUI.Button(r, text, style ?? _button);
        }

        void Shade(Rect r, float a)
        {
            var c = GUI.color;
            GUI.color = new Color(0, 0, 0, a);
            GUI.DrawTexture(r, _black);
            GUI.color = c;
        }

        void DrawTitle(float width)
        {
            Shade(new Rect(0, 0, 820, H), 0.55f);
            GUI.Label(new Rect(110, 240, 1200, 120), "Edmund Fitzgerald", _title);
            GUI.Label(new Rect(116, 350, 1200, 50), "Lake Superior · November 10, 1975", _subtitle);
            if (_panel == Panel.Settings) { DrawSettings(new Rect(110, 430, 760, 600)); return; }
            if (Button(new Rect(116, 500, 600, 56), "Take the watch")) _g.BeginPlaying();
            if (Button(new Rect(116, 566, 600, 56), "Settings")) { _settingsReturn = Panel.None; _panel = Panel.Settings; }
            if (Button(new Rect(116, 632, 600, 56), "Quit")) Application.Quit();
            // the song isn't licensed for the game (docs/EASTER_EGG.md), so it's a link out to listen elsewhere
            if (Button(new Rect(116, 730, 760, 44), "Listen to Gordon Lightfoot's song (opens YouTube)", _link))
                Application.OpenURL("https://www.youtube.com/results?search_query=Gordon+Lightfoot+The+Wreck+of+the+Edmund+Fitzgerald");
            GUI.Label(new Rect(116, H - 150, 660, 100),
                "A short memorial game about the last afternoon of the SS Edmund Fitzgerald.\nTwenty-nine men were lost. Play with headphones.", _small);
        }

        void DrawIntro(float width)
        {
            GUI.DrawTexture(new Rect(0, 0, width, H), _black);
            float t = Time.time - _introStart;
            string[] lines =
            {
                "November 10, 1975. 2:40 PM.",
                "Lake Superior, northwest of Caribou Island.",
                "The SS Edmund Fitzgerald: 729 feet, 26,116 long tons of taconite pellets, bound for Detroit.",
                "Twenty-nine crew aboard.",
                "You are her captain.",
            };
            for (int i = 0; i < lines.Length; i++)
            {
                float a = Mathf.Clamp01((t - 1f - i * 2.6f) / 1.5f);
                var c = GUI.color;
                GUI.color = new Color(1, 1, 1, a);
                GUI.Label(new Rect(width / 2 - 700, 330 + i * 70, 1400, 60), lines[i], _memorial);
                GUI.color = c;
            }
            if (t > 4f) GUI.Label(new Rect(width / 2 - 300, H - 90, 600, 40), "Space to continue", new GUIStyle(_small) { alignment = TextAnchor.MiddleCenter });
        }

        void DrawPlaying(float width)
        {
            // crosshair and hover label
            if (_panel == Panel.None && !_g.Story.Ending)
            {
                GUI.color = new Color(1, 1, 1, 0.55f);
                GUI.DrawTexture(new Rect(width / 2 - 2, H / 2 - 2, 4, 4), _white);
                GUI.color = Color.white;
                var hov = _g.Hover;
                if (hov != null && !string.IsNullOrEmpty(hov.Text))
                {
                    var r = new Rect(width / 2 - 450, H / 2 + 26, 900, 80);
                    Shadowed(r, hov.Text, _hover);
                }
            }

            // hint
            if (_g.Hints && Time.time < _hintUntil && !string.IsNullOrEmpty(_hint) && _panel == Panel.None)
            {
                var r = new Rect(40, 40, 620, 120);
                Shade(new Rect(28, 30, 644, 6 + _body.CalcHeight(new GUIContent(_hint), 620) + 18), 0.45f);
                GUI.Label(r, _hint, _body);
            }

            if (_g.Readouts) DrawReadouts(width);

            // subtitles
            if (_g.Subtitles)
            {
                var line = _g.Dialogue.Current;
                if (line != null) Subtitle(width, line, H - 190);
                var am = _g.Dialogue.CurrentAm;
                if (am != null && _g.Dialogue.AmAudible()) Subtitle(width, am, line != null ? H - 330 : H - 190);
            }

            switch (_panel)
            {
                case Panel.Radio: DrawRadio(width); break;
                case Panel.Orders: DrawOrders(width); break;
                case Panel.Chart: DrawChart(width); break;
                case Panel.Log: DrawLog(width); break;
                case Panel.Pause: DrawPause(width); break;
                case Panel.Settings: DrawSettings(new Rect(width / 2 - 380, 200, 760, 640)); break;
            }

            if (_g.Story.Ending)
            {
                float b = _g.Weather.Blackout;
                if (b > 0) { Shade(new Rect(0, 0, width, H), b); }
            }
        }

        void Shadowed(Rect r, string text, GUIStyle s)
        {
            var c = s.normal.textColor;
            s.normal.textColor = new Color(0, 0, 0, 0.8f);
            GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), text, s);
            s.normal.textColor = c;
            GUI.Label(r, text, s);
        }

        void Subtitle(float width, Line line, float y)
        {
            string who = Dialogue.SpeakerNames.TryGetValue(line.Speaker, out var n) ? n : line.Speaker;
            string tag = line.Channel switch { Channel.Radio => "VHF 16", Channel.Phone => "PHONE", Channel.Broadcast => "AM", _ => "" };
            string header = string.IsNullOrEmpty(tag) ? who.ToUpperInvariant() : $"{who.ToUpperInvariant()}  ·  {tag}";
            float h = _sub.CalcHeight(new GUIContent(line.Text), 1200);
            Shade(new Rect(width / 2 - 640, y - 6, 1280, h + 48), 0.5f);
            GUI.Label(new Rect(width / 2 - 600, y, 1200, 28), header, _speaker);
            GUI.Label(new Rect(width / 2 - 600, y + 32, 1200, h), line.Text, _sub);
        }

        void DrawReadouts(float width)
        {
            var s = _g.Ship;
            string text =
                $"{GameClock.Format(_g.Clock.Minutes)}\n" +
                $"Heading {s.Heading:000}°   Rudder {Mathf.Abs(s.Rudder):0}° {(s.Rudder < -0.5f ? "P" : s.Rudder > 0.5f ? "S" : "")}\n" +
                $"{Ship.OrderName(s.Order)}   {s.SpeedMadeGood:0.0} kn\n" +
                $"List {Mathf.Abs(s.List):0.0}° {(s.List > 0 ? "stbd" : "")}   Wind {_g.Weather.WindKnots:0} kn\n" +
                (s.Wheelsman ? "Wheelsman has the helm" : "");
            var r = new Rect(width - 420, 40, 380, 160);
            Shade(new Rect(r.x - 14, r.y - 10, r.width + 28, r.height + 10), 0.4f);
            GUI.Label(r, text, _body);
        }

        void DrawOptions(Rect area, System.Collections.Generic.List<Story.Option> opts, float y)
        {
            for (int i = 0; i < opts.Count; i++)
            {
                var r = new Rect(area.x + 130, y + i * 44, area.width - 160, 42);
                string label = $"{(i == 9 ? 0 : i + 1)}.  {opts[i].Label}";
                if (opts[i].Enabled)
                {
                    if (GUI.Button(r, label, _inkOption)) { _panel = Panel.None; opts[i].Do(); return; }
                }
                else GUI.Label(r, label, _inkOptionOff);
            }
        }

        /// <summary>A page torn from the deck notebook, for the radio, the orders and the log.</summary>
        Rect Page(Rect area, string title)
        {
            GUI.DrawTexture(area, _paper, ScaleMode.StretchToFill);
            GUI.Label(new Rect(area.x + 130, area.y + 26, area.width - 160, 48), title, _inkTitle);
            return area;
        }

        void DrawRadio(float width)
        {
            var opts = _g.Story.RadioCalls();
            var area = new Rect(width - 790, 80, 740, 360 + opts.Count * 44);
            Page(area, "VHF  ch. 16");
            float x = area.x + 130, w = area.width - 160;
            bool busy = _g.Dialogue.Busy;
            GUI.Label(new Rect(x, area.y + 76, w, 30), busy ? "channel busy, wait for the traffic to clear" : "pick up the handset and call:", _inkSmall);
            DrawOptions(area, opts, area.y + 112);

            float y = area.y + 126 + opts.Count * 44;
            GUI.DrawTexture(new Rect(x, y, w, 2), Solid(new Color(0.3f, 0.28f, 0.3f, 0.5f)));
            y += 14;
            GUI.Label(new Rect(x, y, 400, 40), "AM receiver", new GUIStyle(_inkTitle) { fontSize = 32 });
            if (GUI.Button(new Rect(area.x + area.width - 190, y, 160, 40), _g.Radio.AmOn ? "[O]  on" : "[O]  off", _inkOption)) ToggleAm();
            y += 50;
            if (_g.Radio.AmOn)
            {
                if (GUI.Button(new Rect(x, y, 60, 36), "<<", _inkOption)) _g.Radio.Nudge(-50);
                if (GUI.Button(new Rect(x + 60, y, 50, 36), "<", _inkOption)) _g.Radio.Nudge(-10);
                GUI.Label(new Rect(x + 116, y, 200, 36), $"{_g.Radio.AmKhz:0} kc", new GUIStyle(_ink) { alignment = TextAnchor.MiddleCenter, fontSize = 28 });
                if (GUI.Button(new Rect(x + 322, y, 50, 36), ">", _inkOption)) _g.Radio.Nudge(10);
                if (GUI.Button(new Rect(x + 372, y, 60, 36), ">>", _inkOption)) _g.Radio.Nudge(50);
                GUI.Label(new Rect(x + 440, y, 200, 36), _g.Radio.StationName(), _inkSmall);
                y += 44;
                float k = GUI.HorizontalSlider(new Rect(x, y + 6, w, 20), _g.Radio.AmKhz, 540, 1600);
                if (Mathf.Abs(k - _g.Radio.AmKhz) > 0.5f) _g.Radio.AmKhz = Mathf.Round(k);
                y += 34;
                GUI.Label(new Rect(x, y, w, 30), "Q / E to tune, shift for big steps", _inkSmall);
            }
            GUI.Label(new Rect(x, area.yMax - 40, w, 30), "number keys to call  ·  R or Esc to close", _inkSmall);
        }

        void DrawOrders(float width)
        {
            var opts = _g.Story.Orders();
            var area = new Rect(50, 100, 720, 190 + opts.Count * 44);
            Page(area, "Orders");
            float x = area.x + 130, w = area.width - 160;
            GUI.Label(new Rect(x, area.y + 76, w, 30), _g.Dialogue.Busy ? "wait for the crew to answer" : "through the phone, or over your shoulder:", _inkSmall);
            DrawOptions(area, opts, area.y + 112);
            GUI.Label(new Rect(x, area.yMax - 40, w, 30), "number keys to order  ·  T or Esc to close", _inkSmall);
        }

        /// <summary>Every radio call and phone message so far, newest at the bottom.</summary>
        void DrawLog(float width)
        {
            var area = new Rect(width / 2 - 400, 50, 800, H - 100);
            Page(area, "Radio & phone log   Nov 10");
            float x = area.x + 130, w = area.width - 160;
            var log = _g.Dialogue.Log;
            if (Event.current.type == EventType.ScrollWheel) { _logScroll -= (int)Mathf.Sign(Event.current.delta.y) * 2; Event.current.Use(); }
            // lay the entries out from the bottom up so the newest is always in view
            float top = area.y + 84, bottom = area.yMax - 48;
            var rows = new System.Collections.Generic.List<(string time, string who, string text, float h)>();
            float total = 0;
            for (int i = log.Count - 1; i >= 0; i--)
            {
                var (mins, line) = log[i];
                if (line.Channel == Channel.Broadcast) continue;
                string who = Dialogue.SpeakerNames.TryGetValue(line.Speaker, out var n) ? n : line.Speaker;
                if (line.Speaker == "CAPTAIN") who = "me";
                string tag = line.Channel == Channel.Radio ? " (VHF)" : line.Channel == Channel.Phone ? " (phone)" : "";
                string text = line.Text;
                float h = _ink.CalcHeight(new GUIContent(text), w - 10) + 6;
                rows.Add((GameClock.Format(mins).Replace(" PM", ""), who + tag, text, h));
                total += h + 22;
            }
            _logScroll = Mathf.Clamp(_logScroll, 0, Mathf.Max(0, rows.Count - 1));
            float y = bottom;
            int skipped = 0;
            foreach (var r in rows)
            {
                if (skipped++ < _logScroll) continue;
                y -= r.h + 22;
                if (y < top) break;
                GUI.Label(new Rect(x, y, 200, 24), r.time + "   " + r.who, _inkTime);
                GUI.Label(new Rect(x, y + 20, w - 10, r.h), r.text, _ink);
            }
            if (rows.Count == 0) GUI.Label(new Rect(x, top + 20, w, 40), "nothing yet", _inkSmall);
            GUI.Label(new Rect(x, area.yMax - 40, w, 30), "scroll or PgUp / PgDn  ·  L or Esc to close", _inkSmall);
        }

        void DrawChart(float width)
        {
            var tex = _g.Wheelhouse.ChartTexture;
            float h = H - 160, w = h * 1.39f;
            var r = new Rect(width / 2 - w / 2, 70, w, h);
            Shade(new Rect(0, 0, width, H), 0.6f);
            GUI.DrawTexture(r, tex, ScaleMode.StretchToFill);
            GUI.Label(new Rect(r.x, r.yMax + 12, r.width, 30), "Lake Superior, eastern part. Your track in pencil.  M or Esc to close.", new GUIStyle(_small) { alignment = TextAnchor.UpperCenter });
        }

        void DrawPause(float width)
        {
            Shade(new Rect(0, 0, width, H), 0.6f);
            var x = width / 2 - 300;
            GUI.Label(new Rect(x, 280, 600, 80), "Paused", new GUIStyle(_title) { fontSize = 64 });
            if (Button(new Rect(x, 400, 600, 56), "Resume")) _panel = Panel.None;
            if (Button(new Rect(x, 466, 600, 56), "Settings")) { _settingsReturn = Panel.Pause; _panel = Panel.Settings; }
            if (Button(new Rect(x, 532, 600, 56), "Leave the ship (title screen)")) _g.ReturnToTitle();
            if (Button(new Rect(x, 598, 600, 56), "Quit")) Application.Quit();
            GUI.Label(new Rect(x, 700, 900, 200),
                "A/D or ←/→  steer      W/S or ↑/↓  engine telegraph\nMouse  look      Click  use      H  whistle\nT  orders      R  radio      M  chart      F1  readouts", _small);
        }

        void DrawSettings(Rect area)
        {
            GUI.Box(area, GUIContent.none, _box);
            float y = area.y + 24;
            float x = area.x + 28, w = area.width - 56;
            GUI.Label(new Rect(x, y, w, 40), "Settings", new GUIStyle(_button) { fontSize = 32 });
            y += 60;
            _g.MasterVolume = Slider(x, ref y, w, "Volume", _g.MasterVolume, 0, 1);
            _g.MusicVolume = Slider(x, ref y, w, "Radio music", _g.MusicVolume, 0, 1);
            _g.Sensitivity = Slider(x, ref y, w, "Mouse sensitivity", _g.Sensitivity, 0.2f, 3f);
            _g.InvertY = GUI.Toggle(new Rect(x, y, w, 30), _g.InvertY, "  Invert mouse Y", _toggle); y += 40;
            _g.Subtitles = GUI.Toggle(new Rect(x, y, w, 30), _g.Subtitles, "  Subtitles", _toggle); y += 40;
            _g.Hints = GUI.Toggle(new Rect(x, y, w, 30), _g.Hints, "  Hints", _toggle); y += 40;
            _g.Readouts = GUI.Toggle(new Rect(x, y, w, 30), _g.Readouts, "  Instrument readouts on screen (F1)", _toggle); y += 44;
            GUI.Label(new Rect(x, y, 260, 30), "Screen (F11)", _label);
            y += 36;
            float bx = x;
            for (int i = 0; i <= Game.WindowSizes.Length; i++)
            {
                string name = i == 0 ? "Fullscreen" : $"{Game.WindowSizes[i - 1].x} × {Game.WindowSizes[i - 1].y}";
                float bw = i == 0 ? 160 : 136;
                bool on = _g.DisplayMode == i;
                if (GUI.Button(new Rect(bx, y, bw, 36), (on ? "● " : "○ ") + name, on ? _screenOn : _screenOff) && Time.unscaledTime - _panelSince > 0.3f) { _g.DisplayMode = i; _g.ApplyDisplay(); _g.SaveSettings(); }
                bx += bw;
            }
            y += 56;
            AudioListener.volume = _g.MasterVolume;
            if (Button(new Rect(x, y, 300, 50), "Back")) { _g.SaveSettings(); _panel = _settingsReturn; }
        }

        float Slider(float x, ref float y, float w, string label, float v, float min, float max)
        {
            GUI.Label(new Rect(x, y, 260, 30), label, _label);
            v = GUI.HorizontalSlider(new Rect(x + 270, y + 10, w - 270, 20), v, min, max);
            y += 44;
            return v;
        }

        void DrawMemorial(float width)
        {
            GUI.DrawTexture(new Rect(0, 0, width, H), _black);
            float t = Time.time - _memorialStart;
            string[] paras =
            {
                "At about 7:15 PM on November 10, 1975, the SS Edmund Fitzgerald sank in Lake Superior, seventeen miles from Whitefish Point, in 530 feet of water.",
                "She never sent a distress call. All twenty-nine men aboard were lost.",
                "The Arthur M. Anderson, which had followed her all day, reached shelter in Whitefish Bay and then, at the Coast Guard's request, turned back out into the storm to search.",
                _g.Story.Summary(),
                "Every November 10th her bell, recovered from the wreck in 1995, is rung twenty-nine times, and once more for everyone lost on the Great Lakes.",
            };
            float y = 150;
            for (int i = 0; i < paras.Length; i++)
            {
                float a = Mathf.Clamp01((t - 1.5f - i * 3f) / 2f);
                var c = GUI.color;
                GUI.color = new Color(1, 1, 1, a);
                float h = _memorial.CalcHeight(new GUIContent(paras[i]), 1200);
                GUI.Label(new Rect(width / 2 - 600, y, 1200, h), paras[i], _memorial);
                GUI.color = c;
                y += h + 30;
            }
            if (_bells > 0)
            {
                string count = _bells <= 29 ? _bells.ToString() : "and once more";
                GUI.Label(new Rect(width / 2 - 300, H - 170, 600, 50), count, new GUIStyle(_memorial) { fontSize = 40, alignment = TextAnchor.MiddleCenter });
            }
            if (t > 4f)
            {
                string prompt = _bells >= 30 || _memorialSkipped ? "Press Enter to return" : "Enter to skip";
                GUI.Label(new Rect(width / 2 - 300, H - 80, 600, 40), prompt, new GUIStyle(_small) { alignment = TextAnchor.MiddleCenter });
            }
        }
    }
}
