using System;
using Gameplay.Tools;
using UnityEngine;
using UnityEngine.Rendering;

namespace Restorable
{
    public enum MaskUvSet
    {
        [InspectorName("Авто (UV1, если есть)")] Auto,
        [InspectorName("UV0")] UV0,
        [InspectorName("UV1")] UV1
    }

    /// Кидаешь на предмет, выбираешь пресет износа. Всё остальное настраивается само.
    /// Маска износа (_WearMask): R = пыль, G = грязь, B = старая краска, A = ржавчина.
    /// Маска покрытия (_PaintMask): R = новая краска, G = лак.
    ///
    /// Воздействие: item.Apply(hit, RestoreTool.Clean, dt) — или с SurfaceEffect и Brush (радиус — доля размера предмета).
    [DisallowMultipleComponent]
    public class RestorableItem : MonoBehaviour
    {
        // ---------------- Inspector (имена и порядок полей сохранены для префабов) ----------------
        [Header("1. Предмет и исходный износ")]
        [InspectorName("Пресет износа")]
        [Tooltip("Какие слои есть на вещи и как они распределены: пыль, грязь, старая краска, ржавчина.")]
        public WearPreset preset;
        [InspectorName("Seed рисунка износа")]
        [Tooltip("0 — создать новый seed при запуске. Для воспроизводимости сохраняйте полученное значение.")]
        public int wearSeed;

        [Header("2. Вид загрязнения")]
        [InspectorName("Мягкий визуальный стиль")]
        [Tooltip("Плавные слои покрытия без рваной пыли и яркой линии очистки.")]
        public bool softSurfaceVisuals = true;
        [InspectorName("Цвет пылевого налёта")]
        [Tooltip("Цвет пыли в мягком стиле. Цвета остальных слоёв берутся из пресета.")]
        public Color dustFilmColor = new Color(0.27f, 0.25f, 0.21f, 1f);
        [InspectorName("Плотность цвета налёта"), Range(0f, 1f)]
        [Tooltip("Насколько налёт скрывает исходный цвет. Это внешний вид, а не количество износа в маске.")]
        public float filmOpacity = 0.88f;
        [InspectorName("Обесцвечивание под пылью"), Range(0f, 1f)]
        public float dustDesaturation = 0.75f;
        [InspectorName("Непрозрачность пыли"), Range(0f, 1f)]
        [Tooltip("Итоговое влияние пыли на цвет. 0 скрывает пыль визуально, но не удаляет её из маски и прогресса.")]
        public float dustOpacity = 0.8f;
        [InspectorName("Равномерная пыль")]
        [Tooltip("Полностью покрыть вещь пылью, если слой включён в пресете и coverage больше 0. Иначе использовать рисунок пресета.")]
        public bool uniformDust = true;

        [Header("3. Качество масок")]
        [InspectorName("Разрешение маски"), Range(64, 2048)]
        [Tooltip("512 — стартовое значение. Выше — подробнее след, больше расход памяти.")]
        public int maskSize = 512;
        [InspectorName("UV для маски")]
        [Tooltip("Авто берёт UV1, если он есть (часто это lightmap-развёртка с мелкими кусками). Если UV0 цельная и без перекрытий, выбери UV0: маска получит больше текселей.")]
        public MaskUvSet maskUvSet = MaskUvSet.Auto;

        [Header("4. Ссылки: можно оставить пустыми")]
        [InspectorName("Шейдер поверхности")] public Shader toonShader;
        [InspectorName("Шейдер мазка")] public Shader stampShader;
        [InspectorName("Renderer предмета")] public Renderer targetRenderer;
        [InspectorName("MeshFilter предмета")] public MeshFilter meshFilter;

        // Старые сериализованные поля: оставлены для совместимости префабов, значения уходят в шейдер.
        [HideInInspector] public float dustPixels = 0f;
        [HideInInspector, Range(0, 1)] public float dustRagged = 0.5f;
        [HideInInspector, Range(0, 1)] public float cleanGlow = 0.5f;
        [HideInInspector] public Color dustColor = new Color(0.80f, 0.76f, 0.72f);

