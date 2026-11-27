using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace NeuralHub.Editor
{
    /// <summary>
    /// Menú NeuralHub: construye el estudio (reproducible) y compila el ejecutable. El estudio es un set de televisión
    /// submarino en estilo caricatura: plataforma con aros de luz, ventanal curvo al océano, un podio por tiburón y la
    /// pantalla de presentación del pitch.
    /// </summary>
    public static class ConstruirEstudio
    {
        const string Escena = "Assets/Scenes/Estudio.unity";

        // ---------- ayudas ----------
        static Color Hex(string h) { Color c; ColorUtility.TryParseHtmlString(h, out c); return c; }

        internal static Material Mat(string nombre, Color c, float brillo = 0.3f, float metal = 0f)
        {
            var m = Material_(nombre, "Standard");
            m.color = c; m.SetFloat("_Glossiness", brillo); m.SetFloat("_Metallic", metal);
            EditorUtility.SetDirty(m);
            return m;
        }

        internal static Material Emisivo(string nombre, Color c, float fuerza)
        {
            var m = Mat(nombre, c, 0.4f);
            m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c.linear * fuerza);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            return m;
        }

        internal static Material Material_(string nombre, string shader)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Materiales")) AssetDatabase.CreateFolder("Assets", "Materiales");
            string ruta = "Assets/Materiales/" + nombre + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            var sh = Shader.Find(shader);
            if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, ruta); }
            m.shader = sh;
            return m;
        }

        static Material Plano(string nombre, Color c)
        {
            var m = Material_(nombre, "Unlit/Color"); m.color = c; EditorUtility.SetDirty(m); return m;
        }

        internal static GameObject Malla(string nombre, Transform padre, Mesh malla, Vector3 pos, Material mat, bool sombra = true)
        {
            var g = new GameObject(nombre, typeof(MeshFilter), typeof(MeshRenderer));
            g.transform.SetParent(padre, false); g.transform.localPosition = pos;
            g.GetComponent<MeshFilter>().sharedMesh = malla;
            var r = g.GetComponent<MeshRenderer>(); r.sharedMaterial = mat;
            if (!sombra) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return g;
        }

        internal static TextMesh Texto(string nombre, Transform padre, Vector3 pos, string txt, int tam, float car, Color c,
                                       TextAnchor ancla = TextAnchor.MiddleCenter, string fuente = "Inter-SemiBold")
        {
            var g = new GameObject(nombre); g.transform.SetParent(padre, false); g.transform.localPosition = pos;
            var tm = g.AddComponent<TextMesh>();
            var f = AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/Fuentes/" + fuente + ".ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.font = f; tm.text = txt; tm.fontSize = tam; tm.characterSize = car; tm.color = c; tm.anchor = ancla;
            tm.alignment = ancla == TextAnchor.UpperLeft || ancla == TextAnchor.MiddleLeft ? TextAlignment.Left : ancla == TextAnchor.MiddleRight ? TextAlignment.Right : TextAlignment.Center;
            var m = Material_("Texto_" + fuente, "NeuralHub/Texto3D");
            m.mainTexture = f.material.mainTexture;
            g.GetComponent<MeshRenderer>().sharedMaterial = m;
            g.AddComponent<TextoNitido>().fuente = f;
            return tm;
        }

        // ---------- escenario ----------
        static void Piso(Transform p)
        {
            Malla("Piso", p, Mallas.Guardar(Mallas.Disco(13.2f, 0.2f, 0.05f), "piso"), new Vector3(0, -0.2f, -1f), Mat("Piso", Hex("#102A45"), 0.62f, 0.1f));
            Malla("Plataforma", p, Mallas.Guardar(Mallas.Disco(8.1f, 0.12f, 0.08f), "plataforma"), new Vector3(0, 0f, 1.1f), Mat("Plataforma", Hex("#1C4A6E"), 0.68f, 0.15f));
            Malla("AroPlataforma", p, Mallas.Guardar(Mallas.Aro(8.16f, 8.3f), "aro_plataforma"), new Vector3(0, 0.02f, 1.1f), Emisivo("Neon_cian", Hex("#35C6F4"), 2.2f), false);
            Malla("AroInterior", p, Mallas.Guardar(Mallas.Aro(4.35f, 4.4f), "aro_interior"), new Vector3(0, 0.125f, 1.1f), Emisivo("Neon_cian_suave", Hex("#2D8FD0"), 1.0f), false);
            Malla("CirculoEmprendedor", p, Mallas.Guardar(Mallas.Disco(1.15f, 0.06f, 0.04f), "circulo"), new Vector3(0, 0.1f, -2.5f), Mat("Circulo", Hex("#24577F"), 0.7f, 0.15f));
            Malla("AroEmprendedor", p, Mallas.Guardar(Mallas.Aro(1.2f, 1.29f), "aro_emprendedor"), new Vector3(0, 0.15f, -2.5f), Emisivo("Neon_calido", Hex("#FFC857"), 2f), false);
        }

        static void Ventanal(Transform p)
        {
            var v = new GameObject("Ventanal").transform; v.SetParent(p, false); v.localPosition = new Vector3(0, 0, -1f);
            var oceano = Material_("Oceano", "Unlit/Texture");
            oceano.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Escenario/oceano.png");
            Malla("Oceano", v, Mallas.Guardar(Mallas.Arco(20f, -100, 100, -1.2f, 15f), "oceano"), Vector3.zero, oceano, false);
            Malla("Arena", v, Mallas.Guardar(Mallas.Aro(13.1f, 20f), "arena"), new Vector3(0, -0.12f, 0), Mat("Arena", Hex("#1D4F73"), 0.05f), false);

            var marco = Mat("Marco_ventanal", Hex("#0B1B30"), 0.55f, 0.35f);
            var luz = Emisivo("Neon_ventanal", Hex("#7FE3FF"), 1.4f);
            Malla("Zocalo", v, Mallas.Guardar(Mallas.Arco(13.1f, -100, 100, -0.2f, 0.55f), "zocalo"), Vector3.zero, marco, false);
            Malla("ZocaloLuz", v, Mallas.Guardar(Mallas.Arco(13.08f, -100, 100, 0.55f, 0.6f), "zocalo_luz"), Vector3.zero, luz, false);
            Malla("Dintel", v, Mallas.Guardar(Mallas.Arco(13.1f, -100, 100, 8.7f, 15f), "dintel"), Vector3.zero, Plano("Dintel", Hex("#081426")), false);
            Malla("DintelLuz", v, Mallas.Guardar(Mallas.Arco(13.08f, -100, 100, 8.7f, 8.78f), "dintel_luz"), Vector3.zero, luz, false);
            var pilar = Mallas.Guardar(Mallas.Panel(0.22f, 8.6f, 0.2f, 0.09f), "pilar");
            for (int a = -80; a <= 80; a += 20)
            {
                float r = a * Mathf.Deg2Rad;
                var g = Malla("Pilar", v, pilar, new Vector3(Mathf.Sin(r) * 13.05f, 4.4f, Mathf.Cos(r) * 13.05f), marco, false);
                g.transform.localRotation = Quaternion.Euler(0, a, 0);
            }
        }

        static void FondoMarino(Transform p)
        {
            var f = new GameObject("FondoMarino").transform; f.SetParent(p, false);
            var rnd = new System.Random(5);
            System.Func<float, float, float> R = (a, b) => (float)(a + rnd.NextDouble() * (b - a));
            System.Func<float, float, Vector3> Fuera = (ang, radio) => new Vector3(Mathf.Sin(ang * Mathf.Deg2Rad) * radio, -0.1f, -1f + Mathf.Cos(ang * Mathf.Deg2Rad) * radio);

            var rocas = new[] { Mat("Roca_azul", Hex("#2E5D80"), 0.1f), Mat("Roca_violeta", Hex("#4B4B86"), 0.1f), Mat("Roca_verde", Hex("#2A6B73"), 0.1f) };
            var mallasRoca = new[] { Mallas.Guardar(Mallas.Roca(1), "roca_a"), Mallas.Guardar(Mallas.Roca(2), "roca_b"), Mallas.Guardar(Mallas.Roca(3), "roca_c") };
            var algas = new[] { Emisivo("Alga_verde", Hex("#2FBF87"), 0.2f), Emisivo("Alga_turquesa", Hex("#1FA6A0"), 0.2f), Emisivo("Alga_coral", Hex("#F2766B"), 0.2f), Emisivo("Alga_violeta", Hex("#8E6BE8"), 0.2f) };
            var mallasAlga = new[] { Mallas.Guardar(Mallas.Alga(1.9f, 0.3f, 1), "alga_a"), Mallas.Guardar(Mallas.Alga(1.4f, 0.26f, 2), "alga_b"), Mallas.Guardar(Mallas.Alga(2.4f, 0.34f, 3), "alga_c") };

            for (int i = 0; i < 17; i++)
            {
                float ang = -80 + i * 10 + R(-3, 3), radio = R(14.2f, 17.5f);
                var centro = Fuera(ang, radio);
                var roca = Malla("Roca", f, mallasRoca[i % 3], centro + Vector3.up * 0.15f, rocas[i % 3], false);
                roca.transform.localScale = new Vector3(R(1.2f, 3f), R(0.7f, 1.7f), R(1.2f, 2.2f));
                roca.transform.localRotation = Quaternion.Euler(0, R(0, 360), 0);
                int mata = i % 4;
                int n = 3 + rnd.Next(4);
                for (int k = 0; k < n; k++)
                {
                    var alga = Malla("Alga", f, mallasAlga[rnd.Next(3)], centro + new Vector3(R(-1f, 1f), 0, R(-0.8f, 0.4f)), algas[mata], false);
                    alga.transform.localRotation = Quaternion.Euler(0, R(-40, 40), R(-8, 8));
                    alga.transform.localScale = Vector3.one * R(0.7f, 1.3f);
                    var o = alga.AddComponent<Ondular>(); o.amplitud = R(3, 7); o.velocidad = R(0.7f, 1.3f);
                }
            }

            // Peces animados que nadan detrás del cristal
            string[] peces = { "Peces/Fish1.fbx", "Peces/Fish2.fbx", "Peces/Fish3.fbx" };
            for (int i = 0; i < 18; i++)
            {
                var pez = ConstruirPersonajes.Pez(peces[i % 3], f, R(0.5f, 1.05f));
                var nado = pez.AddComponent<Nadador>();
                nado.centro = new Vector3((i % 2 == 0 ? -1 : 1) * R(0.5f, 6.5f), R(1.4f, 7.4f), R(13.6f, 15f));
                nado.radio = R(2f, 5.5f); nado.velocidad = R(0.18f, 0.4f) * (i % 3 == 0 ? -1 : 1); nado.fase = R(0, 6.28f); nado.ondaVertical = R(0.2f, 0.6f);
                PonerEnRuta(pez.transform, nado);
            }
            var grandes = new[] { ("Characters_Shark.fbx", 3.4f, 7.2f, 7.5f, 0.11f), ("Peces/Manta ray.fbx", 2.6f, 5.6f, 5f, -0.09f), ("Peces/Dolphin.fbx", 2.2f, 3.2f, 8.5f, 0.15f) };
            int j = 0;
            foreach (var (archivo, largo, y, radio, vel) in grandes)
            {
                var pez = ConstruirPersonajes.Pez(archivo, f, largo);
                var nado = pez.AddComponent<Nadador>();
                nado.centro = new Vector3(0, y, 15.2f); nado.radio = radio; nado.velocidad = vel; nado.fase = 0.7f + 2.1f * j++; nado.ondaVertical = 0.25f;
                PonerEnRuta(pez.transform, nado);
            }

            // Burbujas
            var mat = Material_("Burbuja", "Legacy Shaders/Particles/Alpha Blended");
            mat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Escenario/burbuja.png");
            foreach (var pos in new[] { new Vector3(-9.5f, 0, 6f), new Vector3(9.5f, 0, 6.5f), new Vector3(-6.5f, 0, 13f), new Vector3(6.8f, 0, 13.2f), new Vector3(-11.5f, 0, 9f), new Vector3(11.5f, 0, 9f) })
            {
                var b = new GameObject("Burbujas"); b.transform.SetParent(f, false); b.transform.localPosition = pos; b.transform.localRotation = Quaternion.Euler(-90, 0, 0);
                var ps = b.AddComponent<ParticleSystem>();
                var main = ps.main; main.startLifetime = 8f; main.startSpeed = 0.95f; main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.2f); main.prewarm = true;
                var em = ps.emission; em.rateOverTime = 2.5f;
                var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 0.35f;
                var ruido = ps.noise; ruido.enabled = true; ruido.strength = 0.18f; ruido.frequency = 0.5f;
                b.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
            }
        }

        static void PonerEnRuta(Transform t, Nadador n)
        {
            float a = n.fase;
            t.position = n.centro + new Vector3(Mathf.Cos(a) * n.radio, Mathf.Sin(a * 1.7f) * n.ondaVertical, Mathf.Sin(a) * n.radio * 0.4f);
            var d = new Vector3(-Mathf.Sin(a) * n.velocidad, 0, Mathf.Cos(a) * n.velocidad * 0.4f);
            if (d.sqrMagnitude > 1e-6f) t.rotation = Quaternion.LookRotation(d);
        }

        /// <summary>Estación de un tiburón: tarima y podio redondeado (el tiburón y su letrero llegan al iniciar la sesión).</summary>
        static GameObject PrefabEstacion()
        {
            var raiz = new GameObject("Sillon");
            var cuerpo = Mat("Podio", Hex("#1B466C"), 0.62f, 0.2f);
            var tapa = Mat("Podio_tapa", Hex("#8FB4D1"), 0.45f, 0.05f);
            var placa = Mat("Podio_placa", Hex("#07182B"), 0.4f, 0.1f);
            Malla("Tarima", raiz.transform, Mallas.Guardar(Mallas.Disco(0.78f, 0.24f, 0.08f), "tarima"), new Vector3(0, 0.1f, -0.05f), cuerpo);
            Malla("Podio", raiz.transform, Mallas.Guardar(Mallas.Disco(0.66f, 0.95f, 0.12f), "podio"), new Vector3(0, 0.12f, 0.98f), cuerpo);
            Malla("Tapa", raiz.transform, Mallas.Guardar(Mallas.Disco(0.73f, 0.09f, 0.045f), "tapa"), new Vector3(0, 1.06f, 0.98f), tapa);
            Malla("Placa", raiz.transform, Mallas.Guardar(Mallas.Panel(1.12f, 0.36f, 0.07f, 0.15f), "placa"), new Vector3(0, 0.68f, 1.62f), placa);
            var prefab = PrefabUtility.SaveAsPrefabAsset(raiz, "Assets/Prefabs/Sillon.prefab");
            Object.DestroyImmediate(raiz);
            return prefab;
        }

        static Renderer Imagen(string nombre, Transform padre, Vector3 pos, float lado, string material)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = nombre; q.transform.SetParent(padre, false); q.transform.localPosition = pos; q.transform.localScale = Vector3.one * lado;
            Object.DestroyImmediate(q.GetComponent<Collider>());
            var r = q.GetComponent<Renderer>(); r.sharedMaterial = Material_(material, "Unlit/Transparent"); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return r;
        }

        static Transform Monitor(string nombre, Transform padre, Vector3 pos, float rotY, float ancho, float alto)
        {
            var g = new GameObject(nombre).transform; g.SetParent(padre, false); g.localPosition = pos; g.localRotation = Quaternion.Euler(0, rotY, 0);
            Malla("Marco", g, Mallas.Guardar(Mallas.Panel(ancho + 0.34f, alto + 0.34f, 0.22f, 0.36f), nombre + "_marco"), new Vector3(0, 0, 0.16f), Mat("Marco_pantalla", Hex("#0A1A2F"), 0.6f, 0.35f), false);
            Malla("Brillo", g, Mallas.Guardar(Mallas.Panel(ancho + 0.1f, alto + 0.1f, 0.06f, 0.28f), nombre + "_brillo"), new Vector3(0, 0, 0.05f), Emisivo("Neon_pantalla", Hex("#35C6F4"), 1.2f), false);
            Malla("Fondo", g, Mallas.Guardar(Mallas.Panel(ancho, alto, 0.06f, 0.24f), nombre + "_fondo"), new Vector3(0, 0, 0f), Plano("Pantalla_fondo", Hex("#08203A")), false);
            return g;
        }

        static Pantalla PantallaPrincipal(Transform estudio)
        {
            var g = Monitor("Pantalla", estudio, new Vector3(0, 5.3f, 9.7f), 0f, 10f, 4.7f);
            var p = g.gameObject.AddComponent<Pantalla>();
            p.ilustracion = Imagen("Ilustracion", g, new Vector3(-2.95f, 0, -0.06f), 3.7f, "Lamina");
            p.antetitulo = Texto("Antetitulo", g, new Vector3(-0.82f, 2.02f, -0.06f), "NeuralHub", 100, 0.0215f, Hex("#FFC857"), TextAnchor.UpperLeft);
            p.titulo = Texto("Titulo", g, new Vector3(-0.85f, 1.65f, -0.06f), "Panel de tiburones", 100, 0.047f, Color.white, TextAnchor.UpperLeft, "SpaceGrotesk-Bold");
            p.detalle = Texto("Detalle", g, new Vector3(-0.82f, 0.25f, -0.06f), "Inversionistas con\nagentes de IA", 100, 0.0262f, Hex("#CFE8FF"), TextAnchor.UpperLeft, "Inter-Regular");
            p.ilustracion.sharedMaterial.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Laminas/tema_votacion.png");
            return p;
        }

        static PantallasDatos PantallasLaterales(Transform estudio)
        {
            var raiz = new GameObject("PantallasDatos"); raiz.transform.SetParent(estudio, false);
            var d = raiz.AddComponent<PantallasDatos>();

            var izq = Monitor("PantallaOferta", raiz.transform, new Vector3(-8.35f, 4.8f, 7.9f), -32f, 3.7f, 3.5f);
            d.tituloOferta = Texto("Titulo", izq, new Vector3(0, 1.28f, -0.06f), "La oferta", 100, 0.029f, Color.white, TextAnchor.MiddleCenter, "SpaceGrotesk-Bold");
            d.textoOferta = Texto("Datos", izq, new Vector3(0, -0.45f, -0.06f), "", 100, 0.0205f, Hex("#CFE8FF"), TextAnchor.MiddleCenter, "Inter-Regular");
            d.textoOferta.lineSpacing = 1.1f;
            d.icono = Imagen("Icono", izq, new Vector3(0, 0.72f, -0.06f), 0.55f, "Icono_lateral"); d.icono.enabled = false;

            var der = Monitor("PantallaPanel", raiz.transform, new Vector3(8.35f, 4.8f, 7.9f), 32f, 3.7f, 3.5f);
            d.tituloPanel = Texto("Titulo", der, new Vector3(0, 1.28f, -0.06f), "Votos del panel", 100, 0.029f, Color.white, TextAnchor.MiddleCenter, "SpaceGrotesk-Bold");
            var riel = Mallas.Guardar(Mallas.Panel(3.1f, 0.2f, 0.02f, 0.095f), "riel");
            for (int i = 0; i < 3; i++)
            {
                float y = 0.55f - i * 0.82f;
                d.nombres[i] = Texto("Nombre" + i, der, new Vector3(-1.55f, y + 0.18f, -0.06f), "", 100, 0.0205f, Color.white, TextAnchor.MiddleLeft);
                d.estados[i] = Texto("Estado" + i, der, new Vector3(1.55f, y + 0.18f, -0.06f), "", 100, 0.018f, Hex("#FFC857"), TextAnchor.MiddleRight, "Inter-Regular");
                Malla("Riel" + i, der, riel, new Vector3(0, y - 0.14f, -0.05f), Plano("Riel", Hex("#17375C")), false);
                var b = GameObject.CreatePrimitive(PrimitiveType.Cube); b.name = "Barra" + i; Object.DestroyImmediate(b.GetComponent<Collider>());
                b.transform.SetParent(der, false); b.transform.localPosition = new Vector3(-1.55f, y - 0.14f, -0.075f); b.transform.localScale = new Vector3(0.05f, 0.15f, 0.02f);
                b.GetComponent<Renderer>().sharedMaterial = Plano("Barra" + i, Color.gray);
                d.barras[i] = b.transform; d.barrasRend[i] = b.GetComponent<Renderer>();
            }
            return d;
        }

        // ---------- luz y color ----------
        static Light Luz(string nombre, LightType tipo, Color c, float fuerza, Vector3 pos, Quaternion rot)
        {
            var l = new GameObject(nombre).AddComponent<Light>();
            l.type = tipo; l.color = c; l.intensity = fuerza; l.transform.position = pos; l.transform.rotation = rot;
            return l;
        }

        static void Iluminacion()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Hex("#8FC4E8");
            RenderSettings.ambientEquatorColor = Hex("#4C7FA8");
            RenderSettings.ambientGroundColor = Hex("#1F3550");
            RenderSettings.fog = false;
            RenderSettings.reflectionIntensity = 0.3f;

            var key = Luz("LuzPrincipal", LightType.Directional, Hex("#FFF1DC"), 1.05f, Vector3.zero, Quaternion.Euler(42, -22, 0));
            key.shadows = LightShadows.Soft; key.shadowStrength = 0.55f;
            Luz("LuzRelleno", LightType.Directional, Hex("#6FD3FF"), 0.35f, Vector3.zero, Quaternion.Euler(28, 145, 0));
            var agua = Luz("LuzCausticas", LightType.Directional, Hex("#9BE8FF"), 0.2f, Vector3.zero, Quaternion.Euler(72, 15, 0));
            agua.cookie = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/causticas.png"); agua.cookieSize = 7f;
            agua.gameObject.AddComponent<Caustica>();
            foreach (var (pos, color) in new[] { (new Vector3(-7f, 4.5f, 4f), Hex("#7C5CFC")), (new Vector3(7f, 4.5f, 4f), Hex("#35C6F4")) })
            {
                var l = Luz("LuzColor", LightType.Point, color, 1.3f, pos, Quaternion.identity); l.range = 11f;
            }
            var frente = Luz("LuzEmprendedor", LightType.Point, Hex("#FFE3B0"), 0.9f, new Vector3(1.4f, 3.2f, -1.2f), Quaternion.identity); frente.range = 6f;
        }

        static void Posprocesado(GameObject camara)
        {
            const string ruta = "Assets/Posprocesado.asset";
            AssetDatabase.DeleteAsset(ruta);
            var perfil = ScriptableObject.CreateInstance<PostProcessProfile>();
            AssetDatabase.CreateAsset(perfil, ruta);
            var bloom = Agregar<Bloom>(perfil); bloom.intensity.Override(2f); bloom.threshold.Override(1.05f); bloom.softKnee.Override(0.5f); bloom.diffusion.Override(6.5f);
            var color = Agregar<ColorGrading>(perfil); color.gradingMode.Override(GradingMode.HighDefinitionRange); color.tonemapper.Override(Tonemapper.Neutral);
            color.postExposure.Override(0.1f); color.saturation.Override(14f); color.contrast.Override(8f);
            var vineta = Agregar<Vignette>(perfil); vineta.intensity.Override(0.24f); vineta.smoothness.Override(0.45f);
            var ao = Agregar<AmbientOcclusion>(perfil); ao.mode.Override(AmbientOcclusionMode.ScalableAmbientObscurance); ao.intensity.Override(0.4f); ao.radius.Override(0.35f);
            EditorUtility.SetDirty(perfil);

            var capa = camara.AddComponent<PostProcessLayer>();
            capa.Init(AssetDatabase.LoadAssetAtPath<PostProcessResources>("Packages/com.unity.postprocessing/PostProcessing/PostProcessResources.asset"));
            capa.volumeTrigger = camara.transform; capa.volumeLayer = 1 << 0;
            capa.antialiasingMode = PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing;
            var vol = new GameObject("Posprocesado").AddComponent<PostProcessVolume>();
            vol.isGlobal = true; vol.sharedProfile = perfil;
        }

        static T Agregar<T>(PostProcessProfile perfil) where T : PostProcessEffectSettings
        {
            var e = perfil.AddSettings<T>(); e.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
            AssetDatabase.AddObjectToAsset(e, perfil); e.enabled.Override(true); return e;
        }

        // ---------- escena ----------
        [MenuItem("NeuralHub/Construir estudio")]
        public static void Construir()
        {
            foreach (var d in new[] { "Assets/Scenes", "Assets/Prefabs", "Assets/Materiales", "Assets/StreamingAssets", "Assets/Modelos" })
                if (!AssetDatabase.IsValidFolder(d)) AssetDatabase.CreateFolder("Assets", Path.GetFileName(d));
            PlayerSettings.colorSpace = ColorSpace.Linear;
            ImportarTexturas();
            ConstruirPersonajes.ConfigurarImportaciones();
            ConstruirPersonajes.Reiniciar();

            var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var prefabTiburon = ConstruirPersonajes.PrefabTiburon();
            var prefabEstacion = PrefabEstacion();
            var prefabEmprendedor = ConstruirPersonajes.PrefabEmprendedor();
            ConstruirCaracter.Retratos(prefabTiburon);

            var estudioGO = new GameObject("Estudio");
            Piso(estudioGO.transform);
            Ventanal(estudioGO.transform);
            FondoMarino(estudioGO.transform);
            var pantalla = PantallaPrincipal(estudioGO.transform);
            var datos = PantallasLaterales(estudioGO.transform);
            Iluminacion();
            var setEmprendedor = ConstruirCaracter.FondoEmprendedor(estudioGO.transform);

            var letrero = Texto("LetreroResultado", estudioGO.transform, new Vector3(0, 8.2f, 9.5f), "", 150, 0.05f, Color.white, TextAnchor.MiddleCenter, "SpaceGrotesk-Bold");

            var emp = (GameObject)PrefabUtility.InstantiatePrefab(prefabEmprendedor, estudioGO.transform);
            emp.transform.position = new Vector3(0, 0.16f, -2.5f); emp.transform.rotation = Quaternion.identity;

            // 3 estaciones fijas en semicírculo
            var sillones = new Transform[3];
            float[] ang = { -43f, 0f, 43f };
            for (int i = 0; i < 3; i++)
            {
                var s = (GameObject)PrefabUtility.InstantiatePrefab(prefabEstacion, estudioGO.transform);
                s.name = "Sillon_" + (i + 1);
                float a = ang[i] * Mathf.Deg2Rad;
                s.transform.position = new Vector3(6f * Mathf.Sin(a), 0, -1f + 6f * Mathf.Cos(a));
                var hacia = emp.transform.position - s.transform.position; hacia.y = 0;
                s.transform.rotation = Quaternion.LookRotation(hacia);
                sillones[i] = s.transform;
            }

            var reflGO = new GameObject("Reflector");
            var spot = reflGO.AddComponent<Light>();
            spot.type = LightType.Spot; spot.range = 18; spot.spotAngle = 32; spot.intensity = 1.4f; spot.color = Hex("#FFF2D6"); spot.enabled = false;
            reflGO.transform.position = new Vector3(0, 7f, -4f);
            var reflector = reflGO.AddComponent<Reflector>(); reflector.luz = spot;

            var camGO = new GameObject("Main Camera"); camGO.tag = "MainCamera";
            var cam = camGO.AddComponent<Camera>(); cam.fieldOfView = 52; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Hex("#081426"); cam.allowHDR = true;
            camGO.AddComponent<AudioListener>();
            var camara = camGO.AddComponent<CamaraEstudio>();
            camGO.transform.position = camara.posGeneral;
            camGO.transform.rotation = Quaternion.LookRotation(camara.miraGeneral - camara.posGeneral);
            Posprocesado(camGO);

            // Sonido ambiente submarino (opcional: paquete gratuito «Underwater Ambient» de la Asset Store)
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Underwater Ambient.wav");
            if (clip)
            {
                var a = new GameObject("SonidoAmbiente").AddComponent<AudioSource>();
                a.transform.SetParent(estudioGO.transform, false);
                a.clip = clip; a.loop = true; a.playOnAwake = true; a.volume = 0.14f; a.spatialBlend = 0f;
            }

            var estudio = estudioGO.AddComponent<Estudio>();
            estudio.prefabTiburon = prefabTiburon; estudio.sillones = sillones; estudio.emprendedor = emp.GetComponent<Emprendedor>();
            estudio.setEmprendedor = setEmprendedor; estudio.camara = camara; estudio.reflector = reflector; estudio.pantalla = pantalla; estudio.letreroResultado = letrero;

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

        static void ImportarTexturas()
        {
            AssetDatabase.Refresh();
            foreach (var carpeta in new[] { "Assets/Resources/Laminas", "Assets/Resources/Escenario", "Assets/Resources/Retratos" })
                foreach (var f in Directory.Exists(carpeta) ? Directory.GetFiles(carpeta, "*.png") : new string[0])
                {
                    var imp = AssetImporter.GetAtPath(f.Replace((char)92, '/')) as TextureImporter;
                    if (imp == null || imp.wrapMode == TextureWrapMode.Clamp) continue;
                    imp.wrapMode = TextureWrapMode.Clamp; imp.alphaIsTransparency = true; imp.maxTextureSize = 4096; imp.textureCompression = TextureImporterCompression.CompressedHQ;
                    imp.SaveAndReimport();
                }
            var c = AssetImporter.GetAtPath("Assets/Resources/causticas.png") as TextureImporter;
            if (c != null && c.textureType != TextureImporterType.Cookie)
            { c.textureType = TextureImporterType.Cookie; c.alphaSource = TextureImporterAlphaSource.FromGrayScale; c.wrapMode = TextureWrapMode.Repeat; c.SaveAndReimport(); }
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
