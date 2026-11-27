using System.IO;
using UnityEditor;
using UnityEngine;

namespace NeuralHub.Editor
{
    /// <summary>
    /// Ambiente submarino con assets gratuitos: plantas, piedras y naufragio (Fishes And Underwater Environment Set),
    /// peces low poly (Fish - PolyPack), burbujas (Bubbles VFX de Vefects) y sonido ambiente (Underwater Ambient).
    /// Devuelve false si los paquetes no están importados para usar la decoración de primitivas.
    /// </summary>
    public static class ConstruirAmbiente
    {
        const string Env = "Assets/Fishes And Underwater Environment Set/2.prefabs/environment_objects/";
        const string Peces = "Assets/Alstra Infinite/Fish - PolyPack/Models/";
        const string Burbujas = "Assets/Vefects/Bubbles VFX/VFX/Particles/VFX_Bubbles_Area_Transparent_01.prefab";
        const string Audio = "Assets/Underwater Ambient.wav";

        public static bool Disponible() { return Directory.Exists(Env) && Directory.Exists(Peces); }

        public static void Decorar(Transform deco)
        {
            var rnd = new System.Random(11);
            System.Func<float, float, float> R = (a, b) => (float)(a + rnd.NextDouble() * (b - a));
            System.Func<string, Vector3, float, float, GameObject> Poner = (n, p, rot, h) => ConstruirPersonajes.Instanciar(Env + n + ".prefab", deco, p, rot, h);

            // Plantas a los lados del escenario (sin tapar a los tiburones ni la pantalla)
            for (int i = 0; i < 30; i++)
            {
                float x = (i % 2 == 0 ? -1 : 1) * R(7.5f, 12f), z = R(-6, 10);
                Poner("plant_" + (1 + i % 14), new Vector3(x, 0, z), R(0, 360), R(0.8f, 2.6f));
            }
            for (int i = 0; i < 8; i++)     // plantas bajas al frente
            {
                float x = (i % 2 == 0 ? -1 : 1) * R(3.2f, 6.5f);
                Poner("plant_" + (1 + (i * 3) % 14), new Vector3(x, 0, R(-6.5f, -3.5f)), R(0, 360), R(0.4f, 0.9f));
            }
            Poner("chest", new Vector3(9.5f, 0, 6.5f), -30f, 0.7f);
            Poner("crystal", new Vector3(10.5f, 0, 2f), 0f, 1.4f);
            Poner("crystal", new Vector3(-10.8f, 0, 1.5f), 40f, 1.1f);
            var fishes = "Assets/Fishes And Underwater Environment Set/2.prefabs/fishes/";
            ConstruirPersonajes.Instanciar(fishes + "starfish.prefab", deco, new Vector3(-3.6f, 0.02f, -4.6f), 20f, 0.12f);
            ConstruirPersonajes.Instanciar(fishes + "starfish.prefab", deco, new Vector3(4.2f, 0.02f, -5.1f), 70f, 0.1f);

            // Peces que nadan en círculos al fondo
            string[] modelos = { "FishV1", "FishV2", "FishV3", "FishV4" };
            for (int i = 0; i < 16; i++)
            {
                var pez = Pez(Peces + modelos[i % 4] + ".fbx", deco, R(0.35f, 0.8f));
                var n = pez.AddComponent<Nadador>();
                n.centro = new Vector3(R(-3, 3), R(2.2f, 7.5f), R(6.5f, 9.4f)); n.radio = R(3, 9); n.velocidad = R(0.2f, 0.45f) * (i % 2 == 0 ? 1 : -1); n.fase = R(0, 6.28f);
                pez.transform.position = n.centro;
            }
            var tib = Pez(Peces + "SharkV1.fbx", deco, 2.6f);
            var nt = tib.AddComponent<Nadador>(); nt.centro = new Vector3(0, 8.3f, 9f); nt.radio = 11f; nt.velocidad = 0.12f; nt.ondaVertical = 0.2f;
            tib.transform.position = nt.centro;

            // Burbujas
            var bur = AssetDatabase.LoadAssetAtPath<GameObject>(Burbujas);
            if (bur)
                foreach (var p in new[] { new Vector3(-9, 0, 4), new Vector3(9.5f, 0, 5), new Vector3(-6, 0, 9.5f), new Vector3(6.5f, 0, 9.5f) })
                {
                    var b = (GameObject)PrefabUtility.InstantiatePrefab(bur);
                    b.transform.SetParent(deco, false); b.transform.localPosition = p;
                }

            // Sonido ambiente en bucle
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Audio);
            if (clip)
            {
                var a = new GameObject("SonidoAmbiente").AddComponent<AudioSource>();
                a.transform.SetParent(deco, false);
                a.clip = clip; a.loop = true; a.playOnAwake = true; a.volume = 0.18f; a.spatialBlend = 0f;
            }
        }

        /// <summary>Pez low poly centrado y girado para que su hocico apunte a +Z (lo usa Nadador).</summary>
        static GameObject Pez(string ruta, Transform padre, float largo)
        {
            var cont = new GameObject("Pez"); cont.transform.SetParent(padre, false);
            var m = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ruta));
            m.transform.SetParent(cont.transform, false);
            var rs = m.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            float s = largo / Mathf.Max(b.size.x, b.size.z);
            m.transform.localScale = Vector3.one * s;
            m.transform.localPosition = -b.center * s;
            var giro = new GameObject("Giro").transform; giro.SetParent(cont.transform, false);
            m.transform.SetParent(giro, true);
            giro.localRotation = Quaternion.Euler(0, 90, 0);
            return cont;
        }
    }
}
