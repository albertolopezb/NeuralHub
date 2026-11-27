using UnityEditor;
using UnityEngine;

namespace NeuralHub.Editor
{
    /// <summary>
    /// Mejoras del incremento 2 adaptadas al estudio oscuro: pantallas laterales con datos y el ícono del sector en la pantalla grande.
    /// Las funciones de muebles Kenney y luz de cáusticas se conservan, pero el estudio oscuro no las usa.
    /// </summary>
    public static class ConstruirExtras
    {
        static Material Unlit(string nombre, Color c, bool transparente)
        {
            string ruta = "Assets/Materiales/" + nombre + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            var sh = Shader.Find(transparente ? "Unlit/Transparent" : "Unlit/Color");
            if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, ruta); }
            m.shader = sh; if (!transparente) m.color = c; EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Material transparente sin luz y con tinte (_Color), para íconos que se atenúan.</summary>
        static Material Icono(string nombre)
        {
            string ruta = "Assets/Materiales/" + nombre + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            var sh = Shader.Find("Sprites/Default");
            if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, ruta); }
            m.shader = sh; m.color = Color.white; EditorUtility.SetDirty(m);
            return m;
        }

        static Material Aditivo(string nombre, Color c)
        {
            string ruta = "Assets/Materiales/" + nombre + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            var sh = Shader.Find("Legacy Shaders/Particles/Additive");
            if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, ruta); }
            m.shader = sh; m.SetColor("_TintColor", c); EditorUtility.SetDirty(m);
            return m;
        }

        static Renderer Cuadro(string nombre, Transform padre, Vector3 pos, float escala, Material m)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = nombre; q.transform.SetParent(padre, false);
            q.transform.localPosition = pos; q.transform.localScale = Vector3.one * escala;
            Object.DestroyImmediate(q.GetComponent<Collider>());
            var r = q.GetComponent<Renderer>(); r.sharedMaterial = m; r.enabled = false;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            return r;
        }

        /// <summary>Ícono del sector del pitch en la esquina de la pantalla grande (Resources/Iconos).</summary>
        public static Renderer IconoPantalla(Transform pantalla, Vector3 pos, float escala)
        {
            return Cuadro("IconoSector", pantalla, pos, escala, Icono("Icono_sector"));
        }

        static Transform Pantallita(Transform padre, string nombre, Vector3 pos, float rotY)
        {
            // Se construye a la escala del diseño original (4.3 × 3.5) y se reduce con el padre para que quepa junto a la pantalla grande
            var g = new GameObject(nombre); g.transform.SetParent(padre, false);
            g.transform.localPosition = pos; g.transform.localRotation = Quaternion.Euler(0, rotY, 0);
            g.transform.localScale = Vector3.one * 0.42f;
            var t = g.transform;
            ConstruirEstudio.Prim(PrimitiveType.Cube, "Marco", t, new Vector3(0, 0, 0.08f), new Vector3(4.46f, 3.66f, 0.04f),
                ConstruirEstudio.Mat("Marco_pantalla", new Color(0.17f, 0.27f, 0.4f), 0.2f, 0, new Color(0.06f, 0.1f, 0.16f)), false);
            ConstruirEstudio.Prim(PrimitiveType.Cube, "Fondo", t, new Vector3(0, 0, 0.04f), new Vector3(4.3f, 3.5f, 0.04f),
                Unlit("Pantallita_fondo", new Color(0.035f, 0.07f, 0.13f), false), false);
            ConstruirEstudio.Prim(PrimitiveType.Cube, "Franja", t, new Vector3(0, 1.0f, 0.01f), new Vector3(3.7f, 0.025f, 0.02f),
                Unlit("Pantallita_franja", Estilos.Calido * 0.8f, false), false);
            return t;
        }

        /// <summary>Pantallas a los lados de la pantalla grande: oferta (izquierda) y marcador de votos del panel (derecha).</summary>
        public static PantallasDatos PantallasLaterales(Transform estudio, Font titulo, Font texto)
        {
            var regular = ConstruirEstudio.Fuente("Inter-Regular.ttf");
            var raiz = new GameObject("PantallasDatos"); raiz.transform.SetParent(estudio, false);
            var d = raiz.AddComponent<PantallasDatos>();

            var izq = Pantallita(raiz.transform, "PantallaOferta", new Vector3(-3.75f, 2.45f, 3.05f), -18f);
            d.tituloOferta = ConstruirEstudio.Texto("Titulo", izq, new Vector3(0, 1.38f, -0.02f), "Oferta", 0.36f, Estilos.Tinta, titulo, 120);
            d.textoOferta = ConstruirEstudio.Texto("Datos", izq, new Vector3(-0.25f, -0.3f, -0.02f), "", 0.23f, Estilos.Suave, regular, 90);
            d.textoOferta.anchor = TextAnchor.MiddleCenter; d.textoOferta.lineSpacing = 1.1f;
            d.icono = Cuadro("Icono", izq, new Vector3(1.6f, 0.55f, -0.02f), 0.7f, Icono("Icono_lateral"));

            var der = Pantallita(raiz.transform, "PantallaPanel", new Vector3(3.75f, 2.45f, 3.05f), 18f);
            d.tituloPanel = ConstruirEstudio.Texto("Titulo", der, new Vector3(0, 1.38f, -0.02f), "Votos del panel", 0.36f, Estilos.Tinta, titulo, 120);
            var riel = Unlit("Pantallita_riel", Estilos.Linea, false);
            for (int i = 0; i < 3; i++)
            {
                float y = 0.45f - i * 0.85f;
                d.nombres[i] = ConstruirEstudio.Texto("Nombre" + i, der, new Vector3(-1.6f, y + 0.22f, -0.02f), "", 0.25f, Estilos.Tinta, texto, 90);
                d.nombres[i].anchor = TextAnchor.MiddleLeft;
                d.estados[i] = ConstruirEstudio.Texto("Estado" + i, der, new Vector3(1.6f, y + 0.22f, -0.02f), "", 0.22f, Estilos.Tenue, regular, 80);
                d.estados[i].anchor = TextAnchor.MiddleRight;
                ConstruirEstudio.Prim(PrimitiveType.Cube, "Riel" + i, der, new Vector3(0, y - 0.15f, 0.0f), new Vector3(3.2f, 0.14f, 0.01f), riel, false);
                var b = ConstruirEstudio.Prim(PrimitiveType.Cube, "Barra" + i, der, new Vector3(-1.6f, y - 0.15f, -0.01f), new Vector3(0.05f, 0.14f, 0.02f), null, false);
                var mb = new Material(Shader.Find("Unlit/Color")); mb.color = Estilos.Pendiente;
                b.GetComponent<Renderer>().sharedMaterial = mb;
                d.barras[i] = b.transform; d.barrasRend[i] = b.GetComponent<Renderer>();
            }
            return d;
        }

        // ---------- del estudio submarino (incremento 2); el estudio oscuro no las llama ----------

        /// <summary>Coloca un modelo de terceros (Kenney Furniture Kit, CC0) escalado a la altura indicada.</summary>
        public static GameObject Modelo(string archivo, Transform padre, Vector3 pos, float rotY, float altura)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Terceros/Kenney_FurnitureKit/" + archivo + ".fbx");
            if (asset == null) { Debug.LogWarning("NeuralHub: falta el modelo " + archivo); return null; }
            var g = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            g.transform.SetParent(padre, false);
            g.transform.localRotation = Quaternion.Euler(0, rotY, 0);
            var rs = g.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0)
            {
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                if (b.size.y > 1e-4f) g.transform.localScale = Vector3.one * (altura / b.size.y);
            }
            g.transform.localPosition = pos;
            return g;
        }

        public static void DecorarEscritorio(Transform sillon)
        {
            Modelo("laptop", sillon, new Vector3(0.78f, 0.85f, 1.3f), 0f, 0.15f);
            Modelo("lampRoundTable", sillon, new Vector3(-0.85f, 0.85f, 1.55f), 0f, 0.5f);
            Modelo("books", sillon, new Vector3(-0.25f, 0.85f, 1.6f), 15f, 0.16f);
        }

        public static void PlantasEscenario(Transform estudio)
        {
            Modelo("pottedPlant", estudio, new Vector3(-2.3f, 0, -3.2f), 0f, 1.3f);
            Modelo("pottedPlant", estudio, new Vector3(2.3f, 0, -3.2f), 40f, 1.3f);
            Modelo("computerScreen", estudio, new Vector3(0.0f, 1.25f, -1.45f), 180f, 0.32f);
        }

        public static void LucesDeAgua(Transform estudio)
        {
            const string rutaC = "Assets/Resources/causticas.png";
            var imp = AssetImporter.GetAtPath(rutaC) as TextureImporter;
            if (imp != null && imp.textureType != TextureImporterType.Cookie)
            {
                imp.textureType = TextureImporterType.Cookie; imp.alphaSource = TextureImporterAlphaSource.FromGrayScale; imp.wrapMode = TextureWrapMode.Repeat;
                imp.SaveAndReimport();
            }
            var luz = new GameObject("LuzCausticas").AddComponent<Light>();
            luz.type = LightType.Directional; luz.intensity = 0.65f; luz.color = new Color(0.55f, 0.9f, 1f);
            luz.cookie = AssetDatabase.LoadAssetAtPath<Texture2D>(rutaC); luz.cookieSize = 7f;
            luz.shadows = LightShadows.Soft; luz.shadowStrength = 0.45f;
            luz.transform.rotation = Quaternion.Euler(70, 15, 0);
            luz.gameObject.AddComponent<Caustica>();
            var rayo = Aditivo("Rayo_luz", new Color(0.25f, 0.55f, 0.65f, 0.06f));
            float[] xs = { -6f, -2.2f, 2.5f, 6.5f };
            for (int i = 0; i < xs.Length; i++)
            {
                var r = ConstruirEstudio.Prim(PrimitiveType.Cylinder, "RayoDeLuz", estudio, new Vector3(xs[i], 6.5f, 4f + (i % 2) * 2.5f), new Vector3(1.6f, 5.5f, 1.6f), rayo);
                r.transform.localRotation = Quaternion.Euler(0, 0, (i - 1.5f) * 6f);
                r.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
    }
}