        // ---------------- Публичное состояние ----------------
        /// Готовы материал и маски. Apply до завершения Awake игнорируется.
        public bool IsInitialized { get; private set; }
        public bool UseUV0 { get; private set; }

        /// Сколько износа убрано суммарно по всем слоям (0..1)
        public float CleanedPercent => _progress?.CleanedPercent ?? 0f;
        /// Сколько убрано по каждому слою отдельно (индекс = WearKind)
        public float[] LayerCleaned => _progress?.LayerCleaned ?? NoLayers;
        public float GetLayerCleaned(WearKind kind) => LayerCleaned[(int)kind];
        public float PaintCoverage => _progress?.PaintCoverage ?? 0f;
        public float VarnishCoverage => _progress?.VarnishCoverage ?? 0f;
        /// Доля новой краски, лежащей на нетронутой старой
        public float OverPaintRatio => _progress?.OverPaintRatio ?? 0f;

        public event Action<RestorableItem> ProgressChanged;

        // ---------------- Константы и id шейдера ----------------
        const float ProgressReadInterval = 0.25f;
        const float DryInterval = 0.25f;
        const float WetDuration = 12f;
        const float WetDecayPerTick = 0.03f;
        const int PassStamp = 0;
        const int PassDry = 1;

        static readonly float[] NoLayers = new float[4];

        static readonly int BaseMapId    = Shader.PropertyToID("_BaseMap");
        static readonly int BaseColorId  = Shader.PropertyToID("_BaseColor");
        static readonly int WearMaskId   = Shader.PropertyToID("_WearMask");
        static readonly int PaintMaskId  = Shader.PropertyToID("_PaintMask");
        static readonly int DustColorId  = Shader.PropertyToID("_DustColor");
        static readonly int DirtColorId  = Shader.PropertyToID("_DirtColor");
        static readonly int OldPaintId   = Shader.PropertyToID("_OldPaintColor");
        static readonly int RustColorId  = Shader.PropertyToID("_RustColor");
        static readonly int NewPaintId   = Shader.PropertyToID("_NewPaintColor");
        static readonly int MaskUVId     = Shader.PropertyToID("_MaskUVSet");
        static readonly int BrushUVId    = Shader.PropertyToID("_BrushUV");
        static readonly int RadiusId     = Shader.PropertyToID("_Radius");
        static readonly int HardnessId   = Shader.PropertyToID("_Hardness");
        static readonly int StrengthId   = Shader.PropertyToID("_Strength");
        static readonly int GateId       = Shader.PropertyToID("_GateByWear");
        static readonly int StackId      = Shader.PropertyToID("_Stack");
        static readonly int AddId        = Shader.PropertyToID("_Add");
        static readonly int SubId        = Shader.PropertyToID("_Sub");
        static readonly int GateTexId    = Shader.PropertyToID("_GateTex");
        static readonly int WetTexId     = Shader.PropertyToID("_WetTex");
        static readonly int NeedWetId    = Shader.PropertyToID("_NeedWet");
        static readonly int PeelId       = Shader.PropertyToID("_Peel");
        static readonly int DecayId      = Shader.PropertyToID("_Decay");
        static readonly int PosMapId     = Shader.PropertyToID("_PosMap");
        static readonly int BrushPosId   = Shader.PropertyToID("_BrushPos");
        static readonly int PosBrushId   = Shader.PropertyToID("_PosBrush");

        // ---------------- Runtime ----------------
        Color _selectedPaintColor;
        bool _hasSelectedPaintColor;
        bool _ownsPreset;

        WearLayer[] _layers;
        Vector3[] _wearOffsets;

        MaskBuffer _wear, _paint;
        RestorationProgress _progress;
        Material _mat, _stamp;

        // Позиционная кисть: мазок считается по расстоянию на модели, а не в UV, поэтому он круглый
        // независимо от развёртки, растяжения и швов.
        Texture2D _posMap;
        Vector3 _posMin;
        float _posScale = 1f;
        bool _positionBrush;
        bool _warnedRejectedHit;

