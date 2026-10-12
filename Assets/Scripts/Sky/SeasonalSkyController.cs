using System;
using GameCycle;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Rendering;
using MyPackage.Runtime.ServiceLocator_Core; // Пространство имен вашего Service Locator

namespace PawPaw.Game.Systems.Sky
{
    public enum SkySeason
    {
       Spring = 0,
       Summer = 1,
       Autumn = 2,
       Winter = 3
    }

    [Serializable]
    public class SeasonSkyPreset
    {
       public Gradient Zenith = new();
       public Gradient Horizon = new();
       public Gradient SunLight = new();
       public Gradient TwilightGlow = new();
       public Color MountainTint = new(0.37f, 0.66f, 0.55f);
       [Range(0f, 1f)] public float CloudCoverage = 0.3f;
       [Range(0.2f, 1f)] public float SunPeakElevation = 0.7f;
       [Range(0f, 3f)] public float SunIntensity = 1.1f;
       [Range(0f, 5f)] public float StarsVisibility = 2.5f;
    }

    [ExecuteAlways]
    public class SeasonalSkyController : MonoBehaviour
    {
       private const int SEASONS_COUNT = 4;

       private static readonly float[] KeyTimes = { 0f, 0.21f, 0.26f, 0.34f, 0.66f, 0.74f, 0.80f, 1f };

       private static readonly int ZenithId = Shader.PropertyToID("_ZenithColor");
       private static readonly int MidId = Shader.PropertyToID("_MidColor");
       private static readonly int HorizonId = Shader.PropertyToID("_HorizonColor");
       private static readonly int MountainId = Shader.PropertyToID("_MountainColor");
       private static readonly int GroundId = Shader.PropertyToID("_GroundColor");
       private static readonly int SunDirId = Shader.PropertyToID("_SunDir");
       private static readonly int SunColorId = Shader.PropertyToID("_SunColor");
       private static readonly int TwilightId = Shader.PropertyToID("_HorizonGlowColor");
       private static readonly int MoonColorId = Shader.PropertyToID("_MoonColor");
       private static readonly int MoonPhaseId = Shader.PropertyToID("_MoonPhase");
       private static readonly int StarsId = Shader.PropertyToID("_StarsIntensity");
       private static readonly int CloudLitId = Shader.PropertyToID("_CloudLitColor");
       private static readonly int CloudShadeId = Shader.PropertyToID("_CloudShadeColor");
       private static readonly int CloudCoverageId = Shader.PropertyToID("_CloudCoverage");
       private static readonly int SkyTimeId = Shader.PropertyToID("_SkyTime");

       [SerializeField] private Material _skyMaterial;
       [SerializeField] private Light _sunLight;
       [SerializeField] private SeasonSkyPreset[] _presets = new SeasonSkyPreset[SEASONS_COUNT];

       [Header("Game Cycle Sync")]
       [SerializeField] private bool _syncWithGameCycle = true;

       [Header("Dynamic Time & Motion (Fallback)")]
       [SerializeField] private bool _autoTimeProgress = true;
       [SerializeField, Min(1f)] private float _dayLengthSeconds = 60f;
       [SerializeField, Range(0f, 1f)] private float _timeOfDay = 0.72f; 
       [SerializeField] private SkySeason _season = SkySeason.Autumn;

       [Header("Sun / Moon")]
       [SerializeField, Range(0f, 360f)] private float _sunYawOffset = 45f;
       [SerializeField] private Color _moonColor = new(0.85f, 0.92f, 1f);
       [SerializeField] private Color _moonLightColor = new(0.45f, 0.6f, 1f);
       [SerializeField, Range(0f, 1f)] private float _moonLightIntensity = 0.25f;
       [SerializeField, Range(0f, 1f)] private float _moonPhase = 0.2f;

       [Header("Transitions")]
       [SerializeField, Min(0.01f)] private float _seasonTransitionSeconds = 5f;

