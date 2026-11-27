using UnityEditor;
using UnityEngine;

namespace NeuralHub.Editor
{
    /// <summary>Mejoras del incremento 2: pantallas laterales con datos, ícono del sector, luz de cáusticas, rayos de luz y audio.</summary>
    public static class ConstruirExtras
    {
        static Material Unlit(string nombre, Color c, bool transparente)
        {
            string ruta = "Assets/Materiales/" + nombre + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            var sh = Shader.Find(transparente ? "Unlit/Transparent" : "Unlit/Color");
            if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, ruta); }
            m.shader = sh; m.color = c; EditorUtility.SetDirty(m);
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
            Modelo("laptop", sillon, new Vector3(1.0f, 0.85f, 1.35f), -35f, 0.15f);
            Modelo("lampRoundTable", sillon, new Vector3(-0.85f, 0.85f, 1.55f), 0f, 0.5f);
            Modelo("books", sillon, new Vector3(-0.25f, 0.85f, 1.6f), 15f, 0.16f);
        }

        public static void PlantasEscenario(Transform estudio)
        {
            Modelo("pottedPlant", estudio, new Vector3(-2.3f, 0, -3.2f), 0f, 1.3f);
            Modelo("pottedPlant", estudio, new Vector3(2.3f, 0, -3.2f), 40f, 1.3f);
            Modelo("computerScreen", estudio, new Vector3(0.0f, 1.25f, -1.45f), 180f, 0.32f);
        }

        public static Renderer IconoPantalla(Transform pantalla)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "IconoSector"; q.transform.SetParent(pantalla, false);
            q.transform.localPosition = new Vector3(-3.9f, 5.95f, 10.42f); q.transform.localScale = Vector3.one * 1.3f;
            Object.DestroyImmediate(q.GetComponent<Collider>());
            var r = q.GetComponent<Renderer>(); r.sharedMaterial = Unlit("Icono_sector", Color.white, true); r.enabled = false;
            return r;
        }

        static GameObject Pantallita(Transform padre, string nombre, Vector3 pos, float rotY, out Transform cuerpo)
        {
            var g = new GameObject(nombre); g.transform.SetParent(padre, false);
            g.transform.localPosition = pos; g.transform.localRotation = Quaternion.Euler(0, rotY, 0);
            ConstruirEstudio.Prim(PrimitiveType.Cube, "Marco", g.transform, new Vector3(0, 0, 0.08f), new Vector3(4.6f, 3.8f, 0.15f), ConstruirEstudio.Mat("Marco_pantalla", Color.black, 0.4f));
            ConstruirEstudio.Prim(PrimitiveType.Cube, "Fondo", g.transform, new Vector3(0, 0, -0.01f), new Vector3(4.3f, 3.5f, 0.04f), Unlit("Pantallita_fondo", new Color(0.02f, 0.09f, 0.3f), false));
            ConstruirEstudio.Prim(PrimitiveType.Cube, "Franja", g.transform, new Vector3(0, 1.55f, -0.04f), new Vector3(4.3f, 0.06f, 0.02f), Unlit("Pantallita_franja", Estilos.Cian, false));
            cuerpo = g.transform;
            return g;
        }

        public static PantallasDatos PantallasLaterales(Transform estudio)
        {
            var raiz = new GameObject("PantallasDatos"); raiz.transform.SetParent(estudio, false);
            var d = raiz.AddComponent<PantallasDatos>();
            Transform izq, der;
            Pantallita(raiz.transform, "PantallaOferta", new Vector3(-8.0f, 4.4f, 9.4f), -22f, out izq);
            d.tituloOferta = ConstruirEstudio.Texto("Titulo", izq, new Vector3(0, 1.3f, -0.06f), "Oferta", 90, 0.045f, Color.white);
            d.textoOferta = ConstruirEstudio.Texto("Datos", izq, new Vector3(-0.35f, -0.15f, -0.06f), "", 70, 0.036f, Estilos.Cian);
            d.textoOferta.anchor = TextAnchor.MiddleCenter;
            var ic = GameObject.CreatePrimitive(PrimitiveType.Quad); ic.name = "Icono"; ic.transform.SetParent(izq, false);
            ic.transform.localPosition = new Vector3(1.55f, 0.75f, -0.06f); ic.transform.localScale = Vector3.one * 0.8f;
            Object.DestroyImmediate(ic.GetComponent<Collider>());
            d.icono = ic.GetComponent<Renderer>(); d.icono.sharedMaterial = Unlit("Icono_lateral", Color.white, true); d.icono.enabled = false;

            Pantallita(raiz.transform, "PantallaPanel", new Vector3(8.0f, 4.4f, 9.4f), 22f, out der);
            d.tituloPanel = ConstruirEstudio.Texto("Titulo", der, new Vector3(0, 1.3f, -0.06f), "Votos del panel", 90, 0.045f, Color.white);
            for (int i = 0; i < 3; i++)
            {
                float y = 0.55f - i * 0.85f;
                d.nombres[i] = ConstruirEstudio.Texto("Nombre" + i, der, new Vector3(-1.95f, y + 0.22f, -0.06f), "", 70, 0.036f, Color.white, TextAnchor.MiddleLeft);
                d.estados[i] = ConstruirEstudio.Texto("Estado" + i, der, new Vector3(1.95f, y + 0.22f, -0.06f), "", 60, 0.032f, Estilos.Cian, TextAnchor.MiddleRight);
                ConstruirEstudio.Prim(PrimitiveType.Cube, "Riel" + i, der, new Vector3(0, y - 0.15f, -0.045f), new Vector3(3.2f, 0.22f, 0.01f), Unlit("Pantallita_riel", new Color(1, 1, 1, 1) * 0.18f, false));
                var b = ConstruirEstudio.Prim(PrimitiveType.Cube, "Barra" + i, der, new Vector3(-1.6f, y - 0.15f, -0.06f), new Vector3(0.05f, 0.22f, 0.02f), null);
                var mb = new Material(Shader.Find("Unlit/Color")); mb.color = Color.gray;
                b.GetComponent<Renderer>().sharedMaterial = mb;
                d.barras[i] = b.transform; d.barrasRend[i] = b.GetComponent<Renderer>();
            }
            return d;
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
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.25f, 0.5f, 0.65f);
            RenderSettings.ambientEquatorColor = new Color(0.12f, 0.3f, 0.45f);
            RenderSettings.ambientGroundColor = new Color(0.1f, 0.15f, 0.2f);
        }
    }
}
