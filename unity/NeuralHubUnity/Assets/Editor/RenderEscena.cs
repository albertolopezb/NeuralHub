using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NeuralHub.Editor
{
    /// <summary>Renderiza tomas del estudio 3D a PNG (sin Play mode) con 3 tiburones colocados.</summary>
    public static class RenderEscena
    {
        public static void Ejecutar()
        {
            string salida = "D:/TEC/TC2008B/Reto/NeuralHub/unity/capturas";
            Directory.CreateDirectory(salida);
            EditorSceneManager.OpenScene("Assets/Scenes/Estudio.unity");
            var est = Object.FindFirstObjectByType<Estudio>();
            var ids = new[] { "tiburon_financiero", "tiburon_estratega", "tiburon_tecnologico" };
            var panel = new System.Collections.Generic.List<Tiburon>();
            for (int i = 0; i < 3; i++)
            {
                var t = (Tiburon)PrefabUtility.InstantiatePrefab(est.prefabTiburon, est.transform);
                t.transform.position = est.sillones[i].position - est.sillones[i].forward * 0.05f;
                var d = est.emprendedor.transform.position - t.transform.position; d.y = 0;
                t.transform.rotation = Quaternion.LookRotation(d);
                t.Asignar(ids[i], ids[i]);
                panel.Add(t);
            }
            est.pantalla.MostrarOferta("DataSense AI", 500000, 15, 3333333);
            est.letreroResultado.text = "";
            var reflector = est.reflector; reflector.luz.enabled = true;
            reflector.transform.position = panel[1].transform.position + new Vector3(0, 6.5f, -1.2f);
            reflector.transform.rotation = Quaternion.LookRotation(panel[1].transform.position - reflector.transform.position);
            var cam = est.camara.GetComponent<Camera>();
            var cc = est.camara;
            Toma(cam, salida, "estudio_general", cc.posGeneral, Quaternion.LookRotation(cc.miraGeneral - cc.posGeneral));
            Toma(cam, salida, "vista_arriba", new Vector3(0, 17, 1), Quaternion.Euler(90, 0, 0));
            for (int i = 0; i < 3; i++)
            {
                var p = panel[i].PuntoCabeza();
                var hacia = (new Vector3(0, 0, -2.5f) - new Vector3(p.x, 0, p.z)).normalized;
                var lado = Vector3.Cross(Vector3.up, hacia);
                var pos = p + hacia * 3.6f + lado * 1.1f + Vector3.up * 0.35f;
                Toma(cam, salida, "tiburon_" + (i + 1), pos, Quaternion.LookRotation(p - pos));
            }
            Toma(cam, salida, "emprendedor", est.emprendedor.PuntoCabeza() + new Vector3(2.2f, 0.4f, 4.2f),
                 Quaternion.LookRotation(est.emprendedor.PuntoCabeza() - (est.emprendedor.PuntoCabeza() + new Vector3(2.2f, 0.4f, 4.2f))));
            File.WriteAllText(salida + "/listo_render.txt", "ok");
            EditorApplication.Exit(0);
        }

        static void Toma(Camera cam, string dir, string nombre, Vector3 pos, Quaternion rot)
        {
            cam.transform.position = pos; cam.transform.rotation = rot;
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