       [Header("Scene Sync")]
       [SerializeField] private bool _driveAmbient = true;
       [SerializeField, Range(0f, 2f)] private float _ambientIntensity = 0.75f;
       [SerializeField] private Color _ambientFloor = new(0.08f, 0.08f, 0.15f);

       private readonly float[] _weights = new float[SEASONS_COUNT];
       private Material _instancedMaterial;
       private int _targetSeason;
       private double _lastEditorTime;
       private GameCycleManager _gameCycleManager;

       public float TimeOfDay => _timeOfDay;
       public Vector3 SunDirection { get; private set; } = Vector3.up;

       public void SetTimeOfDay(float normalized)
       {
          _timeOfDay = Mathf.Repeat(normalized, 1f);
       }

       public void SetSeason(SkySeason season, bool instant = false)
       {
          _season = season;
          _targetSeason = (int)season;

          if (!instant)
             return;

          for (int i = 0; i < SEASONS_COUNT; i++)
             _weights[i] = i == _targetSeason ? 1f : 0f;
       }

       private void OnEnable()
       {
          EnsureMaterial();
          _targetSeason = (int)_season;
          SetSeason(_season, true);

          if (_syncWithGameCycle && Application.isPlaying)
          {
             TryBindGameCycle();
          }
       }

       private void OnDisable()
       {
          UnbindGameCycle();
       }

       private void OnDestroy()
       {
          UnbindGameCycle();
          if (_instancedMaterial && Application.isPlaying)
             Destroy(_instancedMaterial);
       }

       private void TryBindGameCycle()
       {
          if (_gameCycleManager != null) return;

          _gameCycleManager = ServiceLocator.Resolve<GameCycleManager>();
          
          if (_gameCycleManager == null)
          {
             _gameCycleManager = FindAnyObjectByType<GameCycle.GameCycleManager>();
          }

          if (_gameCycleManager != null)
          {
             _gameCycleManager.TimeUpdated += OnGameTimeUpdated;
             UpdateSkyFromGameCycle();
          }
       }

       private void UnbindGameCycle()
       {
          if (_gameCycleManager != null)
          {
             _gameCycleManager.TimeUpdated -= OnGameTimeUpdated;
             _gameCycleManager = null;
          }
       }

       private void OnGameTimeUpdated()
       {
          UpdateSkyFromGameCycle();
       }

       private void UpdateSkyFromGameCycle()
       {
          if (_gameCycleManager == null) 
             return;

          var timeData = _gameCycleManager.TimeData;
          var timeRange = _gameCycleManager.Config.TimeRange;

          float totalDuration = timeRange.y - timeRange.x;
          if (totalDuration <= 0f) totalDuration = 24f;

          float elapsedHours = (timeData.Hour + timeData.Minute / 60f) - timeRange.x;
          _timeOfDay = Mathf.Clamp01(elapsedHours / totalDuration);
       }

       private void EnsureMaterial()
       {
          if (!_skyMaterial) return;

          if (Application.isPlaying)
          {
             if (!_instancedMaterial)
                _instancedMaterial = new Material(_skyMaterial);
             RenderSettings.skybox = _instancedMaterial;
          }
          else
          {
             RenderSettings.skybox = _skyMaterial;
          }
       }

       private Material ActiveMaterial => Application.isPlaying ? _instancedMaterial : _skyMaterial;

       private void Update()
       {
          EnsureMaterial();
          if (!ActiveMaterial) return;

          float deltaTime = GetDeltaTime();

          // Если синхронизация с игровым циклом выключена (или не найдена), крутим время сами
          if ((!_syncWithGameCycle || _gameCycleManager == null) && _autoTimeProgress && _dayLengthSeconds > 0.1f)
          {
             _timeOfDay = Mathf.Repeat(_timeOfDay + deltaTime / _dayLengthSeconds, 1f);
          }
          else if (_syncWithGameCycle && _gameCycleManager == null && Application.isPlaying)
          {
             // Пытаемся переподключиться, если менеджер появился позже на сцене
             TryBindGameCycle();
          }

          _targetSeason = (int)_season;
          UpdateSeasonWeights(deltaTime);
          Apply();
       }

