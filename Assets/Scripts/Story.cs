using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fitz
{
    /// <summary>
    /// The afternoon and evening of November 10, 1975, as a timeline. The player can
    /// steer, give orders and use the radio, and the story bends to what they do,
    /// but it always ends the same way.
    /// </summary>
    public class Story : MonoBehaviour
    {
        Game _g;
        Ship Ship => _g.Ship;
        Dialogue Dlg => _g.Dialogue;

        class Beat
        {
            public float At;
            public Func<bool> When;
            public Action Do;
            public bool Fired;
        }

        readonly List<Beat> _beats = new();

        // what has happened
        public bool Damaged, DamageReported, RadarsLost, RadarHelpAsked, WhitefishDark, BeaconAsked, AvaforsTalked;
        public bool AwaitingFinalReply, FinalDone, Ending, EndingDone;
        public int MaydayCount;
        public bool SaidHoldingOurOwn;
        public Telegraph AnsweredOrder = Telegraph.FullAhead;
        float _finalAskedAt;
        float _sprayCooldown;
        float _slamCooldown;
        float _answerAt = -1;
        // Caribou Island, the course line, and the small talk of a long watch
        bool _avoiding, _islandPointedOut;
        float _avoidTurn;
        float _offCourseTimer;
        int _offCourseCalls;
        float _idleTimer = 80f;
        readonly HashSet<string> _idleUsed = new();

        public void Init(Game g)
        {
            _g = g;
            AnsweredOrder = Ship.Order;

            At(14, 41, () => Dlg.SayAll(null, "intro_mate_01", "intro_capt_01", "intro_mate_02"));
            At(14, 52, () => Dlg.SayAll(null, "and_0252_01", "capt_and_0252_01", "and_0252_02", "capt_and_0252_02", "and_0252_03"));
            At(15, 5, () => Dlg.Say("chief_0305_01"));
            At(15, 20, Shoal);
            At(15, 30, DamageFound);
            At(15, 47, () => Dlg.SayAll(null, "and_dmg_late_01", "capt_dmg_01", "and_dmg_01", Ship.PumpsOn ? "capt_dmg_02" : "capt_dmg_02b", "and_dmg_02"),
                () => !DamageReported, () => { DamageReported = true; StartPumpsQuietly(); });
            At(16, 10, RadarFailure);
            At(16, 26, () => Dlg.SayAll(null, "and_radar_late_01", "capt_radar_01", "and_radar_01"), () => !RadarHelpAsked, () => RadarHelpAsked = true);
            At(16, 34, () => Dlg.SayAll(null, "and_nav_01", "capt_nav_01"));
            At(16, 40, () => { WhitefishDark = true; Dlg.Say("mate_whitefish_01"); });
            At(17, 6, () => Dlg.SayAll(null, "ava_call_01", "capt_ava_02", "capt_ava_aside", "ava_02"), () => !AvaforsTalked, () => AvaforsTalked = true);
            At(17, 20, () => Dlg.Say("chief_0520_01"));
            At(17, 45, BigSeas);
            At(18, 15, () => Dlg.Say("cg_secur_01"));
            At(18, 30, () => { Dlg.Say("chief_0630_01"); Ship.IngressPerMin = 0.0034f; });
            At(18, 45, () => Dlg.SayAll(null, "and_0645_01", "capt_0645_01"));
            At(18, 52, () => { HitBySea(1f); });
            At(18, 53, () => { HitBySea(1f); });
            At(19, 0, () => Dlg.Say("mate_0700_01"));
            At(19, 10, FinalExchange);
        }

        void At(int h, int m, Action a, Func<bool> when = null, Action after = null)
        {
            _beats.Add(new Beat { At = GameClock.At(h, m), When = when, Do = () => { a(); after?.Invoke(); } });
        }

        /// <summary>Skip straight to a time (for testing): marks earlier beats as done.</summary>
        public void FastForwardTo(float minutes)
        {
            foreach (var b in _beats)
                if (b.At < minutes) b.Fired = true;
            if (minutes > GameClock.At(15, 20)) { Ship.IngressPerMin = 0.0022f; Damaged = true; DamageReported = true; Ship.Flooding = (minutes - GameClock.At(15, 30)) * 0.0011f; }
            if (minutes > GameClock.At(16, 10)) { RadarsLost = true; RadarHelpAsked = true; Ship.RadarsWorking = false; }
            if (minutes > GameClock.At(16, 40)) { WhitefishDark = true; BeaconAsked = true; }
            if (minutes > GameClock.At(17, 6)) AvaforsTalked = true;
            if (minutes > GameClock.At(17, 45)) Ship.IngressPerMin = 0.003f;
            Ship.PumpsOn = minutes > GameClock.At(15, 35);
            // put her roughly where she would have been
            float t = Mathf.InverseLerp(GameClock.StartMinutes, GameClock.SinkMinutes, minutes);
            Ship.NavNm = Vector2.Lerp(Charts.StartPosition, Vector2.zero, t);
            Ship.Track.Clear();
            Ship.Track.Add(Charts.StartPosition);
            Ship.Track.Add(Ship.NavNm);
        }

        // ------------------------------------------------------------------ beats

        void Shoal()
        {
            _g.Audio.Play(_g.Audio.ScrapeClip, 1f);
            Ship.Jolt(-2.5f, 0.6f, -0.4f);
            Ship.IngressPerMin = 0.0010f;
            Damaged = false;
            StartCoroutine(Delay(4.5f, () => Dlg.SayAll(null, "mate_shoal_01", "mate_shoal_02")));
            _g.UI.Hint("Something's wrong. Order the tanks sounded [T].", 40);
        }

        void DamageFound()
        {
            Damaged = true;
            Ship.IngressPerMin = 0.0022f;
            Dlg.SayAll(() => _g.UI.Hint("Report the damage to the Anderson [R]. Get the pumps going [T].", 90), "watch_0330_01", "watch_0330_02");
        }

        void RadarFailure()
        {
            RadarsLost = true;
            Ship.RadarsWorking = false;
            _g.Audio.Play(_g.Audio.ClickClip, 1f);
            Dlg.SayAll(() => _g.UI.Hint("You're blind. Ask the Anderson to keep track of you [R].", 90), "mate_radar_01", "mate_radar_02");
        }

        void BigSeas()
        {
            Ship.IngressPerMin = 0.0030f;
            HitBySea(1f);
            StartCoroutine(Delay(2.5f, () => Dlg.SayAll(null, "watch_0545_01", "mate_0545_01")));
        }

        void HitBySea(float strength)
        {
            Ship.Jolt(-4f * strength, 1.5f * strength, -0.8f * strength);
            _g.Audio.Play(_g.Audio.SlamClip, strength);
            _g.Audio.Play(_g.Audio.CrashClip, 0.8f * strength);
            _g.Weather.SprayBurst(ShipModel.BowSpray.position, -Ship.transform.forward, strength);
            _g.Weather.SprayOnGlass = 1f;
            _g.Audio.Ambience.SeaWash = 1f;
        }

        void FinalExchange()
        {
            Dlg.Say("and_final_01", () =>
            {
                AwaitingFinalReply = true;
                _finalAskedAt = Time.time;
                _g.UI.Hint("Answer the Anderson [R].", 30);
            });
        }

        public void ReplyFinal(bool mayday)
        {
            if (!AwaitingFinalReply) return;
            AwaitingFinalReply = false;
            _g.UI.CloseRadio();
            if (mayday)
            {
                MaydayCount++;
                Dlg.SayAll(() => { FinalDone = true; }, "capt_mayday_01", "and_mayday_01");
            }
            else
            {
                SaidHoldingOurOwn = true;
                Dlg.SayAll(() => { FinalDone = true; }, "capt_final_01", "and_final_02");
            }
        }

        IEnumerator Delay(float s, Action a)
        {
            yield return new WaitForSeconds(s);
            a();
        }

        void StartPumpsQuietly()
        {
            if (!Ship.PumpsOn) Ship.PumpsOn = true;
        }

        // ------------------------------------------------------------------ per frame

        public void Tick(float dt)
        {
            float now = _g.Clock.Minutes;

            // storm and weather curves
            float hs = Curve(now, (14 * 60 + 40, 4.6f), (15 * 60 + 30, 5.6f), (16 * 60 + 30, 6.8f), (17 * 60 + 30, 8.2f), (18 * 60 + 30, 9.6f), (19 * 60, 10.8f));
            _g.Ocean.SignificantHeight = hs;
            _g.Ocean.Storm = Mathf.Clamp01((hs - 4f) / 5f);
            _g.Weather.Storm = _g.Ocean.Storm;
            _g.Weather.Snow = Mathf.Clamp01(Bump(now, 15 * 60 + 55, 14) + Bump(now, 16 * 60 + 45, 20) + Bump(now, 17 * 60 + 40, 12) + Bump(now, 18 * 60 + 35, 16) + Bump(now, 19 * 60 + 8, 8) * 0.7f + (now > 15 * 60 ? 0.15f : 0f));
            _g.Weather.Rain = Mathf.Clamp01(Mathf.InverseLerp(15 * 60 + 25, 14 * 60 + 40, now) * 0.5f);

            if (!Ending)
            {
                foreach (var b in _beats)
                {
                    if (b.Fired || now < b.At) continue;
                    b.Fired = true;
                    if (b.When == null || b.When()) b.Do();
                }
            }

            // engine room answers the telegraph
            if (AnsweredOrder != Ship.Order && _answerAt < 0) _answerAt = Time.time + 1.6f;
            if (_answerAt > 0 && Time.time >= _answerAt)
            {
                _answerAt = -1;
                AnsweredOrder = Ship.Order;
                _g.Audio.Play(_g.Audio.TelegraphClip, 0.7f);
            }

            // spray over the bow and seas over the deck
            _sprayCooldown -= dt;
            _slamCooldown -= dt;
            float imm = Ship.BowImmersion;
            if (imm > -1.2f && _sprayCooldown <= 0)
            {
                float strength = Mathf.Clamp01((imm + 1.2f) / 3f) * (0.5f + _g.Ocean.Storm * 0.6f);
                _g.Weather.SprayBurst(ShipModel.BowSpray.position, -Ship.transform.forward, Mathf.Clamp01(strength));
                _g.Weather.SprayOnGlass = Mathf.Max(_g.Weather.SprayOnGlass, strength);
                _sprayCooldown = UnityEngine.Random.Range(1.5f, 3.5f);
                if (strength > 0.5f && _slamCooldown <= 0)
                {
                    _g.Audio.Play(_g.Audio.SlamClip, strength * 0.7f);
                    _slamCooldown = 6f;
                }
            }
            var amb = _g.Audio.Ambience;
            amb.SeaWash = Mathf.MoveTowards(amb.SeaWash, Mathf.Clamp01(imm * 0.3f + 0.2f) * _g.Ocean.Storm, dt * 0.6f);

            if (!Ending)
            {
                Caribou(now);
                OffCourse(now, dt);
                Idle(dt);
            }

            // the last reply: if the player says nothing, the captain answers as he did
            if (AwaitingFinalReply && Time.time - _finalAskedAt > 20f) ReplyFinal(false);

            if (!Ending && (FinalDone || now >= GameClock.SinkMinutes + 3f) && !Dlg.Busy)
                StartCoroutine(EndSequence());
        }

        /// <summary>
        /// If the player points her at Caribou Island the wheelsman takes the wheel, swings
        /// her clear and only hands it back once the island is past the beam.
        /// </summary>
        void Caribou(float now)
        {
            var d = Charts.CaribouIsland - Ship.NavNm;
            float dist = d.magnitude;
            float brg = Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg;
            float rel = Mathf.DeltaAngle(Ship.Heading, brg);
            if (!_islandPointedOut && dist < 5.5f && Mathf.Abs(rel) < 100f && now > GameClock.At(15, 0) && !Dlg.Busy)
            {
                _islandPointedOut = true;
                Dlg.Say("mate_island_01");
            }
            if (!_avoiding)
            {
                float ahead = dist * Mathf.Cos(rel * Mathf.Deg2Rad), lateral = dist * Mathf.Sin(rel * Mathf.Deg2Rad);
                if (ahead > 0 && ahead < 5f && Mathf.Abs(lateral) < Land.CaribouRadiusNm + 0.45f)
                {
                    _avoiding = true;
                    _avoidTurn = lateral < 0 ? 1f : -1f;        // island to port: come starboard, and vice versa
                    Ship.HelmLocked = true;
                    Ship.Wheelsman = true;
                    Dlg.SayAll(null, "wheel_takes_01", "capt_wheel_takes");
                    _g.UI.Hint("The wheelsman has the helm until she's clear. Take a look at the chart [M].", 30);
                }
            }
            else
            {
                Ship.OrderedHeading = Mathf.Repeat(brg + _avoidTurn * 85f, 360f);
                bool clear = (Mathf.Abs(rel) > 80f && dist > Land.CaribouRadiusNm + 1.0f) || dist > Land.CaribouRadiusNm + 2.5f;
                if (clear && !Dlg.Busy)
                {
                    _avoiding = false;
                    Ship.OrderedHeading = 141f;
                    Dlg.Say("wheel_clear_01", () => { Ship.HelmLocked = false; Ship.Wheelsman = false; Ship.WheelInput = 0; });
                }
            }
        }

        /// <summary>Wander off the line to Whitefish for long enough and the Anderson calls about it. Twice.</summary>
        void OffCourse(float now, float dt)
        {
            if (_avoiding || now < GameClock.At(15, 0) || _offCourseCalls >= 2) return;
            var to = Charts.WhitefishPoint - Ship.NavNm;
            float brg = Mathf.Atan2(to.x, to.y) * Mathf.Rad2Deg;
            float err = Mathf.Abs(Mathf.DeltaAngle(Ship.Heading, brg));
            if (err > 45f) _offCourseTimer += dt; else _offCourseTimer = Mathf.Max(-60f, _offCourseTimer - dt * 2f);
            if (_offCourseTimer > 40f && !Dlg.Busy && !AwaitingFinalReply)
            {
                _offCourseTimer = -90f;      // and leave it a while before nagging again
                _offCourseCalls++;
                if (_offCourseCalls == 1) Dlg.SayAll(null, "and_offcourse_01", "capt_offcourse_01");
                else Dlg.SayAll(null, "and_offcourse_02", "capt_offcourse_02");
                _g.UI.Hint("Come back to one-four-one for Whitefish [A/D], or give the wheelsman the wheel [T].", 25);
            }
        }

        static readonly string[] IdleDay = { "idle_mate_01", "idle_wheel_01", "idle_chief_01", "idle_mate_03", "idle_and_01", "idle_mate_02", "idle_watch_01", "idle_mate_05", "idle_wheel_02", "idle_mate_04", "idle_chief_02", "idle_mate_06", "idle_wheel_03", "idle_and_02" };
        static readonly string[] IdleNight = { "idle_mate_night_01", "idle_wheel_night_01", "idle_and_02", "idle_mate_night_02", "idle_chief_02", "idle_mate_05", "idle_watch_01", "idle_wheel_03", "idle_wheel_02" };

        /// <summary>Small talk to fill the quiet stretches, each line once.</summary>
        void Idle(float dt)
        {
            if (Dlg.Busy || AwaitingFinalReply) return;
            _idleTimer -= dt;
            if (_idleTimer > 0) return;
            _idleTimer = UnityEngine.Random.Range(50f, 85f);
            var pool = _g.Weather.Daylight < 0.3f ? IdleNight : IdleDay;
            foreach (var id in pool)
            {
                if (_idleUsed.Contains(id)) continue;
                if (id == "idle_chief_02" && !Damaged) continue;
                if (id == "idle_wheel_03" && _g.Ocean.Storm < 0.5f) continue;
                _idleUsed.Add(id);
                if (id == "idle_and_01") Dlg.SayAll(null, id, "capt_and_riding");
                else if (id == "idle_and_02") Dlg.SayAll(null, id, "capt_and_ack");
                else Dlg.Say(id);
                return;
            }
        }

        static float Bump(float x, float center, float width) => Mathf.Exp(-((x - center) * (x - center)) / (2 * width * width));

        static float Curve(float x, params (float t, float v)[] pts)
        {
            if (x <= pts[0].t) return pts[0].v;
            for (int i = 1; i < pts.Length; i++)
                if (x <= pts[i].t) return Mathf.Lerp(pts[i - 1].v, pts[i].v, Mathf.InverseLerp(pts[i - 1].t, pts[i].t, x));
            return pts[pts.Length - 1].v;
        }

        IEnumerator EndSequence()
        {
            Ending = true;
            _g.UI.CloseAll();
            _g.UI.Hint("", 0);
            yield return new WaitForSeconds(2f);
            // the first of the two seas; the engine room is already in trouble
            HitBySea(1f);
            _g.Audio.Play(_g.Audio.GroanClip, 1f);
            yield return new WaitForSeconds(2.5f);
            Dlg.Say("chief_end_01");
            yield return new WaitForSeconds(4f);
            // the second one. She doesn't come up.
            HitBySea(1.3f);
            _g.Audio.Play(_g.Audio.GroanClip, 1f);
            _g.Audio.Play(_g.Audio.CrackClip, 0.9f);
            Ship.HelmLocked = true;
            yield return new WaitForSeconds(1.5f);
            Dlg.Say("watch_end_01");
            yield return new WaitForSeconds(2.5f);
            Dlg.Say("mate_end_01");
            // time thickens; you turn and look at the photograph as the lights go
            Time.timeScale = 0.4f;
            _g.LookToward(Wheelhouse.PhotoPosition, 6f);
            float t = 0;
            float nextCrack = 2.5f;
            const float dive = 14f;
            while (t < dive)
            {
                t += Time.unscaledDeltaTime;
                Ship.NoseDive = Mathf.Clamp01(t / dive);
                _g.Audio.Ambience.Rumble = Mathf.Clamp01(t / 4f);
                _g.Audio.Ambience.Muffle = Mathf.Clamp01((t - 7f) / 6f);
                if (t > nextCrack)
                {
                    _g.Audio.Play(_g.Audio.CrackClip, UnityEngine.Random.Range(0.5f, 1f));
                    _g.Audio.Play(_g.Audio.GroanClip, 0.8f);
                    Ship.Jolt(UnityEngine.Random.Range(-1.5f, 1.5f), 0.6f, -0.3f);
                    nextCrack += UnityEngine.Random.Range(1.8f, 3.2f);
                }
                if (t > 5f && t < 8.5f) Ship.Power = UnityEngine.Random.value > 0.3f;
                if (t >= 8.5f) Ship.Power = false;
                _g.Weather.Blackout = Mathf.Clamp01((t - 9f) / 4f);
                yield return null;
            }
            Time.timeScale = 1f;
            Dlg.StopAll();
            t = 0;
            while (t < 3f)
            {
                t += Time.deltaTime;
                AudioListener.volume = Mathf.Lerp(1f, 0f, t / 3f) * _g.MasterVolume;
                yield return null;
            }
            _g.ReleaseLook();
            _g.Audio.Ambience.Master = 0;
            _g.Audio.Am.Volume = 0;
            AudioListener.volume = _g.MasterVolume;
            EndingDone = true;
            _g.ShowMemorial();
        }

        // ------------------------------------------------------------------ radar

        public IEnumerable<Vector2> RadarTargets()
        {
            yield return AndersonPosition();
        }

        /// <summary>The Anderson follows the Fitzgerald's track about ten miles astern.</summary>
        public Vector2 AndersonPosition()
        {
            var tr = Ship.Track;
            float need = 10f;
            var prev = Ship.NavNm;
            for (int i = tr.Count - 1; i >= 0; i--)
            {
                float seg = Vector2.Distance(prev, tr[i]);
                if (seg >= need) return Vector2.Lerp(prev, tr[i], need / seg);
                need -= seg;
                prev = tr[i];
            }
            // not enough track yet: she's further back along the course line
            var back = (Charts.StartPosition - Charts.WhitefishPoint).normalized;
            return tr.Count > 0 ? tr[0] + back * need : Ship.NavNm + back * 10f;
        }

        // ------------------------------------------------------------------ player actions

        public struct Option
        {
            public string Label;
            public Action Do;
            public bool Enabled;
        }

        public List<Option> RadioCalls()
        {
            var list = new List<Option>();
            bool free = !Dlg.Busy && !Ending;
            if (AwaitingFinalReply)
            {
                list.Add(new Option { Label = "\"We are holding our own.\"", Do = () => ReplyFinal(false), Enabled = true });
                list.Add(new Option { Label = "MAYDAY", Do = () => ReplyFinal(true), Enabled = true });
                return list;
            }
            if (Damaged && !DamageReported)
                list.Add(new Option { Label = "Anderson: report the damage", Enabled = free, Do = () =>
                {
                    DamageReported = true;
                    bool pumps = Ship.PumpsOn;
                    Dlg.SayAll(() => _g.UI.Hint("", 0), "capt_dmg_01", "and_dmg_01", pumps ? "capt_dmg_02" : "capt_dmg_02b", "and_dmg_02");
                    StartPumpsQuietly();
                } });
            else if (RadarsLost && !RadarHelpAsked)
                list.Add(new Option { Label = "Anderson: ask them to keep track of you", Enabled = free, Do = () =>
                {
                    RadarHelpAsked = true;
                    Dlg.SayAll(() => _g.UI.Hint("", 0), "capt_radar_01", "and_radar_01");
                } });
            else
                list.Add(new Option { Label = "Anderson: check in", Enabled = free, Do = () =>
                {
                    float now = _g.Clock.Minutes;
                    string reply = now < GameClock.At(16, 30) ? "and_checkin_early" : now < GameClock.At(18, 0) ? "and_checkin_mid" : "and_checkin_late";
                    Dlg.SayAll(null, "capt_and_checkin", reply);
                } });

            if (WhitefishDark && !BeaconAsked)
                list.Add(new Option { Label = "Coast Guard: ask about Whitefish Point", Enabled = free, Do = () =>
                {
                    BeaconAsked = true;
                    Dlg.SayAll(() => _g.UI.Hint("", 0), "capt_cg_beacon_01", "cg_beacon_01", "capt_cg_beacon_02");
                } });
            else
                list.Add(new Option { Label = "Coast Guard: ask for the weather", Enabled = free, Do = () => Dlg.SayAll(null, "capt_cg_weather", "cg_weather_01") });

            if (_g.Clock.Minutes > GameClock.At(16, 50) && !AvaforsTalked)
                list.Add(new Option { Label = "Any vessel near Whitefish Point", Enabled = free, Do = () =>
                {
                    AvaforsTalked = true;
                    Dlg.SayAll(null, "capt_ava_01", "ava_01", "capt_ava_02", "capt_ava_aside", "ava_02");
                } });

            list.Add(new Option { Label = "MAYDAY", Enabled = free, Do = () =>
            {
                MaydayCount++;
                if (MaydayCount > 1) Dlg.SayAll(null, "capt_mayday_01", "cg_mayday_repeat");
                else if (!Damaged) Dlg.SayAll(null, "capt_mayday_01", "cg_mayday_early", "cg_mayday_01");
                else Dlg.SayAll(null, "capt_mayday_01", "cg_mayday_01", "and_mayday_01");
            } });
            return list;
        }

        public List<Option> Orders()
        {
            bool free = !Dlg.Busy && !Ending;
            var list = new List<Option>
            {
                new() { Label = Ship.PumpsOn ? "Engine room: secure the ballast pumps" : "Engine room: start the ballast pumps", Enabled = free, Do = () => IssueOrder("pumps") },
                new() { Label = "Deck watch: sound the tanks", Enabled = free, Do = () => IssueOrder("sound") },
                new() { Label = "Deck watch: damage report", Enabled = free, Do = () => IssueOrder("damage") },
                new() { Label = "Chief: how's she running?", Enabled = free, Do = () => IssueOrder("engine") },
                new() { Label = "Mate: what's on the radar?", Enabled = free, Do = () => IssueOrder("radar") },
                new() { Label = "Mate: where are we?", Enabled = free, Do = () => IssueOrder("position") },
                new() { Label = Ship.Wheelsman ? "Take the wheel back yourself" : "Wheelsman: take the wheel, hold this heading", Enabled = free, Do = () => IssueOrder("wheel") },
                new() { Label = "Nobody goes out on deck", Enabled = free, Do = () => IssueOrder("nodeck") },
                new() { Label = "Galley: send up some coffee", Enabled = free, Do = () => IssueOrder("coffee") },
            };
            if (_g.Clock.Minutes > GameClock.At(18, 30))
                list.Add(new Option { Label = "All hands: life jackets", Enabled = free, Do = () => IssueOrder("lifejackets") });
            return list;
        }

        bool _pumpsOrderInFlight;

        public void IssueOrder(string key)
        {
            if (Dlg.Busy || Ending) return;
            _g.UI.CloseAll();
            switch (key)
            {
                case "pumps":
                    if (_pumpsOrderInFlight) return;
                    if (!Ship.PumpsOn)
                    {
                        _pumpsOrderInFlight = true;
                        Dlg.SayAll(() => { Ship.PumpsOn = true; _pumpsOrderInFlight = false; }, "capt_order_pumps", "chief_pumps_ok");
                    }
                    else Dlg.SayAll(() => Ship.PumpsOn = false, "capt_order_pumps_off", "chief_pumps_off");
                    break;
                case "sound":
                    float f = Ship.Flooding;
                    string r = f < 0.01f ? "watch_sound_dry" : f < 0.08f ? "watch_sound_some" : f < 0.2f ? "watch_sound_bad" : "watch_sound_worse";
                    if (Ship.IngressPerMin > 0 && f < 0.01f) r = "watch_sound_some";
                    Dlg.SayAll(null, "capt_order_sound", r);
                    break;
                case "damage":
                    float now = _g.Clock.Minutes;
                    Dlg.SayAll(null, "capt_order_damage", now >= GameClock.At(17, 45) ? "watch_damage_heavy" : Damaged ? "watch_damage_rail" : "watch_damage_none");
                    break;
                case "engine":
                    Dlg.SayAll(null, "capt_order_engine", Ship.Flooding > 0.15f ? "chief_engine_late" : Ship.List > 3f ? "chief_engine_list" : "chief_engine_ok");
                    break;
                case "radar":
                    Dlg.SayAll(null, "capt_order_radar", Ship.RadarsWorking ? "mate_radar_ok" : "mate_radar_dead");
                    break;
                case "position":
                    var toWf = Charts.WhitefishPoint - Ship.NavNm;
                    float dist = toWf.magnitude;
                    float brg = Mathf.Atan2(toWf.x, toWf.y) * Mathf.Rad2Deg;
                    string p = Mathf.Abs(Mathf.DeltaAngle(brg, Ship.Heading)) > 70f ? "mate_pos_off" : dist > 35 ? "mate_pos_40" : dist > 25 ? "mate_pos_30" : dist > 17 ? "mate_pos_20" : "mate_pos_15";
                    Dlg.SayAll(null, "capt_order_position", p);
                    break;
                case "wheel":
                    if (Ship.Wheelsman) { Ship.Wheelsman = false; Dlg.Say("wheel_return"); }
                    else
                    {
                        Ship.OrderedHeading = Ship.Heading;
                        Dlg.SayAll(() => { Ship.OrderedHeading = Ship.Heading; Ship.Wheelsman = true; }, "capt_order_wheel", "wheel_ack");
                    }
                    break;
                case "nodeck":
                    Dlg.SayAll(null, "capt_order_nodeck", "mate_nodeck");
                    break;
                case "coffee":
                    Dlg.SayAll(null, "capt_order_coffee", "galley_coffee");
                    break;
                case "lifejackets":
                    Dlg.SayAll(null, "capt_order_lifejackets", "mate_lifejackets");
                    break;
            }
        }

        public string Summary()
        {
            var s = new List<string>();
            if (MaydayCount > 0) s.Add("You called Mayday. The Coast Guard heard you. Nothing afloat could have reached you in those seas.");
            else s.Add("Like the real Edmund Fitzgerald, you never called for help. There was no warning.");
            if (SaidHoldingOurOwn) s.Add("Your last words on the radio were the same as hers: \"We are holding our own.\"");
            return string.Join("\n\n", s);
        }
    }
}
