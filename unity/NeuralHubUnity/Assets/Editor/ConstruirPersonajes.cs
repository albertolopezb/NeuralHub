using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace NeuralHub.Editor
{
    /// <summary>
    /// Personajes con assets gratuitos: cuerpo humanoide de traje (Suit Character Pack, BlocBros), cabeza de tiburón
    /// (Fish - PolyPack, Alstra Infinite) y animaciones de Mixamo. Si los paquetes no están importados, el estudio usa
    /// los personajes de primitivas (ver unity/paquetes_terceros.md).
    /// </summary>
    public static class ConstruirPersonajes
    {
        public const string Traje = "Assets/genSuit/genSuit.fbx";
        public const string CabezaTiburon = "Assets/Alstra Infinite/Fish - PolyPack/Models/SharkV1.fbx";
        public const string CarpetaMixamo = "Assets/Terceros/Mixamo";
        const string RutaControlTiburon = "Assets/Animaciones/Tiburon.controller";
        const string RutaControlEmprendedor = "Assets/Animaciones/Emprendedor.controller";

        public static bool Disponibles()
        {
            return File.Exists(Traje) && File.Exists(CabezaTiburon) && Directory.Exists(CarpetaMixamo);
        }

        static readonly string[] Ciclicas = { "Reposo", "Hablando", "Pensando", "ReposoFeliz", "PreguntaDosManos", "Aplauso" };

        /// <summary>Humanoide para el traje y para cada FBX de Mixamo; las animaciones de reposo y de hablar se repiten.</summary>
        public static void ConfigurarImportaciones()
        {
            var rutas = new List<string> { Traje };
            foreach (var f in Directory.GetFiles(CarpetaMixamo, "*.fbx")) rutas.Add(f.Replace('\\', '/'));
            foreach (var ruta in rutas)
            {
                var imp = AssetImporter.GetAtPath(ruta) as ModelImporter;
                if (imp == null) continue;
                bool cambio = false;
                if (imp.animationType != ModelImporterAnimationType.Human) { imp.animationType = ModelImporterAnimationType.Human; imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel; cambio = true; }
                if (ruta.StartsWith(CarpetaMixamo))
                {
                    var nombre = Path.GetFileNameWithoutExtension(ruta);
                    bool ciclo = false; foreach (var c in Ciclicas) if (nombre.Contains(c)) ciclo = true;
                    var clips = imp.defaultClipAnimations;
                    if (clips.Length > 0)
                    {
                        foreach (var c in clips)
                        {
                            c.name = nombre; c.loopTime = ciclo; c.lockRootRotation = true; c.lockRootHeightY = true; c.lockRootPositionXZ = true;
                            c.keepOriginalOrientation = true; c.keepOriginalPositionY = true; c.keepOriginalPositionXZ = true;
                        }
                        imp.clipAnimations = clips; cambio = true;
                    }
                }
                if (cambio) imp.SaveAndReimport();
            }
        }

        public static AnimationClip Clip(string nombre)
        {
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(CarpetaMixamo + "/" + nombre + ".fbx"))
                if (o is AnimationClip c && !c.name.StartsWith("__preview__")) return c;
            Debug.LogWarning("NeuralHub: falta la animación " + nombre);
            return null;
        }

        /// <summary>Controlador con un estado por animación; los scripts Tiburon/Emprendedor cambian de estado con CrossFade.</summary>
        static RuntimeAnimatorController Controlador(string ruta, string prefijo, string inicial)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animaciones")) AssetDatabase.CreateFolder("Assets", "Animaciones");
            AssetDatabase.DeleteAsset(ruta);
            var ctl = AnimatorController.CreateAnimatorControllerAtPath(ruta);
            var sm = ctl.layers[0].stateMachine;
            foreach (var f in Directory.GetFiles(CarpetaMixamo, prefijo + "*.fbx"))
            {
                var nombre = Path.GetFileNameWithoutExtension(f);
                var clip = Clip(nombre); if (clip == null) continue;
                var st = sm.AddState(nombre.Substring(prefijo.Length));
                st.motion = clip;
                if (nombre == prefijo + inicial) sm.defaultState = st;
            }
            AssetDatabase.SaveAssets();
            return ctl;
        }

        public static RuntimeAnimatorController ControlTiburon() { return Controlador(RutaControlTiburon, "T_", "SentadoReposo"); }
        public static RuntimeAnimatorController ControlEmprendedor() { return Controlador(RutaControlEmprendedor, "E_", "ReposoFeliz"); }

        /// <summary>Instancia el humano de traje con la textura indicada (Blue, Black, Beige, Dark Blue, Striped).</summary>
        public static GameObject CuerpoDeTraje(Transform padre, string color, RuntimeAnimatorController ctl)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Traje);
            var g = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            g.name = "CuerpoTraje"; g.transform.SetParent(padre, false);
            var an = g.GetComponent<Animator>(); if (an == null) an = g.AddComponent<Animator>();
            an.runtimeAnimatorController = ctl; an.applyRootMotion = false; an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var mat = MaterialTraje(color);
            if (mat) foreach (var r in g.GetComponentsInChildren<Renderer>()) { var ms = r.sharedMaterials; for (int i = 0; i < ms.Length; i++) ms[i] = mat; r.sharedMaterials = ms; }
            return g;
        }

        static Material MaterialTraje(string color)
        {
            string carpeta = "Assets/genSuit/Textures/" + color;
            if (!Directory.Exists(carpeta)) return null;
            string ruta = "Assets/Materiales/Traje_" + color.Replace(" ", "") + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null) { m = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(m, ruta); }
            foreach (var f in Directory.GetFiles(carpeta))
            {
                var p = f.Replace('\\', '/'); var n = Path.GetFileName(p).ToLower();
                if (p.EndsWith(".meta")) continue;
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(p); if (tex == null) continue;
                if (n.EndsWith("_nm.tif"))
                {
                    var ti = (TextureImporter)AssetImporter.GetAtPath(p);
                    if (ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; ti.SaveAndReimport(); }
                    m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(p)); m.EnableKeyword("_NORMALMAP");
                }
                else if (n.EndsWith("_d.tif")) m.mainTexture = tex;
            }
            m.SetFloat("_Glossiness", 0.25f);
            EditorUtility.SetDirty(m);
            return m;
        }

        // Ajustes de la cabeza de tiburón (espacio del personaje: z al frente, y arriba)
        public static float largoCabeza = 1.05f;
        public static Vector3 posCabeza = new Vector3(0f, 0.1f, -0.24f);
        public static Vector3 giroCabeza = new Vector3(0f, 90f, -8f);
        public static Vector3 offsetSentado = new Vector3(0f, 0f, 0.12f);

        static Transform Hueso(GameObject g, HumanBodyBones b) { var an = g.GetComponent<Animator>(); return an ? an.GetBoneTransform(b) : null; }

        /// <summary>Contenedor con el modelo centrado en su origen y escalado al largo pedido.</summary>
        static GameObject ModeloCentrado(string ruta, string nombre, float largo)
        {
            var cont = new GameObject(nombre);
            var m = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ruta));
            m.transform.SetParent(cont.transform, false);
            var rs = m.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            float s = largo / Mathf.Max(b.size.x, b.size.z);
            m.transform.localScale = Vector3.one * s;
            m.transform.localPosition = -b.center * s;
            foreach (var r in rs) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return cont;
        }

        public static Tiburon PrefabTiburon(Material blanco)
        {
            var ctl = ControlTiburon();
            var raiz = new GameObject("Tiburon");
            var cuerpo = CuerpoDeTraje(raiz.transform, "Dark Blue", ctl);
            cuerpo.transform.localPosition = offsetSentado;
            var head = Hueso(cuerpo, HumanBodyBones.Head);

            var cab = ModeloCentrado(CabezaTiburon, "CabezaTiburon", largoCabeza);
            cab.transform.SetParent(raiz.transform, false);
            // En la pose T se coloca la cabeza de tiburón y se cuelga del cuello: sigue cualquier animación
            cab.transform.position = head.position + raiz.transform.rotation * posCabeza;
            cab.transform.rotation = raiz.transform.rotation * Quaternion.Euler(giroCabeza);
            cab.transform.SetParent(Hueso(cuerpo, HumanBodyBones.Neck), true);
            var post = cuerpo.AddComponent<CabezaPostiza>();
            post.hueso = head; post.soloOcultar = true;

            // Insignia con el color del perfil en el pecho y paleta de voto en la mano derecha
            var pecho = Hueso(cuerpo, HumanBodyBones.UpperChest) ?? Hueso(cuerpo, HumanBodyBones.Chest);
            var insignia = ConstruirEstudio.Prim(PrimitiveType.Cube, "Insignia", pecho, Vector3.zero, Vector3.one, ConstruirEstudio.Mat("Tiburon_corbata", Color.white, 0.5f));
            insignia.transform.position = pecho.position + raiz.transform.forward * 0.16f + raiz.transform.right * 0.11f + Vector3.up * 0.05f;
            insignia.transform.rotation = raiz.transform.rotation;
            SetEscalaMundo(insignia.transform, new Vector3(0.09f, 0.06f, 0.02f));
            var mano = Hueso(cuerpo, HumanBodyBones.RightHand);
            var paleta = ConstruirEstudio.Prim(PrimitiveType.Cube, "PaletaVoto", mano, Vector3.zero, Vector3.one, blanco);
            paleta.transform.position = mano.position + Vector3.up * 0.22f;
            paleta.transform.rotation = raiz.transform.rotation;
            SetEscalaMundo(paleta.transform, new Vector3(0.4f, 0.3f, 0.03f));

            var letrero = ConstruirEstudio.Texto("Letrero", raiz.transform, new Vector3(0, 0.5f, 1.84f), "", 90, 0.022f, Color.white);
            letrero.transform.localRotation = Quaternion.Euler(0, 180, 0);

            var t = raiz.AddComponent<Tiburon>();
            t.animador = cuerpo.GetComponent<Animator>(); t.cabezaPostiza = cab.transform;
            t.paleta = paleta.transform; t.letrero = letrero;
            t.pintables = new[] { insignia.GetComponent<Renderer>() };
            var prefab = PrefabUtility.SaveAsPrefabAsset(raiz, "Assets/Prefabs/Tiburon.prefab");
            Object.DestroyImmediate(raiz);
            return prefab.GetComponent<Tiburon>();
        }

        /// <summary>Instancia un prefab de terceros con altura dada y base en y = pos.y; null si no está importado.</summary>
        public static GameObject Instanciar(string ruta, Transform padre, Vector3 pos, float rotY, float altura)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            if (asset == null) return null;
            var g = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            g.transform.SetParent(padre, false);
            g.transform.localRotation = Quaternion.Euler(0, rotY, 0);
            g.transform.localPosition = Vector3.zero;
            var rs = g.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0)
            {
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                if (altura > 0 && b.size.y > 1e-4f) { g.transform.localScale *= altura / b.size.y; b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); }
                var piso = padre ? padre.InverseTransformPoint(new Vector3(b.center.x, b.min.y, b.center.z)) : new Vector3(b.center.x, b.min.y, b.center.z);
                g.transform.localPosition = pos - new Vector3(piso.x, piso.y, piso.z);
            }
            foreach (var c in g.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            return g;
        }

        public static float giroSilla = 0f;
        public const string Vnb = "Assets/VNB - Office Set/Prefabs/";

        static void SetEscalaMundo(Transform t, Vector3 mundo)
        {
            var p = t.parent ? t.parent.lossyScale : Vector3.one;
            t.localScale = new Vector3(mundo.x / p.x, mundo.y / p.y, mundo.z / p.z);
        }

        /// <summary>Emprendedor humano de traje beige, de pie junto al podio.</summary>
        public static GameObject PrefabEmprendedor(Material podio, Material topePodio)
        {
            var ctl = ControlEmprendedor();
            var raiz = new GameObject("Emprendedor");
            var cuerpo = CuerpoDeTraje(raiz.transform, "Beige", ctl);
            cuerpo.transform.localScale = Vector3.one * 0.92f;
            var head = Hueso(cuerpo, HumanBodyBones.Head);
            var punto = new GameObject("PuntoCabeza").transform; punto.SetParent(head, false);
            ConstruirEstudio.Prim(PrimitiveType.Cube, "Podio", raiz.transform, new Vector3(0, 0.55f, 0.95f), new Vector3(0.9f, 1.1f, 0.5f), podio);
            ConstruirEstudio.Prim(PrimitiveType.Cube, "TopePodio", raiz.transform, new Vector3(0, 1.12f, 0.95f), new Vector3(1.05f, 0.06f, 0.62f), topePodio);
            var e = raiz.AddComponent<Emprendedor>();
            e.animador = cuerpo.GetComponent<Animator>(); e.puntoCabeza = punto;
            var prefab = PrefabUtility.SaveAsPrefabAsset(raiz, "Assets/Prefabs/Emprendedor.prefab");
            Object.DestroyImmediate(raiz);
            return prefab;
        }

        [MenuItem("NeuralHub/Sondear personajes")]
        public static void Sondear()
        {
            ConfigurarImportaciones();
            var traje = AssetDatabase.LoadAssetAtPath<GameObject>(Traje);
            var g = Object.Instantiate(traje);
            var an = g.GetComponent<Animator>();
            Debug.Log("NH_SONDEO traje animator=" + (an != null) + " humano=" + (an && an.avatar ? an.avatar.isHuman.ToString() : "sin avatar"));
            foreach (var r in g.GetComponentsInChildren<Renderer>()) Debug.Log("NH_SONDEO render " + r.name + " " + r.bounds + " mats=" + r.sharedMaterials.Length);
            if (an && an.avatar && an.avatar.isHuman)
                foreach (var b in new[] { HumanBodyBones.Hips, HumanBodyBones.Head, HumanBodyBones.Neck })
                { var t = an.GetBoneTransform(b); Debug.Log("NH_SONDEO hueso " + b + " " + (t ? t.name + " " + t.position : "null")); }
            Object.DestroyImmediate(g);
            foreach (var d in Directory.GetDirectories("Assets/genSuit/Textures")) Debug.Log("NH_SONDEO tex " + d + ": " + string.Join(",", Directory.GetFiles(d)));
            var cab = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(CabezaTiburon));
            foreach (var r in cab.GetComponentsInChildren<Renderer>()) Debug.Log("NH_SONDEO tiburon " + r.name + " " + r.bounds + " rot=" + r.transform.rotation.eulerAngles);
            Object.DestroyImmediate(cab);
            foreach (var f in Directory.GetFiles(CarpetaMixamo, "*.fbx"))
            {
                var c = Clip(Path.GetFileNameWithoutExtension(f));
                Debug.Log("NH_SONDEO clip " + Path.GetFileName(f) + " -> " + (c ? c.name + " " + c.length.ToString("0.0") + "s humano=" + c.humanMotion : "null"));
            }
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
