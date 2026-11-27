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

        // ---------- accesorios de cabeza (se construyen en coordenadas del mundo con el modelo en reposo) ----------
        static Transform Cabeza(string id, Vector3 cima, Vector3 cuello, float frenteCuello, Transform raiz)
        {
            var g = new GameObject("Accesorio_" + id).transform; g.SetParent(raiz, false); g.position = cima;
            switch (id)
            {
                case "tiburon_financiero":      // sombrero de copa
                    Disco("Ala", g, new Vector3(0, -0.04f, 0), 0.3f, 0.04f, M("Sombrero_copa", "#0C1230", 0.55f));
                    Disco("Copa", g, Vector3.zero, 0.19f, 0.34f, M("Sombrero_copa", "#0C1230", 0.55f), 0.03f);
                    Disco("Cinta", g, new Vector3(0, 0.04f, 0), 0.196f, 0.07f, M("Cinta_azul", "#1060D0"), 0.005f);
                    g.localRotation = Quaternion.Euler(-6, 0, -9); break;
                case "tiburon_estratega":       // gorra de capitana
                    Disco("Base", g, new Vector3(0, -0.04f, 0), 0.24f, 0.1f, M("Gorra_banda", "#141C4A"), 0.02f);
                    Disco("Tapa", g, new Vector3(0, 0.05f, 0), 0.29f, 0.11f, M("Gorra_blanca", "#F4F7FF"), 0.05f);
                    var visera = Caja("Visera", g, new Vector3(0, -0.02f, 0.25f), 0.34f, 0.16f, 0.03f, M("Gorra_banda", "#141C4A"), 0.07f);
                    visera.transform.localRotation = Quaternion.Euler(102, 0, 0);
                    Esfera("Insignia", g, new Vector3(0, 0.03f, 0.255f), 0.08f, M("Oro", "#E9B949", 0.75f, 0.7f));
                    g.localRotation = Quaternion.Euler(-4, 0, 6); break;
                case "tiburon_tecnologico":     // audífonos de diadema con micrófono
                    var negro = M("Audifono", "#141B2E", 0.55f, 0.15f);
                    var acolchado = M("Audifono_almohada", "#0A0E1A", 0.15f);
                    var luz = E("Audifono_luz", "#13B8F0", 1.3f);
                    const float x = 0.35f, yCopa = -0.34f, zCopa = -0.2f;    // atrás de los ojos
                    var diadema = new[] { new Vector3(-x, yCopa + 0.08f, zCopa), new Vector3(-x * 0.88f, -0.07f, zCopa), new Vector3(-x * 0.5f, 0.06f, zCopa),
                                          new Vector3(0, 0.1f, zCopa), new Vector3(x * 0.5f, 0.06f, zCopa), new Vector3(x * 0.88f, -0.07f, zCopa), new Vector3(x, yCopa + 0.08f, zCopa) };
                    ConstruirEstudio.Malla("Diadema", g, Mallas.Guardar(Mallas.Tubo(diadema, 0.036f), "diadema"), Vector3.zero, negro);
                    var forro = new[] { new Vector3(-x * 0.62f, 0.0f, zCopa), new Vector3(-x * 0.3f, 0.055f, zCopa), new Vector3(0, 0.065f, zCopa),
                                        new Vector3(x * 0.3f, 0.055f, zCopa), new Vector3(x * 0.62f, 0.0f, zCopa) };
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
                    Collar(g, cuello, frenteCuello, 15, 0.065f, M("Perla", "#FFF3EC", 0.85f, 0.15f), 0.13f); break;
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

                var acc = Cabeza(persona.id, cima, cuello.position, frente, raiz);
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
