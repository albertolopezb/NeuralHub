using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeuralHub
{
    /// <summary>Materiales de la figura (los asigna el prefab Tiburon, creado por NeuralHub → Construir estudio).</summary>
    [Serializable]
    public class MaterialesFigura
    {
        public Material traje, piel, camisa, corbata, zapato, cabello, canas;
    }

    /// <summary>
    /// Construye a un inversionista low-poly sentado: cuerpo de cápsulas, cabeza facetada y cabello según el perfil.
    /// Las poses se definen por articulaciones en coordenadas del sillón (+z mira al emprendedor, metros).
    /// </summary>
    public static class FiguraInversionista
    {
        public class Partes { public Transform raiz, torso, cabeza; }

        class Pose
        {
            public Vector3 pelvis, cuello, cabeza, hombroI, hombroD, giroCabeza;
            public Vector3[][] piernas;   // cadera, rodilla, tobillo, punta
            public Vector3[][] brazos;    // hombro, codo, muñeca
        }

        static Vector3 V(float x, float y, float z) { return new Vector3(x, y, z); }

        static readonly Pose[] Poses =
        {
            new Pose { // recargado, pierna cruzada, brazos sobre los descansabrazos
                pelvis = V(0, 0.55f, -0.1f), cuello = V(0, 1.15f, -0.17f), cabeza = V(0, 1.33f, -0.15f), giroCabeza = V(-4.6f, 0, 0),
                hombroI = V(-0.2f, 1.08f, -0.16f), hombroD = V(0.2f, 1.08f, -0.16f),
                piernas = new[] {
                    new[] { V(0.1f, 0.55f, -0.06f), V(0.12f, 0.55f, 0.38f), V(0.13f, 0.08f, 0.42f), V(0.14f, 0.03f, 0.58f) },
                    new[] { V(-0.1f, 0.55f, -0.06f), V(0.04f, 0.68f, 0.4f), V(0.3f, 0.36f, 0.56f), V(0.4f, 0.33f, 0.68f) } },
                brazos = new[] {
                    new[] { V(0.2f, 1.08f, -0.16f), V(0.34f, 0.8f, -0.06f), V(0.36f, 0.72f, 0.2f) },
                    new[] { V(-0.2f, 1.08f, -0.16f), V(-0.34f, 0.8f, -0.06f), V(-0.36f, 0.72f, 0.2f) } } },
            new Pose { // inclinado hacia el emprendedor, codos en las rodillas, manos juntas
                pelvis = V(0, 0.55f, 0), cuello = V(0, 1.07f, 0.2f), cabeza = V(0, 1.24f, 0.3f), giroCabeza = V(11.5f, 0, 0),
                hombroI = V(-0.19f, 1.01f, 0.17f), hombroD = V(0.19f, 1.01f, 0.17f),
                piernas = new[] {
                    new[] { V(0.1f, 0.55f, 0), V(0.17f, 0.55f, 0.43f), V(0.19f, 0.08f, 0.46f), V(0.21f, 0.03f, 0.62f) },
                    new[] { V(-0.1f, 0.55f, 0), V(-0.17f, 0.55f, 0.43f), V(-0.19f, 0.08f, 0.46f), V(-0.21f, 0.03f, 0.62f) } },
                brazos = new[] {
                    new[] { V(0.19f, 1.01f, 0.17f), V(0.16f, 0.64f, 0.4f), V(0.04f, 0.84f, 0.5f) },
                    new[] { V(-0.19f, 1.01f, 0.17f), V(-0.16f, 0.64f, 0.4f), V(-0.04f, 0.84f, 0.5f) } } },
            new Pose { // pierna cruzada, mano en la barbilla, el otro brazo sobre el regazo
                pelvis = V(0, 0.55f, -0.08f), cuello = V(0.02f, 1.14f, -0.1f), cabeza = V(0.05f, 1.32f, -0.06f), giroCabeza = V(2.9f, -14.3f, 6.9f),
                hombroI = V(-0.2f, 1.07f, -0.1f), hombroD = V(0.21f, 1.07f, -0.1f),
                piernas = new[] {
                    new[] { V(-0.1f, 0.55f, -0.05f), V(-0.12f, 0.55f, 0.38f), V(-0.13f, 0.08f, 0.42f), V(-0.14f, 0.03f, 0.58f) },
                    new[] { V(0.1f, 0.55f, -0.05f), V(-0.04f, 0.68f, 0.4f), V(-0.3f, 0.36f, 0.56f), V(-0.4f, 0.33f, 0.68f) } },
                brazos = new[] {
                    new[] { V(0.21f, 1.07f, -0.1f), V(0.31f, 0.74f, 0.08f), V(0.1f, 1.15f, 0.06f) },
                    new[] { V(-0.2f, 1.07f, -0.1f), V(-0.3f, 0.76f, 0.04f), V(-0.06f, 0.66f, 0.24f) } } },
        };

        public static int TotalPoses { get { return Poses.Length; } }

        /// <summary>Peinado de cada perfil del catálogo.</summary>
        public static string Peinado(string perfilId)
        {
            switch (perfilId)
            {
                case "tiburon_financiero": return "hacia_atras";
                case "tiburon_mentor": return "herradura";
                case "tiburon_estratega": return "copete";
                case "tiburon_tecnologico": return "picos";
                case "tiburona_consumo": return "melena";
                case "tiburon_negociador": return "rapado";
                default: return "hacia_atras";
            }
        }

        public static Partes Construir(Transform padre, int pose, string perfilId, MaterialesFigura m)
        {
            var p = Poses[Mathf.Abs(pose) % Poses.Length];
            var raiz = new GameObject("Figura").transform;
            raiz.SetParent(padre, false);

            // Torso: su eje local y va de la pelvis al cuello y z mira al frente
            Vector3 espalda = p.cuello - p.pelvis; float L = espalda.magnitude; Vector3 y = espalda.normalized;
            Vector3 z = (Vector3.forward - y * Vector3.Dot(Vector3.forward, y)).normalized;
            var torso = new GameObject("Torso").transform; torso.SetParent(raiz, false);
            torso.localPosition = p.pelvis; torso.localRotation = Quaternion.LookRotation(z, y);
            var saco = Pieza("Saco", torso, Capsula(0.15f, Mathf.Max(L - 0.18f, 0.05f)), m.traje);
            saco.localPosition = new Vector3(0, L / 2, 0); saco.localScale = new Vector3(1.22f, 1, 0.74f);
            Pieza("Camisa", torso, Triangulo(0.07f, 0.22f), m.camisa).localPosition = new Vector3(0, L - 0.02f, 0.113f);
            if (perfilId != "tiburona_consumo")
            {
                var corbata = Pieza("Corbata", torso, Caja(), m.corbata);
                corbata.localPosition = new Vector3(0, L - 0.13f, 0.117f); corbata.localScale = new Vector3(0.03f, 0.17f, 0.01f);
            }

            Segmento(raiz, p.hombroI, p.hombroD, 0.075f, m.traje, "Hombros");
            Segmento(raiz, p.piernas[0][0], p.piernas[1][0], 0.09f, m.traje, "Cadera");
            Segmento(raiz, p.cuello, p.cabeza + V(0, -0.09f, 0), 0.05f, m.piel, "Cuello");

            var cabeza = Pieza("Cabeza", raiz, CabezaMesh(), m.piel);
            cabeza.localPosition = p.cabeza; cabeza.localRotation = Quaternion.Euler(p.giroCabeza);
            foreach (float s in new[] { -1f, 1f })
            {
                var oreja = Pieza("Oreja", cabeza, OrejaMesh(), m.piel);
                oreja.localPosition = new Vector3(s * 0.086f, -0.006f, -0.012f); oreja.localScale = new Vector3(0.45f, 1, 0.75f);
            }
            string peinado = Peinado(perfilId);
            Pieza("Cabello", cabeza, CabelloMesh(peinado), peinado == "herradura" ? m.canas : m.cabello);

            foreach (var pierna in p.piernas)
            {
                Segmento(raiz, pierna[0], pierna[1], 0.075f, m.traje, "Muslo");
                Segmento(raiz, pierna[1], pierna[2], 0.058f, m.traje, "Pantorrilla");
                var zapato = Pieza("Zapato", raiz, Caja(), m.zapato);
                zapato.localPosition = Vector3.Lerp(pierna[2], pierna[3], 0.55f);
                zapato.localRotation = Quaternion.LookRotation(pierna[3] - pierna[2]);
                zapato.localScale = new Vector3(0.1f, 0.07f, 0.26f);
            }
            foreach (var brazo in p.brazos)
            {
                Segmento(raiz, brazo[0], brazo[1], 0.055f, m.traje, "Brazo");
                Segmento(raiz, brazo[1], brazo[2], 0.047f, m.traje, "Antebrazo");
                var mano = Pieza("Mano", raiz, Esfera(0.045f, 8, 6), m.piel);
                mano.localPosition = brazo[2] + (brazo[2] - brazo[1]).normalized * 0.05f;
            }
            return new Partes { raiz = raiz, torso = torso, cabeza = cabeza };
        }

        // ---------- piezas ----------
        static Transform Pieza(string nombre, Transform padre, Mesh mesh, Material mat)
        {
            var g = new GameObject(nombre);
            g.transform.SetParent(padre, false);
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; r.receiveShadows = true;
            return g.transform;
        }

        static void Segmento(Transform padre, Vector3 a, Vector3 b, float radio, Material mat, string nombre)
        {
            Vector3 d = b - a; float largo = Mathf.Max(d.magnitude, 0.001f);
            var t = Pieza(nombre, padre, Capsula(radio, largo), mat);
            t.localPosition = (a + b) * 0.5f;
            t.localRotation = Quaternion.FromToRotation(Vector3.up, d.normalized);
        }

        // ---------- mallas (en caché) ----------
        static readonly Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>();

        static Mesh Guardar(string clave, Func<Mesh> crear)
        {
            Mesh m;
            if (!cache.TryGetValue(clave, out m) || m == null) { m = crear(); m.name = clave; cache[clave] = m; }
            return m;
        }

        /// <summary>Cápsula a lo largo de y, centrada; largo = distancia entre los centros de las semiesferas.</summary>
        static Mesh Capsula(float r, float largo)
        {
            string k = "capsula_" + Mathf.RoundToInt(r * 1000) + "_" + Mathf.RoundToInt(largo * 1000);
            return Guardar(k, () =>
            {
                const int seg = 14, anillos = 5;
                var verts = new List<Vector3>(); var norms = new List<Vector3>(); var tris = new List<int>();
                var perfil = new List<(float y, float rad, float ny)>();
                for (int i = 0; i <= anillos; i++) { float a = Mathf.PI / 2 * (1 - i / (float)anillos); perfil.Add((largo / 2 + r * Mathf.Sin(a), r * Mathf.Cos(a), Mathf.Sin(a))); }
                for (int i = 0; i <= anillos; i++) { float a = -Mathf.PI / 2 * (i / (float)anillos); perfil.Add((-largo / 2 + r * Mathf.Sin(a), r * Mathf.Cos(a), Mathf.Sin(a))); }
                foreach (var f in perfil)
                    for (int j = 0; j <= seg; j++)
                    {
                        float th = j / (float)seg * Mathf.PI * 2, c = Mathf.Cos(th), s = Mathf.Sin(th);
                        verts.Add(new Vector3(f.rad * c, f.y, f.rad * s));
                        float nr = Mathf.Sqrt(Mathf.Max(0, 1 - f.ny * f.ny));
                        norms.Add(new Vector3(nr * c, f.ny, nr * s));
                    }
                int fila = seg + 1;
                for (int i = 0; i < perfil.Count - 1; i++)
                    for (int j = 0; j < seg; j++)
                    {
                        int a = i * fila + j, b = a + 1, c = a + fila, d = c + 1;
                        tris.Add(a); tris.Add(b); tris.Add(d); tris.Add(a); tris.Add(d); tris.Add(c);
                    }
                var m = new Mesh(); m.SetVertices(verts); m.SetNormals(norms); m.SetTriangles(tris, 0); m.RecalculateBounds();
                return m;
            });
        }

        static Mesh Caja()
        {
            return Guardar("caja", () =>
            {
                var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var m = UnityEngine.Object.Instantiate(g.GetComponent<MeshFilter>().sharedMesh);
                if (Application.isPlaying) UnityEngine.Object.Destroy(g); else UnityEngine.Object.DestroyImmediate(g);
                return m;
            });
        }

        /// <summary>Triángulo en V (camisa bajo el saco), visible por ambos lados.</summary>
        static Mesh Triangulo(float medio, float alto)
        {
            return Guardar("camisa_v", () =>
            {
                var m = new Mesh();
                // vértices separados por cara para que cada lado tenga su propia normal
                var a = new Vector3(-medio, 0, 0); var b = new Vector3(medio, 0, 0); var c = new Vector3(0, -alto, 0);
                m.vertices = new[] { a, c, b, a, b, c };
                m.triangles = new[] { 0, 1, 2, 3, 4, 5 };
                m.RecalculateNormals(); m.RecalculateBounds();
                return m;
            });
        }

        /// <summary>Malla de esfera (misma topología que SphereGeometry de three.js): ancho × alto segmentos.</summary>
        static void Rejilla(int ancho, int alto, float r, out List<Vector3> v, out List<int[]> idx, out List<int> col)
        {
            v = new List<Vector3>(); col = new List<int>(); idx = new List<int[]>();
            var grid = new int[alto + 1, ancho + 1];
            for (int iy = 0; iy <= alto; iy++)
                for (int ix = 0; ix <= ancho; ix++)
                {
                    float u = ix / (float)ancho, w = iy / (float)alto;
                    float phi = u * Mathf.PI * 2, th = w * Mathf.PI;
                    grid[iy, ix] = v.Count;
                    v.Add(new Vector3(-r * Mathf.Cos(phi) * Mathf.Sin(th), r * Mathf.Cos(th), r * Mathf.Sin(phi) * Mathf.Sin(th)));
                    col.Add(ix % ancho);
                }
            for (int iy = 0; iy < alto; iy++)
                for (int ix = 0; ix < ancho; ix++)
                {
                    int a = grid[iy, ix + 1], b = grid[iy, ix], c = grid[iy + 1, ix], d = grid[iy + 1, ix + 1];
                    if (iy != 0) idx.Add(new[] { a, b, d });
                    if (iy != alto - 1) idx.Add(new[] { b, c, d });
                }
        }

        /// <summary>Convierte a malla facetada (un vértice por cara para normales planas).</summary>
        static Mesh Facetada(List<Vector3> v, List<int[]> idx)
        {
            var verts = new List<Vector3>(); var tris = new List<int>();
            foreach (var t in idx) { foreach (var i in t) { tris.Add(verts.Count); verts.Add(v[i]); } }
            var m = new Mesh(); m.SetVertices(verts); m.SetTriangles(tris, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        static Mesh Esfera(float r, int ancho, int alto)
        {
            return Guardar("esfera_" + Mathf.RoundToInt(r * 1000) + "_" + ancho + "x" + alto, () =>
            {
                List<Vector3> v; List<int[]> idx; List<int> col; Rejilla(ancho, alto, r, out v, out idx, out col);
                return Facetada(v, idx);
            });
        }

        static Mesh OrejaMesh() { return Esfera(0.025f, 4, 3); }

        /// <summary>Forma de la cabeza: cráneo amplio atrás, lados planos, mandíbula en V y barbilla al frente.</summary>
        static Vector3 FormaCabeza(Vector3 v)
        {
            float u = v.y / 0.1f;
            float sx = 0.8f, sz = v.z < 0 ? (u > -0.3f ? 1.05f : 0.8f) : 0.92f;
            if (u < 0) { float t = Mathf.Min(1, -u); sx *= 1 - 0.2f * t - 0.3f * t * t; }
            v.x *= sx; v.z *= sz; v.y *= 1.26f;
            if (v.z > 0 && u < -0.55f) v.z += 0.02f * (-u - 0.55f) / 0.45f;
            return v * 1.12f;
        }

        static Mesh CabezaMesh()
        {
            return Guardar("cabeza", () =>
            {
                List<Vector3> v; List<int[]> idx; List<int> col; Rejilla(8, 6, 0.1f, out v, out idx, out col);
                for (int i = 0; i < v.Count; i++) v[i] = FormaCabeza(v[i]);
                return Facetada(v, idx);
            });
        }

        /// <summary>
        /// Cabello: la misma malla de la cabeza; cada vértice sale (k &gt; 1, es pelo) o se esconde dentro (k &lt; 1, piel).
        /// u = altura (-1 barbilla, 1 coronilla), fz = qué tan al frente (1 cara, -1 nuca), col = columna (para picos).
        /// </summary>
        static Vector3 Peinar(string estilo, float u, float fz, int col)
        {
            switch (estilo)
            {
                case "herradura": // calvo arriba, pelo corto a los lados y en la nuca (canas)
                    return u > 0.8f ? V(0.86f, 0, 0) : u > 0.4f ? V(fz < -0.3f ? 1.06f : 0.88f, 0, 0)
                         : u > -0.1f ? V(fz > 0.5f ? 0.85f : 1.05f, 0, 0) : u > -0.6f ? V(fz < -0.3f ? 1.03f : 0.85f, 0, 0) : V(0.85f, 0, 0);
                case "copete": // volumen al frente y arriba, lados cortos
                    return u > 0.8f ? V(1.12f, 0.02f, 0.022f) : u > 0.4f ? (fz > 0.6f ? V(1.16f, 0.032f, 0.022f) : V(fz > 0 ? 1.08f : 1.06f, 0.008f, 0))
                         : u > -0.1f ? V(fz > 0.2f ? 0.85f : fz < -0.3f ? 1.05f : 0.93f, 0, 0) : u > -0.6f ? V(fz < -0.5f ? 1.02f : 0.85f, 0, 0) : V(0.85f, 0, 0);
                case "picos": // corto y despeinado: puntas alternadas arriba
                    return u > 0.8f ? V(1.16f, 0.01f, 0) : u > 0.4f ? V(fz > 0.6f ? 1.06f : (col % 2 == 0 ? 1.2f : 1.08f), col % 2 == 0 ? 0.015f : 0, 0)
                         : u > -0.1f ? V(fz > 0.2f ? 0.85f : 1.04f, 0, 0) : u > -0.6f ? V(fz < -0.5f ? 1.02f : 0.85f, 0, 0) : V(0.85f, 0, 0);
                case "melena": // a la altura de la mandíbula, con fleco
                    return u > 0.8f ? V(1.12f, 0, 0) : u > 0.4f ? V(fz > 0.6f ? 1.1f : 1.13f, 0, fz > 0.6f ? 0.01f : 0)
                         : u > -0.1f ? V(fz > 0.4f ? 0.86f : 1.14f, 0, 0) : u > -0.6f ? V(fz > 0.4f ? 0.85f : 1.12f, -0.01f, 0) : V(0.85f, 0, 0);
                case "rapado": // muy corto, pegado a la cabeza
                    return u > 0.4f ? V(1.035f, 0, 0) : u > -0.1f ? V(fz > 0.5f ? 0.85f : 1.03f, 0, 0) : u > -0.6f ? V(fz < -0.5f ? 1.02f : 0.85f, 0, 0) : V(0.85f, 0, 0);
                default: // hacia atrás: volumen arriba, cubre la nuca, deja ver frente y orejas
                    return u > 0.8f ? V(1.17f, 0.006f, -0.016f) : u > 0.4f ? V(fz > 0.6f ? 1.04f : 1.13f, 0, -0.01f)
                         : u > -0.1f ? V(fz < -0.3f ? 1.07f : 0.88f, 0, 0) : u > -0.6f ? V(fz < -0.5f ? 1.03f : 0.85f, 0, 0) : V(0.85f, 0, 0);
            }
        }

        static Mesh CabelloMesh(string estilo)
        {
            return Guardar("cabello_" + estilo, () =>
            {
                List<Vector3> v; List<int[]> idx; List<int> col; Rejilla(8, 6, 0.1f, out v, out idx, out col);
                for (int i = 0; i < v.Count; i++)
                {
                    var p = v[i]; float u = p.y / 0.1f, anillo = Mathf.Sqrt(p.x * p.x + p.z * p.z), fz = anillo > 1e-6f ? p.z / anillo : 0;
                    var k = Peinar(estilo, u, fz, col[i]);
                    var f = FormaCabeza(p) * k.x;
                    v[i] = new Vector3(f.x, f.y + k.y, f.z + k.z);
                }
                return Facetada(v, idx);
            });
        }
    }
}
