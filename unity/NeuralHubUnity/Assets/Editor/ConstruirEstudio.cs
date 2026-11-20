using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeuralHub.Editor
{
    /// <summary>Menú NeuralHub: construye el estudio de TV con primitivas (reproducible) y compila el ejecutable.</summary>
    public static class ConstruirEstudio
    {
        const string Escena = "Assets/Scenes/Estudio.unity";

        // ---------- materiales ----------
        static Material Mat(string nombre, Color c, float brillo = 0.3f, bool emision = false)
        {
            string ruta = "Assets/Materiales/" + nombre + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null) { m = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(m, ruta); }
            m.color = c; m.SetFloat("_Glossiness", brillo);
            if (emision) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * 1.6f); }
            EditorUtility.SetDirty(m);
            return m;
        }

        static GameObject Prim(PrimitiveType t, string nombre, Transform padre, Vector3 pos, Vector3 esc, Material m)
        {
            var g = GameObject.CreatePrimitive(t);
            g.name = nombre; g.transform.SetParent(padre, false);
            g.transform.localPosition = pos; g.transform.localScale = esc;
            if (m) g.GetComponent<Renderer>().sharedMaterial = m;
            var col = g.GetComponent<Collider>(); if (col) Object.DestroyImmediate(col);
            return g;
        }

        static TextMesh Texto(string nombre, Transform padre, Vector3 pos, string txt, int tam, float car, Color c, TextAnchor ancla = TextAnchor.MiddleCenter)
        {
            var g = new GameObject(nombre); g.transform.SetParent(padre, false); g.transform.localPosition = pos;
            var tm = g.AddComponent<TextMesh>();
            tm.text = txt; tm.fontSize = tam; tm.characterSize = car; tm.color = c; tm.anchor = ancla; tm.alignment = TextAlignment.Center;
            var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.font = f; g.GetComponent<MeshRenderer>().sharedMaterial = f.material;
            return tm;
        }

        // ---------- prefabs ----------
        static Tiburon CrearPrefabTiburon()
        {
            var raiz = new GameObject("Tiburon");
            var piel = Mat("Tiburon_piel", new Color(0.55f, 0.6f, 0.7f));
            var oscuro = Mat("Tiburon_aleta", new Color(0.25f, 0.28f, 0.38f));
            var blanco = Mat("Blanco", Color.white, 0.6f);
            var negro = Mat("Negro", Color.black, 0.2f);
            var cuerpo = Prim(PrimitiveType.Capsule, "Cuerpo", raiz.transform, new Vector3(0, 1.0f, 0), new Vector3(0.75f, 0.55f, 0.6f), piel);
            var cabeza = Prim(PrimitiveType.Sphere, "Cabeza", raiz.transform, new Vector3(0, 1.85f, 0), new Vector3(0.5f, 0.46f, 0.5f), piel);
            var aleta = Prim(PrimitiveType.Cube, "AletaDorsal", raiz.transform, new Vector3(0, 2.28f, -0.1f), new Vector3(0.06f, 0.5f, 0.32f), oscuro);
            aleta.transform.localRotation = Quaternion.Euler(-12, 0, 0);
            foreach (float x in new[] { -0.12f, 0.12f })
            {
                Prim(PrimitiveType.Sphere, "Ojo", cabeza.transform, new Vector3(x * 2.0f, 0.18f, 0.42f), new Vector3(0.26f, 0.26f, 0.26f), blanco);
                Prim(PrimitiveType.Sphere, "Pupila", cabeza.transform, new Vector3(x * 2.0f, 0.18f, 0.55f), new Vector3(0.12f, 0.12f, 0.12f), negro);
            }
            Prim(PrimitiveType.Cube, "Boca", cabeza.transform, new Vector3(0, -0.22f, 0.45f), new Vector3(0.55f, 0.07f, 0.1f), negro);
            var paleta = Prim(PrimitiveType.Cube, "PaletaVoto", raiz.transform, new Vector3(0.62f, 1.55f, 0.45f), new Vector3(0.55f, 0.4f, 0.04f), blanco);
            var letrero = Texto("Letrero", raiz.transform, new Vector3(0, 0.93f, 1.03f), "", 90, 0.028f, Color.white);
            letrero.transform.localRotation = Quaternion.Euler(20, 180, 0);

            var t = raiz.AddComponent<Tiburon>();
            t.cuerpo = cuerpo.transform; t.cabeza = cabeza.transform; t.aleta = aleta.transform; t.paleta = paleta.transform; t.letrero = letrero;
            t.pintables = new[] { cuerpo.GetComponent<Renderer>(), aleta.GetComponent<Renderer>(), cabeza.GetComponent<Renderer>() };
            raiz.SetActive(true);
            var prefab = PrefabUtility.SaveAsPrefabAsset(raiz, "Assets/Prefabs/Tiburon.prefab");
            Object.DestroyImmediate(raiz);
            return prefab.GetComponent<Tiburon>();
        }

        static GameObject CrearPrefabSillon()
        {
            var raiz = new GameObject("Sillon");
            var cuero = Mat("Sillon_cuero", new Color(0.12f, 0.13f, 0.2f), 0.5f);
            var madera = Mat("Escritorio_madera", new Color(0.35f, 0.2f, 0.12f), 0.4f);
            var tope = Mat("Escritorio_tope", new Color(0.08f, 0.09f, 0.16f), 0.7f);
            Prim(PrimitiveType.Cylinder, "Base", raiz.transform, new Vector3(0, 0.22f, 0), new Vector3(0.25f, 0.22f, 0.25f), tope);
            Prim(PrimitiveType.Cube, "Asiento", raiz.transform, new Vector3(0, 0.47f, 0), new Vector3(1.25f, 0.18f, 1.05f), cuero);
            Prim(PrimitiveType.Cube, "Respaldo", raiz.transform, new Vector3(0, 1.1f, -0.52f), new Vector3(1.25f, 1.25f, 0.18f), cuero);
            Prim(PrimitiveType.Cube, "BrazoI", raiz.transform, new Vector3(-0.68f, 0.78f, 0), new Vector3(0.14f, 0.2f, 0.9f), cuero);
            Prim(PrimitiveType.Cube, "BrazoD", raiz.transform, new Vector3(0.68f, 0.78f, 0), new Vector3(0.14f, 0.2f, 0.9f), cuero);
            Prim(PrimitiveType.Cube, "Escritorio", raiz.transform, new Vector3(0, 0.4f, 1.42f), new Vector3(2.3f, 0.8f, 0.7f), madera);
            Prim(PrimitiveType.Cube, "TopeEscritorio", raiz.transform, new Vector3(0, 0.82f, 1.42f), new Vector3(2.4f, 0.06f, 0.8f), tope);
            var prefab = PrefabUtility.SaveAsPrefabAsset(raiz, "Assets/Prefabs/Sillon.prefab");
            Object.DestroyImmediate(raiz);
            return prefab;
        }

        static GameObject CrearPrefabEmprendedor()
        {
            var raiz = new GameObject("Emprendedor");
            var traje = Mat("Emprendedor_traje", Estilos.Azul, 0.3f);
            var piel = Mat("Emprendedor_piel", new Color(0.87f, 0.7f, 0.58f), 0.2f);
            var podioM = Mat("Podio", new Color(0.18f, 0.12f, 0.3f), 0.5f);
            var cuerpo = Prim(PrimitiveType.Capsule, "Cuerpo", raiz.transform, new Vector3(0, 1.0f, 0), new Vector3(0.75f, 0.95f, 0.6f), traje);
            var cabeza = Prim(PrimitiveType.Sphere, "Cabeza", raiz.transform, new Vector3(0, 2.15f, 0), new Vector3(0.48f, 0.5f, 0.48f), piel);
            Prim(PrimitiveType.Cube, "Podio", raiz.transform, new Vector3(0, 0.6f, 0.95f), new Vector3(0.9f, 1.2f, 0.6f), podioM);
            Prim(PrimitiveType.Cube, "TopePodio", raiz.transform, new Vector3(0, 1.22f, 0.95f), new Vector3(1.05f, 0.06f, 0.72f), Mat("Podio_tope", Estilos.Violeta, 0.6f));
            var e = raiz.AddComponent<Emprendedor>();
            e.cuerpo = cuerpo.transform; e.cabeza = cabeza.transform;
            var prefab = PrefabUtility.SaveAsPrefabAsset(raiz, "Assets/Prefabs/Emprendedor.prefab");
            Object.DestroyImmediate(raiz);
            return prefab;
        }

        // ---------- escena ----------
        [MenuItem("NeuralHub/Construir estudio")]
        public static void Construir()
        {
            foreach (var d in new[] { "Assets/Scenes", "Assets/Prefabs", "Assets/Materiales", "Assets/StreamingAssets" })
                if (!AssetDatabase.IsValidFolder(d)) AssetDatabase.CreateFolder("Assets", Path.GetFileName(d));

            var prefabTiburon = CrearPrefabTiburon();
            var prefabSillon = CrearPrefabSillon();
            var prefabEmprendedor = CrearPrefabEmprendedor();

            var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.30f, 0.33f, 0.46f);
            RenderSettings.fog = false;

            var estudioGO = new GameObject("Estudio");
            var piso = Mat("Piso", new Color(0.05f, 0.07f, 0.16f), 0.75f);
            var muro = Mat("Muro", new Color(0.08f, 0.06f, 0.2f), 0.2f);
            Prim(PrimitiveType.Cube, "Piso", estudioGO.transform, new Vector3(0, -0.05f, 2f), new Vector3(26, 0.1f, 28), piso);
            Prim(PrimitiveType.Cube, "MuroFondo", estudioGO.transform, new Vector3(0, 5f, 11f), new Vector3(26, 10, 0.3f), muro);
            Prim(PrimitiveType.Cube, "MuroIzq", estudioGO.transform, new Vector3(-13, 5f, 2), new Vector3(0.3f, 10, 28), muro);
            Prim(PrimitiveType.Cube, "MuroDer", estudioGO.transform, new Vector3(13, 5f, 2), new Vector3(0.3f, 10, 28), muro);
            // Franjas de luz del fondo en colores del equipo
            Prim(PrimitiveType.Cube, "FranjaViolera", estudioGO.transform, new Vector3(0, 0.6f, 10.8f), new Vector3(24, 0.18f, 0.1f), Mat("Neon_violeta", Estilos.Violeta, 0.5f, true));
            Prim(PrimitiveType.Cube, "FranjaCian", estudioGO.transform, new Vector3(0, 8.6f, 10.8f), new Vector3(24, 0.14f, 0.1f), Mat("Neon_cian", Estilos.Cian, 0.5f, true));
            // Círculo iluminado del emprendedor
            Prim(PrimitiveType.Cylinder, "CirculoEmprendedor", estudioGO.transform, new Vector3(0, 0.01f, -2.5f), new Vector3(3.4f, 0.015f, 3.4f), Mat("Neon_circulo", Estilos.Cian, 0.5f, true));

            // Pantalla grande del fondo
            var pantallaGO = new GameObject("Pantalla"); pantallaGO.transform.SetParent(estudioGO.transform, false);
            Prim(PrimitiveType.Cube, "Marco", pantallaGO.transform, new Vector3(0, 4.6f, 10.7f), new Vector3(10.4f, 5.4f, 0.2f), Mat("Marco_pantalla", Color.black, 0.4f));
            Prim(PrimitiveType.Cube, "Fondo", pantallaGO.transform, new Vector3(0, 4.6f, 10.55f), new Vector3(10f, 5f, 0.05f), Mat("Pantalla_fondo", Estilos.Navy * 1.4f, 0.1f, true));
            var tit = Texto("Titulo", pantallaGO.transform, new Vector3(0, 5.9f, 10.4f), "NeuralHub", 140, 0.06f, Color.white);
            var det = Texto("Detalle", pantallaGO.transform, new Vector3(0, 4.1f, 10.4f), "Panel de inversionistas con agentes de IA", 80, 0.06f, Estilos.Cian);
            var pantalla = pantallaGO.AddComponent<Pantalla>(); pantalla.titulo = tit; pantalla.detalle = det;

            // Letrero 3D del resultado
            var letrero = Texto("LetreroResultado", estudioGO.transform, new Vector3(0, 8.0f, 9.8f), "", 200, 0.1f, Color.white);

            // Emprendedor + podio
            var emp = (GameObject)PrefabUtility.InstantiatePrefab(prefabEmprendedor, estudioGO.transform);
            emp.transform.position = new Vector3(0, 0, -2.5f); emp.transform.rotation = Quaternion.identity;

            // 3 sillones fijos en semicírculo
            var sillones = new Transform[3];
            float[] ang = { -45f, 0f, 45f };
            for (int i = 0; i < 3; i++)
            {
                var s = (GameObject)PrefabUtility.InstantiatePrefab(prefabSillon, estudioGO.transform);
                s.name = "Sillon_" + (i + 1);
                float a = ang[i] * Mathf.Deg2Rad;
                s.transform.position = new Vector3(6f * Mathf.Sin(a), 0, -1f + 6f * Mathf.Cos(a));
                var hacia = emp.transform.position - s.transform.position; hacia.y = 0;
                s.transform.rotation = Quaternion.LookRotation(hacia);
                sillones[i] = s.transform;
            }

            // Luces
            var dir = new GameObject("LuzGeneral").AddComponent<Light>();
            dir.type = LightType.Directional; dir.intensity = 0.55f; dir.color = new Color(0.8f, 0.85f, 1f);
            dir.transform.rotation = Quaternion.Euler(50, 20, 0);
            foreach (var par in new[] { (new Vector3(-8, 6, 6), Estilos.Violeta), (new Vector3(8, 6, 6), Estilos.Cian) })
            {
                var l = new GameObject("LuzColor").AddComponent<Light>();
                l.type = LightType.Point; l.range = 16; l.intensity = 1.6f; l.color = par.Item2; l.transform.position = par.Item1;
            }
            var reflGO = new GameObject("Reflector");
            var spot = reflGO.AddComponent<Light>();
            spot.type = LightType.Spot; spot.range = 18; spot.spotAngle = 38; spot.intensity = 7f; spot.color = new Color(1f, 0.96f, 0.85f); spot.enabled = false;
            reflGO.transform.position = new Vector3(0, 7f, -4f);
            var reflector = reflGO.AddComponent<Reflector>(); reflector.luz = spot;

            // Cámara principal
            var camGO = new GameObject("Main Camera"); camGO.tag = "MainCamera";
            var cam = camGO.AddComponent<Camera>(); cam.fieldOfView = 55; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Estilos.Navy * 0.4f;
            camGO.AddComponent<AudioListener>();
            var camara = camGO.AddComponent<CamaraEstudio>();
            camGO.transform.position = camara.posGeneral;
            camGO.transform.rotation = Quaternion.LookRotation(camara.miraGeneral - camara.posGeneral);

            // Lógica
            var estudio = estudioGO.AddComponent<Estudio>();
            estudio.prefabTiburon = prefabTiburon; estudio.sillones = sillones; estudio.emprendedor = emp.GetComponent<Emprendedor>();
            estudio.camara = camara; estudio.reflector = reflector; estudio.pantalla = pantalla; estudio.letreroResultado = letrero;

            var app = new GameObject("NeuralHub");
            var cliente = app.AddComponent<ClienteServicio>();
            var reproductor = app.AddComponent<ReproductorSesion>();
            reproductor.cliente = cliente; reproductor.estudio = estudio;
            var ctl = app.AddComponent<AppNeuralHub>();
            ctl.cliente = cliente; ctl.reproductor = reproductor; ctl.estudio = estudio; ctl.camara = camara;

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