       private float GetDeltaTime()
       {
          if (Application.isPlaying)
          {
             return Time.deltaTime;
          }
          else
          {
#if UNITY_EDITOR
             double timeSinceStartup = EditorApplication.timeSinceStartup;
             float delta = (float)(timeSinceStartup - _lastEditorTime);
             _lastEditorTime = timeSinceStartup;
             return Mathf.Clamp(delta, 0f, 0.1f);
#else
             return 0.016f;
#endif
          }
       }

       private void UpdateSeasonWeights(float deltaTime)
       {
          float step = deltaTime / _seasonTransitionSeconds;
          float sum = 0f;

          for (int i = 0; i < SEASONS_COUNT; i++)
          {
             _weights[i] = Mathf.MoveTowards(_weights[i], i == _targetSeason ? 1f : 0f, step);
             sum += _weights[i];
          }

          if (sum <= 0.0001f)
          {
             _weights[_targetSeason] = 1f;
             return;
          }

          for (int i = 0; i < SEASONS_COUNT; i++)
             _weights[i] /= sum;
       }

       private void Apply()
       {
          Color zenith = default;
          Color horizon = default;
          Color sunColor = default;
          Color twilight = default;
          Color mountainTint = default;
          float coverage = 0f;
          float peak = 0f;
          float sunIntensity = 0f;
          float stars = 0f;

          for (int i = 0; i < SEASONS_COUNT; i++)
          {
             float w = _weights[i];
             if (w <= 0.0001f) continue;

             SeasonSkyPreset p = _presets[i];
             zenith += p.Zenith.Evaluate(_timeOfDay) * w;
             horizon += p.Horizon.Evaluate(_timeOfDay) * w;
             sunColor += p.SunLight.Evaluate(_timeOfDay) * w;
             twilight += p.TwilightGlow.Evaluate(_timeOfDay) * w;
             mountainTint += p.MountainTint * w;
             coverage += p.CloudCoverage * w;
             peak += p.SunPeakElevation * w;
             sunIntensity += p.SunIntensity * w;
             stars += p.StarsVisibility * w;
          }

          zenith.a = horizon.a = sunColor.a = twilight.a = mountainTint.a = 1f;

          float angle = (_timeOfDay - 0.25f) * Mathf.PI * 2f;
          float tilt = Mathf.Acos(Mathf.Clamp01(peak)) * Mathf.Rad2Deg;
          Vector3 sunDir = Quaternion.Euler(0f, _sunYawOffset, 0f) *
                           Quaternion.AngleAxis(-tilt, Vector3.right) *
                           new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
          sunDir.Normalize();
          SunDirection = sunDir;

          float sunElev = sunDir.y;
          float dayFactor = SmoothStep01(-0.1f, 0.2f, sunElev);

          Color mid = Color.Lerp(horizon, zenith, 0.5f);
          Color mountainNight = zenith * 0.4f + mountainTint * 0.1f;
          Color mountain = Color.Lerp(mountainNight, mountainTint, dayFactor) + twilight * 0.2f;

          Color cloudLitDay = Color.Lerp(Color.white, sunColor, 0.5f);
          Color cloudLitNight = Color.Lerp(horizon, Color.white, 0.15f) * 0.3f;
          Color cloudLit = Color.Lerp(cloudLitNight, cloudLitDay, dayFactor) + twilight * 0.5f;
          Color cloudShade = Color.Lerp(zenith, horizon, 0.35f) * Mathf.Lerp(0.6f, 0.85f, dayFactor);

          Material mat = ActiveMaterial;
          mat.SetColor(ZenithId, zenith);
          mat.SetColor(MidId, mid);
          mat.SetColor(HorizonId, horizon);
          mat.SetColor(MountainId, mountain);
          mat.SetVector(SunDirId, sunDir);
          mat.SetColor(SunColorId, sunColor);
          mat.SetColor(TwilightId, twilight);
          mat.SetColor(MoonColorId, _moonColor);
          mat.SetFloat(MoonPhaseId, _moonPhase);
          mat.SetFloat(StarsId, stars);
          mat.SetColor(CloudLitId, cloudLit);
          mat.SetColor(CloudShadeId, cloudShade);
          mat.SetFloat(CloudCoverageId, coverage);

          float skyTime = Application.isPlaying ? Time.time : (float)
#if UNITY_EDITOR
             EditorApplication.timeSinceStartup;
#else
             Time.time;
#endif
          mat.SetFloat(SkyTimeId, skyTime);

          ApplyLight(sunDir, sunColor, sunIntensity);

          if (_driveAmbient)
          {
             RenderSettings.ambientMode = AmbientMode.Trilight;

             Color neutralSky = new Color(0.30f, 0.30f, 0.36f);
             Color neutralEquator = new Color(0.24f, 0.23f, 0.27f);
             Color neutralGround = new Color(0.12f, 0.11f, 0.14f);

             RenderSettings.ambientSkyColor = Color.Lerp(neutralSky, zenith, 0.2f) * _ambientIntensity;
             RenderSettings.ambientEquatorColor = Color.Lerp(neutralEquator, horizon, 0.15f) * _ambientIntensity;
             RenderSettings.ambientGroundColor = Color.Lerp(neutralGround, _ambientFloor, 0.5f);
          }
       }

