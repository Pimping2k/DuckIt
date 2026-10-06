using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Restorable
{
    public enum RestoreTool
    {
        Clean, 
        Scrape, 
        Paint, 
        Varnish
    }

    /// Кидаешь на предмет, выбираешь пресет износа. Всё остальное настраивается само.
    /// Маска: R = мягкий слой, G = жёсткий слой, B = новая краска, A = лак.
    [DisallowMultipleComponent]
    public class RestorableItem : MonoBehaviour
    {
        [Header("Main")]
        public WearPreset preset;
        public Color newPaintColor = new Color(0.55f, 0.80f, 0.75f);

        [Header("Quality")]
        public int maskSize = 512;

        [Header("AutoSetup")]
        public Shader toonShader;
        public Shader stampShader;
        public Renderer targetRenderer;
        public MeshFilter meshFilter;

        // ---- Прогресс (0..1) ----
        public float CleanedPercent { get; private set; }
        public float OldPaintStripped { get; private set; }
        public float PaintCoverage { get; private set; }
        public float VarnishCoverage { get; private set; }
        public float OverPaintRatio { get; private set; }
        public bool UseUV0 { get; private set; }

        public event Action<RestorableItem> ProgressChanged;

        const int SmallSize = 64;

        static readonly int BaseMapId   = Shader.PropertyToID("_BaseMap");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int StateMaskId = Shader.PropertyToID("_StateMask");
        static readonly int DirtColorId = Shader.PropertyToID("_DirtColor");
        static readonly int OldPaintId  = Shader.PropertyToID("_OldPaintColor");
        static readonly int NewPaintId  = Shader.PropertyToID("_NewPaintColor");
        static readonly int MaskUVId    = Shader.PropertyToID("_MaskUVSet");
        static readonly int BrushUVId   = Shader.PropertyToID("_BrushUV");
        static readonly int RadiusId    = Shader.PropertyToID("_Radius");
        static readonly int HardnessId  = Shader.PropertyToID("_Hardness");
        static readonly int StrengthId  = Shader.PropertyToID("_Strength");
        static readonly int GateId      = Shader.PropertyToID("_GateByDirt");
        static readonly int AddId       = Shader.PropertyToID("_Add");
        static readonly int SubId       = Shader.PropertyToID("_Sub");

        RenderTexture _a, _b, _small;
        Material _mat, _stamp;
        Texture2D _readTex;
        bool[] _surfaceSmall;
        int _surfaceCount;
        bool _needRead, _initCounted;
        float _nextRead, _initDirt, _initOld;

        void Reset()
        {
            toonShader = Shader.Find("Restoration/ToonRestorable");
            stampShader = Shader.Find("Hidden/Restoration/Stamp");
        }

        void Awake()
        {
            if (targetRenderer == null) targetRenderer = GetComponentInChildren<Renderer>();
            if (meshFilter == null) meshFilter = targetRenderer.GetComponent<MeshFilter>();
            if (toonShader == null) toonShader = Shader.Find("Restoration/ToonRestorable");
            if (stampShader == null) stampShader = Shader.Find("Hidden/Restoration/Stamp");
            if (preset == null) preset = ScriptableObject.CreateInstance<WearPreset>();

            var mesh = meshFilter.sharedMesh;
            UseUV0 = !mesh.HasVertexAttribute(VertexAttribute.TexCoord1);
            if (UseUV0)
                Debug.LogWarning($"{name}: у меша нет UV1, маска рисуется по UV0 (нужна развёртка без перекрытий).", this);

            SetupCollider(mesh);
            SetupMaterial();

            _stamp = new Material(stampShader);
            _a = CreateMask();
            _b = CreateMask();
            _small = new RenderTexture(SmallSize, SmallSize, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            _readTex = new Texture2D(SmallSize, SmallSize, TextureFormat.RGBA32, false, true);

            var surface = BuildSurface(mesh, maskSize, UseUV0);
            BuildInitialMask(surface);
            BuildSmallSurface(surface);

            _mat.SetTexture(StateMaskId, _a);
            ReadProgress();
        }

        void OnDestroy()
        {
            if (_a) _a.Release();
            if (_b) _b.Release();
            if (_small) _small.Release();
            if (_stamp) Destroy(_stamp);
            if (_readTex) Destroy(_readTex);
            if (_mat) Destroy(_mat);
        }

        // ---------------------------------------------------------------------
        // Автонастройка
        // ---------------------------------------------------------------------
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

        void SetupMaterial()
        {
            var src = targetRenderer.sharedMaterial;
            _mat = new Material(toonShader) { name = (src ? src.name : "Item") + " (Restorable)" };

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
            }

            _mat.SetColor(DirtColorId, preset.softColor);
            _mat.SetColor(OldPaintId, preset.hardColor);
            _mat.SetColor(NewPaintId, newPaintColor);
            _mat.SetFloat(MaskUVId, UseUV0 ? 0f : 1f);

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

        // ---------------------------------------------------------------------
        // Мазки
        // ---------------------------------------------------------------------
        /// UV точки попадания с учётом того, какая развёртка используется
        public Vector2 GetUV(RaycastHit hit) => UseUV0 ? hit.textureCoord : hit.textureCoord2;

        /// Старый вариант (для RestorationBrush)
        public void Stroke(Vector2 uv, float uvRadius, RestoreTool tool, float strength, float hardness = 0.6f)
        {
            Vector4 add = Vector4.zero, sub = Vector4.zero;
            bool gate = false;
            switch (tool)
            {
                case RestoreTool.Clean:   sub = new Vector4(1, 0, 0, 0); break;
                case RestoreTool.Scrape:  sub = new Vector4(1, 1, 0, 0); break;
                case RestoreTool.Paint:   add = new Vector4(0, 0, 1, 0); gate = true; break;
                case RestoreTool.Varnish: add = new Vector4(0, 0, 0, 1); gate = true; break;
            }
            Stroke(uv, uvRadius, hardness, strength, add, sub, gate);
        }

        /// Универсальный вариант (под ToolProfile)
        public void Stroke(Vector2 uv, float uvRadius, float hardness, float strength,
            Vector4 add, Vector4 sub, bool gateByDirt)
        {
            _stamp.SetVector(BrushUVId, uv);
            _stamp.SetFloat(RadiusId, uvRadius);
            _stamp.SetFloat(HardnessId, Mathf.Clamp(hardness, 0f, 0.99f));
            _stamp.SetFloat(StrengthId, strength);
            _stamp.SetFloat(GateId, gateByDirt ? 1f : 0f);
            _stamp.SetVector(AddId, add);
            _stamp.SetVector(SubId, sub);

            Graphics.Blit(_a, _b, _stamp);
            (_a, _b) = (_b, _a);
            _mat.SetTexture(StateMaskId, _a);
            _needRead = true;
        }

        public void SetNewPaintColor(Color c)
        {
            newPaintColor = c;
            _mat.SetColor(NewPaintId, c);
        }

        void Update()
        {
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
        bool HasHardLayer => preset.hardCoverage > 0.001f;

        void ReadProgress()
        {
            Graphics.Blit(_a, _small);
            var prev = RenderTexture.active;
            RenderTexture.active = _small;
            _readTex.ReadPixels(new Rect(0, 0, SmallSize, SmallSize), 0, 0, false);
            RenderTexture.active = prev;

            var d = _readTex.GetRawTextureData<Color32>();
            int dirt = 0, old = 0, paint = 0, varn = 0, over = 0;

            for (int i = 0; i < d.Length; i++)
            {
                if (!_surfaceSmall[i]) continue;
                var c = d[i];
                bool isDirt = c.r > 127, isOld = c.g > 127, isPaint = c.b > 127;
                if (isDirt) dirt++;
                if (isOld) old++;
                if (isPaint) { paint++; if (isOld) over++; }
                if (c.a > 127) varn++;
            }

            if (!_initCounted) { _initDirt = dirt; _initOld = old; _initCounted = true; }

            float surf = Mathf.Max(1, _surfaceCount);
            CleanedPercent = _initDirt > 0 ? 1f - dirt / _initDirt : 1f;
            OldPaintStripped = (HasHardLayer && _initOld > 0) ? 1f - old / _initOld : 1f;
            PaintCoverage = paint / surf;
            VarnishCoverage = varn / surf;
            OverPaintRatio = paint > 0 ? over / (float)paint : 0f;
        }

        // ---------------------------------------------------------------------
        // Начальная маска из пресета
        // ---------------------------------------------------------------------
        static float Patchy(int x, int y, int size, float scale, float off, float coverage)
        {
            if (coverage <= 0f) return 0f;
            float nx = x / (float)size * scale + off;
            float ny = y / (float)size * scale + off;
            float n = Mathf.PerlinNoise(nx, ny) * 0.7f + Mathf.PerlinNoise(nx * 2.3f + 7f, ny * 2.3f + 3f) * 0.3f;
            float t = Mathf.Lerp(0.75f, 0.25f, coverage);
            return Mathf.Clamp01((n - t) * 6f + 0.5f);
        }

        void BuildInitialMask(bool[] surface)
        {
            int size = maskSize;
            var px = new Color32[size * size];
            float offSoft = UnityEngine.Random.value * 100f;
            float offHard = UnityEngine.Random.value * 100f + 50f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int i = y * size + x;
                    if (!surface[i]) { px[i] = new Color32(0, 0, 0, 0); continue; }

                    float soft = Patchy(x, y, size, preset.softNoiseScale, offSoft, preset.softCoverage);
                    float hard = preset.hardSolid
                        ? (preset.hardCoverage > 0f ? 1f : 0f)
                        : Patchy(x, y, size, preset.hardNoiseScale, offHard, preset.hardCoverage);

                    px[i] = new Color32((byte)(soft * 255f), (byte)(hard * 255f), 0, 0);
                }
            }

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            tex.SetPixels32(px);
            tex.Apply();
            Graphics.Blit(tex, _a);
            Destroy(tex);
        }

        void BuildSmallSurface(bool[] surface)
        {
            _surfaceSmall = new bool[SmallSize * SmallSize];
            int scale = maskSize / SmallSize;
            _surfaceCount = 0;
            for (int y = 0; y < SmallSize; y++)
            for (int x = 0; x < SmallSize; x++)
            {
                bool on = surface[(y * scale + scale / 2) * maskSize + (x * scale + scale / 2)];
                _surfaceSmall[y * SmallSize + x] = on;
                if (on) _surfaceCount++;
            }
        }

        static bool[] BuildSurface(Mesh mesh, int size, bool useUV0)
        {
            var s = new bool[size * size];

            if (mesh == null || !mesh.isReadable)
            {
                Debug.LogWarning("RestorableItem: у меша выключен Read/Write, прогресс считается по всей текстуре.");
                for (int i = 0; i < s.Length; i++) s[i] = true;
                return s;
            }

            var uv = useUV0 ? mesh.uv : mesh.uv2;
            var tris = mesh.triangles;

            for (int t = 0; t < tris.Length; t += 3)
            {
                Vector2 a = uv[tris[t]] * size, b = uv[tris[t + 1]] * size, c = uv[tris[t + 2]] * size;

                int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, b.x, c.x)));
                int maxX = Mathf.Min(size - 1, Mathf.CeilToInt(Mathf.Max(a.x, b.x, c.x)));
                int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, b.y, c.y)));
                int maxY = Mathf.Min(size - 1, Mathf.CeilToInt(Mathf.Max(a.y, b.y, c.y)));

                float den = (b.y - c.y) * (a.x - c.x) + (c.x - b.x) * (a.y - c.y);
                if (Mathf.Abs(den) < 1e-8f) continue;

                for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                {
                    float px = x + 0.5f, py = y + 0.5f;
                    float w1 = ((b.y - c.y) * (px - c.x) + (c.x - b.x) * (py - c.y)) / den;
                    float w2 = ((c.y - a.y) * (px - c.x) + (a.x - c.x) * (py - c.y)) / den;
                    float w3 = 1f - w1 - w2;
                    if (w1 >= -0.02f && w2 >= -0.02f && w3 >= -0.02f) s[y * size + x] = true;
                }
            }
            return s;
        }
    }
}