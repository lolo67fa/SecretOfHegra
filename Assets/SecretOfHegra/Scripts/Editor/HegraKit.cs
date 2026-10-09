using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SecretOfHegra.EditorTools
{
    /// <summary>
    /// Shared helpers for the scene builder: asset folders, shared materials (created once, updated in place),
    /// primitives, procedural meshes saved as assets, and placeholder character construction.
    /// </summary>
    public static class HegraKit
    {
        public const string Root = "Assets/SecretOfHegra";
        public const string MatDir = Root + "/Materials";
        public const string MeshDir = Root + "/Generated/Meshes";
        public const string TexDir = Root + "/Textures";
        public const string PrefabDir = Root + "/Prefabs";

        static readonly Dictionary<string, Material> s_Mats = new Dictionary<string, Material>();

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        public static void ResetCache() => s_Mats.Clear();

        // ───────────── materials ─────────────

        static Material GetOrCreateMat(string name, Shader shader)
        {
            if (s_Mats.TryGetValue(name, out var m) && m) return m;
            EnsureFolder(MatDir);
            var path = $"{MatDir}/{name}.mat";
            m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m)
            {
                m = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(m, path);
            }
            else if (m.shader != shader) m.shader = shader;
            s_Mats[name] = m;
            return m;
        }

        /// <summary>Mobile-friendly lit material (URP Simple Lit).</summary>
        public static Material Lit(string name, Color color, Texture2D tex = null, float smooth = 0.1f, Color? emission = null)
        {
            var m = GetOrCreateMat(name, Shader.Find("Universal Render Pipeline/Simple Lit"));
            m.SetColor("_BaseColor", color);
            if (tex) m.SetTexture("_BaseMap", tex);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_SpecularHighlights", 0f);
            m.DisableKeyword("_SPECULAR_COLOR");
            if (emission.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission.Value);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            else m.DisableKeyword("_EMISSION");
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        public static Material Unlit(string name, Color color)
        {
            var m = GetOrCreateMat(name, Shader.Find("Universal Render Pipeline/Unlit"));
            m.SetColor("_BaseColor", color);
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Additive glow for particle systems (tinted/faded by the particle colour).</summary>
        public static Material GlowParticle(string name, Color color, float intensity = 1f, float falloff = 2f)
        {
            var m = GetOrCreateMat(name, Shader.Find("SecretOfHegra/GlowParticle"));
            m.SetColor("_Color", color);
            m.SetFloat("_Intensity", intensity);
            m.SetFloat("_Falloff", falloff);
            EditorUtility.SetDirty(m);
            return m;
        }

        public static Material Glow(string name, Color color, float intensity = 1f, float falloff = 2f)
        {
            var m = GetOrCreateMat(name, Shader.Find("SecretOfHegra/Glow"));
            m.SetColor("_Color", color);
            m.SetFloat("_Intensity", intensity);
            m.SetFloat("_Falloff", falloff);
            EditorUtility.SetDirty(m);
            return m;
        }

        public static Material Custom(string name, string shaderName)
        {
            var sh = Shader.Find(shaderName);
            if (!sh) { Debug.LogError($"[Hegra] Shader not found: {shaderName}"); sh = Shader.Find("Universal Render Pipeline/Unlit"); }
            var m = GetOrCreateMat(name, sh);
            EditorUtility.SetDirty(m);
            return m;
        }

        // ───────────── textures ─────────────

        /// <summary>Small tiling noise texture saved as PNG (placeholder for real material textures).</summary>
        public static Texture2D NoiseTexture(string name, Color a, Color b, float scale, int seed, bool strata = false, int size = 128)
        {
            EnsureFolder(TexDir);
            var path = $"{TexDir}/{name}.png";
            if (!File.Exists(path))
            {
                var tex = new Texture2D(size, size, TextureFormat.RGB24, false);
                var rnd = new System.Random(seed);
                float ox = (float)rnd.NextDouble() * 100f, oy = (float)rnd.NextDouble() * 100f;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float u = (float)x / size, v = (float)y / size;
                        // tileable noise by sampling on a torus
                        float n = TileNoise(u, v, scale, ox, oy) * 0.65f + TileNoise(u, v, scale * 3f, oy, ox) * 0.35f;
                        if (strata) n = n * 0.6f + 0.4f * (0.5f + 0.5f * Mathf.Sin(v * Mathf.PI * 2f * 6f + n * 4f));
                        tex.SetPixel(x, y, Color.Lerp(a, b, n));
                    }
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
                var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                imp.maxTextureSize = size;
                imp.mipmapEnabled = true;
                imp.wrapMode = TextureWrapMode.Repeat;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static float TileNoise(float u, float v, float scale, float ox, float oy)
        {
            float a = u * Mathf.PI * 2f, b = v * Mathf.PI * 2f;
            float r = scale / (Mathf.PI * 2f);
            float x = Mathf.Cos(a) * r + ox, y = Mathf.Sin(a) * r + oy;
            float z = Mathf.Cos(b) * r + ox * 0.5f, w = Mathf.Sin(b) * r + oy * 0.5f;
            return (Mathf.PerlinNoise(x + z, y + w) + Mathf.PerlinNoise(x - w, y + z)) * 0.5f;
        }

        // ───────────── primitives ─────────────

        public static GameObject Prim(PrimitiveType type, Transform parent, string name, Vector3 localPos, Vector3 scale, Material mat, Vector3? euler = null, bool collider = false)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localEulerAngles = euler ?? Vector3.zero;
            go.transform.localScale = scale;
            if (mat) go.GetComponent<Renderer>().sharedMaterial = mat;
            var r = go.GetComponent<Renderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        public static GameObject MeshObject(string name, Transform parent, Mesh mesh, Material mat, Vector3 localPos, Vector3? euler = null, Vector3? scale = null, bool meshCollider = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localEulerAngles = euler ?? Vector3.zero;
            go.transform.localScale = scale ?? Vector3.one;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            if (meshCollider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
            return go;
        }

        public static GameObject Group(string name, Transform parent, Vector3 pos, float yaw = 0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = new Vector3(0f, yaw, 0f);
            return go;
        }

        public static void MarkStatic(GameObject root)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(t.gameObject,
                    StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
        }

        // ───────────── procedural meshes (saved as assets so the scene references them) ─────────────

        public static Mesh SaveMesh(Mesh mesh, string name)
        {
            EnsureFolder(MeshDir);
            var path = $"{MeshDir}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            mesh.name = name;
            if (existing)
            {
                existing.Clear();
                EditorUtility.CopySerialized(mesh, existing);
                existing.name = name;
                EditorUtility.SetDirty(existing);
                Object.DestroyImmediate(mesh);
                return existing;
            }
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        /// <summary>Triangular prism (tent): base on y=0, ridge at height h, along z.</summary>
        public static Mesh Prism(string name, float width, float height, float length)
        {
            float w = width * 0.5f, l = length * 0.5f;
            var v = new List<Vector3>();
            var t = new List<int>();
            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                int i = v.Count; v.Add(a); v.Add(b); v.Add(c); v.Add(d);
                t.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
            }
            void Tri(Vector3 a, Vector3 b, Vector3 c)
            {
                int i = v.Count; v.Add(a); v.Add(b); v.Add(c);
                t.AddRange(new[] { i, i + 1, i + 2 });
            }
            var top0 = new Vector3(0, height, -l); var top1 = new Vector3(0, height, l);
            Quad(new Vector3(-w, 0, -l), top0, top1, new Vector3(-w, 0, l));
            Quad(new Vector3(w, 0, l), top1, top0, new Vector3(w, 0, -l));
            Tri(new Vector3(-w, 0, l), top1, new Vector3(w, 0, l));
            Tri(new Vector3(w, 0, -l), top0, new Vector3(-w, 0, -l));
            var mesh = new Mesh();
            mesh.SetVertices(v);
            mesh.SetTriangles(t, 0);
            var uv = new Vector2[v.Count];
            for (int i = 0; i < v.Count; i++) uv[i] = new Vector2(v[i].x + v[i].z, v[i].y);
            mesh.uv = uv;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return SaveMesh(mesh, name);
        }

        /// <summary>
        /// Sandstone butte/outcrop in the Hegra style: near-vertical eroded flanks, a rounded crown, vertical
        /// fluting, stepped ledges and a few cracks. Fits roughly the old unit envelope (radius about 1, crown about +1,
        /// foot at -flattenBottom) so existing placements stay valid. Vertex colour: rgb = cavity/crack shade,
        /// a = normalized height (used by the SecretOfHegra/Sandstone shader for the foot-to-crown gradient).
        /// </summary>
        public static Mesh Rock(string name, int seed, int lon = 18, int lat = 12, float roughness = 0.22f, float flattenBottom = 0.25f)
            => BuildRock(name, seed, Mathf.Max(lon, 12) * 2, Mathf.Max(lat, 8) * 2, roughness, flattenBottom);

        /// <summary>Low-detail version of <see cref="Rock"/> with the same silhouette (LOD1 and collider).</summary>
        public static Mesh RockLow(string name, int seed, float roughness = 0.22f, float flattenBottom = 0.25f)
            => BuildRock(name + "_LOD1", seed, 14, 9, roughness, flattenBottom);

        /// <summary>
        /// Rounded sandstone mass (same erosion detail as <see cref="Rock"/> but a softer, domed profile), for
        /// sculpted formations such as the Elephant Rock. exponent 0.42 = butte, 1 = ellipsoid.
        /// </summary>
        public static (Mesh high, Mesh low) RoundRock(string name, int seed, float exponent, float roughness = 0.16f, float flattenBottom = 0.3f)
            => (BuildRock(name, seed, 40, 28, roughness, flattenBottom, exponent), BuildRock(name + "_LOD1", seed, 16, 11, roughness, flattenBottom, exponent));

        static Mesh BuildRock(string name, int seed, int lon, int lat, float roughness, float flattenBottom, float e1 = 0.42f)
        {
            var rnd = new System.Random(seed);
            float ox = (float)rnd.NextDouble() * 50f, oy = (float)rnd.NextDouble() * 50f;
            int flutes = 7 + rnd.Next(6);
            var cracks = new float[3];
            for (int i = 0; i < cracks.Length; i++) cracks[i] = (float)rnd.NextDouble() * Mathf.PI * 2f;
            // e1 below 1: flat-topped, steep-sided superellipsoid profile
            float yMin = -flattenBottom;

            var verts = new List<Vector3>();
            var cols = new List<Color>();
            var uvs = new List<Vector2>();
            for (int y = 0; y <= lat; y++)
            {
                float v = (float)y / lat;
                float phi = Mathf.Lerp(-Mathf.PI * 0.5f, Mathf.PI * 0.5f, v);
                float sp = Mathf.Sin(phi), cp = Mathf.Cos(phi);
                float py = Mathf.Sign(sp) * Mathf.Pow(Mathf.Abs(sp), e1);
                float pr = Mathf.Pow(Mathf.Max(0f, cp), e1);
                for (int x = 0; x <= lon; x++)
                {
                    float u = (float)x / lon;
                    float th = (x == lon ? 0f : u) * Mathf.PI * 2f;
                    float hy = Mathf.Clamp01((py + 1f) * 0.5f);
                    // erosion: broad lobes + vertical fluting (strong mid-height) + stepped ledges
                    float lobes = Mathf.PerlinNoise(Mathf.Cos(th) * 1.3f + ox, Mathf.Sin(th) * 1.3f + oy) - 0.5f;
                    float flute = Mathf.PerlinNoise(th * flutes * 0.5f + ox, hy * 1.6f + oy) - 0.5f;
                    float midBand = Mathf.Sin(hy * Mathf.PI);
                    float tier = hy * 4.0f + (Mathf.PerlinNoise(th * 2f + oy, ox) - 0.5f) * 0.6f;
                    float ledge = Mathf.SmoothStep(0f, 1f, Mathf.Repeat(tier, 1f)) - 0.5f;
                    float rn = 1f + roughness * (lobes * 1.6f + flute * 1.1f * midBand + ledge * 0.35f);
                    float taper = Mathf.Lerp(1.04f, 0.8f, hy);
                    float r = pr * rn * taper;
                    float yy = py;
                    if (yy > 0.55f) yy += (Mathf.PerlinNoise(Mathf.Cos(th) * 2.2f * r + oy, Mathf.Sin(th) * 2.2f * r + ox) - 0.5f) * roughness * 0.9f;
                    if (yy < yMin) yy = yMin;
                    verts.Add(new Vector3(Mathf.Cos(th) * r, yy, Mathf.Sin(th) * r));

                    // shading: concave flutes and the foot are darker; a few thin vertical cracks
                    float cavity = Mathf.Clamp01(0.5f + (rn - 1f) / Mathf.Max(0.05f, roughness) * 0.9f);
                    float shade = Mathf.Lerp(0.68f, 1f, cavity);
                    foreach (var c in cracks)
                    {
                        float d = Mathf.Abs(Mathf.DeltaAngle(th * Mathf.Rad2Deg, c * Mathf.Rad2Deg)) * Mathf.Deg2Rad;
                        float w = 0.025f + 0.02f * Mathf.PerlinNoise(hy * 6f + c, ox);
                        if (d < w && hy > 0.15f && hy < 0.9f) shade *= Mathf.Lerp(0.5f, 1f, d / w);
                    }
                    float foot = Mathf.Clamp01((yy - yMin) / 0.35f);
                    shade *= Mathf.Lerp(0.72f, 1f, foot);
                    float hNorm = Mathf.Clamp01((yy - yMin) / (1f - yMin + 0.2f));
                    cols.Add(new Color(shade, shade, shade, hNorm));
                    uvs.Add(new Vector2(u * 4f, v * 3f));
                }
            }
            var tris = new List<int>();
            for (int y = 0; y < lat; y++)
                for (int x = 0; x < lon; x++)
                {
                    int a = y * (lon + 1) + x, b = a + lon + 1;
                    tris.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
                }
            var mesh = new Mesh();
            mesh.SetVertices(verts);
            mesh.SetColors(cols);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return SaveMesh(mesh, name);
        }

        /// <summary>
        /// Horizon ring of rounded sandstone buttes (Hegra-like massifs) as one closed band: a crenellated top
        /// edge built from overlapping domes, darker foot / lighter crown via vertex alpha for the sandstone shader.
        /// </summary>
        public static Mesh MountainRing(string name, float radius, float baseHeight, int segments, int seed)
        {
            var rnd = new System.Random(seed);
            int domes = Mathf.Max(8, segments / 4);
            var centers = new float[domes];
            var widths = new float[domes];
            var heights = new float[domes];
            for (int i = 0; i < domes; i++)
            {
                centers[i] = (i + (float)rnd.NextDouble() * 0.6f) / domes;
                widths[i] = (0.6f + (float)rnd.NextDouble() * 0.9f) / domes;
                heights[i] = baseHeight * (0.45f + (float)rnd.NextDouble() * 0.75f);
            }
            int seg = segments * 3;
            var v = new List<Vector3>();
            var c = new List<Color>();
            var t = new List<int>();
            const int rows = 4;
            for (int i = 0; i <= seg; i++)
            {
                float f = (float)(i % seg) / seg;
                float h = baseHeight * 0.18f;
                for (int d = 0; d < domes; d++)
                {
                    float dx = Mathf.Abs(f - centers[d]); dx = Mathf.Min(dx, 1f - dx);
                    float k = dx / widths[d];
                    if (k < 1f) h = Mathf.Max(h, heights[d] * Mathf.Pow(1f - Mathf.Pow(k, 2.6f), 0.45f)); // steep flanks, round crown
                }
                h += (Mathf.PerlinNoise(f * 60f, seed) - 0.5f) * baseHeight * 0.05f;
                float a = f * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                for (int r = 0; r <= rows; r++)
                {
                    float k = (float)r / rows;
                    float y = Mathf.Lerp(-2f, h, Mathf.Pow(k, 0.8f));
                    float inset = (1f - k) * 6f; // slight batter: wider at the foot
                    v.Add(dir * (radius + inset) + Vector3.up * y);
                    float shade = Mathf.Lerp(0.8f, 1f, Mathf.PerlinNoise(f * 140f, k * 3f + seed));
                    c.Add(new Color(shade, shade, shade, k));
                }
            }
            int stride = rows + 1;
            for (int i = 0; i < seg; i++)
                for (int r = 0; r < rows; r++)
                {
                    int a0 = i * stride + r, b0 = (i + 1) * stride + r;
                    t.AddRange(new[] { a0, b0, a0 + 1, a0 + 1, b0, b0 + 1 }); // faces point inward, toward the valley
                }
            var mesh = new Mesh();
            mesh.SetVertices(v);
            mesh.SetColors(c);
            mesh.SetTriangles(t, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return SaveMesh(mesh, name);
        }

        /// <summary>Static star field: tiny camera-facing quads on a dome (one draw call).</summary>
        public static Mesh StarField(string name, int count, float radius, int seed)
        {
            var rnd = new System.Random(seed);
            var v = new List<Vector3>();
            var uv = new List<Vector2>();
            var t = new List<int>();
            for (int i = 0; i < count; i++)
            {
                float y = 0.08f + (float)rnd.NextDouble() * 0.92f;
                float a = (float)rnd.NextDouble() * Mathf.PI * 2f;
                float r = Mathf.Sqrt(1f - y * y);
                var p = new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r) * radius;
                float s = radius * (0.0016f + (float)rnd.NextDouble() * 0.0024f);
                var n = p.normalized;
                var right = Vector3.Cross(Vector3.up, n).normalized;
                if (right.sqrMagnitude < 0.01f) right = Vector3.right;
                var up = Vector3.Cross(n, right);
                int b = v.Count;
                v.Add(p + (-right - up) * s); v.Add(p + (right - up) * s); v.Add(p + (right + up) * s); v.Add(p + (-right + up) * s);
                uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(1, 0)); uv.Add(new Vector2(1, 1)); uv.Add(new Vector2(0, 1));
                t.AddRange(new[] { b, b + 2, b + 1, b, b + 3, b + 2 });
            }
            var mesh = new Mesh();
            mesh.SetVertices(v);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(t, 0);
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * radius * 2.2f);
            return SaveMesh(mesh, name);
        }

        /// <summary>
        /// Weathered stone column swept along a path (e.g. the Elephant Rock's trunk): rings of
        /// <paramref name="sides"/> vertices with per-ring radius, slightly elliptical and noisy, capped at both ends.
        /// Vertex colour follows the sandstone convention (rgb = cavity shade, a = normalized height).
        /// </summary>
        public static Mesh Column(string name, int seed, Vector3[] path, float[] radii, int sides = 12, float squash = 0.85f, float roughness = 0.12f)
        {
            var rnd = new System.Random(seed);
            float ox = (float)rnd.NextDouble() * 40f;
            float yMin = float.MaxValue, yMax = float.MinValue;
            foreach (var p in path) { yMin = Mathf.Min(yMin, p.y); yMax = Mathf.Max(yMax, p.y); }
            var verts = new List<Vector3>();
            var cols = new List<Color>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            int rings = path.Length;
            for (int i = 0; i < rings; i++)
            {
                var fwd = (path[Mathf.Min(i + 1, rings - 1)] - path[Mathf.Max(i - 1, 0)]).normalized;
                var side = Vector3.Cross(fwd, Vector3.forward).sqrMagnitude > 0.01f ? Vector3.Cross(fwd, Vector3.forward).normalized : Vector3.right;
                var up = Vector3.Cross(side, fwd).normalized;
                float hNorm = Mathf.InverseLerp(yMin, yMax + 0.5f, path[i].y);
                for (int k = 0; k <= sides; k++)
                {
                    float th = (k == sides ? 0 : k) * Mathf.PI * 2f / sides;
                    float n = Mathf.PerlinNoise(Mathf.Cos(th) * 1.4f + ox, i * 0.55f + ox) - 0.5f;
                    float flute = Mathf.PerlinNoise(th * 3f + ox, i * 0.2f) - 0.5f;
                    float r = radii[i] * (1f + roughness * (n * 1.6f + flute));
                    var off = side * Mathf.Cos(th) * r + up * Mathf.Sin(th) * r * squash;
                    verts.Add(path[i] + off);
                    float shade = Mathf.Lerp(0.7f, 1f, Mathf.Clamp01(0.5f + (n + flute) * 1.2f));
                    cols.Add(new Color(shade, shade, shade, hNorm));
                    uvs.Add(new Vector2((float)k / sides * 2f, i * 0.4f));
                }
            }
            for (int i = 0; i < rings - 1; i++)
                for (int k = 0; k < sides; k++)
                {
                    int a = i * (sides + 1) + k, b = a + sides + 1;
                    tris.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
                }
            void Cap(int ring, bool flip)
            {
                int c = verts.Count;
                verts.Add(path[ring]);
                cols.Add(new Color(0.8f, 0.8f, 0.8f, Mathf.InverseLerp(yMin, yMax + 0.5f, path[ring].y)));
                uvs.Add(Vector2.zero);
                for (int k = 0; k < sides; k++)
                {
                    int a = ring * (sides + 1) + k;
                    if (flip) tris.AddRange(new[] { c, a + 1, a });
                    else tris.AddRange(new[] { c, a, a + 1 });
                }
            }
            Cap(0, false);
            Cap(rings - 1, true);
            var mesh = new Mesh();
            mesh.SetVertices(verts);
            mesh.SetColors(cols);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return SaveMesh(mesh, name);
        }

        /// <summary>Flat quad lying on the ground (for light pools and markers), uv 0..1.</summary>
        public static Mesh GroundQuad()
        {
            var path = $"{MeshDir}/GroundQuad.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing) return existing;
            var mesh = new Mesh();
            mesh.vertices = new[] { new Vector3(-0.5f, 0, -0.5f), new Vector3(0.5f, 0, -0.5f), new Vector3(0.5f, 0, 0.5f), new Vector3(-0.5f, 0, 0.5f) };
            mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return SaveMesh(mesh, "GroundQuad");
        }
    }
}