        bool _needRead;
        float _nextRead, _wetUntil, _nextDry;

        // =====================================================================
        // Жизненный цикл
        // =====================================================================
        void Reset()
        {
            toonShader = Shader.Find("Restoration/ToonRestorable");
            stampShader = Shader.Find("Hidden/Restoration/Stamp");
        }

        void Awake()
        {
            if (!ResolveReferences())
            {
                enabled = false;
                return;
            }

            maskSize = Mathf.Clamp(maskSize, RestorationProgress.SmallSize, 2048);
            if (preset == null)
            {
                preset = ScriptableObject.CreateInstance<WearPreset>();
                _ownsPreset = true;
            }

            var mesh = meshFilter.sharedMesh;
            bool hasUV1 = mesh.HasVertexAttribute(VertexAttribute.TexCoord1);
            UseUV0 = maskUvSet == MaskUvSet.UV0 || !hasUV1;
            if (maskUvSet == MaskUvSet.UV1 && !hasUV1)
                Debug.LogWarning($"{name}: выбран UV1, но у меша его нет, используется UV0.", this);
            else if (UseUV0 && maskUvSet == MaskUvSet.Auto)
                Debug.LogWarning($"{name}: у меша нет UV1, маска рисуется по UV0 (нужна развёртка без перекрытий).", this);
            Debug.Log($"{name}: маска {maskSize}px, UV{(UseUV0 ? 0 : 1)}", this);

            InitializeWear();
            SetupCollider(mesh);
            SetupMaterial();
            SetupWearCoordinates(mesh);
            SetupMasks(mesh);

            _progress.Read(_wear.Texture, _paint.Texture);
            IsInitialized = true;
        }

        void OnDestroy()
        {
            IsInitialized = false;
            if (_ownsPreset && preset) Destroy(preset);
            _wear?.Release();
            _paint?.Release();
            _progress?.Release();
            if (_stamp) Destroy(_stamp);
            if (_posMap) Destroy(_posMap);
            if (_mat) Destroy(_mat);
        }

        void Update()
        {
            if (!IsInitialized) return;

            if (Time.time < _wetUntil && Time.time >= _nextDry)
            {
                _nextDry = Time.time + DryInterval;
                Dry();
            }

            if (_needRead && Time.unscaledTime >= _nextRead)
            {
                _needRead = false;
                _nextRead = Time.unscaledTime + ProgressReadInterval;
                _progress.Read(_wear.Texture, _paint.Texture);
                ProgressChanged?.Invoke(this);
            }
        }

        // =====================================================================
        // Публичный API
        // =====================================================================

        /// UV точки попадания с учётом того, какая развёртка используется
        public Vector2 GetUV(RaycastHit hit) => UseUV0 ? hit.textureCoord : hit.textureCoord2;

        /// <summary>Вызывается системой выбора цвета, до или после Awake.</summary>
        public void SetNewPaintColor(Color c)
        {
            _selectedPaintColor = c;
            _hasSelectedPaintColor = true;
            if (IsInitialized) _mat.SetColor(NewPaintId, c);
        }

        /// <summary>
        /// Самый простой вызов: item.Apply(hit, RestoreTool.Wash, Time.deltaTime).
        /// Кисть по умолчанию. Принимает только попадание в MeshCollider этой модели.
        /// </summary>
        public bool Apply(RaycastHit hit, RestoreTool tool, float deltaTime)
            => Apply(hit, SurfaceEffect.Of(tool), Brush.Default, deltaTime);

        /// <summary>Попадание луча + эффект + кисть. Проверяет, что попали именно в эту модель.</summary>
        public bool Apply(RaycastHit hit, in SurfaceEffect effect, in Brush brush, float deltaTime)
        {
            if (!IsInitialized) return false;

            // Принимаем любой MeshCollider этого предмета (не только на объекте с Renderer),
            // чтобы мазок не терялся молча.
            if (!(hit.collider is MeshCollider collider) || collider.GetComponentInParent<RestorableItem>() != this)
            {
                if (!_warnedRejectedHit)
                {
                    _warnedRejectedHit = true;
                    Debug.LogWarning($"{name}: мазок отклонён, попадание не в MeshCollider этого предмета (collider: {(hit.collider ? hit.collider.name : "null")}).", this);
                }
                return false;
            }

            return Apply(GetUV(hit), hit.point, effect, brush, deltaTime);
        }

