using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeuralHub.Editor
{
    /// <summary>
    /// Menú NeuralHub: construye el estudio de TV oscuro con primitivas y mallas generadas (reproducible) y compila el ejecutable.
    /// Vista en primera persona desde el podio; 3 sillones en semicírculo; los inversionistas se crean al iniciar la sesión.
    /// </summary>
    public static class ConstruirEstudio
    {
        const string Escena = "Assets/Scenes/Estudio.unity";
        const string Modelos = "Assets/Modelos";

        // ---------- materiales ----------
        internal static Material Mat(string nombre, Color c, float brillo = 0.3f, float metal = 0f, Color? emision = null)
        {
            string ruta = "Assets/Materiales/" + nombre + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null) { m = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(m, ruta); }
            m.shader = Shader.Find("Standard");
            m.color = c; m.SetFloat("_Glossiness", brillo); m.SetFloat("_Metallic", metal);
            if (emision.HasValue) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emision.Value); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; }
            else { m.DisableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.black); }
            EditorUtility.SetDirty(m);
            return m;
        }

        internal static Material MatPlano(string nombre, Color c)
        {
            string ruta = "Assets/Materiales/" + nombre + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null) { m = new Material(Shader.Find("Unlit/Color")); AssetDatabase.CreateAsset(m, ruta); }
            m.shader = Shader.Find("Unlit/Color"); m.color = c;
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material MatLuz(string nombre, Texture tex, Color tinte)
        {
            string ruta = "Assets/Materiales/" + nombre + ".mat";
            var sh = Shader.Find("Legacy Shaders/Particles/Additive");
            var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, ruta); }
            m.shader = sh; m.mainTexture = tex; m.SetColor("_TintColor", tinte);
            EditorUtility.SetDirty(m);
            return m;
        }

        internal static GameObject Prim(PrimitiveType t, string nombre, Transform padre, Vector3 pos, Vector3 esc, Material m, bool sombras = true)
        {
            var g = GameObject.CreatePrimitive(t);
            g.name = nombre; g.transform.SetParent(padre, false);
            g.transform.localPosition = pos; g.transform.localScale = esc;
            var r = g.GetComponent<Renderer>();
            if (m) r.sharedMaterial = m;
            r.shadowCastingMode = sombras ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            var col = g.GetComponent<Collider>(); if (col) Object.DestroyImmediate(col);
            return g;
        }

        static GameObject ConMalla(string nombre, Transform padre, Mesh malla, Material m)
        {
            var g = new GameObject(nombre); g.transform.SetParent(padre, false);
            g.AddComponent<MeshFilter>().sharedMesh = malla;
            var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            return g;
        }

        internal static Font Fuente(string archivo)
        {
            var f = AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/Fuentes/" + archivo);
            return f != null ? f : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        internal static TextMesh Texto(string nombre, Transform padre, Vector3 pos, string txt, float alto, Color c, Font f, int tam = 120)
        {
            var g = new GameObject(nombre); g.transform.SetParent(padre, false); g.transform.localPosition = pos;
            var tm = g.AddComponent<TextMesh>();
            tm.font = f; tm.text = txt; tm.fontSize = tam; tm.characterSize = alto * 10f / tam; tm.color = c;
            tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center;
            g.GetComponent<MeshRenderer>().sharedMaterial = f.material;
            return tm;
        }

        // ---------- mallas generadas (se guardan como assets) ----------
        static T Guardar<T>(T obj, string archivo) where T : Object
        {
            string ruta = Modelos + "/" + archivo;
            var existente = AssetDatabase.LoadAssetAtPath<T>(ruta);
            if (existente != null) { EditorUtility.CopySerialized(obj, existente); EditorUtility.SetDirty(existente); return existente; }
            AssetDatabase.CreateAsset(obj, ruta);
            return obj;
        }

        /// <summary>Cono abierto a lo largo de +z (haz del reflector), de doble cara y con alfa que se desvanece hacia la base.</summary>
        static Mesh Cono(float largo, float radio, int seg = 40)
        {
            var v = new List<Vector3>(); var c = new List<Color>(); var t = new List<int>();
            for (int lado = 0; lado < 2; lado++)
            {
                int b0 = v.Count;
                for (int j = 0; j <= seg; j++)
                {
                    float a = j / (float)seg * Mathf.PI * 2;
                    v.Add(Vector3.zero); c.Add(new Color(1, 1, 1, 1f));
                    v.Add(new Vector3(Mathf.Cos(a) * radio, Mathf.Sin(a) * radio, largo)); c.Add(new Color(1, 1, 1, 0.12f));
                }
                for (int j = 0; j < seg; j++)
                {
                    int p = b0 + j * 2, q = p + 2;
                    if (lado == 0) { t.Add(p); t.Add(p + 1); t.Add(q + 1); } else { t.Add(p); t.Add(q + 1); t.Add(p + 1); }
                }
            }
            var m = new Mesh { name = "Haz" }; m.SetVertices(v); m.SetColors(c); m.SetTriangles(t, 0); m.RecalculateBounds();
            return m;
        }

        /// <summary>Anillo plano (borde luminoso del escenario), visible por ambos lados.</summary>
        static Mesh Anillo(float radio, float ancho, int seg = 160)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            for (int j = 0; j <= seg; j++)
            {
                float a = j / (float)seg * Mathf.PI * 2;
                var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                v.Add(d * (radio - ancho / 2)); v.Add(d * (radio + ancho / 2));
            }
            for (int j = 0; j < seg; j++)
            {
                int p = j * 2, q = p + 2;
                t.AddRange(new[] { p, p + 1, q + 1, p, q + 1, q, p, q + 1, p + 1, p, q, q + 1 });
            }
            var m = new Mesh { name = "Anillo" }; m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        /// <summary>Cuadro horizontal con colores de vértice blancos (círculo de luz en el piso).</summary>
        static Mesh CuadroPiso(float lado)
        {
            float h = lado / 2;
            var m = new Mesh { name = "CuadroPiso" };
            m.vertices = new[] { new Vector3(-h, 0, -h), new Vector3(-h, 0, h), new Vector3(h, 0, h), new Vector3(h, 0, -h) };
            m.uv = new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) };
            m.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            m.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        static Texture2D Radial()
        {
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "Radial", wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                    float a = Mathf.Clamp01(1 - d); a = a * a * (3 - 2 * a);
                    tex.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            tex.Apply();
            return tex;
        }

        // ---------- prefabs ----------
        static Tiburon CrearPrefabTiburon(Font fuente)
        {
            var raiz = new GameObject("Tiburon");
            var t = raiz.AddComponent<Tiburon>();
            t.materiales = new MaterialesFigura
            {
                traje = Mat("Figura_traje", new Color(0.043f, 0.047f, 0.063f), 0.4f, 0.1f),
                piel = Mat("Figura_piel", new Color(0.072f, 0.056f, 0.044f), 0.18f),
                camisa = Mat("Figura_camisa", new Color(0.235f, 0.25f, 0.28f), 0.2f),
                corbata = Mat("Figura_corbata", new Color(0.118f, 0.04f, 0.05f), 0.4f),
                zapato = Mat("Figura_zapato", new Color(0.024f, 0.027f, 0.035f), 0.7f, 0.3f),
                cabello = Mat("Figura_cabello", new Color(0.04f, 0.031f, 0.024f), 0.25f),
                canas = Mat("Figura_canas", new Color(0.2f, 0.21f, 0.23f), 0.2f),
            };
            // Placa con el perfil, en el piso frente al sillón
            var placa = new GameObject("Placa").transform; placa.SetParent(raiz.transform, false);
            placa.localPosition = new Vector3(0, 0.1f, 0.95f); placa.localRotation = Quaternion.Euler(-14.3f, 0, 0);
            Prim(PrimitiveType.Cube, "Base", placa, Vector3.zero, new Vector3(0.62f, 0.13f, 0.03f), Mat("Placa", new Color(0.043f, 0.067f, 0.11f), 0.3f), false);
            var letrero = Texto("Letrero", placa, new Vector3(0, 0, 0.017f), "", 0.065f, new Color(0.79f, 0.71f, 0.54f), fuente, 80);
            letrero.transform.localRotation = Quaternion.Euler(0, 180, 0);
            t.letrero = letrero;
            t.pintables = new Renderer[0];
            var prefab = PrefabUtility.SaveAsPrefabAsset(raiz, "Assets/Prefabs/Tiburon.prefab");
            Object.DestroyImmediate(raiz);
            return prefab.GetComponent<Tiburon>();
        }

        static GameObject CrearPrefabSillon()
        {
            var raiz = new GameObject("Sillon");
            var cuero = Mat("Sillon_cuero", new Color(0.07f, 0.05f, 0.04f), 0.55f);
            var metal = Mat("Metal", new Color(0.23f, 0.26f, 0.32f), 0.65f, 0.8f);
            Prim(PrimitiveType.Cube, "Asiento", raiz.transform, new Vector3(0, 0.38f, 0), new Vector3(0.76f, 0.14f, 0.64f), cuero);
            var resp = Prim(PrimitiveType.Cube, "Respaldo", raiz.transform, new Vector3(0, 0.84f, -0.32f), new Vector3(0.76f, 0.86f, 0.14f), cuero);
            resp.transform.localRotation = Quaternion.Euler(-7.4f, 0, 0);
            foreach (float s in new[] { -1f, 1f })
                Prim(PrimitiveType.Cube, "Descansabrazos", raiz.transform, new Vector3(0.38f * s, 0.56f, 0), new Vector3(0.12f, 0.24f, 0.64f), cuero);
            Prim(PrimitiveType.Cylinder, "Poste", raiz.transform, new Vector3(0, 0.16f, 0), new Vector3(0.1f, 0.15f, 0.1f), metal);
            Prim(PrimitiveType.Cylinder, "Base", raiz.transform, new Vector3(0, 0.03f, 0), new Vector3(0.66f, 0.015f, 0.66f), metal);
            var prefab = PrefabUtility.SaveAsPrefabAsset(raiz, "Assets/Prefabs/Sillon.prefab");
            Object.DestroyImmediate(raiz);
            return prefab;
        }

        static GameObject CrearPrefabEmprendedor()
        {
            // El emprendedor es el usuario: solo se ve su podio en primer plano
            var raiz = new GameObject("Emprendedor");
            Prim(PrimitiveType.Cube, "Podio", raiz.transform, new Vector3(0, 0.54f, 0.67f), new Vector3(1.3f, 1.08f, 0.55f), Mat("Podio", new Color(0.043f, 0.063f, 0.098f), 0.3f, 0.2f));
            Prim(PrimitiveType.Cube, "TopePodio", raiz.transform, new Vector3(0, 1.1f, 0.69f), new Vector3(1.38f, 0.04f, 0.62f), Mat("Podio_tope", new Color(0.027f, 0.04f, 0.063f), 0.1f));
            var tarjetas = Prim(PrimitiveType.Cube, "Tarjetas", raiz.transform, new Vector3(0.22f, 1.125f, 0.75f), new Vector3(0.3f, 0.008f, 0.2f), Mat("Tarjetas", new Color(0.165f, 0.184f, 0.22f), 0.1f));
            tarjetas.transform.localRotation = Quaternion.Euler(0, 8.6f, 0);
            raiz.AddComponent<Emprendedor>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(raiz, "Assets/Prefabs/Emprendedor.prefab");
            Object.DestroyImmediate(raiz);
            return prefab;
        }

        // ---------- escena ----------
        [MenuItem("NeuralHub/Construir estudio")]
        public static void Construir()
        {
            foreach (var d in new[] { "Assets/Scenes", "Assets/Prefabs", "Assets/Materiales", "Assets/StreamingAssets", Modelos })
                if (!AssetDatabase.IsValidFolder(d)) AssetDatabase.CreateFolder("Assets", Path.GetFileName(d));

            var fuenteTitulo = Fuente("SpaceGrotesk-Bold.ttf");
            var fuenteTexto = Fuente("Inter-SemiBold.ttf");
            var radial = Guardar(Radial(), "Radial.asset");
            var mallaHaz = Guardar(Cono(4.5f, Mathf.Tan(17f * Mathf.Deg2Rad) * 4.5f), "Haz.asset");
            var mallaAnillo = Guardar(Anillo(3.1f, 0.024f), "Anillo.asset");
            var mallaPiso = Guardar(CuadroPiso(2.4f), "CuadroPiso.asset");

            var prefabTiburon = CrearPrefabTiburon(fuenteTexto);
            var prefabSillon = CrearPrefabSillon();
            var prefabEmprendedor = CrearPrefabEmprendedor();

            var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var colorFondo = new Color(0.02f, 0.027f, 0.043f);
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.09f, 0.12f, 0.18f);
            RenderSettings.ambientEquatorColor = new Color(0.04f, 0.05f, 0.08f);
            RenderSettings.ambientGroundColor = new Color(0.01f, 0.012f, 0.02f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogColor = colorFondo; RenderSettings.fogDensity = 0.075f;

            var estudioGO = new GameObject("Estudio");
            var e = estudioGO.transform;
            Prim(PrimitiveType.Cube, "Piso", e, new Vector3(0, -0.05f, 0), new Vector3(40, 0.1f, 40), Mat("Piso", new Color(0.02f, 0.027f, 0.043f), 0.1f), false);
            Prim(PrimitiveType.Cylinder, "Escenario", e, new Vector3(0, 0.04f, 0.35f), new Vector3(6.2f, 0.04f, 6.2f), Mat("Escenario", new Color(0.024f, 0.031f, 0.051f), 0.15f, 0.25f), false);
            var anillo = ConMalla("BordeEscenario", e, mallaAnillo, MatPlano("Borde_escenario", new Color(0.18f, 0.36f, 0.59f)));
            anillo.transform.localPosition = new Vector3(0, 0.085f, 0.35f);
            Prim(PrimitiveType.Cube, "MuroFondo", e, new Vector3(0, 3.5f, 3.45f), new Vector3(16, 7, 0.1f), Mat("Muro", new Color(0.031f, 0.047f, 0.082f), 0.05f), false);
            var franja = MatPlano("Franja_luz", new Color(0.106f, 0.173f, 0.282f));
            foreach (float x in new[] { -6.9f, -6.2f, -2.3f, 2.3f, 6.2f, 6.9f })   // dejan libre el lugar de las pantallas laterales
                Prim(PrimitiveType.Cube, "FranjaLuz", e, new Vector3(x, 2.8f, 3.39f), new Vector3(0.05f, 5.2f, 0.02f), franja, false);

            // Pantalla grande del fondo
            var pantallaGO = new GameObject("Pantalla"); pantallaGO.transform.SetParent(e, false);
            var p = pantallaGO.transform;
            Prim(PrimitiveType.Cube, "Marco", p, new Vector3(0, 2.45f, 3.385f), new Vector3(3.48f, 1.78f, 0.02f), Mat("Marco_pantalla", new Color(0.17f, 0.27f, 0.4f), 0.2f, 0, new Color(0.06f, 0.1f, 0.16f)), false);
            var fondo = Prim(PrimitiveType.Cube, "Fondo", p, new Vector3(0, 2.45f, 3.37f), new Vector3(3.4f, 1.7f, 0.02f), Mat("Pantalla_fondo", new Color(0.06f, 0.13f, 0.25f), 0.1f, 0, new Color(0.04f, 0.09f, 0.17f)), false);
            var etq = Texto("Etiqueta", p, new Vector3(0, 2.93f, 3.35f), "", 0.075f, new Color(0.6f, 0.71f, 0.84f), fuenteTexto, 80);
            var tit = Texto("Titulo", p, new Vector3(0, 2.56f, 3.35f), "NeuralHub", 0.24f, new Color(0.91f, 0.93f, 0.96f), fuenteTitulo, 120);
            var det = Texto("Detalle", p, new Vector3(0, 2.13f, 3.35f), "Practica tu pitch ante el panel", 0.095f, new Color(0.79f, 0.84f, 0.91f), fuenteTexto, 90);
            var brillo = new GameObject("BrilloPantalla").AddComponent<Light>(); brillo.transform.SetParent(p, false);
            brillo.type = LightType.Point; brillo.color = new Color(0.24f, 0.48f, 0.82f); brillo.range = 7; brillo.intensity = 0.8f;
            brillo.transform.localPosition = new Vector3(0, 2.3f, 2.6f);
            var pantalla = pantallaGO.AddComponent<Pantalla>(); pantalla.titulo = tit; pantalla.detalle = det; pantalla.etiqueta = etq;
            pantalla.fondo = fondo.GetComponent<Renderer>(); pantalla.brillo = brillo;
            pantalla.icono = ConstruirExtras.IconoPantalla(p, new Vector3(-1.38f, 2.95f, 3.35f), 0.3f);

            // Pantallas laterales: datos de la oferta (izquierda) y marcador del panel (derecha)
            var datos = ConstruirExtras.PantallasLaterales(e, fuenteTitulo, fuenteTexto);

            // Letrero 3D del resultado (arriba de la pantalla)
            var letrero = Texto("LetreroResultado", e, new Vector3(0, 3.65f, 3.3f), "", 0.36f, Color.white, fuenteTitulo, 160);

            // Emprendedor (el usuario) con su podio en primer plano
            var emp = (GameObject)PrefabUtility.InstantiatePrefab(prefabEmprendedor, e);
            emp.transform.position = new Vector3(0, 0, -4.35f); emp.transform.rotation = Quaternion.identity;

            // 3 sillones en semicírculo frente al podio (el del centro, más atrás)
            var sillones = new Transform[3];
            var lugares = new[] { new Vector3(-1.9f, 0.08f, -0.15f), new Vector3(0, 0.08f, 0.75f), new Vector3(1.9f, 0.08f, -0.15f) };
            for (int i = 0; i < 3; i++)
            {
                var s = (GameObject)PrefabUtility.InstantiatePrefab(prefabSillon, e);
                s.name = "Sillon_" + (i + 1);
                s.transform.position = lugares[i];
                var hacia = emp.transform.position - s.transform.position; hacia.y = 0;
                s.transform.rotation = Quaternion.LookRotation(hacia);
                sillones[i] = s.transform;
            }

            // Luces: penumbra fría con contraluces azules; el reflector cálido se enciende al hablar un tiburón
            var contra = new GameObject("Contraluz").AddComponent<Light>();
            contra.type = LightType.Directional; contra.intensity = 0.9f; contra.color = new Color(0.435f, 0.584f, 0.84f);
            contra.transform.rotation = Quaternion.LookRotation(-new Vector3(0, 4.5f, 6f));
            foreach (float s in new[] { -1f, 1f })
            {
                var l = new GameObject("ContraluzLateral").AddComponent<Light>();
                l.type = LightType.Spot; l.color = new Color(0.3f, 0.455f, 0.72f); l.range = 12; l.spotAngle = 52; l.intensity = 1.4f;
                l.transform.position = new Vector3(4.5f * s, 4f, 3.2f);
                l.transform.rotation = Quaternion.LookRotation(new Vector3(1f * s, 1.3f, 0) - l.transform.position);
            }
            var relleno = new GameObject("Relleno").AddComponent<Light>();
            relleno.type = LightType.Point; relleno.color = new Color(0.165f, 0.227f, 0.345f); relleno.range = 9; relleno.intensity = 0.25f;
            relleno.transform.position = new Vector3(0, 2.2f, -3f);

            var reflGO = new GameObject("Reflector");
            var spot = reflGO.AddComponent<Light>();
            spot.type = LightType.Spot; spot.range = 9; spot.spotAngle = 34; spot.intensity = 0f; spot.color = new Color(1f, 0.835f, 0.6f);
            spot.shadows = LightShadows.Soft; spot.shadowBias = 0.02f; spot.enabled = false;
            reflGO.transform.position = new Vector3(0, 4.6f, 0); reflGO.transform.rotation = Quaternion.Euler(90, 0, 0);
            var haz = ConMalla("Haz", reflGO.transform, mallaHaz, MatLuz("Haz_reflector", null, new Color(1f, 0.835f, 0.6f, 0f)));
            var charco = ConMalla("CirculoDeLuz", e, mallaPiso, MatLuz("Circulo_luz", radial, new Color(1f, 0.835f, 0.6f, 0f)));
            charco.transform.position = new Vector3(0, 0.09f, 0);
            var reflector = reflGO.AddComponent<Reflector>();
            reflector.luz = spot; reflector.haz = haz.GetComponent<Renderer>(); reflector.charco = charco.GetComponent<Renderer>();

            // Cámara principal (ojos del emprendedor)
            var camGO = new GameObject("Main Camera"); camGO.tag = "MainCamera";
            var cam = camGO.AddComponent<Camera>(); cam.fieldOfView = 50; cam.nearClipPlane = 0.05f; cam.farClipPlane = 60f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = colorFondo;
            camGO.AddComponent<AudioListener>();
            var camara = camGO.AddComponent<CamaraEstudio>();
            camGO.transform.position = camara.posGeneral;
            camGO.transform.rotation = Quaternion.LookRotation(camara.miraGeneral - camara.posGeneral);

            // Lógica
            var estudio = estudioGO.AddComponent<Estudio>();
            estudio.prefabTiburon = prefabTiburon; estudio.sillones = sillones; estudio.emprendedor = emp.GetComponent<Emprendedor>();
            estudio.camara = camara; estudio.reflector = reflector; estudio.pantalla = pantalla; estudio.letreroResultado = letrero;
            estudio.podio = emp.transform;

            var app = new GameObject("NeuralHub");
            var cliente = app.AddComponent<ClienteServicio>();
            var reproductor = app.AddComponent<ReproductorSesion>();
            reproductor.cliente = cliente; reproductor.estudio = estudio;
            var ctl = app.AddComponent<AppNeuralHub>();
            ctl.cliente = cliente; ctl.reproductor = reproductor; ctl.estudio = estudio; ctl.camara = camara;
            var sonidos = app.AddComponent<Sonidos>();
            ctl.sonidos = sonidos; ctl.datos = datos; reproductor.sonidos = sonidos; reproductor.datos = datos;

            EditorSceneManager.SaveScene(escena, Escena);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Escena, true) };
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log("NeuralHub: estudio construido en " + Escena);
        }

        // ---------- compilación ----------
        [MenuItem("NeuralHub/Compilar para Windows")]
        public static void Compilar()
        {
            Construir();
            PlayerSettings.companyName = "NeuralHub";
            PlayerSettings.productName = "NeuralHub";
            PlayerSettings.defaultScreenWidth = 1920; PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = true;
            Directory.CreateDirectory("Build");
            var rep = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { Escena }, locationPathName = "Build/NeuralHub.exe",
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.None,
            });
            Debug.Log("NeuralHub: compilación " + rep.summary.result + " (" + rep.summary.totalSize / 1024 / 1024 + " MB)");
            if (Application.isBatchMode) EditorApplication.Exit(rep.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
        }
    }
}
