from pathlib import Path

p = Path(r"D:\TEC\TC2008B\Reto\NeuralHub\unity\NeuralHubUnity\Assets\Scripts\AppNeuralHub.cs")
s = p.read_text(encoding="utf8")

a = 'if (a == "-nh-capturar") { cliente.usarMock = true; StartCoroutine(Capturas()); }'
assert a in s
s = s.replace(a, a + '\n            foreach (var a in Environment.GetCommandLineArgs()) if (a == "-nh-video") { cliente.usarMock = true; StartCoroutine(Video()); }')

metodo = '''
        // ---------- grabación de video (fotogramas a velocidad fija; se unen con ffmpeg) ----------
        int fotograma;
        const int FPS = 12;

        IEnumerator Grabar(string carpeta, float segundos)
        {
            int total = Mathf.Max(1, (int)(segundos * FPS));
            for (int i = 0; i < total; i++)
            {
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(carpeta, "f" + (fotograma++).ToString("D5") + ".png"));
            }
        }

        IEnumerator GrabarHasta(string carpeta, Func<bool> cond, float maxSeg)
        {
            int max = (int)(maxSeg * FPS), i = 0;
            while (!cond() && i++ < max) yield return Grabar(carpeta, 1f / FPS);
        }

        IEnumerator Video()
        {
            string carpeta = ArgValor("-nh-salida", Path.Combine(Application.persistentDataPath, "video"));
            Directory.CreateDirectory(carpeta);
            Time.captureFramerate = FPS;
            yield return Grabar(carpeta, 2.5f);
            IrAFormulario();
            yield return GrabarHasta(carpeta, () => ejemplos != null, 10);
            formulario.Cargar(ejemplos[ejemplos.Length - 1].pitch);
            yield return Grabar(carpeta, 3.5f);
            IrAEleccionPanel(formulario.Json());
            yield return GrabarHasta(carpeta, () => vista == Vista.Panel, 10);
            yield return Grabar(carpeta, 3.5f);
            IniciarSesion(new List<string>(eleccion.elegidos));
            reproductor.factorExtra = 2.2f;
            yield return GrabarHasta(carpeta, () => vista == Vista.Resultado, 400);
            yield return Grabar(carpeta, 6f);
            mostrarHistorial = true;
            yield return Grabar(carpeta, 3f);
            File.WriteAllText(Path.Combine(carpeta, "listo.txt"), fotograma.ToString());
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
'''
i = s.index("        // ---------- capturas automáticas")
s = s[:i] + metodo + "\n" + s[i:]
p.write_text(s, encoding="utf8")
print("ok")