       private void ApplyLight(Vector3 sunDir, Color sunColor, float sunIntensity)
       {
          if (!_sunLight) return;

          float sunElev = sunDir.y;
          bool sunIsKey = sunElev >= 0f;
          Vector3 keyDir = sunIsKey ? sunDir : -sunDir;

          float intensity = sunIsKey
             ? sunIntensity * SmoothStep01(-0.02f, 0.25f, sunElev)
             : _moonLightIntensity * SmoothStep01(-0.02f, 0.25f, -sunElev);

          _sunLight.color = sunIsKey ? sunColor : _moonLightColor;
          _sunLight.intensity = intensity;
          _sunLight.transform.rotation = Quaternion.LookRotation(-keyDir);
       }

       private static float SmoothStep01(float a, float b, float x)
       {
          float t = Mathf.Clamp01((x - a) / (b - a));
          return t * t * (3f - 2f * t);
       }

       private static Color MaxColor(Color a, Color b)
       {
          return new Color(Mathf.Max(a.r, b.r), Mathf.Max(a.g, b.g), Mathf.Max(a.b, b.b), 1f);
       }

       #region Default Presets

       [ContextMenu("Fill Default Presets")]
       private void FillDefaultPresets()
       {
          _presets = CreateDefaultPresets();
       }