        /// <summary>
        /// Основной метод. brush.radius — доля наибольшего размера предмета (круг на поверхности),
        /// brush.strength — сила в секунду, умножение на deltaTime внутри.
        /// true — команда принята; сколько реально снялось, зависит от слоёв, влажности и маски.
        /// </summary>
        public bool Apply(Vector2 uv, Vector3 worldPoint, in SurfaceEffect effect, in Brush brush, float deltaTime)
        {
            float hardness = effect.forcedHardness > 0f ? effect.forcedHardness : brush.hardness;
            float amount = brush.strength * deltaTime;

            if (!IsInitialized || effect.IsEmpty ||
                !IsFinite(uv.x) || !IsFinite(uv.y) ||
                !IsFinite(worldPoint.x) || !IsFinite(worldPoint.y) || !IsFinite(worldPoint.z) ||
                !IsFinite(deltaTime) || deltaTime <= 0f ||
                !IsFinite(brush.radius) || brush.radius <= 0f ||
                !IsFinite(hardness) ||
                !IsFinite(amount) || amount <= 0f)
                return false;

            ConfigureBrush(uv, worldPoint, brush.radius, hardness, amount);
            // Порядок важен: сначала увлажнить, затем удалить требующую влаги грязь.
            if (effect.AddsCoating) ApplyCoating(effect);
            if (effect.RemovesWear) RemoveWear(effect);
            _needRead = true;
            return true;
        }

        static bool IsFinite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);

        // =====================================================================
        // Мазок
        // =====================================================================
        void ConfigureBrush(Vector2 uv, Vector3 worldPoint, float radius, float hardness, float amount)
        {
            _stamp.SetVector(BrushUVId, uv);
            if (_positionBrush)
            {
                Vector3 local = meshFilter.transform.InverseTransformPoint(worldPoint);
                Vector3 pos = (local - _posMin) / _posScale;
                _stamp.SetVector(BrushPosId, new Vector4(pos.x, pos.y, pos.z, 0f));
            }
            _stamp.SetFloat(RadiusId, radius);
            _stamp.SetFloat(HardnessId, Mathf.Clamp(hardness, 0f, 0.99f));
            _stamp.SetFloat(StrengthId, amount);
        }

        void ApplyCoating(in SurfaceEffect e)
        {
            _stamp.SetVector(AddId, new Vector4(e.paintAdd.x, e.paintAdd.y, e.wetAdd, 0));
            _stamp.SetVector(SubId, Vector4.zero);
            _stamp.SetFloat(GateId, e.gateByWear ? 1f : 0f);
            _stamp.SetFloat(StackId, 0f);
            _stamp.SetFloat(PeelId, 0f);
            _stamp.SetVector(NeedWetId, Vector4.zero);
            _stamp.SetTexture(GateTexId, _wear.Texture);

            Blit(_paint, PaintMaskId, PassStamp);
            if (e.wetAdd > 0f) _wetUntil = Time.time + WetDuration;
        }

        void RemoveWear(in SurfaceEffect e)
        {
            _stamp.SetVector(AddId, Vector4.zero);
            _stamp.SetVector(SubId, e.wearSub);
            _stamp.SetFloat(GateId, 0f);
            _stamp.SetFloat(StackId, preset.stacked ? 1f : 0f);
            _stamp.SetFloat(PeelId, e.peel ? 1f : 0f);
            _stamp.SetVector(NeedWetId, e.needWet);
            _stamp.SetTexture(WetTexId, _paint.Texture);

            Blit(_wear, WearMaskId, PassStamp);
        }

        // Влага высыхает: раз в четверть секунды убавляем канал B маски покрытия.
        void Dry()
        {
            _stamp.SetFloat(DecayId, WetDecayPerTick);
            Blit(_paint, PaintMaskId, PassDry);
        }

