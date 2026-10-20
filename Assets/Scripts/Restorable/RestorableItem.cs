using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Restorable
{
    public enum RestoreTool
    {
        Clean,    // сухая кисть: пыль
        Wash,     // губка: пыль, грязь
        Scrape,   // скребок: старая краска
        Sand,     // шкурка: старая краска, ржавчина
        Peel,     // пинцет: отходящие края старой краски
        Paint,
        Varnish
    }

    /// Кидаешь на предмет, выбираешь пресет износа. Всё остальное настраивается само.
    /// Маска износа (_WearMask): R = пыль, G = грязь, B = старая краска, A = ржавчина.
    /// Маска покрытия (_PaintMask): R = новая краска, G = лак.
    [DisallowMultipleComponent]
    public class RestorableItem : MonoBehaviour
    {
        [Header("Main")]
        public WearPreset preset;
        public Color newPaintColor = new Color(0.55f, 0.80f, 0.75f);

        [Header("Surface visuals")]
        [Tooltip("Мягкий свет и плавная очистка исходной текстуры вместо рваной пыли и яркого контура")]
        public bool softSurfaceVisuals = true;
        public Color dustFilmColor = new Color(0.27f, 0.25f, 0.21f, 1f);
        [Range(0f, 1f)] public float filmOpacity = 0.88f;
        [Range(0f, 1f)] public float dustDesaturation = 0.75f;

        [Header("Dust coverage")]
        // Retain serialized legacy fields so existing prefabs do not lose data.
        [Tooltip("Размер сетки пыли в пикселях. 0 = гладкая пыль, 48-128 = крупные пиксели")]
        [HideInInspector] public float dustPixels = 0f;
        [HideInInspector, Range(0, 1)] public float dustRagged = 0.5f;
        [HideInInspector, Range(0, 1)] public float cleanGlow = 0.5f;
        [Range(0, 1)] public float dustOpacity = 0.8f;
        [Tooltip("Пыль лежит ровным слоем по всему предмету, а не пятнами из пресета")]
        public bool uniformDust = true;
        public Color dustColor = new Color(0.80f, 0.76f, 0.72f);

        [Tooltip("0 = новый рисунок при создании. Сохраняйте выданное значение вместе с предметом; одинаковый seed воспроизводит износ.")]
        public int wearSeed;
        WearLayer[] _layers;
        Vector3[] _wearOffsets;

        [Header("Quality")]
        public int maskSize = 512;

        [Header("AutoSetup")]
        public Shader toonShader;
        public Shader stampShader;
        public Renderer targetRenderer;
        public MeshFilter meshFilter;

        // ---- Прогресс (0..1) ----
        /// Сколько износа убрано суммарно по всем включённым слоям
        public float CleanedPercent { get; private set; }
        /// Сколько убрано по каждому слою отдельно (индекс = WearKind)
        public float[] LayerCleaned { get; } = new float[4];
        public float PaintCoverage { get; private set; }
        public float VarnishCoverage { get; private set; }
        /// Доля новой краски, лежащей на нетронутой старой
        public float OverPaintRatio { get; private set; }
        public bool UseUV0 { get; private set; }

        public float GetLayerCleaned(WearKind kind) => LayerCleaned[(int)kind];

        public event Action<RestorableItem> ProgressChanged;

        const int SmallSize = 64;

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

        RenderTexture _wearA, _wearB, _paintA, _paintB, _small;
        Material _mat, _stamp;
        Texture2D _readTex;
        bool[] _surfaceSmall;
        bool[] _oldOn;
        int _surfaceCount;
        readonly int[] _cnt = new int[4];
        readonly int[] _init = new int[4];
        bool _needRead, _initCounted;
        float _nextRead;
        float _wetUntil, _nextDry;

        void Reset()
        {
            toonShader = Shader.Find("Restoration/ToonRestorable");
            stampShader = Shader.Find("Hidden/Restoration/Stamp");
        }

        void Awake()
        {
            if (targetRenderer == null) targetRenderer = GetComponentInChildren<Renderer>();
            if (targetRenderer == null)
            {
                Debug.LogError("RestorableItem: не найден Renderer.", this);
                enabled = false;
                return;
            }
            if (meshFilter == null) meshFilter = targetRenderer.GetComponent<MeshFilter>();
            // The earlier visual prototypes rendered only a subset of wear channels.
            // Route those known prototypes to the integrated shader; keep custom shaders.
            if (toonShader == null || IsVisualPrototype(toonShader))
                toonShader = Shader.Find("Restoration/ToonRestorable");
            if (stampShader == null) stampShader = Shader.Find("Hidden/Restoration/Stamp");
            if (meshFilter == null || meshFilter.sharedMesh == null || toonShader == null || stampShader == null)
            {
                Debug.LogError("RestorableItem: нужны MeshFilter с мешем и оба шейдера реставрации.", this);
                enabled = false;
                return;
            }
            maskSize = Mathf.Clamp(maskSize, SmallSize, 2048);
            if (preset == null)
            {
                preset = ScriptableObject.CreateInstance<WearPreset>();
                _ownsPreset = true;
            }

            var mesh = meshFilter.sharedMesh;
            UseUV0 = !mesh.HasVertexAttribute(VertexAttribute.TexCoord1);
            if (UseUV0)
                Debug.LogWarning($"{name}: у меша нет UV1, маска рисуется по UV0 (нужна развёртка без перекрытий).", this);

            InitializeWear();
            SetupCollider(mesh);
            SetupMaterial();
            float dimension = Mathf.Max(0.0001f, Mathf.Max(mesh.bounds.size.x, Mathf.Max(mesh.bounds.size.y, mesh.bounds.size.z)));
            Vector3 origin = -mesh.bounds.min / dimension;
            _mat.SetVector("_WearCoordinates", new Vector4(origin.x, origin.y, origin.z, 1f / dimension));
            _mat.SetVector("_WearDetailOffset", _wearOffsets[0]);

            _stamp = new Material(stampShader);
            _wearA = CreateMask();
            _wearB = CreateMask();
            _paintA = CreateMask();
            _paintB = CreateMask();
            ClearMask(_paintA);
            _small = new RenderTexture(SmallSize, SmallSize, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            _readTex = new Texture2D(SmallSize, SmallSize, TextureFormat.RGBA32, false, true);
            _oldOn = new bool[SmallSize * SmallSize];

            var data = BuildSurfaceData(mesh, maskSize, UseUV0, preset.edgeWidth);
            BuildInitialMask(data);
            BuildSmallSurface(data.on);

            _mat.SetTexture(WearMaskId, _wearA);
            _mat.SetTexture(PaintMaskId, _paintA);
            ReadProgress();
        }

        void OnDestroy()
        {
            if (_ownsPreset && preset) Destroy(preset);
            if (_wearA) _wearA.Release();
            if (_wearB) _wearB.Release();
            if (_paintA) _paintA.Release();
            if (_paintB) _paintB.Release();
            if (_small) _small.Release();
            if (_stamp) Destroy(_stamp);
            if (_readTex) Destroy(_readTex);
            if (_mat) Destroy(_mat);
        }

        bool _ownsPreset;

        // ---------------------------------------------------------------------
        // Автонастройка
        // ---------------------------------------------------------------------
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

            // Box/Sphere и прочие не отдают UV, чтобы они не перехватывали луч, отключаем их
            foreach (var c in go.GetComponents<Collider>())
                if (c != mc) c.enabled = false;
        }

        static bool IsVisualPrototype(Shader shader)
        {
            if (!shader) return false;
            return shader.name == "Restoration/CozySurface" || shader.name == "Restoration/SurfaceWipe"
                || shader.name == "Restoration/PainterlyRestorable" || shader.name == "Restoration/CozySatin";
        }

        void SetupMaterial()
        {
            var src = targetRenderer.sharedMaterial;
            _mat = src && src.shader == toonShader ? new Material(src) : new Material(toonShader);
            if (src && IsVisualPrototype(src.shader))
                _mat.CopyPropertiesFromMaterial(src);
            _mat.name = (src ? src.name : "Item") + " (Restorable)";

            if (src != null)
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

            _mat.SetColor(DustColorId, dustColor);
            _mat.SetColor(DirtColorId, _layers[1].color);
            _mat.SetColor(OldPaintId, _layers[2].color);
            _mat.SetColor(RustColorId, _layers[3].color);
            _mat.SetColor(NewPaintId, newPaintColor);
            _mat.SetFloat("_TexturedCoating", preset.texturedCoating && _layers[2].enabled ? 1f : 0f);
            _mat.SetColor("_SubstrateColor", preset.substrateColor);
            _mat.SetFloat(MaskUVId, UseUV0 ? 0f : 1f);
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

            targetRenderer.sharedMaterial = _mat;
        }

        RenderTexture CreateMask()
        {
            var rt = new RenderTexture(maskSize, maskSize, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            rt.Create();
            return rt;
        }

        static void ClearMask(RenderTexture rt)
        {
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(false, true, Color.clear);
            RenderTexture.active = prev;
        }

        // ---------------------------------------------------------------------
        // Мазки
        // ---------------------------------------------------------------------
        /// UV точки попадания с учётом того, какая развёртка используется
        public Vector2 GetUV(RaycastHit hit) => UseUV0 ? hit.textureCoord : hit.textureCoord2;

        /// Простой вариант (для тестовой RestorationBrush)
        public void Stroke(Vector2 uv, float uvRadius, RestoreTool tool, float strength, float hardness = 0.6f)
        {
            Vector4 sub = Vector4.zero, needWet = Vector4.zero;
            Vector2 add = Vector2.zero;
            bool gate = false, peel = false;
            float wet = 0f;
            switch (tool)
            {
                case RestoreTool.Clean:   sub = new Vector4(1, 0.15f, 0, 0); break;
                case RestoreTool.Wash:    sub = new Vector4(1, 1, 0, 0); needWet = new Vector4(0, 1, 0, 0); wet = 3f; break;
                case RestoreTool.Scrape:  sub = new Vector4(0, 0.4f, 1, 0.2f); break;
                case RestoreTool.Sand:    sub = new Vector4(0, 0.3f, 0.6f, 1); break;
                case RestoreTool.Peel:    sub = new Vector4(0, 0, 1, 0); peel = true; hardness = 0.9f; break;
                case RestoreTool.Paint:   add = new Vector2(1, 0); gate = true; break;
                case RestoreTool.Varnish: add = new Vector2(0, 1); gate = true; break;
            }
            Stroke(uv, uvRadius, hardness, strength, sub, add, gate, wet, needWet, peel);
        }

        /// Универсальный вариант (под ToolProfile).
        /// wearSub: насколько тул берёт каждый слой = (пыль, грязь, старая краска, ржавчина), 0..1.
        /// paintAdd: что тул наносит = (новая краска, лак), 0..1.
        /// gateByWear: не наносить краску и лак там, где виден износ.
        /// wetAdd: сколько влаги оставляет тул (губка: около 3, остальные 0).
        /// needWet: по слоям, какие отмываются только по мокрому (губка: грязь = (0,1,0,0)).
        /// peel: пинцет, берёт только отходящие края слоя.
        public void Stroke(Vector2 uv, float uvRadius, float hardness, float strength,
            Vector4 wearSub, Vector2 paintAdd, bool gateByWear,
            float wetAdd = 0f, Vector4 needWet = default, bool peel = false)
        {
            _stamp.SetVector(BrushUVId, uv);
            _stamp.SetFloat(RadiusId, uvRadius);
            _stamp.SetFloat(HardnessId, Mathf.Clamp(hardness, 0f, 0.99f));
            _stamp.SetFloat(StrengthId, strength);

            // Сначала краска и влага: губка мочит и тут же отмывает
            if (paintAdd != Vector2.zero || wetAdd > 0f)
            {
                _stamp.SetVector(AddId, new Vector4(paintAdd.x, paintAdd.y, wetAdd, 0));
                _stamp.SetVector(SubId, Vector4.zero);
                _stamp.SetFloat(GateId, gateByWear ? 1f : 0f);
                _stamp.SetFloat(StackId, 0f);
                _stamp.SetFloat(PeelId, 0f);
                _stamp.SetVector(NeedWetId, Vector4.zero);
                _stamp.SetTexture(GateTexId, _wearA);

                Graphics.Blit(_paintA, _paintB, _stamp, 0);
                (_paintA, _paintB) = (_paintB, _paintA);
                _mat.SetTexture(PaintMaskId, _paintA);
                if (wetAdd > 0f) _wetUntil = Time.time + 12f;
            }

            if (wearSub != Vector4.zero)
            {
                _stamp.SetVector(AddId, Vector4.zero);
                _stamp.SetVector(SubId, wearSub);
                _stamp.SetFloat(GateId, 0f);
                _stamp.SetFloat(StackId, preset.stacked ? 1f : 0f);
                _stamp.SetFloat(PeelId, peel ? 1f : 0f);
                _stamp.SetVector(NeedWetId, needWet);
                _stamp.SetTexture(WetTexId, _paintA);

                Graphics.Blit(_wearA, _wearB, _stamp, 0);
                (_wearA, _wearB) = (_wearB, _wearA);
                _mat.SetTexture(WearMaskId, _wearA);
            }

            _needRead = true;
        }

        // Влага высыхает: раз в четверть секунды убавляем канал B маски покрытия
        void Dry()
        {
            _stamp.SetFloat(DecayId, 0.03f);
            Graphics.Blit(_paintA, _paintB, _stamp, 1);
            (_paintA, _paintB) = (_paintB, _paintA);
            _mat.SetTexture(PaintMaskId, _paintA);
        }

        public void SetNewPaintColor(Color c)
        {
            newPaintColor = c;
            _mat.SetColor(NewPaintId, c);
        }

        void Update()
        {
            if (Time.time < _wetUntil && Time.time >= _nextDry)
            {
                _nextDry = Time.time + 0.25f;
                Dry();
            }

            if (_needRead && Time.unscaledTime >= _nextRead)
            {
                _needRead = false;
                _nextRead = Time.unscaledTime + 0.25f;
                ReadProgress();
                ProgressChanged?.Invoke(this);
            }
        }

        // ---------------------------------------------------------------------
        // Прогресс
        // ---------------------------------------------------------------------
        NativeArray<Color32> Read(RenderTexture rt)
        {
            Graphics.Blit(rt, _small);
            var prev = RenderTexture.active;
            RenderTexture.active = _small;
            _readTex.ReadPixels(new Rect(0, 0, SmallSize, SmallSize), 0, 0, false);
            RenderTexture.active = prev;
            return _readTex.GetRawTextureData<Color32>();
        }

        void ReadProgress()
        {
            Array.Clear(_cnt, 0, 4);

            var d = Read(_wearA);
            for (int i = 0; i < d.Length; i++)
            {
                _oldOn[i] = false;
                if (!_surfaceSmall[i]) continue;
                var c = d[i];
                _cnt[0] += c.r;
                _cnt[1] += c.g;
                _cnt[2] += c.b;
                _cnt[3] += c.a;
                _oldOn[i] = c.b > 127;
            }

            d = Read(_paintA);
            int paint = 0, varn = 0, over = 0;
            for (int i = 0; i < d.Length; i++)
            {
                if (!_surfaceSmall[i]) continue;
                var c = d[i];
                if (c.r > 127) { paint++; if (_oldOn[i]) over++; }
                if (c.g > 127) varn++;
            }

            if (!_initCounted)
            {
                Array.Copy(_cnt, _init, 4);
                _initCounted = true;
            }

            float totInit = 0f, totNow = 0f;
            for (int k = 0; k < 4; k++)
            {
                LayerCleaned[k] = _init[k] > 0 ? Mathf.Clamp01(1f - _cnt[k] / (float)_init[k]) : 1f;
                totInit += _init[k];
                totNow += _cnt[k];
            }
            CleanedPercent = totInit > 0f ? 1f - totNow / totInit : 1f;

            float surf = Mathf.Max(1, _surfaceCount);
            PaintCoverage = paint / surf;
            VarnishCoverage = varn / surf;
            OverPaintRatio = paint > 0 ? over / (float)paint : 0f;
        }

        // ---------------------------------------------------------------------
        // Данные поверхности и начальная маска из пресета
        // ---------------------------------------------------------------------
        void BuildInitialMask(SurfaceData d)
        {
            int size = maskSize;
            var px = new Color32[size * size];
            for (int i = 0; i < px.Length; i++)
            {
                if (!d.on[i]) continue;
                px[i] = new Color32(
                    uniformDust && _layers[0].enabled && _layers[0].coverage > 0f ? (byte)255 : WearPatterns.Sample(_layers[0], d, i, _wearOffsets[0]),
                    WearPatterns.Sample(_layers[1], d, i, _wearOffsets[1]),
                    WearPatterns.Sample(_layers[2], d, i, _wearOffsets[2]),
                    WearPatterns.Sample(_layers[3], d, i, _wearOffsets[3]));
            }

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            PadMask(px, d.on, size);
            tex.SetPixels32(px);
            tex.Apply();
            Graphics.Blit(tex, _wearA);
            Destroy(tex);
        }

        // Fill only the UV gutter, so bilinear filtering does not invent clean seams.
        static void PadMask(Color32[] pixels, bool[] surface, int size)
        {
            var filled = (bool[])surface.Clone();
            var previous = new bool[filled.Length];
            for (int pass = 0; pass < 2; pass++)
            {
                Array.Copy(filled, previous, filled.Length);
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int i = y * size + x;
                    if (previous[i]) continue;
                    int source = x > 0 && previous[i - 1] ? i - 1 :
                        x + 1 < size && previous[i + 1] ? i + 1 :
                        y > 0 && previous[i - size] ? i - size :
                        y + 1 < size && previous[i + size] ? i + size : -1;
                    if (source < 0) continue;
                    pixels[i] = pixels[source];
                    filled[i] = true;
                }
            }
        }

        void BuildSmallSurface(bool[] surface)
        {
            _surfaceSmall = new bool[SmallSize * SmallSize];
            _surfaceCount = 0;
            for (int y = 0; y < SmallSize; y++)
            for (int x = 0; x < SmallSize; x++)
            {
                int sx = Mathf.Min(maskSize - 1, (int)((x + 0.5f) * maskSize / SmallSize));
                int sy = Mathf.Min(maskSize - 1, (int)((y + 0.5f) * maskSize / SmallSize));
                bool on = surface[sy * maskSize + sx];
                _surfaceSmall[y * SmallSize + x] = on;
                if (on) _surfaceCount++;
            }
        }

        /// Растеризует меш в UV-пространство и запоминает для каждого тексела: позицию на модели,
        /// куда он смотрит, высоту и близость к рёбрам. По этим данным пресет раскладывает износ.
        SurfaceData BuildSurfaceData(Mesh mesh, int size, bool useUV0, int edgeWidth)
        {
            var d = new SurfaceData(size);

            if (mesh == null || !mesh.isReadable)
            {
                Debug.LogWarning("RestorableItem: у меша выключен Read/Write, паттерны износа будут упрощены.");
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int i = y * size + x;
                    d.on[i] = true;
                    d.pos[i] = new Vector3(x / (float)size, y / (float)size, 0f);
                    d.up[i] = 0.5f;
                    d.height[i] = y / (float)size;
                }
                return d;
            }

            var uv = useUV0 ? mesh.uv : mesh.uv2;
            var verts = mesh.vertices;
            var norms = mesh.normals;
            var tris = mesh.triangles;
            bool hasN = norms != null && norms.Length == verts.Length;
            var b = mesh.bounds;
            float maxDim = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            if (maxDim < 1e-5f) maxDim = 1f;
            d.center = b.size / (maxDim * 2f);
            float hy = Mathf.Max(b.size.y, 1e-5f);
            var tf = meshFilter != null ? meshFilter.transform : transform;
            var nrm = new Vector3[size * size];
            var normalMatrix = tf.worldToLocalMatrix.transpose;

            for (int t = 0; t < tris.Length; t += 3)
            {
                int i0 = tris[t], i1 = tris[t + 1], i2 = tris[t + 2];
                Vector2 a = uv[i0] * size, bb = uv[i1] * size, c = uv[i2] * size;

                int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, bb.x, c.x)));
                int maxX = Mathf.Min(size - 1, Mathf.CeilToInt(Mathf.Max(a.x, bb.x, c.x)));
                int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, bb.y, c.y)));
                int maxY = Mathf.Min(size - 1, Mathf.CeilToInt(Mathf.Max(a.y, bb.y, c.y)));

                float den = (bb.y - c.y) * (a.x - c.x) + (c.x - bb.x) * (a.y - c.y);
                if (Mathf.Abs(den) < 1e-8f) continue;

                for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                {
                    float px = x + 0.5f, py = y + 0.5f;
                    float w1 = ((bb.y - c.y) * (px - c.x) + (c.x - bb.x) * (py - c.y)) / den;
                    float w2 = ((c.y - a.y) * (px - c.x) + (a.x - c.x) * (py - c.y)) / den;
                    float w3 = 1f - w1 - w2;
                    if (w1 < -1e-5f || w2 < -1e-5f || w3 < -1e-5f) continue;

                    int idx = y * size + x;
                    Vector3 p = verts[i0] * w1 + verts[i1] * w2 + verts[i2] * w3;
                    Vector3 n = hasN ? (norms[i0] * w1 + norms[i1] * w2 + norms[i2] * w3).normalized : Vector3.up;

                    d.on[idx] = true;
                    d.pos[idx] = (p - b.min) / maxDim;
                    d.height[idx] = Mathf.Clamp01((p.y - b.min.y) / hy);
                    nrm[idx] = n;
                    d.up[idx] = Mathf.Clamp01(normalMatrix.MultiplyVector(n).normalized.y);
                }
            }

            // Рёбра: там, где нормаль резко меняется от тексела к соседу
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int i = y * size + x;
                if (!d.on[i]) continue;
                float crease = 0f, curvature = 0f;
                int samples = 0;
                if (x > 0) MeasureSurfaceNeighbour(d, nrm, i, i - 1, ref crease, ref curvature, ref samples);
                if (x + 1 < size) MeasureSurfaceNeighbour(d, nrm, i, i + 1, ref crease, ref curvature, ref samples);
                if (y > 0) MeasureSurfaceNeighbour(d, nrm, i, i - size, ref crease, ref curvature, ref samples);
                if (y + 1 < size) MeasureSurfaceNeighbour(d, nrm, i, i + size, ref crease, ref curvature, ref samples);
                d.edge[i] = Mathf.Clamp01((crease - 0.08f) * 5f);
                d.cavity[i] = samples > 0 ? Mathf.Clamp01(-curvature / samples * 0.07f) : 0f;
            }

            // Расширяем полосу вокруг рёбер
            var tmp = new float[size * size];
            for (int pass = 0; pass < edgeWidth; pass++)
            {
                Array.Copy(d.edge, tmp, tmp.Length);
                for (int y = 1; y < size - 1; y++)
                for (int x = 1; x < size - 1; x++)
                {
                    int i = y * size + x;
                    if (!d.on[i]) continue;
                    float m = Mathf.Max(Mathf.Max(tmp[i], tmp[i - 1]), Mathf.Max(tmp[i + 1], Mathf.Max(tmp[i - size], tmp[i + size])));
                    d.edge[i] = m;
                }
            }

            return d;
        }

        static void MeasureSurfaceNeighbour(SurfaceData d, Vector3[] normals, int i, int j,
            ref float crease, ref float curvature, ref int samples)
        {
            if (!d.on[j]) return;
            Vector3 delta = d.pos[j] - d.pos[i];
            float distanceSquared = delta.sqrMagnitude;
            // Adjacent pixels in an atlas may belong to unrelated UV charts.
            if (distanceSquared < 1e-9f || distanceSquared > 0.0025f) return;
            Vector3 normalDelta = normals[j] - normals[i];
            crease = Mathf.Max(crease, normalDelta.magnitude);
            curvature += Vector3.Dot(normalDelta, delta) / distanceSquared;
            samples++;
        }
    }
}