       private static SeasonSkyPreset[] CreateDefaultPresets()
       {
          SeasonSkyPreset spring = Preset(
             zenith: H("#0B132B", "#111B3D", "#4B81C3", "#1B6CA8", "#1B6CA8", "#594B8F", "#151635", "#0B132B"),
             horizon: H("#192245", "#27335C", "#F4A385", "#D8EAA5", "#D8EAA5", "#E87C7E", "#25244C", "#192245"),
             sun: H("#FFA56B", "#FFA56B", "#FF8D54", "#FFF8E7", "#FFF8E7", "#FF6B4A", "#F44B36", "#FFA56B"),
             glow: H("#000000", "#1C101A", "#F78DA7", "#000000", "#000000", "#D64B7D", "#211126", "#000000"),
             mountain: "#2A735E", coverage: 0.28f, peak: 0.72f, sunIntensity: 1.1f, stars: 2.5f);

          SeasonSkyPreset summer = Preset(
             zenith: H("#080D26", "#10163C", "#3C72B5", "#0A4E8A", "#0A4E8A", "#3B4A94", "#121334", "#080D26"),
             horizon: H("#1B2A57", "#2D3D73", "#FFA15C", "#FFF1BE", "#FFF1BE", "#FF6C3B", "#22204B", "#1B2A57"),
             sun: H("#FF9C45", "#FF9C45", "#FF7D3B", "#FFFFE8", "#FFFFE8", "#FF5123", "#E32A15", "#FF9C45"),
             glow: H("#000000", "#21151E", "#FF7A45", "#000000", "#000000", "#FF451C", "#28122D", "#000000"),
             mountain: "#185C56", coverage: 0.32f, peak: 0.95f, sunIntensity: 1.3f, stars: 2.0f);

          SeasonSkyPreset autumn = Preset(
             zenith: H("#0B0817", "#150C24", "#702D52", "#AA3116", "#AE3616", "#3D142E", "#150C24", "#0B0817"),
             horizon: H("#22102A", "#381A38", "#ED5C30", "#FF9848", "#FF9848", "#C4301A", "#220C2B", "#22102A"),
             sun: H("#F55B25", "#F55B25", "#FF4C19", "#FFAD54", "#FFAC51", "#D8200F", "#9A0C02", "#F55B25"),
             glow: H("#000000", "#210918", "#FF4C25", "#000000", "#000000", "#C7180D", "#210617", "#000000"),
             mountain: "#6E230D", coverage: 0.25f, peak: 0.6f, sunIntensity: 1.2f, stars: 3.0f);

          SeasonSkyPreset winter = Preset(
             zenith: H("#06091D", "#0F142D", "#535EA1", "#415C9C", "#415C9C", "#3C3A7A", "#11142F", "#06091D"),
             horizon: H("#1B2245", "#2C345C", "#E2909C", "#CADFF2", "#CADFF2", "#D67A95", "#1B2245", "#1B2245"),
             sun: H("#DCC9FF", "#DCC9FF", "#FFAEA5", "#EFF5FF", "#EFF5FF", "#EB7C93", "#C74B72", "#DCC9FF"),
             glow: H("#000000", "#181424", "#EB93A4", "#000000", "#000000", "#D15C7B", "#1B172D", "#000000"),
             mountain: "#4A5A87", coverage: 0.40f, peak: 0.42f, sunIntensity: 0.9f, stars: 3.5f);

          return new[] { spring, summer, autumn, winter };
       }

       private static SeasonSkyPreset Preset(string[] zenith, string[] horizon, string[] sun, string[] glow,
          string mountain, float coverage, float peak, float sunIntensity, float stars)
       {
          return new SeasonSkyPreset
          {
             Zenith = Grad(zenith),
             Horizon = Grad(horizon),
             SunLight = Grad(sun),
             TwilightGlow = Grad(glow),
             MountainTint = Hex(mountain),
             CloudCoverage = coverage,
             SunPeakElevation = peak,
             SunIntensity = sunIntensity,
             StarsVisibility = stars
          };
       }

       private static Color Hex(string hex)
       {
          ColorUtility.TryParseHtmlString(hex, out Color color);
          return color;
       }

       private static string[] H(params string[] hex)
       {
          return hex;
       }

       private static Gradient Grad(string[] hex)
       {
          int count = Mathf.Min(hex.Length, KeyTimes.Length);
          GradientColorKey[] colorKeys = new GradientColorKey[count];

          for (int i = 0; i < count; i++)
          {
             ColorUtility.TryParseHtmlString(hex[i], out Color color);
             colorKeys[i] = new GradientColorKey(color, KeyTimes[i]);
          }

          Gradient gradient = new();
          gradient.SetKeys(colorKeys, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
          return gradient;
       }

       #endregion
    }
}