        void Blit(MaskBuffer mask, int propertyId, int pass)
        {
            mask.Blit(_stamp, pass);
            _mat.SetTexture(propertyId, mask.Texture);
        }

        // =====================================================================
        // Настройка при старте
        // =====================================================================
        bool ResolveReferences()
        {
            if (targetRenderer == null) targetRenderer = GetComponentInChildren<Renderer>();
            if (targetRenderer == null)
            {
                Debug.LogError("RestorableItem: не найден Renderer.", this);
                return false;
            }
            if (meshFilter == null) meshFilter = targetRenderer.GetComponent<MeshFilter>();

            // Ранние визуальные прототипы рисовали не все каналы износа: переключаем их на полный шейдер.
            if (toonShader == null || IsVisualPrototype(toonShader))
                toonShader = Shader.Find("Restoration/ToonRestorable");
            if (stampShader == null) stampShader = Shader.Find("Hidden/Restoration/Stamp");

            if (meshFilter == null || meshFilter.sharedMesh == null || toonShader == null || stampShader == null)
            {
                Debug.LogError("RestorableItem: нужны MeshFilter с мешем и оба шейдера реставрации.", this);
                return false;
            }
            return true;
        }

        void InitializeWear()
        {
            if (wearSeed == 0)
                wearSeed = (Guid.NewGuid().GetHashCode() & int.MaxValue) | 1;

            _layers = new WearLayer[4];
            _wearOffsets = new Vector3[4];
            for (int k = 0; k < 4; k++)
            {
                var random = new System.Random(unchecked(wearSeed + k * 486187739));
                _layers[k] = preset.Get((WearKind)k).CreateVariation(random,
                    preset.coverageVariation, preset.scaleVariation, preset.toneVariation);
                _wearOffsets[k] = new Vector3((float)random.NextDouble(), (float)random.NextDouble(),
                    (float)random.NextDouble()) * 100f + Vector3.one * 10f;
            }
        }

        void SetupCollider(Mesh mesh)
        {
            var go = targetRenderer.gameObject;
            var mc = go.GetComponent<MeshCollider>();
            if (mc == null) mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
            mc.convex = false;

            // Box/Sphere и прочие не отдают UV: отключаем, чтобы они не перехватывали луч.
            foreach (var c in go.GetComponents<Collider>())
                if (c != mc) c.enabled = false;
        }

        static bool IsVisualPrototype(Shader shader)
        {
            if (!shader) return false;
            return shader.name == "Restoration/CozySurface" || shader.name == "Restoration/SurfaceWipe"
                || shader.name == "Restoration/PainterlyRestorable" || shader.name == "Restoration/CozySatin";
        }

        void SetupMasks(Mesh mesh)
        {
            _stamp = new Material(stampShader);
            _wear = new MaskBuffer(maskSize);
            _paint = new MaskBuffer(maskSize);
            _paint.Clear();

            var data = SurfaceDataBuilder.Build(mesh, meshFilter.transform, maskSize, UseUV0, preset.edgeWidth);
            UploadInitialWear(data);
            SetupPositionBrush(mesh, data);
            _progress = new RestorationProgress(data.on, maskSize);

            _mat.SetTexture(WearMaskId, _wear.Texture);
            _mat.SetTexture(PaintMaskId, _paint.Texture);
        }

        void SetupPositionBrush(Mesh mesh, SurfaceData data)
        {
            // Без Read/Write позиций нет: остаёмся на UV-кисти.
            _positionBrush = mesh.isReadable;
            _stamp.SetFloat(PosBrushId, _positionBrush ? 1f : 0f);
            if (!_positionBrush) return;

            SurfaceDataBuilder.PositionSpace(mesh, out _posMin, out _posScale);
            _posMap = new Texture2D(maskSize, maskSize, TextureFormat.RGBAFloat, false, true)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            _posMap.SetPixels(SurfaceDataBuilder.BuildPositionMap(data, maskSize));
            _posMap.Apply(false, true);
            _stamp.SetTexture(PosMapId, _posMap);
        }

