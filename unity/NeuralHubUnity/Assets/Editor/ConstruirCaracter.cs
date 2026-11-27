using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NeuralHub.Editor
{
    /// <summary>
    /// Identidad de cada tiburón (sombrero o accesorio, objeto sobre su podio y emblema de su especialidad),
    /// sus retratos y el set que aparece detrás del emprendedor cuando la cámara lo enfoca.
    /// </summary>
    public static class ConstruirCaracter
    {
        static Color Hex(string h) { Color c; ColorUtility.TryParseHtmlString(h, out c); return c; }
        static Material M(string n, string hex, float brillo = 0.35f, float metal = 0f) { return ConstruirEstudio.Mat(n, Hex(hex), brillo, metal); }
        static Material E(string n, string hex, float f) { return ConstruirEstudio.Emisivo(n, Hex(hex), f); }

        static GameObject Disco(string nombre, Transform p, Vector3 pos, float radio, float alto, Material m, float bisel = 0.02f)
        {
            return ConstruirEstudio.Malla(nombre, p, Mallas.Guardar(Mallas.Disco(radio, alto, Mathf.Min(bisel, alto * 0.45f), 32), "d_" + (int)(radio * 1000) + "_" + (int)(alto * 1000) + "_" + (int)(bisel * 1000)), pos, m);
        }

        static GameObject Caja(string nombre, Transform p, Vector3 pos, float ancho, float alto, float fondo, Material m, float radio = 0.03f)
        {
            radio = Mathf.Min(radio, Mathf.Min(ancho, alto) * 0.45f);
            return ConstruirEstudio.Malla(nombre, p, Mallas.Guardar(Mallas.Panel(ancho, alto, fondo, radio), "c_" + (int)(ancho * 1000) + "_" + (int)(alto * 1000) + "_" + (int)(fondo * 1000) + "_" + (int)(radio * 1000)), pos, m);
        }

        static GameObject Esfera(string nombre, Transform p, Vector3 pos, float d, Material m)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Sphere); g.name = nombre; Object.DestroyImmediate(g.GetComponent<Collider>());
            g.transform.SetParent(p, false); g.transform.localPosition = pos; g.transform.localScale = Vector3.one * d;
            g.GetComponent<Renderer>().sharedMaterial = m;
            return g;
        }

        /// <summary>Lo que se necesita saber de la cabeza para ponerle lentes: centro de cada ojo y la superficie de la cara.</summary>
        class Cara
        {
            public Vector3 ojoIzq, ojoDer;
            public Vector3[] puntos;

            /// <summary>Punto de la superficie frontal de la cabeza a esa altura y posición lateral.</summary>
            public float Frente(float x, float y)
            {
                float z = float.MinValue;
                foreach (var p in puntos) if (Mathf.Abs(p.x - x) < 0.055f && Mathf.Abs(p.y - y) < 0.055f) z = Mathf.Max(z, p.z);
                return z > float.MinValue ? z : ojoDer.z;
            }
        }

        static Cara MedirCara(SkinnedMeshRenderer piel, Mesh horneada, Vector3[] pts, Transform cabeza)
        {
            var tex = new Texture2D(2, 2);
            tex.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(piel.sharedMaterials[0].mainTexture)));
            var uv = horneada.uv;
            var oscuros = Enumerable.Range(0, pts.Length).Where(i =>
            {
                if (pts[i].y < cabeza.position.y) return false;
                var c = tex.GetPixelBilinear(uv[i].x, uv[i].y);
                return c.r < 0.16f && c.g < 0.16f && c.b < 0.16f;
            }).Select(i => pts[i]).ToArray();
            Object.DestroyImmediate(tex);
            System.Func<Vector3[], Vector3> Medio = l =>
            {
                var b = new Bounds(l[0], Vector3.zero); foreach (var p in l) b.Encapsulate(p); return b.center;
            };
            var izq = oscuros.Where(p => p.x < cabeza.position.x - 0.1f).ToArray();
            var der = oscuros.Where(p => p.x > cabeza.position.x + 0.1f).ToArray();
            return new Cara { ojoIzq = Medio(izq), ojoDer = Medio(der), puntos = pts };
        }

        /// <summary>Armazón sobre cada ojo, puente por delante de la cara y patillas hacia atrás.</summary>
        static void Lentes(Transform g, Cara cara, Material armazon, float ancho, float alto, float grosor, bool gato)
        {
            var centro = (cara.ojoIzq + cara.ojoDer) * 0.5f;
            var bordes = new Vector3[2];
            int i = 0;
            foreach (var ojo in new[] { cara.ojoIzq, cara.ojoDer })
            {
                float lado = ojo.x < centro.x ? -1f : 1f;
                var normal = new Vector3(lado * 0.55f, 0.06f, 0.83f).normalized;
                var derecha = Vector3.Cross(Vector3.up, normal).normalized;
                if (Vector3.Dot(derecha, new Vector3(-lado, 0, 1)) < 0) derecha = -derecha;       // apunta hacia la nariz
                var arriba = Vector3.Cross(normal, derecha).normalized; if (arriba.y < 0) arriba = -arriba;
                var c = ojo + normal * 0.115f;
                var puntos = new Vector3[25];
                for (int k = 0; k <= 24; k++)
                {
                    float a = k / 24f * Mathf.PI * 2f;
                    float cx = Mathf.Cos(a), cy = Mathf.Sin(a);
                    // casi rectangular (Teo) o con la esquina de fuera levantada (cat-eye de Estela)
                    float ex = gato ? cx : Mathf.Sign(cx) * Mathf.Pow(Mathf.Abs(cx), 0.55f);
                    float ey = gato ? cy : Mathf.Sign(cy) * Mathf.Pow(Mathf.Abs(cy), 0.55f);
                    float punta = gato && cx < 0 && cy > 0 ? Mathf.Pow(-cx * cy * 2f, 2f) * alto * 0.38f : 0f;
                    puntos[k] = c + derecha * (ex * ancho * 0.5f - punta * 0.5f) + arriba * (ey * alto * 0.5f + punta);
                }
                Pieza3D("Lente", g, puntos, grosor, armazon, "lente_" + (gato ? "g" : "r") + (lado < 0 ? "i" : "d"));
                bordes[i] = c + derecha * ancho * 0.3f + arriba * alto * 0.47f;
                var atras = c - derecha * ancho * 0.5f + arriba * alto * 0.15f;
                Pieza3D("Patilla", g, new[] { atras, atras + new Vector3(-0.005f * lado, 0.01f, -0.13f), atras + new Vector3(-0.035f * lado, -0.02f, -0.27f) }, grosor * 0.75f, armazon,
                        "patilla_" + (gato ? "g" : "r") + (lado < 0 ? "i" : "d"));
                i++;
            }
            // puente: sigue la superficie de la cara de un lente al otro
            var puente = new Vector3[9];
            for (int k = 0; k <= 8; k++)
            {
                float u = k / 8f;
                var p = Vector3.Lerp(bordes[0], bordes[1], u) + Vector3.up * (0.13f * Mathf.Sin(u * Mathf.PI));   // sube sobre el hocico
                if (k > 0 && k < 8) p.z = Mathf.Max(p.z - 0.02f * Mathf.Sin(u * Mathf.PI), cara.Frente(p.x, p.y) + 0.024f);
                puente[k] = p;
            }
            Pieza3D("Puente", g, puente, grosor * 0.8f, armazon, "puente_" + (gato ? "g" : "r"));
        }

        /// <summary>Tubo definido en coordenadas del mundo, colgado del accesorio.</summary>
        static GameObject Pieza3D(string nombre, Transform g, Vector3[] mundo, float radio, Material m, string clave)
        {
            var origen = mundo[0];
            var local = mundo.Select(p => p - origen).ToArray();
            var o = ConstruirEstudio.Malla(nombre, g.parent, Mallas.Guardar(Mallas.Tubo(local, radio, Mathf.Max(24, local.Length * 3), 10), clave), Vector3.zero, m, false);
            o.transform.position = origen; o.transform.SetParent(g, true);
            return o;
        }

        static void Bolita(Transform g, Vector3 mundo, Vector3 escala, Material m, Quaternion? giro = null, string nombre = "Detalle")
        {
            var s = Esfera(nombre, g.parent, Vector3.zero, 1f, m);
            s.transform.position = mundo; s.transform.localScale = escala;
            if (giro.HasValue) s.transform.rotation = giro.Value;
            s.transform.SetParent(g, true);
        }

        // ---------- accesorios de cabeza (se construyen en coordenadas del mundo con el modelo en reposo) ----------
        static Transform Cabeza(string id, Vector3 cima, Vector3 cuello, float frenteCuello, Transform raiz, Cara cara)
        {
            var g = new GameObject("Accesorio_" + id).transform; g.SetParent(raiz, false); g.position = cima;
            switch (id)
            {
                case "tiburon_financiero":      // sombrero de copa
                    Disco("Ala", g, new Vector3(0, -0.04f, 0), 0.3f, 0.04f, M("Sombrero_copa", "#0C1230", 0.55f));
                    Disco("Copa", g, Vector3.zero, 0.19f, 0.34f, M("Sombrero_copa", "#0C1230", 0.55f), 0.03f);
                    Disco("Cinta", g, new Vector3(0, 0.04f, 0), 0.196f, 0.07f, M("Cinta_azul", "#1060D0"), 0.005f);
                    g.localRotation = Quaternion.Euler(-6, 0, -9); break;
                case "tiburon_estratega":       // lentes de gato, pestañas, aretes y collar de perlas, peinado recogido y moño
                    var violeta = M("Lentes_estela", "#7B3FF2", 0.75f, 0.35f);
                    var pestana = M("Pestana", "#0B0B14", 0.2f);
                    var perla = M("Perla", "#FFF3EC", 0.85f, 0.15f);
                    var pelo = M("Pelo_estela", "#4A2545", 0.45f, 0.1f);
                    Lentes(g, cara, violeta, 0.215f, 0.165f, 0.017f, true);
                    foreach (var ojo in new[] { cara.ojoIzq, cara.ojoDer })
                    {
                        float lado = ojo.x < cima.x ? -1f : 1f;
                        var normal = new Vector3(lado * 0.55f, 0.06f, 0.83f).normalized;
                        var atras = new Vector3(lado * 0.52f, 0, -0.85f);                // hacia la sien
                        for (int k = 0; k < 3; k++)
                        {
                            var raizP = ojo + normal * 0.112f + Vector3.up * (0.088f - k * 0.012f) + atras * (0.01f + k * 0.034f);
                            Pieza3D("Pestana", g, new[] { raizP, raizP + (Vector3.up * 0.045f + normal * 0.012f + atras * 0.012f),
                                    raizP + (Vector3.up * 0.075f + normal * 0.026f + atras * 0.032f) }, 0.009f, pestana, "pestana" + k + (lado < 0 ? "i" : "d"));
                        }
                        var oreja = ojo + new Vector3(lado * 0.075f, -0.2f, -0.13f);     // aretes
                        Bolita(g, oreja, Vector3.one * 0.05f, M("Oro", "#E9B949", 0.75f, 0.7f));
                        Bolita(g, oreja + new Vector3(0, -0.065f, 0), Vector3.one * 0.085f, perla);
                    }
                    float anchoCara = Mathf.Abs(cara.ojoDer.x - cima.x);
                    Bolita(g, cima + new Vector3(0, -0.015f, -0.07f), new Vector3(0.46f, 0.2f, 0.42f), pelo, Quaternion.Euler(-12, 0, 0));
                    foreach (int lado in new[] { -1, 1 })
                    {
                        float yo = cara.ojoDer.y, zo = cara.ojoDer.z;
                        // melena: mechones que bajan por detrás de los ojos y terminan abiertos a la altura de la mandíbula
                        var mechon = new[] { cima + new Vector3(0.1f * lado, 0.0f, -0.12f), new Vector3(cima.x + (anchoCara + 0.03f) * lado, yo + 0.12f, zo - 0.16f),
                                             new Vector3(cima.x + (anchoCara + 0.08f) * lado, yo - 0.14f, zo - 0.17f), new Vector3(cima.x + (anchoCara + 0.07f) * lado, yo - 0.38f, zo - 0.14f),
                                             new Vector3(cima.x + (anchoCara + 0.045f) * lado, yo - 0.5f, zo - 0.13f) };
                        Pieza3D("Melena", g, mechon, 0.085f, pelo, "melena" + (lado < 0 ? "i" : "d"));
                        Bolita(g, mechon[4], new Vector3(0.168f, 0.1f, 0.168f), pelo);
                        Bolita(g, cima + new Vector3(0.2f + 0.065f * lado, 0.02f, 0.06f), new Vector3(0.15f, 0.085f, 0.06f), violeta, Quaternion.Euler(0, 0, 24 * lado));
                    }
                    Bolita(g, cima + new Vector3(0.2f, 0.02f, 0.07f), Vector3.one * 0.055f, violeta);
                    Collar(g, cuello, frenteCuello, 19, 0.058f, perla, 0.17f); break;
                case "tiburon_tecnologico":     // lentes rectangulares con luz y audífonos de diadema con micrófono
                    Lentes(g, cara, E("Lentes_teo", "#13B8F0", 1.1f), 0.22f, 0.15f, 0.018f, false);
                    var negro = M("Audifono", "#141B2E", 0.55f, 0.15f);
                    var acolchado = M("Audifono_almohada", "#0A0E1A", 0.15f);
                    var luz = E("Audifono_luz", "#13B8F0", 1.3f);
                    // las copas van justo detrás de los ojos, medidas sobre el modelo
                    float x = Mathf.Abs(cara.ojoDer.x - cima.x) + 0.085f;
                    float yCopa = cara.ojoDer.y - cima.y - 0.06f, zCopa = cara.ojoDer.z - cima.z - 0.19f;
                    var diadema = new[] { new Vector3(-x, yCopa + 0.08f, zCopa), new Vector3(-x * 0.9f, -0.07f, zCopa), new Vector3(-x * 0.52f, 0.06f, zCopa),
                                          new Vector3(0, 0.1f, zCopa), new Vector3(x * 0.52f, 0.06f, zCopa), new Vector3(x * 0.9f, -0.07f, zCopa), new Vector3(x, yCopa + 0.08f, zCopa) };
                    ConstruirEstudio.Malla("Diadema", g, Mallas.Guardar(Mallas.Tubo(diadema, 0.036f), "diadema"), Vector3.zero, negro);
                    var forro = new[] { new Vector3(-x * 0.62f, 0.0f, zCopa), new Vector3(-x * 0.3f, 0.056f, zCopa), new Vector3(0, 0.067f, zCopa),
                                        new Vector3(x * 0.3f, 0.056f, zCopa), new Vector3(x * 0.62f, 0.0f, zCopa) };
                    ConstruirEstudio.Malla("Forro", g, Mallas.Guardar(Mallas.Tubo(forro, 0.043f, 28), "forro"), Vector3.zero, acolchado);
                    foreach (int lado in new[] { -1, 1 })
                    {
                        var eje = Quaternion.Euler(0, 0, -90 * lado);       // el disco apunta hacia afuera
                        var almohada = Disco("Almohada", g, new Vector3((x - 0.065f) * lado, yCopa, zCopa), 0.125f, 0.055f, acolchado, 0.025f);
                        almohada.transform.localRotation = eje;
                        var copa = Disco("Copa", g, new Vector3((x - 0.015f) * lado, yCopa, zCopa), 0.115f, 0.07f, negro, 0.032f);
                        copa.transform.localRotation = eje;
                        var aro = ConstruirEstudio.Malla("Aro", g, Mallas.Guardar(Mallas.Aro(0.056f, 0.07f, 40), "aro_audifono"), new Vector3((x + 0.058f) * lado, yCopa, zCopa), luz, false);
                        aro.transform.localRotation = eje;
                    }
                    var brazo = new[] { new Vector3(-x - 0.03f, yCopa - 0.06f, zCopa + 0.05f), new Vector3(-x - 0.03f, yCopa - 0.17f, zCopa + 0.2f),
                                        new Vector3(-x + 0.06f, yCopa - 0.2f, zCopa + 0.38f), new Vector3(-x + 0.16f, yCopa - 0.19f, zCopa + 0.46f) };
                    ConstruirEstudio.Malla("Brazo", g, Mallas.Guardar(Mallas.Tubo(brazo, 0.013f, 32, 8), "brazo_micro"), Vector3.zero, negro);
                    var espuma = Esfera("Micro", g, brazo[3], 0.075f, acolchado);
                    espuma.transform.localScale = new Vector3(0.1f, 0.065f, 0.065f);
                    Esfera("LuzMicro", g, brazo[3] + new Vector3(0.048f, 0, 0.012f), 0.022f, luz);
                    break;
                case "tiburona_consumo":        // flor grande y collar de perlas
                    var petalo = M("Flor", "#FF6FA5", 0.4f);
                    var flor = new GameObject("Flor").transform; flor.SetParent(g, false); flor.localPosition = new Vector3(0.2f, -0.03f, 0.08f); flor.localRotation = Quaternion.Euler(30, 25, -25);
                    for (int k = 0; k < 6; k++)
                    {
                        float a = k * Mathf.PI / 3;
                        var s = Esfera("Petalo", flor, new Vector3(Mathf.Cos(a) * 0.12f, 0, Mathf.Sin(a) * 0.12f), 0.15f, petalo);
                        s.transform.localScale = new Vector3(0.16f, 0.07f, 0.16f);
                    }
                    Esfera("Centro", flor, new Vector3(0, 0.03f, 0), 0.12f, M("Oro", "#E9B949", 0.75f, 0.7f));
                    var perlaC = M("Perla", "#FFF3EC", 0.85f, 0.15f);
                    CollarSobrePecho(g, cara, cuello + Vector3.up * 0.14f, 25, 0.064f, perlaC, 0.16f, 0.27f);
                    CollarSobrePecho(g, cara, cuello + Vector3.up * 0.14f, 1, 0.125f, M("Dije_coral", "#FF7A45", 0.8f, 0.3f), 0.215f, 0f); break;
                case "tiburon_mentor":          // birrete de maestro
                    var tela = M("Birrete", "#15331F", 0.25f);
                    Disco("Casquete", g, new Vector3(0, -0.06f, 0), 0.21f, 0.12f, tela, 0.05f);
                    var tabla = Caja("Tabla", g, new Vector3(0, 0.075f, 0), 0.58f, 0.58f, 0.035f, tela, 0.03f);
                    tabla.transform.localRotation = Quaternion.Euler(90, 45, 0);
                    for (int k = 0; k <= 7; k++) Esfera("Cordon", g, new Vector3(Mathf.Min(0.3f, k * 0.06f), 0.1f - Mathf.Max(0, k - 5) * 0.08f, 0), 0.035f, M("Oro", "#E9B949", 0.75f, 0.7f));
                    Caja("Borla", g, new Vector3(0.31f, -0.16f, 0), 0.07f, 0.12f, 0.07f, M("Oro", "#E9B949", 0.75f, 0.7f), 0.02f);
                    g.localRotation = Quaternion.Euler(-5, 0, -5); break;
                default:                        // negociador: sombrero fedora y cadena de oro
                    var fieltro = M("Fedora", "#2B1A1A", 0.2f);
                    Disco("Ala", g, new Vector3(0, -0.03f, 0), 0.36f, 0.04f, fieltro);
                    Disco("Copa", g, Vector3.zero, 0.22f, 0.2f, fieltro, 0.08f);
                    Disco("Cinta", g, new Vector3(0, 0.035f, 0), 0.226f, 0.06f, M("Cinta_roja", "#C92A2A"), 0.005f);
                    g.localRotation = Quaternion.Euler(8, 0, 12);
                    Collar(g, cuello, frenteCuello, 21, 0.045f, M("Oro", "#E9B949", 0.75f, 0.7f), 0.27f); break;
            }
            return g;
        }

        /// <summary>Collar que sigue la forma del pecho (el cuerpo musculoso sobresale y escondía las cuentas).</summary>
        static void CollarSobrePecho(Transform g, Cara cara, Vector3 cuello, int cuentas, float d, Material m, float caida, float medioAncho)
        {
            for (int k = 0; k < cuentas; k++)
            {
                float u = cuentas == 1 ? 0f : k / (cuentas - 1f) * 2f - 1f;
                float x = cuello.x + u * medioAncho, y = cuello.y - 0.08f - caida * (1 - u * u);
                float z = cara.Frente(x, y);
                Bolita(g, new Vector3(x, y, z + d * 0.38f), Vector3.one * d, m, null, "Cuenta");
            }
        }

        static void Collar(Transform g, Vector3 cuello, float frente, int cuentas, float d, Material m, float caida)
        {
            for (int k = 0; k < cuentas; k++)
            {
                float u = k / (cuentas - 1f) * 2f - 1f;
                var mundo = new Vector3(cuello.x + u * 0.23f, cuello.y - 0.12f - caida * (1 - u * u), frente + 0.03f - 0.1f * u * u);
                var s = Esfera("Cuenta", g.parent, Vector3.zero, d, m);
                s.transform.position = mundo; s.transform.SetParent(g, true);
            }
        }

        // ---------- objeto sobre el podio ----------
        static GameObject SobrePodio(string id, Transform raiz)
        {
            var g = new GameObject("Podio_" + id).transform; g.SetParent(raiz, false); g.localPosition = new Vector3(0.28f, 1.155f, 1.12f);
            var oro = M("Oro", "#E9B949", 0.75f, 0.7f);
            switch (id)
            {
                case "tiburon_financiero":
                    int[] alturas = { 6, 4, 2 };
                    for (int i = 0; i < 3; i++)
                        for (int k = 0; k < alturas[i]; k++) Disco("Moneda", g, new Vector3(-0.19f * i + 0.06f, k * 0.037f, (i % 2) * 0.08f), 0.085f, 0.033f, oro, 0.008f);
                    break;
                case "tiburon_estratega":
                    var pieza = M("Ajedrez", "#E8E4FF", 0.6f);
                    Disco("Base", g, Vector3.zero, 0.12f, 0.05f, pieza); Disco("Cuerpo", g, new Vector3(0, 0.05f, 0), 0.062f, 0.24f, pieza);
                    Disco("Collarin", g, new Vector3(0, 0.29f, 0), 0.095f, 0.035f, pieza); Disco("Corona", g, new Vector3(0, 0.32f, 0), 0.055f, 0.1f, pieza, 0.03f);
                    Caja("Cruz", g, new Vector3(0, 0.47f, 0), 0.04f, 0.12f, 0.04f, oro, 0.008f); Caja("Cruz2", g, new Vector3(0, 0.485f, 0), 0.1f, 0.035f, 0.04f, oro, 0.008f);
                    break;
                case "tiburon_tecnologico":
                    var carcasa = M("Laptop", "#C9D6E8", 0.7f, 0.4f);
                    var base_ = Caja("Base", g, new Vector3(-0.08f, 0.015f, 0), 0.42f, 0.28f, 0.025f, carcasa); base_.transform.localRotation = Quaternion.Euler(90, 0, 0);
                    var tapa = new GameObject("Tapa").transform; tapa.SetParent(g, false); tapa.localPosition = new Vector3(-0.08f, 0.16f, -0.17f); tapa.localRotation = Quaternion.Euler(-14, 180, 0);
                    Caja("Marco", tapa, Vector3.zero, 0.42f, 0.28f, 0.022f, carcasa);
                    Caja("Pantalla", tapa, new Vector3(0, 0, -0.014f), 0.37f, 0.23f, 0.006f, E("Laptop_luz", "#13B8F0", 1.4f), 0.01f);
                    break;
                case "tiburona_consumo":
                    var bolsa = M("Bolsa", "#E8590C", 0.45f);
                    Caja("Bolsa", g, new Vector3(-0.06f, 0.17f, 0), 0.3f, 0.34f, 0.14f, bolsa, 0.025f);
                    for (int k = 0; k <= 10; k++) { float a = k / 10f * Mathf.PI; Esfera("Asa", g, new Vector3(-0.06f + Mathf.Cos(a) * 0.09f, 0.34f + Mathf.Sin(a) * 0.09f, 0), 0.028f, M("Bolsa_asa", "#FFF3EC")); }
                    Esfera("Marca", g, new Vector3(-0.06f, 0.17f, 0.08f), 0.09f, M("Bolsa_asa", "#FFF3EC"));
                    break;
                case "tiburon_mentor":
                    string[] colores = { "#2F9E44", "#E9B949", "#1060D0" };
                    for (int i = 0; i < 3; i++)
                    {
                        var libro = Caja("Libro", g, new Vector3(-0.07f, 0.04f + i * 0.075f, 0), 0.38f - i * 0.04f, 0.26f - i * 0.02f, 0.07f, M("Libro" + i, colores[i]), 0.015f);
                        libro.transform.localRotation = Quaternion.Euler(90, (i - 1) * 14f, 0);
                    }
                    break;
                default:
                    var cuero = M("Maletin", "#7A3E1D", 0.45f);
                    Caja("Maletin", g, new Vector3(-0.08f, 0.15f, 0), 0.44f, 0.3f, 0.13f, cuero, 0.04f);
                    for (int k = 0; k <= 8; k++) { float a = k / 8f * Mathf.PI; Esfera("Asa", g, new Vector3(-0.08f + Mathf.Cos(a) * 0.09f, 0.3f + Mathf.Sin(a) * 0.07f, 0), 0.035f, cuero); }
                    Caja("Broche", g, new Vector3(-0.08f, 0.2f, 0.072f), 0.07f, 0.06f, 0.012f, oro, 0.012f);
                    break;
            }
            return g.gameObject;
        }

        /// <summary>Emblema flotante de la especialidad, detrás del tiburón.</summary>
        static GameObject Emblema(string id, string lamina, Transform raiz)
        {
            var g = new GameObject("Emblema_" + id).transform; g.SetParent(raiz, false); g.localPosition = new Vector3(-1.02f, 2.75f, -0.75f);
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad); Object.DestroyImmediate(q.GetComponent<Collider>());
            q.name = "Imagen"; q.transform.SetParent(g, false); q.transform.localRotation = Quaternion.Euler(0, 180, 0); q.transform.localScale = Vector3.one * 0.82f;
            var m = ConstruirEstudio.Material_("Emblema_" + id, "Unlit/Transparent");
            m.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Laminas/" + lamina + ".png");
            q.GetComponent<Renderer>().sharedMaterial = m;
            var o = g.gameObject.AddComponent<Ondular>(); o.vertical = true; o.amplitud = 2.5f; o.velocidad = 0.9f;
            return g.gameObject;
        }

        /// <summary>Agrega a un prefab de tiburón en construcción un juego de piezas por perfil.</summary>
        public static Tiburon.Juego[] Juegos(Transform raiz, List<Tiburon.Modelo> modelos)
        {
            var juegos = new List<Tiburon.Juego>();
            foreach (var persona in Personas.Todas)
            {
                int v = Tiburon.Variante(persona.id);
                var modelo = modelos[v];
                bool activo = modelo.raiz.activeSelf; modelo.raiz.SetActive(true);
                var huesos = modelo.raiz.GetComponentsInChildren<Transform>(true);
                var cabeza = huesos.First(t => t.name == "Head"); var cuello = huesos.First(t => t.name == "Neck"); var torso = huesos.First(t => t.name == "Torso");
                var piel = modelo.raiz.GetComponentsInChildren<SkinnedMeshRenderer>(true).OrderByDescending(r => r.sharedMesh.vertexCount).First();
                var horneada = new Mesh(); piel.BakeMesh(horneada, true);
                var pts = horneada.vertices.Select(p => piel.transform.TransformPoint(p)).ToArray();
                var centro = pts.Where(p => Mathf.Abs(p.x - cabeza.position.x) < 0.14f).ToArray();
                float yMax = centro.Max(p => p.y);
                float zCima = centro.Where(p => p.y > yMax - 0.16f).Average(p => p.z);
                var cima = new Vector3(cabeza.position.x, yMax - 0.07f, zCima - 0.03f);
                var pecho = centro.Where(p => Mathf.Abs(p.y - (cuello.position.y - 0.35f)) < 0.08f).ToArray();
                float frente = pecho.Length > 0 ? pecho.Max(p => p.z) : cuello.position.z + 0.25f;

                var acc = Cabeza(persona.id, cima, cuello.position, frente, raiz, MedirCara(piel, horneada, pts, cabeza));
                var cuentas = acc.GetComponentsInChildren<Transform>().Where(t => t.name == "Cuenta").ToArray();
                var piezas = new List<GameObject>();
                var collar = new GameObject("Collar_" + persona.id).transform; collar.SetParent(raiz, false);
                foreach (var c in cuentas) c.SetParent(collar, true);
                acc.SetParent(cabeza, true); collar.SetParent(torso, true);
                piezas.Add(acc.gameObject); piezas.Add(collar.gameObject);
                piezas.Add(SobrePodio(persona.id, raiz));
                piezas.Add(Emblema(persona.id, persona.lamina, raiz));
                foreach (var p in piezas) p.SetActive(false);
                juegos.Add(new Tiburon.Juego { id = persona.id, piezas = piezas.ToArray() });
                modelo.raiz.SetActive(activo);
            }
            return juegos.ToArray();
        }

        // ---------- retratos ----------
        public static void Retratos(Tiburon prefab)
        {
            Directory.CreateDirectory("Assets/Resources/Retratos");
            var camGO = new GameObject("CamaraRetrato"); var cam = camGO.AddComponent<Camera>();
            cam.fieldOfView = 30; cam.clearFlags = CameraClearFlags.SolidColor; cam.nearClipPlane = 0.1f;
            var key = new GameObject("LuzRetrato").AddComponent<Light>(); key.type = LightType.Directional; key.intensity = 1.15f; key.color = new Color(1f, 0.95f, 0.9f);
            key.transform.rotation = Quaternion.Euler(30, 155, 0);
            foreach (var persona in Personas.Todas)
            {
                var t = (Tiburon)PrefabUtility.InstantiatePrefab(prefab);
                t.transform.position = new Vector3(0, 200, 0);
                t.Asignar(persona.id, persona.nombre);
                if (t.paleta) t.paleta.gameObject.SetActive(false);
                t.transform.Find("AroPerfil").gameObject.SetActive(false);
                t.letrero.gameObject.SetActive(false);
                foreach (var hijo in t.GetComponentsInChildren<Transform>()) if (hijo.name.StartsWith("Podio_") || hijo.name.StartsWith("Emblema_")) hijo.gameObject.SetActive(false);
                int v = Tiburon.Variante(persona.id);
                var clip = ConstruirPersonajes.Clip(v == 0 ? "Characters_Sharky.fbx" : "Characters_Mako.fbx", "Idle");
                if (clip) clip.SampleAnimation(t.modelos[v].raiz, 0.2f);
                float alto = v == 0 ? 2.12f : 2.2f;
                var mira = t.transform.position + new Vector3(0, alto, 0.15f);
                cam.transform.position = mira + new Vector3(0.75f, 0.25f, 3.35f);
                cam.transform.rotation = Quaternion.LookRotation(mira - cam.transform.position);
                var fondo = Color.Lerp(Hex("#00165C"), Perfiles.Color(persona.id), 0.55f); cam.backgroundColor = fondo;
                var rt = new RenderTexture(384, 384, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 8 };
                cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
                var tex = new Texture2D(384, 384, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, 384, 384), 0, 0); tex.Apply();
                File.WriteAllBytes("Assets/Resources/Retratos/" + persona.id + ".png", tex.EncodeToPNG());
                cam.targetTexture = null; RenderTexture.active = null; Object.DestroyImmediate(rt); Object.DestroyImmediate(tex);
                Object.DestroyImmediate(t.gameObject);
            }
            Object.DestroyImmediate(camGO); Object.DestroyImmediate(key.gameObject);
            AssetDatabase.Refresh();
        }

        // ---------- set detrás del emprendedor ----------
        public static SetEmprendedor FondoEmprendedor(Transform estudio)
        {
            var raiz = new GameObject("SetEmprendedor").transform; raiz.SetParent(estudio, false);
            var set = raiz.gameObject.AddComponent<SetEmprendedor>();
            var contenido = new GameObject("Contenido").transform; contenido.SetParent(raiz, false);
            set.contenido = contenido.gameObject;

            // La toma del emprendedor mira hacia atrás en diagonal: el set se orienta según esa dirección
            var mirada = new Vector3(-1.8f, 0, -3.6f).normalized;
            var derecha = Vector3.Cross(Vector3.up, mirada);
            var origen = new Vector3(0, 0, -2.5f);
            System.Func<float, float, float, Vector3> En = (lado, alto, lejos) => origen + mirada * lejos + derecha * lado + Vector3.up * alto;
            var giro = Quaternion.LookRotation(mirada);

            var oceano = ConstruirEstudio.Material_("Oceano", "Unlit/Texture");
            var fondo = Caja("Ventana", contenido, En(0, 3.2f, 6.2f), 15f, 7.4f, 0.1f, oceano, 0.6f);
            fondo.transform.localRotation = giro;
            var mf = fondo.GetComponent<MeshFilter>();
            var malla = Object.Instantiate(mf.sharedMesh);
            malla.uv = malla.vertices.Select(v => new Vector2(v.x / 15f + 0.5f, v.y / 7.4f + 0.5f)).ToArray();
            mf.sharedMesh = Mallas.Guardar(malla, "ventana_emprendedor");

            var marco = M("Marco_ventanal", "#0B1B30", 0.55f, 0.35f);
            var neon = E("Neon_ventanal", "#7FE3FF", 1.4f);
            foreach (float x in new[] { -4.4f, 4.4f })
            {
                var pilar = Caja("Pilar", contenido, En(x, 3.2f, 6.05f), 0.22f, 7.4f, 0.16f, marco, 0.09f); pilar.transform.localRotation = giro;
            }
            var zocalo = Caja("Zocalo", contenido, En(0, 0.15f, 5.9f), 15.2f, 0.7f, 0.2f, marco, 0.1f); zocalo.transform.localRotation = giro;
            var luz = Caja("ZocaloLuz", contenido, En(0, 0.53f, 5.85f), 15.2f, 0.06f, 0.1f, neon, 0.02f); luz.transform.localRotation = giro;

            // Letrero del programa
            var placa = Caja("PlacaPrograma", contenido, En(0, 4.05f, 5.7f), 6.4f, 0.95f, 0.14f, M("Placa_programa", "#00165C", 0.5f), 0.4f); placa.transform.localRotation = giro;
            var borde = Caja("PlacaBorde", contenido, En(0, 4.05f, 5.76f), 6.56f, 1.11f, 0.08f, E("Neon_violeta", "#4F1FD0", 2.4f), 0.45f); borde.transform.localRotation = giro;
            var titulo = ConstruirEstudio.Texto("Programa", contenido, En(0, 4.05f, 5.58f), Personas.Programa, 100, 0.05f, Color.white, TextAnchor.MiddleCenter, "SpaceGrotesk-Bold");
            titulo.transform.localRotation = giro;

            // Emblema del sector (izquierda de la toma) y nombre de la empresa (derecha)
            var aro = Disco("AroLogo", contenido, En(-2.85f, 2.2f, 5.55f), 1.08f, 0.06f, E("Neon_cian", "#35C6F4", 2.2f), 0.02f); aro.transform.localRotation = giro * Quaternion.Euler(-90, 0, 0);
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad); Object.DestroyImmediate(q.GetComponent<Collider>());
            q.name = "LogoSector"; q.transform.SetParent(contenido, false); q.transform.localPosition = En(-2.85f, 2.2f, 5.42f); q.transform.localRotation = giro; q.transform.localScale = Vector3.one * 2f;
            var ml = ConstruirEstudio.Material_("LogoSector", "Unlit/Transparent");
            ml.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Laminas/sector_tecnologia.png");
            q.GetComponent<Renderer>().sharedMaterial = ml; set.logo = q.GetComponent<Renderer>();

            var cartel = Caja("CartelEmpresa", contenido, En(2.85f, 2.25f, 5.65f), 3.1f, 1.7f, 0.12f, M("Placa_programa", "#00165C", 0.5f), 0.3f); cartel.transform.localRotation = giro;
            var rotulo = ConstruirEstudio.Texto("Rotulo", contenido, En(2.85f, 2.82f, 5.55f), "HOY PRESENTA", 100, 0.017f, Hex("#13B8F0"), TextAnchor.MiddleCenter);
            rotulo.transform.localRotation = giro;
            set.empresa = ConstruirEstudio.Texto("Empresa", contenido, En(2.85f, 2.12f, 5.55f), "Tu empresa", 100, 0.032f, Color.white, TextAnchor.MiddleCenter, "SpaceGrotesk-Bold");
            set.empresa.transform.localRotation = giro;

            // Algas y rocas al pie
            var rnd = new System.Random(9);
            var mallas = new[] { Mallas.Guardar(Mallas.Alga(1.9f, 0.3f, 1), "alga_a"), Mallas.Guardar(Mallas.Alga(1.4f, 0.26f, 2), "alga_b"), Mallas.Guardar(Mallas.Alga(2.4f, 0.34f, 3), "alga_c") };
            var colores = new[] { E("Alga_verde", "#2FBF87", 0.2f), E("Alga_coral", "#F2766B", 0.2f), E("Alga_violeta", "#8E6BE8", 0.2f), E("Alga_turquesa", "#1FA6A0", 0.2f) };
            var rocas = new[] { Mallas.Guardar(Mallas.Roca(1), "roca_a"), Mallas.Guardar(Mallas.Roca(2), "roca_b") };
            int n = 0;
            foreach (float x in new[] { -6.2f, -5.2f, -4.1f, 4.2f, 5.3f, 6.3f, -1.7f, 1.6f })
            {
                bool bajo = Mathf.Abs(x) < 2f;
                var roca = ConstruirEstudio.Malla("Roca", contenido, rocas[n % 2], En(x, 0.25f, 5.1f), M("Roca_azul", "#2E5D80", 0.1f), false);
                roca.transform.localScale = new Vector3(1.4f, bajo ? 0.45f : 0.9f, 1f);
                for (int k = 0; k < (bajo ? 3 : 5); k++)
                {
                    var alga = ConstruirEstudio.Malla("Alga", contenido, mallas[rnd.Next(3)], En(x + (float)(rnd.NextDouble() - 0.5) * 0.9f, 0.2f, 5.25f + (float)rnd.NextDouble() * 0.3f), colores[n % 4], false);
                    alga.transform.localRotation = giro * Quaternion.Euler(0, rnd.Next(-35, 35), rnd.Next(-8, 8));
                    alga.transform.localScale = Vector3.one * (bajo ? 0.5f : 0.75f + (float)rnd.NextDouble() * 0.4f);
                    var o = alga.AddComponent<Ondular>(); o.amplitud = 4f + (float)rnd.NextDouble() * 3f; o.velocidad = 0.8f + (float)rnd.NextDouble() * 0.5f;
                }
                n++;
            }
            string[] peces = { "Peces/Fish1.fbx", "Peces/Fish2.fbx", "Peces/Fish3.fbx" };
            for (int i = 0; i < 5; i++)
            {
                var pez = ConstruirPersonajes.Pez(peces[i % 3], contenido, 0.42f + 0.07f * i);
                pez.transform.position = En(-5.5f + i * 2.7f, 1.1f + (i % 3) * 1.25f + (i == 2 ? 2.2f : 0), 5.5f);
                pez.transform.rotation = Quaternion.LookRotation(derecha * (i % 2 == 0 ? 1 : -1));
                var o = pez.AddComponent<Ondular>(); o.vertical = true; o.amplitud = 4f; o.velocidad = 0.9f + i * 0.13f;
            }
            var foco = new GameObject("LuzSet").AddComponent<Light>(); foco.transform.SetParent(contenido, false); foco.transform.position = En(0, 3.5f, 3.2f);
            foco.type = LightType.Point; foco.range = 7f; foco.intensity = 1.2f; foco.color = Hex("#8FB8FF");
            contenido.gameObject.SetActive(false);
            return set;
        }
    }
}
