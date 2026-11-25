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
        internal static Material Mat(string nombre, Color c, float brillo = 0.3f, bool emision = false)
        {
            string ruta = "Assets/Materiales/" + nombre + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null) { m = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(m, ruta); }
            m.color = c; m.SetFloat("_Glossiness", brillo);
            if (emision) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * 1.6f); }
            EditorUtility.SetDirty(m);
            return m;
        }

        internal static GameObject Prim(PrimitiveType t, string nombre, Transform padre, Vector3 pos, Vector3 esc, Material m)
        {
            var g = GameObject.CreatePrimitive(t);
            g.name = nombre; g.transform.SetParent(padre, false);
            g.transform.localPosition = pos; g.transform.localScale = esc;
            if (m) g.GetComponent<Renderer>().sharedMaterial = m;
            var col = g.GetComponent<Collider>(); if (col) Object.DestroyImmediate(col);
            return g;
        }

        internal static TextMesh Texto(string nombre, Transform padre, Vector3 pos, string txt, int tam, float car, Color c, TextAnchor ancla = TextAnchor.MiddleCenter)
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
            var piel = Mat("Tiburon_piel", new Color(0.50f, 0.58f, 0.68f), 0.45f);
            var panza = Mat("Tiburon_panza", new Color(0.85f, 0.9f, 0.95f), 0.4f);
            var traje = Mat("Tiburon_traje", new Color(0.05f, 0.08f, 0.22f), 0.35f);
            var camisa = Mat("Tiburon_camisa", new Color(0.95f, 0.96f, 1f), 0.3f);
            var blanco = Mat("Blanco", Color.white, 0.6f);
            var negro = Mat("Negro", Color.black, 0.2f);
            var corbataM = Mat("Tiburon_corbata", Color.white, 0.5f);
            var cuerpo = Prim(PrimitiveType.Capsule, "Cuerpo", raiz.transform, new Vector3(0, 1.0f, 0), new Vector3(0.8f, 0.6f, 0.62f), traje);
            Prim(PrimitiveType.Cube, "Camisa", cuerpo.transform, new Vector3(0, 0.45f, 0.5f), new Vector3(0.28f, 0.55f, 0.12f), camisa);
            var corbata = Prim(PrimitiveType.Cube, "Corbata", raiz.transform, new Vector3(0, 1.15f, 0.34f), new Vector3(0.1f, 0.5f, 0.04f), corbataM);
            var broche = Prim(PrimitiveType.Sphere, "Insignia", raiz.transform, new Vector3(0.22f, 1.3f, 0.33f), new Vector3(0.09f, 0.09f, 0.04f), corbataM);
            var cabeza = Prim(PrimitiveType.Sphere, "Cabeza", raiz.transform, new Vector3(0, 1.88f, 0.02f), new Vector3(0.56f, 0.5f, 0.72f), piel);
            Prim(PrimitiveType.Sphere, "Hocico", cabeza.transform, new Vector3(0, -0.12f, 0.42f), new Vector3(0.72f, 0.5f, 0.55f), piel);
            Prim(PrimitiveType.Sphere, "Mejilla", cabeza.transform, new Vector3(0, -0.25f, 0.2f), new Vector3(0.9f, 0.45f, 0.8f), panza);
            foreach (float x in new[] { -0.2f, 0.2f })
            {
                Prim(PrimitiveType.Sphere, "Ojo", cabeza.transform, new Vector3(x * 1.6f, 0.22f, 0.5f), new Vector3(0.2f, 0.2f, 0.18f), blanco);
                Prim(PrimitiveType.Sphere, "Pupila", cabeza.transform, new Vector3(x * 1.6f, 0.22f, 0.6f), new Vector3(0.09f, 0.09f, 0.08f), negro);
                Prim(PrimitiveType.Cube, "Ceja", cabeza.transform, new Vector3(x * 1.6f, 0.42f, 0.5f), new Vector3(0.28f, 0.05f, 0.08f), negro).transform.localRotation = Quaternion.Euler(0, 0, x > 0 ? -14 : 14);
            }
            for (int k = -3; k <= 3; k++)   // dientes
                Prim(PrimitiveType.Cube, "Diente", cabeza.transform, new Vector3(k * 0.1f, -0.2f, 0.66f), new Vector3(0.06f, 0.08f, 0.04f), blanco);
            var aleta = Prim(PrimitiveType.Cube, "AletaDorsal", raiz.transform, new Vector3(0, 2.42f, -0.08f), new Vector3(0.07f, 0.62f, 0.4f), piel);
            aleta.transform.localRotation = Quaternion.Euler(-25, 0, 0);
            Prim(PrimitiveType.Cube, "ColaA", raiz.transform, new Vector3(0, 0.55f, -0.75f), new Vector3(0.08f, 0.9f, 0.35f), piel).transform.localRotation = Quaternion.Euler(30, 0, 0);
            Prim(PrimitiveType.Cube, "ColaB", raiz.transform, new Vector3(0, 0.2f, -0.85f), new Vector3(0.08f, 0.6f, 0.3f), piel).transform.localRotation = Quaternion.Euler(-35, 0, 0);
            foreach (float x in new[] { -1f, 1f })
            {
                var brazo = Prim(PrimitiveType.Capsule, "Brazo", raiz.transform, new Vector3(0.5f * x, 1.05f, 0.35f), new Vector3(0.2f, 0.34f, 0.2f), traje);
                brazo.transform.localRotation = Quaternion.Euler(70, 0, 12 * x);
                Prim(PrimitiveType.Sphere, "Aleta", raiz.transform, new Vector3(0.52f * x, 0.95f, 0.82f), new Vector3(0.2f, 0.12f, 0.26f), piel);
            }
            var paleta = Prim(PrimitiveType.Cube, "PaletaVoto", raiz.transform, new Vector3(0.62f, 1.6f, 0.5f), new Vector3(0.55f, 0.4f, 0.04f), blanco);
            var letrero = Texto("Letrero", raiz.transform, new Vector3(0, 1.02f, 1.12f), "", 90, 0.028f, Color.white);
            letrero.transform.localRotation = Quaternion.Euler(20, 180, 0);

            var t = raiz.AddComponent<Tiburon>();
            t.cuerpo = cuerpo.transform; t.cabeza = cabeza.transform; t.aleta = aleta.transform; t.paleta = paleta.transform; t.letrero = letrero;
            t.pintables = new[] { corbata.GetComponent<Renderer>(), broche.GetComponent<Renderer>() };
            var prefab = PrefabUtility.SaveAsPrefabAsset(raiz, "Assets/Prefabs/Tiburon.prefab");
            Object.DestroyImmediate(raiz);
            return prefab.GetComponent<Tiburon>();
        }

        static GameObject CrearPrefabSillon()
        {
            var raiz = new GameObject("Sillon");
            var concha = Mat("Sillon_concha", new Color(0.96f, 0.72f, 0.66f), 0.55f);
            var interior = Mat("Sillon_interior", new Color(0.85f, 0.35f, 0.5f), 0.4f);
            var madera = Mat("Escritorio_madera", new Color(0.45f, 0.30f, 0.18f), 0.4f);
            var tope = Mat("Escritorio_tope", new Color(0.08f, 0.2f, 0.32f), 0.8f);
            Prim(PrimitiveType.Cylinder, "Base", raiz.transform, new Vector3(0, 0.22f, 0), new Vector3(0.3f, 0.22f, 0.3f), tope);
            Prim(PrimitiveType.Cube, "Asiento", raiz.transform, new Vector3(0, 0.47f, 0), new Vector3(1.25f, 0.18f, 1.05f), interior);
            var resp = Prim(PrimitiveType.Sphere, "RespaldoConcha", raiz.transform, new Vector3(0, 1.25f, -0.55f), new Vector3(1.9f, 1.9f, 0.28f), concha);
            Prim(PrimitiveType.Cube, "Escritorio", raiz.transform, new Vector3(0, 0.4f, 1.42f), new Vector3(2.3f, 0.8f, 0.7f), madera);
            Prim(PrimitiveType.Cube, "TopeEscritorio", raiz.transform, new Vector3(0, 0.82f, 1.42f), new Vector3(2.4f, 0.06f, 0.8f), tope);
            ConstruirExtras.DecorarEscritorio(raiz.transform);
            var prefab = PrefabUtility.SaveAsPrefabAsset(raiz, "Assets/Prefabs/Sillon.prefab");
            Object.DestroyImmediate(raiz);
            return prefab;
        }

        static GameObject CrearPrefabEmprendedor()
        {
            var raiz = new GameObject("Emprendedor");
            var naranja = Mat("Emprendedor_pez", new Color(1f, 0.5f, 0.05f), 0.5f, true);
            var blanco = Mat("Blanco", Color.white, 0.6f);
            var negro = Mat("Negro", Color.black, 0.2f);
            var podioM = Mat("Podio", new Color(0.95f, 0.45f, 0.5f), 0.5f);
            var cuerpo = Prim(PrimitiveType.Capsule, "Cuerpo", raiz.transform, new Vector3(0, 1.0f, 0), new Vector3(0.85f, 0.95f, 0.55f), naranja);
            foreach (float y in new[] { 0.5f, 1.0f, 1.55f })
                Prim(PrimitiveType.Cylinder, "Franja", raiz.transform, new Vector3(0, y, 0), new Vector3(0.88f, 0.045f, 0.58f), blanco);
            var cabeza = Prim(PrimitiveType.Sphere, "Cabeza", raiz.transform, new Vector3(0, 2.12f, 0.02f), new Vector3(0.62f, 0.55f, 0.55f), naranja);
            foreach (float x in new[] { -1f, 1f })
            {
                Prim(PrimitiveType.Sphere, "Ojo", cabeza.transform, new Vector3(0.28f * x, 0.15f, 0.42f), new Vector3(0.26f, 0.26f, 0.22f), blanco);
                Prim(PrimitiveType.Sphere, "Pupila", cabeza.transform, new Vector3(0.28f * x, 0.15f, 0.54f), new Vector3(0.11f, 0.11f, 0.1f), negro);
                Prim(PrimitiveType.Sphere, "AletaLateral", raiz.transform, new Vector3(0.55f * x, 1.0f, 0.1f), new Vector3(0.12f, 0.4f, 0.3f), naranja);
            }
            Prim(PrimitiveType.Cube, "Cola", raiz.transform, new Vector3(0, 0.75f, -0.5f), new Vector3(0.08f, 0.7f, 0.5f), naranja);
            Prim(PrimitiveType.Cube, "AletaDorsal", raiz.transform, new Vector3(0, 2.55f, -0.05f), new Vector3(0.06f, 0.3f, 0.5f), naranja);
            Prim(PrimitiveType.Cube, "Podio", raiz.transform, new Vector3(0, 0.6f, 1.05f), new Vector3(0.9f, 1.2f, 0.6f), podioM);
            Prim(PrimitiveType.Cube, "TopePodio", raiz.transform, new Vector3(0, 1.22f, 1.05f), new Vector3(1.05f, 0.06f, 0.72f), Mat("Podio_tope", Estilos.Violeta, 0.6f));
            var e = raiz.AddComponent<Emprendedor>();
            e.cuerpo = cuerpo.transform; e.cabeza = cabeza.transform;
            var prefab = PrefabUtility.SaveAsPrefabAsset(raiz, "Assets/Prefabs/Emprendedor.prefab");
            Object.DestroyImmediate(raiz);
            return prefab;
        }

        // ---------- ambiente de pecera ----------
        static void Decorar(Transform padre)
        {
            var arena = Mat("Arena", new Color(0.55f, 0.5f, 0.38f), 0.2f);
            var roca = Mat("Roca", new Color(0.25f, 0.3f, 0.34f), 0.3f);
            var alga = Mat("Alga", new Color(0.1f, 0.6f, 0.35f), 0.4f);
            var algaB = Mat("Alga_clara", new Color(0.35f, 0.8f, 0.3f), 0.4f);
            var cor = new[] { Mat("Coral_rosa", new Color(1f, 0.4f, 0.55f), 0.5f), Mat("Coral_naranja", new Color(1f, 0.55f, 0.2f), 0.5f), Mat("Coral_violeta", new Color(0.6f, 0.35f, 0.9f), 0.5f) };
            var rnd = new System.Random(7);
            System.Func<float, float, float> R = (a, b) => (float)(a + rnd.NextDouble() * (b - a));
            var deco = new GameObject("Decoracion").transform; deco.SetParent(padre, false);

            for (int i = 0; i < 22; i++)    // piedras
                Prim(PrimitiveType.Sphere, "Piedra", deco, new Vector3(R(-11, 11), 0.05f, R(-6, 9)), Vector3.one * R(0.15f, 0.45f), i % 3 == 0 ? roca : arena);
            for (int i = 0; i < 7; i++)     // rocas grandes
            {
                float x = i < 3 ? -10.5f + i * 1.4f : (i < 6 ? 8.5f + (i - 3) * 1.4f : 0);
                Prim(PrimitiveType.Sphere, "Roca", deco, new Vector3(x, 0.4f, i == 6 ? 9.5f : R(4, 9)), new Vector3(R(1.4f, 2.4f), R(0.9f, 1.8f), R(1.2f, 2f)), roca);
            }
            for (int i = 0; i < 12; i++)    // coral de ramas
            {
                float x = (i % 2 == 0 ? -1 : 1) * R(7.5f, 11f), z = R(-5, 8.5f);
                var g = new GameObject("Coral").transform; g.SetParent(deco, false); g.localPosition = new Vector3(x, 0, z);
                var m = cor[i % 3];
                for (int r = 0; r < 4; r++)
                {
                    var rama = Prim(PrimitiveType.Capsule, "Rama", g, new Vector3(R(-0.3f, 0.3f), 0.5f, R(-0.3f, 0.3f)), new Vector3(0.18f, R(0.5f, 1.0f), 0.18f), m);
                    rama.transform.localRotation = Quaternion.Euler(R(-25, 25), 0, R(-25, 25));
                    Prim(PrimitiveType.Sphere, "Punta", rama.transform, new Vector3(0, 1f, 0), new Vector3(1.4f, 0.3f, 1.4f), m);
                }
            }
            for (int i = 0; i < 26; i++)    // algas que se mecen
            {
                float x = R(-12, 12); if (Mathf.Abs(x) < 4.5f) x += x < 0 ? -5f : 5f;
                var g = new GameObject("Alga").transform; g.SetParent(deco, false); g.localPosition = new Vector3(x, 0, R(-4, 9.5f));
                int tramos = (int)R(3, 6);
                var cur = g;
                for (int k = 0; k < tramos; k++)
                {
                    var t = new GameObject("Tramo").transform; t.SetParent(cur, false); t.localPosition = new Vector3(0, k == 0 ? 0 : 0.55f, 0);
                    Prim(PrimitiveType.Capsule, "Hoja", t, new Vector3(0, 0.3f, 0), new Vector3(0.16f, 0.32f, 0.05f), k % 2 == 0 ? alga : algaB);
                    var o = t.gameObject.AddComponent<Ondular>(); o.amplitud = 5f + k * 1.5f; o.velocidad = R(0.8f, 1.4f);
                    cur = t;
                }
            }
            var colores = new[] { new Color(1f, 0.8f, 0.1f), new Color(0.2f, 0.8f, 1f), new Color(1f, 0.4f, 0.5f), new Color(0.6f, 0.9f, 0.3f), Color.white };
            for (int i = 0; i < 14; i++)    // peces de fondo
            {
                var pez = new GameObject("Pez").transform; pez.SetParent(deco, false);
                var c = Mat("Pez_" + (i % 5), colores[i % 5], 0.6f);
                float s = R(0.4f, 0.9f);
                Prim(PrimitiveType.Sphere, "Cuerpo", pez, Vector3.zero, new Vector3(0.18f, 0.3f, 0.5f) * s, c);
                Prim(PrimitiveType.Cube, "Cola", pez, new Vector3(0, 0, -0.34f * s), new Vector3(0.04f, 0.28f, 0.22f) * s, c);
                Prim(PrimitiveType.Cube, "Aleta", pez, new Vector3(0, 0.17f * s, 0), new Vector3(0.03f, 0.18f, 0.25f) * s, c);
                var n = pez.gameObject.AddComponent<Nadador>();
                n.centro = new Vector3(R(-3, 3), R(2.2f, 7.5f), R(7, 9.4f)); n.radio = R(3, 9); n.velocidad = R(0.2f, 0.5f) * (i % 2 == 0 ? 1 : -1); n.fase = R(0, 6.28f);
            }
            var med = Mat("Medusa", new Color(0.8f, 0.5f, 1f), 0.7f, true);
            for (int i = 0; i < 6; i++)     // medusas
            {
                var g = new GameObject("Medusa").transform; g.SetParent(deco, false); g.localPosition = new Vector3(R(-10, 10), R(3, 8), R(5, 10));
                Prim(PrimitiveType.Sphere, "Campana", g, Vector3.zero, new Vector3(0.7f, 0.45f, 0.7f), med);
                for (int k = 0; k < 5; k++) Prim(PrimitiveType.Capsule, "Tentaculo", g, new Vector3(R(-0.2f, 0.2f), -0.5f, R(-0.2f, 0.2f)), new Vector3(0.05f, 0.35f, 0.05f), med);
                var o = g.gameObject.AddComponent<Ondular>(); o.vertical = true; o.amplitud = 3; o.velocidad = R(0.6f, 1f);
            }
            for (int i = 0; i < 6; i++)     // burbujas
            {
                var b = new GameObject("Burbujas"); b.transform.SetParent(deco, false);
                b.transform.localPosition = new Vector3(R(-11, 11), 0.1f, R(-3, 9));
                b.transform.localRotation = Quaternion.Euler(-90, 0, 0);
                var ps = b.AddComponent<ParticleSystem>();
                var main = ps.main; main.startLifetime = 7f; main.startSpeed = 1.1f; main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.2f); main.startColor = new Color(0.8f, 0.95f, 1f, 0.55f);
                var em = ps.emission; em.rateOverTime = 3;
                var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 0.3f;
                var rend = b.GetComponent<ParticleSystemRenderer>();
                var mat = new Material(Shader.Find("Sprites/Default")); mat.name = "Burbuja";
                rend.sharedMaterial = mat;
            }
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
            RenderSettings.ambientLight = new Color(0.22f, 0.4f, 0.55f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Exponential; RenderSettings.fogColor = new Color(0.03f, 0.28f, 0.42f); RenderSettings.fogDensity = 0.022f;

            var estudioGO = new GameObject("Estudio");
            var piso = Mat("Piso", new Color(0.42f, 0.4f, 0.34f), 0.3f);
            var muro = Mat("Muro", new Color(0.02f, 0.16f, 0.3f), 0.2f);
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
            pantalla.icono = ConstruirExtras.IconoPantalla(pantallaGO.transform);

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

            Decorar(estudioGO.transform);
            var datos = ConstruirExtras.PantallasLaterales(estudioGO.transform);
            ConstruirExtras.PlantasEscenario(estudioGO.transform);
            ConstruirExtras.LucesDeAgua(estudioGO.transform);

            // Luces
            var dir = new GameObject("LuzGeneral").AddComponent<Light>();
            dir.type = LightType.Directional; dir.intensity = 0.35f; dir.color = new Color(0.8f, 0.85f, 1f);
            dir.transform.rotation = Quaternion.Euler(50, 20, 0);
            foreach (var par in new[] { (new Vector3(-8, 6, 6), new Color(0.2f, 0.9f, 0.9f)), (new Vector3(8, 6, 6), new Color(0.3f, 0.6f, 1f)) })
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
            var cam = camGO.AddComponent<Camera>(); cam.fieldOfView = 55; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.03f, 0.28f, 0.42f);
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
