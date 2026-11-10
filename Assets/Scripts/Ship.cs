using System.Collections.Generic;
using UnityEngine;

namespace Fitz
{
    public enum Telegraph { FullAstern, HalfAstern, SlowAstern, Stop, SlowAhead, HalfAhead, FullAhead }

    /// <summary>
    /// The ship's state and how it moves. Positions for the chart and radar are in
    /// nautical miles from the wreck site (east, north), advanced in game time.
    /// The visible ship stays at the scene origin and the lake slides past.
    /// </summary>
    public class Ship : MonoBehaviour
    {
        public const float NmToM = 1852f;

        // helm
        public float Heading = 130f;     // degrees true
        public float WheelInput;         // -1..1, where the wheel is turned
        public float Rudder;             // degrees, + = starboard
        public bool Wheelsman;           // a crewman is holding the course
        public bool HelmLocked;          // the wheelsman won't give it back yet
        public float OrderedHeading = 130f;
        public Telegraph Order = Telegraph.FullAhead;
        public float EngineKnots;        // through the water
        public float VisualSpeed;        // m/s used for the slide of the water

        // motion
        public float Roll, Pitch, Heave;
        float _rollVel, _pitchVel, _heaveVel;
        public float List;               // degrees, + = starboard
        public float Sinkage;            // metres lower than design
        public float NoseDive;           // 0..1 the end

        // damage
        public float Flooding;           // 0..1
        public float IngressPerMin;      // set by the story
        public bool PumpsOn;
        public bool RadarsWorking = true;
        public bool DeckLights = true;
        public bool Searchlight;
        public bool Power = true;

        public Vector2 NavNm;            // position, nm from the wreck site
        public readonly List<Vector2> Track = new();
        float _trackTimer;

        public Ocean Ocean;
        public float BowImmersion;       // how deep the bow is buried this frame

        public static float OrderKnots(Telegraph t) => t switch
        {
            Telegraph.FullAstern => -6f,
            Telegraph.HalfAstern => -4f,
            Telegraph.SlowAstern => -2f,
            Telegraph.Stop => 0f,
            Telegraph.SlowAhead => 6f,
            Telegraph.HalfAhead => 10f,
            _ => 14.5f,
        };

        public static string OrderName(Telegraph t) => t switch
        {
            Telegraph.FullAstern => "Full Astern",
            Telegraph.HalfAstern => "Half Astern",
            Telegraph.SlowAstern => "Slow Astern",
            Telegraph.Stop => "Stop",
            Telegraph.SlowAhead => "Slow Ahead",
            Telegraph.HalfAhead => "Half Ahead",
            _ => "Full Ahead",
        };

        public float EngineRpm => Power ? Mathf.Abs(EngineKnots) / 14.5f * 112f : 0f;
        public float SpeedMadeGood => EngineKnots * (1f - 0.42f * Ocean.Storm);

        public void Init(Vector2 startNm, float heading)
        {
            NavNm = startNm;
            Heading = heading;
            OrderedHeading = heading;
            EngineKnots = OrderKnots(Order);
            Track.Add(NavNm);
        }