        void UploadInitialWear(SurfaceData data)
        {
            var pixels = SurfaceDataBuilder.BuildInitialWear(_layers, _wearOffsets, data, maskSize, uniformDust);
            var tex = new Texture2D(maskSize, maskSize, TextureFormat.RGBA32, false, true);
            tex.SetPixels32(pixels);
            tex.Apply();
            Graphics.Blit(tex, _wear.Texture);
            Destroy(tex);
        }

        // =====================================================================
        // Материал
        // =====================================================================
        void SetupMaterial()
        {
            var src = targetRenderer.sharedMaterial;
            _mat = src && src.shader == toonShader ? new Material(src) : new Material(toonShader);
            if (src && IsVisualPrototype(src.shader))
                _mat.CopyPropertiesFromMaterial(src);
            _mat.name = (src ? src.name : "Item") + " (Restorable)";

            if (src != null) CopySourceAppearance(src);
            ApplyLayerColors();
            ApplyVisualSettings();

            targetRenderer.sharedMaterial = _mat;
        }

        void CopySourceAppearance(Material src)
        {
            string texProp = src.HasProperty("_BaseMap") ? "_BaseMap" :
                src.HasProperty("_MainTex") ? "_MainTex" : null;
            var tex = texProp != null ? src.GetTexture(texProp) : null;
            if (tex != null)
            {
                _mat.SetTexture(BaseMapId, tex);
                _mat.SetTextureScale(BaseMapId, src.GetTextureScale(texProp));
                _mat.SetTextureOffset(BaseMapId, src.GetTextureOffset(texProp));
            }
            else Debug.LogWarning($"{name}: не нашёл текстуру в материале, будет просто цвет.", this);

            if (src.HasProperty("_BaseColor")) _mat.SetColor(BaseColorId, src.GetColor("_BaseColor"));
            else if (src.HasProperty("_Color")) _mat.SetColor(BaseColorId, src.GetColor("_Color"));
            if (src.shader != toonShader && src.HasProperty("_Smoothness"))
                _mat.SetFloat("_BareGloss", Mathf.Lerp(0.04f, 0.28f, src.GetFloat("_Smoothness")));
        }

        void ApplyLayerColors()
        {
            _mat.SetColor(DustColorId, dustColor);
            _mat.SetColor(DirtColorId, _layers[1].color);
            _mat.SetColor(OldPaintId, _layers[2].color);
            _mat.SetColor(RustColorId, _layers[3].color);
            if (_hasSelectedPaintColor) _mat.SetColor(NewPaintId, _selectedPaintColor);
            _mat.SetFloat("_TexturedCoating", preset.texturedCoating && _layers[2].enabled ? 1f : 0f);
            _mat.SetColor("_SubstrateColor", preset.substrateColor);
            _mat.SetFloat(MaskUVId, UseUV0 ? 0f : 1f);
        }

        void ApplyVisualSettings()
        {
            _mat.SetFloat("_DustPixels", dustPixels);
            _mat.SetFloat("_DustRagged", dustRagged);
            _mat.SetFloat("_CleanGlow", cleanGlow);
            _mat.SetFloat("_DustOpacity", dustOpacity);
            if (_mat.HasProperty("_SoftVisualStyle"))
            {
                _mat.SetFloat("_SoftVisualStyle", softSurfaceVisuals ? 1f : 0f);
                _mat.SetColor("_DustFilmColor", dustFilmColor);
                _mat.SetFloat("_FilmOpacity", filmOpacity);
                _mat.SetFloat("_FilmDesaturation", dustDesaturation);
            }
        }

        void SetupWearCoordinates(Mesh mesh)
        {
            var size = mesh.bounds.size;
            float dimension = Mathf.Max(0.0001f, Mathf.Max(size.x, Mathf.Max(size.y, size.z)));
            Vector3 origin = -mesh.bounds.min / dimension;
            _mat.SetVector("_WearCoordinates", new Vector4(origin.x, origin.y, origin.z, 1f / dimension));
            _mat.SetVector("_WearDetailOffset", _wearOffsets[0]);
        }
    }
}