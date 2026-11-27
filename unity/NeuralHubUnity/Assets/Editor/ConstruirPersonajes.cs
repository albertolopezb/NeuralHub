using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace NeuralHub.Editor
{
    /// <summary>
    /// Personajes y animales de Quaternius (CC0): tiburones inversionistas (Sharky y Mako, con corbata del color de su perfil),
    /// el emprendedor (Ultimate Modular Men) y los peces animados que nadan detrás del ventanal.
    /// </summary>
    public static class ConstruirPersonajes
    {
        public const string Q = "Assets/Terceros/Quaternius/";
        public const string ModeloEmprendedor = "Emprendedor_Traje.fbx";
        static readonly string[] Ciclicas = { "Idle", "Idle_Neutral", "Yes", "No", "Wave", "Jump", "Interact", "Swim", "Swim.001", "Walk" };

        static string NombreCorto(string clip) { int i = clip.LastIndexOf('|'); return i >= 0 ? clip.Substring(i + 1) : clip; }

        /// <summary>Nombra los clips sin el prefijo del esqueleto y marca los que se repiten.</summary>
        public static void ConfigurarImportaciones()
        {
            foreach (var f in Directory.GetFiles(Q, "*.fbx", SearchOption.AllDirectories))
            {
                var ruta = f.Replace((char)92, '/');
                var imp = AssetImporter.GetAtPath(ruta) as ModelImporter;
                if (imp == null) continue;
                var clips = imp.defaultClipAnimations;
                if (clips.Length == 0) continue;
                foreach (var c in clips) { c.name = NombreCorto(c.takeName); c.loopTime = Ciclicas.Contains(c.name); }
                var actuales = imp.clipAnimations;
                bool igual = actuales.Length == clips.Length && actuales.Zip(clips, (a, b) => a.name == b.name && a.loopTime == b.loopTime).All(x => x);
                if (!igual) { imp.clipAnimations = clips; imp.SaveAndReimport(); }
            }
            var atlas = AssetImporter.GetAtPath(Q + "Atlas_Tiburon.png") as TextureImporter;
            if (atlas != null && (atlas.filterMode != FilterMode.Point || atlas.mipmapEnabled))
            { atlas.filterMode = FilterMode.Point; atlas.mipmapEnabled = false; atlas.textureCompression = TextureImporterCompression.Uncompressed; atlas.SaveAndReimport(); }
        }

        public static List<AnimationClip> Clips(string archivo)
        {
            return AssetDatabase.LoadAllAssetsAtPath(Q + archivo).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__")).ToList();
        }

        public static AnimationClip Clip(string archivo, string nombre) { return Clips(archivo).FirstOrDefault(c => c.name == nombre); }

        /// <summary>Controlador con un estado por animación; «inicial» es el estado por defecto.</summary>
        public static RuntimeAnimatorController Controlador(string nombre, string archivo, string inicial)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animaciones")) AssetDatabase.CreateFolder("Assets", "Animaciones");
            string ruta = "Assets/Animaciones/" + nombre + ".controller";
            var existente = AssetDatabase.LoadAssetAtPath<AnimatorController>(ruta);
            if (existente != null && hechos.Contains(ruta)) return existente;
            AssetDatabase.DeleteAsset(ruta);
            var ctl = AnimatorController.CreateAnimatorControllerAtPath(ruta);
            var sm = ctl.layers[0].stateMachine;
            foreach (var clip in Clips(archivo))
            {
                var st = sm.AddState(clip.name.Replace('.', '_'));
                st.motion = clip;
                if (clip.name == inicial) sm.defaultState = st;
            }
            hechos.Add(ruta);
            return ctl;
        }
        static readonly HashSet<string> hechos = new HashSet<string>();

        public static void Reiniciar() { hechos.Clear(); }

        /// <summary>
        /// Los colores de estos FBX vienen en espacio lineal: se copian a materiales propios convertidos a sRGB.
        /// Las piezas con «Atlas» usan la textura de paleta (con el pantalón teñido de traje).
        /// </summary>
        static void Colorear(GameObject g, string prefijo)
        {
            foreach (var r in g.GetComponentsInChildren<Renderer>(true))
            {
                var ms = r.sharedMaterials;
                for (int i = 0; i < ms.Length; i++)
                {
                    if (ms[i] == null) continue;
                    if (ms[i].name == "Atlas")
                    {
                        var a = ConstruirEstudio.Mat("Q_Atlas", Color.white, 0.2f);
                        a.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Q + "Atlas_Tiburon.png");
                        ms[i] = a;
                    }
                    else if (!ms[i].name.StartsWith("Q_"))
                    {
                        var c = ms[i].color.gamma; c.a = 1;
                        ms[i] = ConstruirEstudio.Mat("Q_" + prefijo + "_" + ms[i].name, c, 0.2f);
                    }
                }
                r.sharedMaterials = ms;
            }
        }

        static GameObject Instanciar(string archivo, Transform padre, string nombre)
        {
            var g = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Q + archivo));
            PrefabUtility.UnpackPrefabInstance(g, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            g.name = nombre; g.transform.SetParent(padre, false);
            return g;
        }

        static Animator Animar(GameObject g, RuntimeAnimatorController ctl)
        {
            var an = g.GetComponent<Animator>(); if (an == null) an = g.AddComponent<Animator>();
            an.runtimeAnimatorController = ctl; an.applyRootMotion = false; an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            return an;
        }

        static Transform Buscar(GameObject g, string nombre) { return g.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == nombre); }

        /// <summary>Cuello y corbata sobre el pecho: se ajusta a la superficie del modelo y sigue al hueso del torso.</summary>
        static Renderer Corbata(GameObject modelo, string sufijo, float bajar, float largo)
        {
            var cuello = Buscar(modelo, "Neck"); var torso = Buscar(modelo, "Torso");
            var piel = modelo.GetComponentsInChildren<SkinnedMeshRenderer>(true).OrderByDescending(r => r.sharedMesh.vertexCount).First();
            var horneada = new Mesh(); piel.BakeMesh(horneada, true);
            var pts = horneada.vertices.Select(p => piel.transform.TransformPoint(p)).ToArray();
            float y0 = cuello.position.y - 0.02f - bajar;
            System.Func<float, float> frente = y =>
            {
                var cerca = pts.Where(p => Mathf.Abs(p.x - cuello.position.x) < 0.1f && Mathf.Abs(p.y - y) < 0.07f).ToList();
                return cerca.Count > 0 ? cerca.Max(p => p.z) : cuello.position.z + 0.18f;
            };
            float[] bajada = { 0.00f, 0.07f, 0.11f, 0.2f, 0.3f, 0.39f, 0.46f };
            float[] anchoCorbata = { 0.11f, 0.07f, 0.062f, 0.11f, 0.14f, 0.14f, 0.005f };
            var centro = bajada.Select(d => new Vector3(cuello.position.x, y0 - d * largo, frente(y0 - d * largo) + 0.03f)).ToArray();
            var corbata = new GameObject("Corbata", typeof(MeshFilter), typeof(MeshRenderer));
            corbata.GetComponent<MeshFilter>().sharedMesh = Mallas.Guardar(Mallas.Cinta(centro, anchoCorbata, Vector3.right, Vector3.forward), "corbata_" + sufijo);
            var rc = corbata.GetComponent<Renderer>(); rc.sharedMaterial = ConstruirEstudio.Mat("Corbata", Color.white, 0.5f);
            var blanco = ConstruirEstudio.Mat("Camisa", new Color(0.97f, 0.97f, 1f), 0.25f);
            foreach (int lado in new[] { -1, 1 })
            {
                var solapa = new GameObject("Solapa", typeof(MeshFilter), typeof(MeshRenderer));
                var a = centro[0] + new Vector3(0.015f * lado, 0.035f, 0.012f);
                var puntos = new[] { a + new Vector3(0.07f * lado, 0.03f, -0.03f), a + new Vector3(0.085f * lado, -0.085f, 0f) };
                solapa.GetComponent<MeshFilter>().sharedMesh = Mallas.Guardar(Mallas.Cinta(puntos, new[] { 0.11f, 0.008f }, Vector3.right, Vector3.forward), "solapa_" + sufijo + (lado < 0 ? "_i" : "_d"));
                solapa.GetComponent<Renderer>().sharedMaterial = blanco;
                solapa.transform.SetParent(corbata.transform, true);
            }
            corbata.transform.SetParent(torso, true);
            return rc;
        }

        public static Tiburon PrefabTiburon()
        {
            var raiz = new GameObject("Tiburon");
            var giro = new GameObject("Giro").transform; giro.SetParent(raiz.transform, false);
            var modelos = new List<Tiburon.Modelo>();
            var pintables = new List<Renderer>();
            foreach (var (archivo, nombre, escala) in new[] { ("Characters_Sharky.fbx", "Sharky", 1.42f), ("Characters_Mako.fbx", "Mako", 1.36f) })
            {
                var g = Instanciar(archivo, giro, nombre);
                foreach (var r in g.GetComponentsInChildren<Renderer>(true)) if (r.name.StartsWith("Weapon")) Object.DestroyImmediate(r.gameObject);
                var an = Animar(g, Controlador("Tiburon_" + nombre, archivo, "Idle"));
                var idle = Clip(archivo, "Idle"); if (idle) idle.SampleAnimation(g, 0f);
                Colorear(g, "Tiburon");
                pintables.Add(Corbata(g, nombre, nombre == "Sharky" ? 0.17f : 0.02f, nombre == "Sharky" ? 0.66f : 1f));
                g.transform.localScale = Vector3.one * escala;
                g.transform.localPosition = new Vector3(0, 0.46f, -0.02f);
                modelos.Add(new Tiburon.Modelo { raiz = g, animador = an, cabeza = Buscar(g, "Head") });
            }
            modelos[1].raiz.SetActive(false);

            // Aro de luz del podio y letrero con el perfil
            var aro = ConstruirEstudio.Malla("AroPerfil", raiz.transform, Mallas.Guardar(Mallas.Aro(0.6f, 0.69f), "aro_perfil"), new Vector3(0, 1.157f, 0.98f), ConstruirEstudio.Emisivo("Aro_perfil", Color.white, 1.6f), false);
            pintables.Add(aro.GetComponent<Renderer>());
            var letrero = ConstruirEstudio.Texto("Letrero", raiz.transform, new Vector3(0, 0.68f, 1.668f), "", 90, 0.017f, Color.white);
            letrero.transform.localRotation = Quaternion.Euler(0, 180, 0);

            // Paleta de voto: placa redondeada con la decisión
            var paleta = new GameObject("PaletaVoto").transform; paleta.SetParent(raiz.transform, false); paleta.localPosition = new Vector3(0, 3.22f, 0.3f); paleta.localScale = Vector3.one * 0.68f;
            ConstruirEstudio.Malla("Placa", paleta, Mallas.Guardar(Mallas.Panel(1.15f, 0.46f, 0.07f, 0.2f), "paleta"), Vector3.zero, ConstruirEstudio.Emisivo("Paleta", Color.white, 0.3f), false);
            var tp = ConstruirEstudio.Texto("Voto", paleta, new Vector3(0, 0, 0.05f), "", 90, 0.022f, new Color(0.03f, 0.06f, 0.12f));
            tp.transform.localRotation = Quaternion.Euler(0, 180, 0);

            var juegos = ConstruirCaracter.Juegos(raiz.transform, modelos);
            var t = raiz.AddComponent<Tiburon>();
            t.juegos = juegos;
            t.corbatas = pintables.Take(2).Select(r => r.gameObject).ToArray();
            t.modelos = modelos.ToArray(); t.giro = giro; t.paleta = paleta; t.letrero = letrero; t.pintables = pintables.ToArray();
            var prefab = PrefabUtility.SaveAsPrefabAsset(raiz, "Assets/Prefabs/Tiburon.prefab");
            Object.DestroyImmediate(raiz);
            return prefab.GetComponent<Tiburon>();
        }

        public static GameObject PrefabEmprendedor()
        {
            var raiz = new GameObject("Emprendedor");
            var g = Instanciar(ModeloEmprendedor, raiz.transform, "Modelo");
            // El FBX trae accesorios de otros juegos (armas): se quitan
            foreach (var r in g.GetComponentsInChildren<Renderer>(true))
                if (new[] { "Gun", "Sword", "Pistol", "Rifle", "Weapon" }.Any(x => r.name.Contains(x))) Object.DestroyImmediate(r.gameObject);
            var an = Animar(g, Controlador("Emprendedor", ModeloEmprendedor, "Idle"));
            Colorear(g, "Emprendedor");
            var e = raiz.AddComponent<Emprendedor>();
            e.animador = an;
            var prefab = PrefabUtility.SaveAsPrefabAsset(raiz, "Assets/Prefabs/Emprendedor.prefab");
            Object.DestroyImmediate(raiz);
            return prefab;
        }

        /// <summary>Pez animado centrado, con el largo pedido y mirando a +Z (lo mueve Nadador).</summary>
        public static GameObject Pez(string archivo, Transform padre, float largo)
        {
            var cont = new GameObject("Pez"); cont.transform.SetParent(padre, false);
            var g = Instanciar(archivo, cont.transform, "Modelo");
            var clave = "Pez_" + Path.GetFileNameWithoutExtension(archivo).Replace(' ', '_');
            var clips = Clips(archivo);
            if (clips.Count > 0) Animar(g, Controlador(clave, archivo, clips[0].name));
            Colorear(g, clave);
            var rs = g.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            float s = largo / Mathf.Max(b.size.x, b.size.y, b.size.z);
            var centro = cont.transform.InverseTransformPoint(b.center);
            g.transform.localScale *= s;
            g.transform.localPosition = -centro * s;
            foreach (var r in rs) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return cont;
        }
    }
}