        public void Step(float dt, float gameMinutesDt, float steerInput)
        {
            // ---- helm ----
            if (Mathf.Abs(steerInput) > 0.01f && !HelmLocked)
            {
                Wheelsman = false;
                WheelInput = Mathf.Clamp(WheelInput + steerInput * dt * 0.6f, -1f, 1f);
            }
            else if (Wheelsman)
            {
                float err = Mathf.DeltaAngle(Heading, OrderedHeading);
                float want = Mathf.Clamp(err / 12f, -0.7f, 0.7f);
                WheelInput = Mathf.MoveTowards(WheelInput, want, dt * 0.35f);
            }
            float rudderTarget = WheelInput * 35f;
            Rudder = Mathf.MoveTowards(Rudder, rudderTarget, dt * 4.5f); // steering engine slews ~4.5 deg/s
            if (!Power) Rudder = Mathf.MoveTowards(Rudder, 0, dt);

            // ---- engine ----
            float target = Power ? OrderKnots(Order) : 0f;
            EngineKnots = Mathf.MoveTowards(EngineKnots, target, dt * 0.08f);
            VisualSpeed = SpeedMadeGood * 0.514f;

            // ---- turning ----
            float speedFactor = Mathf.Clamp(Mathf.Abs(EngineKnots) / 14.5f, 0.15f, 1f) * Mathf.Sign(EngineKnots == 0 ? 1 : EngineKnots);
            // quicker than life so a turn means something against the compressed clock
            float yawRate = Rudder / 35f * 1.5f * speedFactor;
            // the seas push her around, more so with the sea on the quarter
            float seaRel = Mathf.DeltaAngle(Heading, Ocean.WindFromDeg + 180f);
            yawRate += Mathf.Sin(Time.time * 0.31f) * 0.05f * Ocean.Storm * Mathf.Abs(Mathf.Sin(seaRel * Mathf.Deg2Rad));
            Heading = Mathf.Repeat(Heading + yawRate * dt, 360f);

            // ---- navigation (game time) ----
            float hr = Heading * Mathf.Deg2Rad;
            float nm = SpeedMadeGood * gameMinutesDt / 60f;
            NavNm += new Vector2(Mathf.Sin(hr), Mathf.Cos(hr)) * nm;
            _trackTimer += gameMinutesDt;
            if (_trackTimer > 2f)
            {
                _trackTimer = 0;
                Track.Add(NavNm);
                if (Track.Count > 2000) Track.RemoveAt(0);
            }

            // the water slides past at real speed
            Ocean.NavOffset += new Vector2(Mathf.Sin(hr), Mathf.Cos(hr)) * VisualSpeed * dt;

            // ---- flooding ----
            float beam = Mathf.Abs(Mathf.Sin(seaRel * Mathf.Deg2Rad));
            float inflow = IngressPerMin * (0.8f + 0.4f * beam) * (0.6f + 0.6f * Ocean.Storm);
            float outflow = PumpsOn && Power ? 0.0009f : 0f;
            Flooding = Mathf.Clamp01(Flooding + (inflow - outflow * Mathf.Clamp01(Flooding * 20f)) * gameMinutesDt);
            // the list builds with the water: a degree or two after the shoal, a dozen by the end
            List = Mathf.MoveTowards(List, Flooding > 0.001f ? Mathf.Clamp(1.5f + Flooding * 40f, 0f, 12f) : 0f, dt * 0.08f);
            Sinkage = Flooding * 2.6f;

            // ---- motion on the waves ----
            var rot = Quaternion.Euler(0, Heading, 0);
            float hBow = Ocean.SampleHeight(rot * new Vector3(0, 0, 105));
            float hStern = Ocean.SampleHeight(rot * new Vector3(0, 0, -95));
            float hPort = Ocean.SampleHeight(rot * new Vector3(-11.4f, 0, 0));
            float hStbd = Ocean.SampleHeight(rot * new Vector3(11.4f, 0, 0));
            float hMid = Ocean.SampleHeight(Vector3.zero);
            float hFwdQ = Ocean.SampleHeight(rot * new Vector3(0, 0, 50));
            float hAftQ = Ocean.SampleHeight(rot * new Vector3(0, 0, -50));

            float heaveTarget = (hBow + hStern + hPort + hStbd + hMid * 2 + hFwdQ + hAftQ) / 8f - Sinkage;
            _heaveVel += ((heaveTarget - Heave) * 0.9f - _heaveVel * 0.9f) * dt;
            Heave += _heaveVel * dt;

            float pitchTarget = Mathf.Atan2(hStern - hBow, 200f) * Mathf.Rad2Deg * 1.6f;
            _pitchVel += ((pitchTarget - Pitch) * 0.8f - _pitchVel * 0.9f) * dt;
            Pitch += _pitchVel * dt;

            float rollTarget = Mathf.Atan2(hStbd - hPort, 22.9f) * Mathf.Rad2Deg * 0.9f - List;
            _rollVel += ((rollTarget - Roll) * 0.45f - _rollVel * 0.12f) * dt;
            Roll += _rollVel * dt;
            Roll = Mathf.Clamp(Roll, -30f, 30f);

            BowImmersion = hBow - (Heave + 6f - Pitch * Mathf.Deg2Rad * 105f);

            // ---- the end ----
            float dive = NoseDive * NoseDive;
            float pitchOut = Pitch + dive * 32f;
            float heaveOut = Heave - dive * 30f;

            transform.position = new Vector3(0, heaveOut, 0);
            transform.rotation = rot * Quaternion.Euler(pitchOut, 0, Roll - dive * 8f);
        }

        /// <summary>Nudges the hull, for the shoal and big seas.</summary>
        public void Jolt(float rollKick, float pitchKick, float heaveKick)
        {
            _rollVel += rollKick;
            _pitchVel += pitchKick;
            _heaveVel += heaveKick;
        }
    }
}
