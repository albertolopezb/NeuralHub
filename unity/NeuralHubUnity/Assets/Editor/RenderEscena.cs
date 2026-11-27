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
            string salida = Path.GetFullPath("../capturas_inc2");
            Directory.CreateDirectory(salida);
            EditorSceneManager.OpenScene("Assets/Scenes/Estudio.unity");
            var est = Object.FindFirstObjectByType<Estudio>();
            var ids = new[] { "tiburon_financiero", "tiburon_estratega", "tiburon_tecnologico" };
            string[] poses = { "No", "Yes", "Wave" };
            var panel = new System.Collections.Generic.List<Tiburon>();
            for (int i = 0; i < 3; i++)
            {
                var t = (Tiburon)PrefabUtility.InstantiatePrefab(est.prefabTiburon, est.transform);
                t.transform.position = est.sillones[i].position;
                t.transform.rotation = est.sillones[i].rotation;
                t.Asignar(ids[i], ids[i]);
                panel.Add(t);
                // Sin Play mode el Animator no corre: se toma un cuadro de cada animación
                int v = Tiburon.Variante(ids[i]);
                Posar(t.modelos[v].raiz, v == 0 ? "Characters_Sharky.fbx" : "Characters_Mako.fbx", i == 1 ? "Wave" : "Idle", 0.55f);
            }
            Posar(est.emprendedor.animador.gameObject, ConstruirPersonajes.ModeloEmprendedor, "Interact", 0.4f);

            var pitch = new PitchDto
            {
                nombre = "DataSense AI", sector = "tecnologia", monto_mxn = 500000, porcentaje = 15, ventas_12m_mxn = 270000, margen_bruto_pct = 72,
                solucion = "Plataforma que conecta las hojas de cálculo de una PyME y genera análisis y alertas con IA.",
                mercado_objetivo = "PyMEs mexicanas de comercio y servicios con 10 a 100 empleados.",
                validacion = "50 clientes activos y 3 pilotos con cámaras de comercio.",
                problema = "Las PyMEs tienen datos pero no saben usarlos.",
            };
            TemasPitch.Mostrar(est.pantalla, pitch, 3333333, "mercado", "Pregunta de Estratega");
            var dat = Object.FindFirstObjectByType<PantallasDatos>();
            if (dat)
            {
                dat.MostrarOferta(pitch, 3333333);
                dat.FijarPanel(new[] { new PanelItem { tiburon_id = ids[0] }, new PanelItem { tiburon_id = ids[1] }, new PanelItem { tiburon_id = ids[2] } });
                dat.Voto(ids[0], "salir"); dat.Voto(ids[1], "contraoferta"); dat.Voto(ids[2], "invertir");
                dat.Inmediato();
            }
            est.letreroResultado.text = "";
            foreach (var tn in Object.FindObjectsByType<TextoNitido>(FindObjectsSortMode.None)) tn.Aplicar();
            var cam = est.camara.GetComponent<Camera>();
            var cc = est.camara;
            Toma(cam, salida, "estudio_general", cc.posGeneral, Quaternion.LookRotation(cc.miraGeneral - cc.posGeneral));
            Toma(cam, salida, "vista_arriba", new Vector3(0, 19, 1), Quaternion.Euler(90, 0, 0));
            for (int i = 0; i < 3; i++)
            {
                var p = panel[i].PuntoCabeza();
                var hacia = (new Vector3(0, 0, -2.5f) - new Vector3(p.x, 0, p.z)).normalized;
                var lado = Vector3.Cross(Vector3.up, hacia);
                var pos = p + hacia * 4.2f + lado * 1.1f + Vector3.up * 0.2f;
                Toma(cam, salida, "tiburon_" + (i + 1), pos, Quaternion.LookRotation(p + Vector3.down * 0.35f - pos));
            }
            var e = est.emprendedor.PuntoCabeza();
            var pe = e + new Vector3(1.8f, 0.3f, 3.6f);
            Toma(cam, salida, "emprendedor", pe, Quaternion.LookRotation(e + Vector3.down * 0.3f - pe));
            panel[1].Votar("invertir");
            foreach (var tn in Object.FindObjectsByType<TextoNitido>(FindObjectsSortMode.None)) tn.Aplicar();
            var pv = panel[1].PuntoCabeza();
            Toma(cam, salida, "voto", pv + new Vector3(1.1f, 0.4f, -4.6f), Quaternion.LookRotation(new Vector3(-1.1f, -0.2f, 4.6f)));
            File.WriteAllText(salida + "/listo_render.txt", "ok");
            EditorApplication.Exit(0);
        }

        static void Posar(GameObject g, string archivo, string clip, float t)
        {
            var c = ConstruirPersonajes.Clip(archivo, clip);
            if (c) c.SampleAnimation(g, Mathf.Min(t, c.length));
        }

        static void Toma(Camera cam, string dir, string nombre, Vector3 pos, Quaternion rot)
        {
            cam.transform.position = pos; cam.transform.rotation = rot;
            var rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            cam.targetTexture = rt; cam.Render(); cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); tex.Apply();
            File.WriteAllBytes(dir + "/" + nombre + ".png", tex.EncodeToPNG());
            cam.targetTexture = null; RenderTexture.active = null; Object.DestroyImmediate(rt); Object.DestroyImmediate(tex);
        }
    }
}
