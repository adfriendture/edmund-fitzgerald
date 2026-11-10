using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Fitz
{
    /// <summary>
    /// Light, fog, sky, snow, rain and spray, all driven by the game clock.
    /// Sunset on Lake Superior on Nov 10 was a little after 5 PM; by the end it is
    /// fully dark and the only light is the ship's own.
    /// </summary>
    public class Weather : MonoBehaviour
    {
        public Light Sun;
        public float Daylight = 1;
        public float Storm;
        public float Snow;
        public float Rain;
        public float WindKnots = 40;
        public float Gust;
        public float SprayOnGlass;
        public float FogDensity;
        public float Blackout; // 0..1 for the final moment

        ParticleSystem _rain, _snow, _spray;
        Material _sky;
        float _gustTarget, _gustTimer;
        Volume _volume;
        ColorAdjustments _color;
        Vignette _vignette;

        static readonly int IdTop = Shader.PropertyToID("_FitzSkyTop");
        static readonly int IdHorizon = Shader.PropertyToID("_FitzSkyHorizon");
        static readonly int IdSun = Shader.PropertyToID("_FitzSunDir");
        static readonly int IdRain = Shader.PropertyToID("_FitzRain");
        static readonly int IdSpray = Shader.PropertyToID("_FitzSpray");
        static readonly int IdFlash = Shader.PropertyToID("_FitzFlash");

        public void Build()
        {
            var sunGo = new GameObject("Sun");
            sunGo.transform.SetParent(transform, false);
            Sun = sunGo.AddComponent<Light>();
            Sun.type = LightType.Directional;
            Sun.shadows = LightShadows.Soft;
            Sun.shadowStrength = 0.6f;

            _sky = Mats.Custom("Sky");
            RenderSettings.skybox = _sky;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.sun = Sun;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;

            var precip = Mats.Custom("Precip");
            _rain = MakeSystem("Rain", precip, 0f, 6000);
            var snowMat = Mats.Custom("Precip");
            snowMat.SetFloat("_Shape", 1);
            _snow = MakeSystem("Snow", snowMat, 1f, 7000);
            var sprayMat = Mats.Custom("Precip");
            sprayMat.SetFloat("_Shape", 1);
            sprayMat.SetFloat("_Brightness", 1.3f);
            _spray = MakeSystem("Spray", sprayMat, 1f, 1500);

            var r = _rain.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.04f;
            r.lengthScale = 1f;

            var volGo = new GameObject("PostFX");
            volGo.transform.SetParent(transform, false);
            _volume = volGo.AddComponent<Volume>();
            _volume.isGlobal = true;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);
            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(0.9f);
            bloom.threshold.Override(0.9f);
            bloom.scatter.Override(0.65f);
            _vignette = profile.Add<Vignette>(true);
            _vignette.intensity.Override(0.32f);
            _vignette.smoothness.Override(0.5f);
            var grain = profile.Add<FilmGrain>(true);
            grain.type.Override(FilmGrainLookup.Medium3);
            grain.intensity.Override(0.28f);
            _color = profile.Add<ColorAdjustments>(true);
            _color.saturation.Override(-22f);
            _color.contrast.Override(8f);
            _color.postExposure.Override(0.2f);
            _volume.profile = profile;
        }

        ParticleSystem MakeSystem(string name, Material mat, float shape, int max)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = max;
            main.startSpeed = 0;
            main.playOnAwake = false;
            main.loop = true;
            var shapeM = ps.shape;
            shapeM.shapeType = ParticleSystemShapeType.Box;
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            var rend = go.GetComponent<ParticleSystemRenderer>();
            rend.sharedMaterial = mat;
            rend.shadowCastingMode = ShadowCastingMode.Off;
            rend.receiveShadows = false;
            ps.Play();
            return ps;
        }

        /// <summary>Called every frame by the game with the current clock and ship state.</summary>
        public void Tick(float minutes, Camera cam, Ship ship, Ocean ocean, float dt)
        {
            // ---- daylight and sun ----
            Daylight = Mathf.Clamp01(Mathf.InverseLerp(GameClock.At(17, 35), GameClock.At(15, 40), minutes));
            Daylight = Daylight * Daylight;
            float elev = Mathf.Lerp(14f, -6f, Mathf.InverseLerp(GameClock.At(14, 40), GameClock.At(17, 10), minutes));
            Sun.transform.rotation = Quaternion.Euler(Mathf.Max(elev, 4f) + 20f, 230f, 0);
            Sun.intensity = Mathf.Lerp(0.0f, 0.75f, Daylight) * (1 - Blackout);
            Sun.color = Color.Lerp(new Color(0.55f, 0.6f, 0.7f), new Color(0.82f, 0.84f, 0.86f), Daylight);
            Sun.enabled = Sun.intensity > 0.01f;

            var dayTop = new Color(0.20f, 0.23f, 0.26f);
            var dayHorizon = new Color(0.36f, 0.39f, 0.41f);
            var nightTop = new Color(0.006f, 0.008f, 0.011f);
            var nightHorizon = new Color(0.012f, 0.015f, 0.019f);
            float squall = Mathf.Max(Snow, Rain * 0.6f);
            var top = Color.Lerp(nightTop, dayTop, Daylight) * Mathf.Lerp(1f, 0.75f, squall);
            var horizon = Color.Lerp(nightHorizon, dayHorizon, Daylight);
            horizon = Color.Lerp(horizon, horizon * 1.1f + new Color(0.02f, 0.02f, 0.02f) * Daylight, squall);
            top *= 1 - Blackout;
            horizon *= 1 - Blackout;
            Shader.SetGlobalColor(IdTop, top);
            Shader.SetGlobalColor(IdHorizon, horizon);
            Shader.SetGlobalVector(IdSun, new Vector4(-Sun.transform.forward.x, -Sun.transform.forward.y, -Sun.transform.forward.z, Daylight));
            Shader.SetGlobalFloat(IdFlash, 0);

            RenderSettings.fogColor = horizon;
            FogDensity = Mathf.Lerp(0.00032f, 0.0065f, squall) + Storm * 0.0003f;
            RenderSettings.fogDensity = FogDensity;
            RenderSettings.ambientSkyColor = Color.Lerp(new Color(0.012f, 0.014f, 0.02f), top * 3.2f, Daylight) * (1 - Blackout);
            RenderSettings.ambientEquatorColor = Color.Lerp(new Color(0.008f, 0.01f, 0.013f), horizon * 2.2f, Daylight) * (1 - Blackout);
            RenderSettings.ambientGroundColor = Color.Lerp(new Color(0.003f, 0.004f, 0.005f), horizon * 0.9f, Daylight) * (1 - Blackout);

            if (_color != null) _color.postExposure.value = Mathf.Lerp(0.6f, 0.2f, Daylight) - Blackout * 4f;
            if (_vignette != null) _vignette.intensity.value = 0.3f + Blackout * 0.5f;

            // ---- wind ----
            _gustTimer -= dt;
            if (_gustTimer <= 0)
            {
                _gustTimer = Random.Range(2f, 7f);
                _gustTarget = Random.value < 0.3f ? Random.Range(0.4f, 1f) : Random.Range(0f, 0.3f);
            }
            Gust = Mathf.MoveTowards(Gust, _gustTarget, dt * 0.4f);
            WindKnots = Mathf.Lerp(32f, 58f, Storm) + Gust * Mathf.Lerp(10f, 20f, Storm);

            // ---- precipitation ----
            float windMs = WindKnots * 0.514f;
            var travel = ocean.WaveTravelDir;
            var shipVel = ship.transform.forward * ship.VisualSpeed;
            shipVel.y = 0;
            var rel = new Vector3(travel.x, 0, travel.y) * windMs - shipVel;
            UpdatePrecip(_rain, cam, rel + Vector3.down * 9f, Rain * 5500f, 0.05f, new Color(0.7f, 0.75f, 0.8f, 0.35f));
            UpdatePrecip(_snow, cam, rel * 0.8f + Vector3.down * 2.5f, Snow * 6500f, 0.07f, new Color(0.92f, 0.94f, 0.97f, 0.85f));

            SprayOnGlass = Mathf.MoveTowards(SprayOnGlass, 0, dt * 0.35f);
            Shader.SetGlobalFloat(IdRain, Mathf.Clamp01(Rain + Snow * 0.6f + Storm * 0.25f));
            Shader.SetGlobalFloat(IdSpray, SprayOnGlass);
        }

        void UpdatePrecip(ParticleSystem ps, Camera cam, Vector3 velocity, float rate, float size, Color color)
        {
            float life = 2.2f;
            var main = ps.main;
            main.startLifetime = life;
            main.startSize = size;
            main.startColor = color;
            var em = ps.emission;
            em.rateOverTime = rate;
            var vel = ps.velocityOverLifetime;
            vel.x = velocity.x;
            vel.y = velocity.y;
            vel.z = velocity.z;
            var shape = ps.shape;
            shape.scale = new Vector3(46, 24, 46);
            // Spawn upwind so the box is full where the camera is.
            ps.transform.position = cam.transform.position - velocity * (life * 0.5f);
        }

        /// <summary>Throws a sheet of spray over the bow towards the wheelhouse.</summary>
        public void SprayBurst(Vector3 origin, Vector3 aft, float strength)
        {
            var emit = new ParticleSystem.EmitParams();
            int n = Mathf.RoundToInt(Mathf.Lerp(120, 520, strength));
            var main = _spray.main;
            for (int i = 0; i < n; i++)
            {
                var side = Random.insideUnitSphere;
                emit.position = origin + new Vector3(side.x * 10f, Random.Range(0f, 2f), side.z * 4f);
                emit.velocity = aft * Random.Range(10f, 24f) * strength + Vector3.up * Random.Range(5f, 13f) * strength + side * 3f;
                emit.startLifetime = Random.Range(1.2f, 2.4f);
                emit.startSize = Random.Range(0.9f, 3.2f);
                emit.startColor = new Color(0.94f, 0.96f, 0.98f, Random.Range(0.45f, 0.85f));
                _spray.Emit(emit, 1);
            }
            var grav = _spray.main;
            grav.gravityModifier = 1.2f;
            var vel = _spray.velocityOverLifetime;
            vel.enabled = false;
        }
    }
}
