using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace Fitz
{
    public enum Channel { Room, Phone, Radio, Broadcast }

    public class Line
    {
        public string Id, Speaker, Text, Direction;
        public Channel Channel;
    }

    /// <summary>
    /// Every spoken line lives in StreamingAssets/Dialogue/lines.tsv. A recording
    /// named &lt;id&gt;.wav / .ogg / .mp3 in StreamingAssets/Voice is played if present;
    /// otherwise the line shows as a subtitle for a reading-speed duration.
    /// </summary>
    public class Dialogue : MonoBehaviour
    {
        readonly Dictionary<string, Line> _lines = new();
        readonly Dictionary<string, AudioClip> _clips = new();
        readonly Queue<(string id, Action done)> _main = new();
        readonly Queue<string> _am = new();
        GameAudio _audio;
        public Line Current, CurrentAm;
        public float CurrentEnds, CurrentAmEnds;
        public bool Busy => Current != null || _main.Count > 0;
        public int VoiceFilesFound => _clips.Count;
        public Func<bool> AmAudible = () => false;
        /// <summary>Everything said so far, for the clipboard: game minutes, who, what.</summary>
        public readonly List<(float minutes, Line line)> Log = new();
        public static readonly Dictionary<string, string> SpeakerNames = new()
        {
            ["CAPTAIN"] = "You",
            ["MATE"] = "First Mate",
            ["WHEELSMAN"] = "Wheelsman",
            ["CHIEF"] = "Chief Engineer",
            ["WATCHMAN"] = "Deck Watch",
            ["ANDERSON"] = "Arthur M. Anderson",
            ["COASTGUARD"] = "Coast Guard Sault",
            ["AVAFORS"] = "Avafors",
            ["NEWS"] = "WGLR 1230",
            ["DJ"] = "CKSM 850",
            ["NIGHTDJ"] = "1360 (late show)",
        };

        public void Init(GameAudio audio)
        {
            _audio = audio;
            Load();
            StartCoroutine(LoadVoices());
            StartCoroutine(MainLoop());
            StartCoroutine(AmLoop());
        }

        public Line Get(string id) => _lines.TryGetValue(id, out var l) ? l : null;
        public IEnumerable<Line> All => _lines.Values;

        void Load()
        {
            string path = Path.Combine(Application.streamingAssetsPath, "Dialogue", "lines.tsv");
            if (!File.Exists(path)) { Debug.LogError("[Fitz] Missing " + path); return; }
            foreach (var raw in File.ReadAllLines(path))
            {
                if (string.IsNullOrWhiteSpace(raw) || raw.StartsWith("#") || raw.StartsWith("id\t")) continue;
                var c = raw.Split('\t');
                if (c.Length < 4) continue;
                var line = new Line
                {
                    Id = c[0].Trim(),
                    Speaker = c[1].Trim(),
                    Channel = Enum.TryParse<Channel>(c[2].Trim(), true, out var ch) ? ch : Channel.Room,
                    Text = c[3].Trim(),
                    Direction = c.Length > 4 ? c[4].Trim() : "",
                };
                _lines[line.Id] = line;
            }
            Debug.Log($"[Fitz] {_lines.Count} dialogue lines loaded");
        }

        IEnumerator LoadVoices()
        {
            string dir = Path.Combine(Application.streamingAssetsPath, "Voice");
            if (!Directory.Exists(dir)) yield break;
            foreach (var file in Directory.GetFiles(dir))
            {
                string ext = Path.GetExtension(file).ToLowerInvariant();
                AudioType type = ext switch { ".wav" => AudioType.WAV, ".ogg" => AudioType.OGGVORBIS, ".mp3" => AudioType.MPEG, _ => AudioType.UNKNOWN };
                if (type == AudioType.UNKNOWN) continue;
                string id = Path.GetFileNameWithoutExtension(file);
                using var req = UnityWebRequestMultimedia.GetAudioClip(new Uri(file).AbsoluteUri, type);
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    var clip = DownloadHandlerAudioClip.GetContent(req);
                    clip.name = id;
                    _clips[id] = clip;
                }
                else Debug.LogWarning($"[Fitz] Could not load voice {file}: {req.error}");
            }
            Debug.Log($"[Fitz] {_clips.Count} voice recordings loaded");
        }

        public void Say(string id, Action done = null)
        {
            if (!_lines.ContainsKey(id)) { Debug.LogWarning("[Fitz] Unknown line " + id); done?.Invoke(); return; }
            _main.Enqueue((id, done));
        }

        public void SayAll(Action done, params string[] ids)
        {
            for (int i = 0; i < ids.Length; i++) Say(ids[i], i == ids.Length - 1 ? done : null);
            if (ids.Length == 0) done?.Invoke();
        }

        public void Broadcast(string id)
        {
            if (_lines.ContainsKey(id)) _am.Enqueue(id);
        }

        public void ClearAm() => _am.Clear();

        public void StopAll()
        {
            _main.Clear();
            _am.Clear();
            _audio.Voice.Stop();
            _audio.AmVoice.Stop();
            Current = null;
            CurrentAm = null;
        }

        public static float ReadingTime(string text)
        {
            int words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
            return Mathf.Max(1.6f, words / 2.7f + 0.6f);
        }

        IEnumerator MainLoop()
        {
            while (true)
            {
                if (_main.Count == 0) { yield return null; continue; }
                var (id, done) = _main.Dequeue();
                var line = _lines[id];
                if (line.Channel == Channel.Radio) { _audio.Play(_audio.SquelchClip, 0.5f); yield return new WaitForSeconds(0.2f); }
                if (line.Channel == Channel.Phone && line.Speaker != "CAPTAIN") { _audio.Play(_audio.PhoneClip, 0.4f); yield return new WaitForSeconds(0.5f); }
                float dur = ReadingTime(line.Text);
                if (_clips.TryGetValue(id, out var clip))
                {
                    _audio.SetVoiceStyle(line.Channel);
                    _audio.Voice.clip = clip;
                    _audio.Voice.Play();
                    dur = clip.length + 0.15f;
                }
                Current = line;
                CurrentEnds = Time.time + dur;
                Log.Add((Game.I.Clock.Minutes, line));
                // real seconds: the voice clip doesn't slow down when the ending does
                yield return new WaitForSecondsRealtime(dur);
                Current = null;
                if (line.Channel == Channel.Radio) _audio.Play(_audio.SquelchClip, 0.35f);
                yield return new WaitForSecondsRealtime(0.45f);
                done?.Invoke();
            }
        }

        IEnumerator AmLoop()
        {
            while (true)
            {
                if (_am.Count == 0) { yield return null; continue; }
                var id = _am.Dequeue();
                var line = _lines[id];
                float dur = ReadingTime(line.Text);
                if (_clips.TryGetValue(id, out var clip))
                {
                    _audio.AmVoice.clip = clip;
                    _audio.AmVoice.Play();
                    dur = clip.length + 0.2f;
                }
                CurrentAm = line;
                CurrentAmEnds = Time.time + dur;
                if (AmAudible()) Log.Add((Game.I.Clock.Minutes, line));
                float end = Time.time + dur;
                while (Time.time < end)
                {
                    _audio.AmVoice.mute = !AmAudible();
                    yield return null;
                }
                CurrentAm = null;
                yield return new WaitForSeconds(0.6f);
            }
        }
    }
}
