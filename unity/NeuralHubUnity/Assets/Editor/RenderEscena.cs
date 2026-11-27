using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NeuralHub.Editor
{
    /// <summary>Renderiza tomas del estudio 3D a PNG (sin Play mode) con 3 tiburones colocados. Las guarda en unity/capturas.</summary>
    public static class RenderEscena
    {
        [MenuItem("NeuralHub/Renderizar tomas del estudio")]
        public static void Menu() { Renderizar(false); }

        public static void Ejecutar() { Renderizar(true); }

        static void Renderizar(bool salir)
        {
            string salida = Path.GetFullPath(Path.Combine(Application.dataPath, "../../capturas"));
            Directory.CreateDirectory(salida);
            EditorSceneManager.OpenScene("Assets/Scenes/Estudio.unity");
            var est = Object.FindFirstObjectByType<Estudio>();
            var ids = new[] { "tiburon_financiero", "tiburon_mentor", "tiburon_estratega" };
            var panel = new System.Collections.Generic.List<Tiburon>();
            for (int i = 0; i < 3; i++)
            {
                var t = (Tiburon)PrefabUtility.InstantiatePrefab(est.prefabTiburon, est.transform);
                t.transform.position = est.sillones[i].position; t.transform.rotation = est.sillones[i].rotation;
                t.pose = i;
                t.Asignar(ids[i], ids[i]);
                panel.Add(t);
            }
            est.pantalla.MostrarOferta("DataSense AI", 500000, 15, 3333333, "tecnologia");
            var dat = Object.FindFirstObjectByType<PantallasDatos>();
            if (dat)
            {
                dat.MostrarOferta(new PitchDto { nombre = "DataSense AI", monto_mxn = 500000, porcentaje = 15, ventas_12m_mxn = 270000, margen_bruto_pct = 72, sector = "tecnologia" }, 3333333);
                dat.FijarPanel(new[] { new PanelItem { tiburon_id = ids[0] }, new PanelItem { tiburon_id = ids[1] }, new PanelItem { tiburon_id = ids[2] } });
                dat.Voto(ids[0], "salir"); dat.Voto(ids[1], "contraoferta"); dat.Voto(ids[2], "invertir");
                dat.Inmediato();
            }
            est.letreroResultado.text = "";
            var cam = est.camara.GetComponent<Camera>();
            Vector3 pos; Quaternion rot; float fov;

            est.camara.Calcular(Plano.General, null, out pos, out rot, out fov);
            Toma(cam, salida, "estudio_general", pos, rot, fov);
            est.camara.Calcular(Plano.Arriba, null, out pos, out rot, out fov);
            Toma(cam, salida, "vista_arriba", pos, rot, fov);

            // Spotlight encendido a mano (sin Play mode el Reflector no corre su Update)
            var luz = est.reflector.luz;
            luz.enabled = true; luz.intensity = est.reflector.intensidad;
            for (int i = 0; i < 3; i++)
            {
                var tp = panel[i].transform.position;
                luz.transform.position = new Vector3(tp.x, est.reflector.alturaLuz, tp.z - 0.2f);
                luz.transform.rotation = Quaternion.LookRotation(new Vector3(tp.x, 0.6f, tp.z) - luz.transform.position);
                est.camara.Calcular(Plano.Tiburon, panel[i].PuntoCabeza(), out pos, out rot, out fov);
                Toma(cam, salida, "tiburon_" + (i + 1), pos, rot, fov);
            }
            luz.enabled = false;
            foreach (var t in panel) Object.DestroyImmediate(t.gameObject);
            File.WriteAllText(salida + "/listo_render.txt", "ok");
            Debug.Log("NeuralHub: tomas guardadas en " + salida);
            if (salir) EditorApplication.Exit(0);
        }

        static void Toma(Camera cam, string dir, string nombre, Vector3 pos, Quaternion rot, float fov)
        {
            cam.transform.position = pos; cam.transform.rotation = rot; cam.fieldOfView = fov;
            var rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); tex.Apply();
            File.WriteAllBytes(dir + "/" + nombre + ".png", tex.EncodeToPNG());
            cam.targetTexture = null; RenderTexture.active = null; Object.DestroyImmediate(rt); Object.DestroyImmediate(tex);
        }
    }
}
