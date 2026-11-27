using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NeuralHub.Editor
{
    /// <summary>Mallas del escenario generadas por código (bordes redondeados, discos, aros, ventanal curvo, rocas y algas).</summary>
    public static class Mallas
    {
        class M
        {
            readonly List<Vector3> v = new List<Vector3>();
            readonly List<Vector2> uv = new List<Vector2>();
            readonly List<int> t = new List<int>();

            int P(Vector3 p, Vector2 u) { v.Add(p); uv.Add(u); return v.Count - 1; }

            /// <summary>Triángulo orientado hacia «frente» (corrige el orden de los vértices si hace falta).</summary>
            public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 frente, Vector2 ua = default, Vector2 ub = default, Vector2 uc = default)
            {
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), frente) < 0) { var p = b; b = c; c = p; var q = ub; ub = uc; uc = q; }
                t.Add(P(a, ua)); t.Add(P(b, ub)); t.Add(P(c, uc));
            }

            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 frente, Vector2 ua = default, Vector2 ub = default, Vector2 uc = default, Vector2 ud = default)
            {
                Tri(a, b, c, frente, ua, ub, uc); Tri(a, c, d, frente, ua, uc, ud);
            }

            public Mesh Fin(string nombre, bool suave)
            {
                var m = new Mesh { name = nombre };
                if (suave) Soldar();
                m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
                return m;
            }

            // Une vértices en la misma posición para que las superficies curvas se vean lisas.
            void Soldar()
            {
                var mapa = new Dictionary<string, int>(); var nv = new List<Vector3>(); var nu = new List<Vector2>(); var nt = new List<int>();
                foreach (var i in t)
                {
                    string k = Mathf.RoundToInt(v[i].x * 1000) + "," + Mathf.RoundToInt(v[i].y * 1000) + "," + Mathf.RoundToInt(v[i].z * 1000) + "," + Mathf.RoundToInt(uv[i].x * 1000) + "," + Mathf.RoundToInt(uv[i].y * 1000);
                    int j; if (!mapa.TryGetValue(k, out j)) { j = nv.Count; mapa[k] = j; nv.Add(v[i]); nu.Add(uv[i]); }
                    nt.Add(j);
                }
                v.Clear(); v.AddRange(nv); uv.Clear(); uv.AddRange(nu); t.Clear(); t.AddRange(nt);
            }
        }

        public static Mesh Guardar(Mesh m, string nombre)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Modelos")) AssetDatabase.CreateFolder("Assets", "Modelos");
            string ruta = "Assets/Modelos/" + nombre + ".asset";
            var viejo = AssetDatabase.LoadAssetAtPath<Mesh>(ruta);
            if (viejo != null) { viejo.Clear(); EditorUtility.CopySerialized(m, viejo); viejo.name = nombre; return viejo; }
            m.name = nombre; AssetDatabase.CreateAsset(m, ruta); return m;
        }

        static Vector3 Circ(float ang, float r, float y) { return new Vector3(Mathf.Sin(ang) * r, y, Mathf.Cos(ang) * r); }

        /// <summary>Disco con canto superior redondeado; base en y = 0.</summary>
        public static Mesh Disco(float radio, float alto, float bisel, int lados = 72)
        {
            var m = new M();
            bisel = Mathf.Min(bisel, alto, radio * 0.5f);
            var perfil = new List<Vector2> { new Vector2(radio, 0) };     // (radio, altura)
            for (int k = 0; k <= 5; k++)
            {
                float a = k / 5f * Mathf.PI / 2;
                perfil.Add(new Vector2(radio - bisel + bisel * Mathf.Cos(a), alto - bisel + bisel * Mathf.Sin(a)));
            }
            for (int i = 0; i < lados; i++)
            {
                float a0 = i * 2 * Mathf.PI / lados, a1 = (i + 1) * 2 * Mathf.PI / lados;
                var fuera = Circ((a0 + a1) / 2, 1, 0);
                for (int k = 0; k + 1 < perfil.Count; k++)
                    m.Quad(Circ(a0, perfil[k].x, perfil[k].y), Circ(a1, perfil[k].x, perfil[k].y), Circ(a1, perfil[k + 1].x, perfil[k + 1].y), Circ(a0, perfil[k + 1].x, perfil[k + 1].y),
                           fuera + Vector3.up * (k / (float)perfil.Count));
                m.Tri(new Vector3(0, alto, 0), Circ(a0, radio - bisel, alto), Circ(a1, radio - bisel, alto), Vector3.up);
            }
            return m.Fin("disco", true);
        }

        /// <summary>Aro plano horizontal.</summary>
        public static Mesh Aro(float r0, float r1, int lados = 96)
        {
            var m = new M();
            for (int i = 0; i < lados; i++)
            {
                float a0 = i * 2 * Mathf.PI / lados, a1 = (i + 1) * 2 * Mathf.PI / lados;
                m.Quad(Circ(a0, r0, 0), Circ(a1, r0, 0), Circ(a1, r1, 0), Circ(a0, r1, 0), Vector3.up);
            }
            return m.Fin("aro", true);
        }

        /// <summary>Pared en arco vista desde adentro; los ángulos van en grados respecto a +Z.</summary>
        public static Mesh Arco(float radio, float desde, float hasta, float y0, float y1, int tramos = 96)
        {
            var m = new M();
            for (int i = 0; i < tramos; i++)
            {
                float u0 = i / (float)tramos, u1 = (i + 1) / (float)tramos;
                float a0 = Mathf.Lerp(desde, hasta, u0) * Mathf.Deg2Rad, a1 = Mathf.Lerp(desde, hasta, u1) * Mathf.Deg2Rad;
                var dentro = -Circ((a0 + a1) / 2, 1, 0);
                m.Quad(Circ(a0, radio, y0), Circ(a1, radio, y0), Circ(a1, radio, y1), Circ(a0, radio, y1), dentro,
                       new Vector2(u0, 0), new Vector2(u1, 0), new Vector2(u1, 1), new Vector2(u0, 1));
            }
            return m.Fin("arco", true);
        }

        /// <summary>Panel rectangular con esquinas redondeadas y grosor, centrado; la cara visible mira a −Z.</summary>
        public static Mesh Panel(float ancho, float alto, float fondo, float radio)
        {
            var m = new M();
            var borde = new List<Vector2>();
            var centros = new[] { new Vector2(ancho / 2 - radio, alto / 2 - radio), new Vector2(-ancho / 2 + radio, alto / 2 - radio),
                                  new Vector2(-ancho / 2 + radio, -alto / 2 + radio), new Vector2(ancho / 2 - radio, -alto / 2 + radio) };
            for (int c = 0; c < 4; c++)
                for (int k = 0; k <= 8; k++)
                {
                    float a = (c * 90 + k * 90f / 8) * Mathf.Deg2Rad;
                    borde.Add(centros[c] + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radio);
                }
            for (int i = 0; i < borde.Count; i++)
            {
                Vector2 a = borde[i], b = borde[(i + 1) % borde.Count];
                Vector3 af = new Vector3(a.x, a.y, -fondo / 2), bf = new Vector3(b.x, b.y, -fondo / 2), at = new Vector3(a.x, a.y, fondo / 2), bt = new Vector3(b.x, b.y, fondo / 2);
                m.Tri(new Vector3(0, 0, -fondo / 2), af, bf, Vector3.back);
                m.Tri(new Vector3(0, 0, fondo / 2), at, bt, Vector3.forward);
                m.Quad(af, bf, bt, at, new Vector3((a.x + b.x) / 2, (a.y + b.y) / 2, 0));
            }
            return m.Fin("panel", false);
        }

        /// <summary>Roca low poly: esfera facetada con deformación aleatoria.</summary>
        public static Mesh Roca(int semilla)
        {
            var rnd = new System.Random(semilla);
            const int la = 6, lo = 9;
            var p = new Vector3[la + 1, lo];
            for (int i = 0; i <= la; i++)
                for (int j = 0; j < lo; j++)
                {
                    float th = Mathf.PI * i / la, ph = 2 * Mathf.PI * (j + (i % 2) * 0.5f) / lo;
                    float r = (i == 0 || i == la) ? 0.5f : 0.5f * (0.75f + 0.45f * (float)rnd.NextDouble());
                    p[i, j] = new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph)) * r;
                }
            var m = new M();
            for (int i = 0; i < la; i++)
                for (int j = 0; j < lo; j++)
                {
                    int k = (j + 1) % lo;
                    Vector3 a = p[i, j], b = p[i, k], c = p[i + 1, k], d = p[i + 1, j];
                    if (i > 0) m.Tri(a, b, c, a + b + c);
                    if (i < la - 1) m.Tri(a, c, d, a + c + d);
                }
            return m.Fin("roca", false);
        }

        /// <summary>Hoja de alga: cinta que se adelgaza y ondula; base en y = 0 y de dos caras.</summary>
        public static Mesh Alga(float alto, float ancho, int semilla)
        {
            var m = new M(); const int n = 14;
            var rnd = new System.Random(semilla); float fase = (float)rnd.NextDouble() * 6f;
            System.Func<int, Vector3> centro = i => new Vector3(Mathf.Sin(fase + i * 0.55f) * 0.16f * i / n * 2, alto * i / n, Mathf.Cos(fase + i * 0.4f) * 0.08f);
            System.Func<int, float> w = i => ancho * (1f - Mathf.Pow(i / (float)n, 2f) * 0.9f) / 2;
            for (int i = 0; i < n; i++)
            {
                Vector3 a = centro(i) - Vector3.right * w(i), b = centro(i) + Vector3.right * w(i), c = centro(i + 1) + Vector3.right * w(i + 1), d = centro(i + 1) - Vector3.right * w(i + 1);
                m.Quad(a, b, c, d, Vector3.back); m.Quad(a, b, c, d, Vector3.forward);
            }
            return m.Fin("alga", false);
        }

        /// <summary>Cinta que sigue una lista de puntos con un ancho por punto (corbata); mira a «frente».</summary>
        public static Mesh Cinta(Vector3[] puntos, float[] anchos, Vector3 derecha, Vector3 frente)
        {
            var m = new M();
            for (int i = 0; i + 1 < puntos.Length; i++)
                m.Quad(puntos[i] - derecha * anchos[i] / 2, puntos[i] + derecha * anchos[i] / 2, puntos[i + 1] + derecha * anchos[i + 1] / 2, puntos[i + 1] - derecha * anchos[i + 1] / 2, frente);
            return m.Fin("cinta", false);
        }
    }
}
