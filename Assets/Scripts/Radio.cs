using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace Fitz
{
    /// <summary>
    /// The VHF is always on channel 16 (calling and distress). The AM broadcast
    /// set tunes 540-1600 kHz and has three stations worth finding:
    ///   850  CKSM  music      (original procedural guitar)
    ///   1230 WGLR  news and the marine forecast
    ///   1360 the late show    (the easter egg; see docs/EASTER_EGG.md)
    /// </summary>
    public class Radio : MonoBehaviour
    {
        public bool VhfOn = true;
        public bool AmOn;
        public float AmKhz = 1230;
        public bool Transmitting;

        Game _g;
        float _newsTimer = 2f, _djTimer = 1f;
        int _newsIndex, _djIndex;
        bool _nightDjDone;
        bool _hasCustom;

        public const float Music = 850, News = 1230, Late = 1360;

        public void Init(Game g)
        {
            _g = g;
            _g.Dialogue.AmAudible = () =>
            {
                var l = _g.Dialogue.CurrentAm;
                if (l == null || !AmOn || !_g.Ship.Power) return false;
                return Tune(StationOf(l.Speaker)) > 0.4f;
            };
            StartCoroutine(LoadCustomStation());
        }

        static float StationOf(string speaker) => speaker switch { "NEWS" => News, "DJ" => Music, "NIGHTDJ" => Late, _ => -1000 };

        /// <summary>1 when dead on frequency, fading out over about 25 kHz.</summary>
        public float Tune(float khz) => Mathf.Clamp01(1f - (Mathf.Abs(AmKhz - khz) - 6f) / 22f);

        IEnumerator LoadCustomStation()
        {
            string dir = Path.Combine(Application.streamingAssetsPath, "Radio");
            if (!Directory.Exists(dir)) yield break;
            foreach (var ext in new[] { ".ogg", ".wav", ".mp3" })
            {
                string file = Path.Combine(dir, "station_1360" + ext);
                if (!File.Exists(file)) continue;
                var type = ext == ".ogg" ? AudioType.OGGVORBIS : ext == ".wav" ? AudioType.WAV : AudioType.MPEG;
                using var req = UnityWebRequestMultimedia.GetAudioClip(new System.Uri(file).AbsoluteUri, type);
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success) continue;
                _g.Audio.CustomStation.clip = DownloadHandlerAudioClip.GetContent(req);
                _g.Audio.CustomStation.volume = 0;
                _g.Audio.CustomStation.Play();
                _hasCustom = true;
                Debug.Log("[Fitz] Custom 1360 station loaded from " + file);
                yield break;
            }
        }

        public void Nudge(float khz)
        {
            AmKhz = Mathf.Clamp(AmKhz + khz, 540f, 1600f);
        }

        public void Tick(float dt)
        {
            bool power = _g.Ship.Power;
            var am = _g.Audio.Am;
            bool on = AmOn && power;
            float tA = Tune(Late), tB = Tune(Music), tN = Tune(News);
            var cur = _g.Dialogue.CurrentAm;
            bool djTalking = cur != null && cur.Speaker != "NEWS";
            float duck = djTalking ? 0.25f : 1f;
            am.UseCustomA = _hasCustom;
            am.StationA = on ? tA * duck : 0;
            am.StationB = on ? tB * duck : 0;
            am.Static = on ? Mathf.Clamp01(1f - Mathf.Max(tA, Mathf.Max(tB, tN)) * 0.92f) : 0;
            am.Volume = on ? 0.55f * _g.MusicVolume : 0;
            if (_hasCustom) _g.Audio.CustomStation.volume = on ? tA * duck * 0.8f * _g.MusicVolume : 0;
            Transmitting = _g.Dialogue.Current != null && _g.Dialogue.Current.Speaker == "CAPTAIN" && _g.Dialogue.Current.Channel == Channel.Radio;

            if (!on) return;
            float mins = _g.Clock.Minutes;

            // news bulletins whenever you're listening to 1230
            if (tN > 0.5f && cur == null)
            {
                _newsTimer -= dt;
                if (_newsTimer <= 0)
                {
                    string[] early = { "news_01", "news_03", "news_06", "news_02", "news_08" };
                    string[] late = { "news_02", "news_04", "news_07", "news_05", "news_06", "news_08" };
                    var pool = mins < GameClock.At(17, 0) ? early : late;
                    _g.Dialogue.Broadcast(pool[_newsIndex++ % pool.Length]);
                    _newsTimer = 25f;
                }
            }
            if (tB > 0.5f && cur == null)
            {
                _djTimer -= dt;
                if (_djTimer <= 0)
                {
                    string[] djs = { "dj_01", "dj_03", "dj_02", "dj_04" };
                    _g.Dialogue.Broadcast(djs[_djIndex++ % djs.Length]);
                    _djTimer = 72f;   // between tunes
                }
            }
            if (tA > 0.5f && !_nightDjDone && cur == null)
            {
                _nightDjDone = true;
                _g.Dialogue.Broadcast("nightdj_01");
                _g.Dialogue.Broadcast("nightdj_02");
                _g.Dialogue.Broadcast("nightdj_03");
            }
        }

        public string StationName()
        {
            if (Tune(News) > 0.5f) return "WGLR 1230 · news";
            if (Tune(Music) > 0.5f) return "CKSM 850 · music";
            if (Tune(Late) > 0.5f) return "1360 · the late show";
            return "static";
        }
    }
}
