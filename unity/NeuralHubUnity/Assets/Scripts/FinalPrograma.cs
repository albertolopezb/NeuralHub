using System.Collections.Generic;
using UnityEngine;

namespace NeuralHub
{
    /// <summary>
    /// Los tres finales del programa: celebración si hay trato (confeti y fanfarria), hundimiento si no lo hay
    /// (la pantalla se oscurece y todo se va al fondo) y el cierre del episodio antes de la retroalimentación.
    /// </summary>
    public class FinalPrograma
    {
        public enum Fase { Nada, Trato, SinTrato, Cierre }
        public Fase fase = Fase.Nada;
        public bool cierreVisto;
        public const float DuraResultado = 7f, DuraCierre = 6.2f;

        float desde;
        string empresa, condiciones;
        string[] socios = new string[0], panel = new string[0];

        class Pieza { public float x, y, vx, vy, giro, vgiro, ancho, alto, fase; public Color color; }
        readonly List<Pieza> piezas = new List<Pieza>();
        Texture2D burbuja;

        public bool Activo { get { return fase != Fase.Nada; } }
        public bool EnCierre { get { return fase == Fase.Cierre; } }
        float T { get { return Time.unscaledTime - desde; } }

        public void Reiniciar() { fase = Fase.Nada; cierreVisto = false; piezas.Clear(); }

        public void Resultado(AppNeuralHub app, bool trato, string nombreEmpresa, string textoCondiciones)
        {
            fase = trato ? Fase.Trato : Fase.SinTrato; desde = Time.unscaledTime;
            empresa = nombreEmpresa; condiciones = textoCondiciones;
            var dentro = new List<string>();
            foreach (var kv in app.reproductor.votos) if (kv.Value == "invertir" || kv.Value == "contraoferta") dentro.Add(kv.Key);
            socios = trato ? dentro.ToArray() : new string[0];
            panel = app.reproductor.panelIds;
            piezas.Clear();
            Color[] colores = { Perfiles.Hex("#13B8F0"), Perfiles.Hex("#4F1FD0"), Perfiles.Hex("#3DDC97"), Perfiles.Hex("#FFC857"), Color.white, Perfiles.Hex("#FF7A6B") };
            int n = trato ? 240 : 70;
            for (int i = 0; i < n; i++)
            {
                var p = new Pieza { fase = Random.value * 6.28f, giro = Random.value * 360f };
                if (trato)
                {
                    // dos cañones de confeti desde las esquinas de abajo
                    bool izq = i % 2 == 0;
                    p.x = izq ? Random.Range(-0.02f, 0.06f) : Random.Range(0.94f, 1.02f); p.y = Random.Range(0.92f, 1.05f);
                    p.vx = (izq ? 1 : -1) * Random.Range(0.08f, 0.5f); p.vy = -Random.Range(0.55f, 1.45f); p.vgiro = Random.Range(-420f, 420f);
                    p.ancho = Random.Range(10f, 20f); p.alto = Random.Range(6f, 12f); p.color = colores[i % colores.Length];
                }
                else
                {
                    // burbujas que suben mientras todo se hunde
                    p.x = Random.value; p.y = Random.Range(0.4f, 1.8f); p.vy = -Random.Range(0.07f, 0.2f);
                    p.ancho = p.alto = Random.Range(12f, 52f); p.color = new Color(0.8f, 0.9f, 1f, Random.Range(0.25f, 0.65f));
                }
                piezas.Add(p);
            }
            if (app.sonidos) app.sonidos.Musical(trato ? "fanfarria" : "hundimiento", DuraResultado);
        }

        public void Cerrar(AppNeuralHub app, string nombreEmpresa)
        {
            fase = Fase.Cierre; desde = Time.unscaledTime; empresa = nombreEmpresa; cierreVisto = true; piezas.Clear();
            panel = app.reproductor.panelIds;
            if (app.sonidos) app.sonidos.Musical("cierre", DuraCierre);
        }

        public void Saltar() { fase = Fase.Nada; piezas.Clear(); }

        public void Actualizar()
        {
            if (fase == Fase.Nada) return;
            float dt = Mathf.Min(0.05f, Time.unscaledDeltaTime);
            foreach (var p in piezas)
            {
                if (fase == Fase.Trato) { p.vy += 0.75f * dt; p.vx *= 1f - 0.9f * dt; p.vy *= 1f - 0.55f * dt; p.giro += p.vgiro * dt; }
                p.x += (p.vx + 0.02f * Mathf.Sin(Time.unscaledTime * 2.4f + p.fase)) * dt;
                p.y += p.vy * dt;
            }
            if (T > (fase == Fase.Cierre ? DuraCierre : DuraResultado)) { fase = Fase.Nada; piezas.Clear(); }
        }

        static float Suave(float x) { x = Mathf.Clamp01(x); return 1f - Mathf.Pow(1f - x, 3f); }
        static float Rebote(float x) { x = Mathf.Clamp01(x); float k = x - 1f; return 1f + 2.7f * k * k * k + 1.7f * k * k; }

        static void Texto(Rect r, string s, Font f, int tam, Color c, float alfa = 1f)
        {
            var e = new GUIStyle { font = f, fontSize = Mathf.Max(1, tam), alignment = TextAnchor.MiddleCenter, wordWrap = true };
            c.a = alfa; e.normal.textColor = Estilos.Letra(c);
            GUI.Label(r, s, e);
        }

        public void Dibujar(AppNeuralHub app, float W, float H)
        {
            if (fase == Fase.Trato) DibujarTrato(W, H);
            else if (fase == Fase.SinTrato) DibujarSinTrato(W, H);
            else if (fase == Fase.Cierre) DibujarCierre(W, H);
            if (fase != Fase.Nada && GUI.Button(new Rect(W - 210, H - 74, 170, 48), "Continuar ▶▶", Estilos.BotonChico)) { app.Clic(); Saltar(); }
        }

        void Confeti(float W, float H)
        {
            var antes = GUI.matrix; var color = GUI.color;
            foreach (var p in piezas)
            {
                if (p.y > 1.1f) continue;
                var centro = new Vector2(p.x * W, p.y * H);
                GUI.matrix = antes;
                GUIUtility.RotateAroundPivot(p.giro, centro * (Screen.height / 1080f));
                GUI.color = p.color;
                GUI.DrawTexture(new Rect(centro.x - p.ancho / 2, centro.y - p.alto / 2, p.ancho, p.alto * Mathf.Abs(Mathf.Cos(T * 6f + p.fase)) + 2f), Texture2D.whiteTexture);
            }
            GUI.matrix = antes; GUI.color = color;
        }

        void DibujarTrato(float W, float H)
        {
            float t = T, sale = Suave((t - (DuraResultado - 0.5f)) / 0.5f);
            float destello = Mathf.Clamp01(1f - t / 0.45f);
            Estilos.Rect(new Rect(0, 0, W, H), new Color(1f, 1f, 1f, destello * 0.55f));
            Confeti(W, H);
            float k = Rebote((t - 0.15f) / 0.6f);
            float ancho = 1100f * Mathf.Max(0.01f, k);
            var banda = new Rect(W / 2 - ancho / 2, 150, ancho, 190);
            var verde = Perfiles.Hex("#0E7A55"); verde.a = 0.95f * (1f - sale);
            Estilos.Rect(banda, verde);
            Estilos.Rect(new Rect(banda.x, banda.y - 14, banda.width, 10), new Color(0.07f, 0.72f, 0.94f, 1f - sale));
            Estilos.Rect(new Rect(banda.x, banda.yMax + 4, banda.width, 10), new Color(0.31f, 0.12f, 0.82f, 1f - sale));
            if (k > 0.5f)
            {
                Texto(new Rect(0, 158, W, 110), "¡TRATO HECHO!", Estilos.Display, Mathf.RoundToInt(100 * Mathf.Min(1.06f, k)), Color.white, 1f - sale);
                Texto(new Rect(0, 268, W, 50), condiciones, Estilos.Semi, 32, Perfiles.Hex("#DFFBEF"), 1f - sale);
            }
            if (t > 1.6f && socios.Length > 0)
            {
                float a = Suave((t - 1.6f) / 0.5f) * (1f - sale);
                var antes = GUI.color; GUI.color = new Color(1, 1, 1, a);
                float total = socios.Length * 220f, x0 = W / 2 - total / 2;
                GUI.Box(new Rect(x0 - 30, 372, total + 60, 250), GUIContent.none, Estilos.Caja);
                Texto(new Rect(x0, 386, total, 34), socios.Length == 1 ? "TU NUEVO SOCIO" : "TUS POSIBLES SOCIOS", Estilos.Semi, 22, Estilos.Calido, a);
                for (int i = 0; i < socios.Length; i++)
                {
                    var foto = Retratos.De(socios[i]); var persona = Personas.De(socios[i]);
                    float sube = 30f * (1f - Rebote((t - 1.7f - i * 0.2f) / 0.5f));
                    if (foto) GUI.DrawTexture(new Rect(x0 + i * 220 + 40, 428 + sube, 140, 140), foto);
                    Texto(new Rect(x0 + i * 220, 574 + sube, 220, 34), persona != null ? persona.nombre : socios[i], Estilos.Semi, 20, Color.white, a);
                }
                GUI.color = antes;
            }
        }

        void DibujarSinTrato(float W, float H)
        {
            float t = T, sale = Suave((t - (DuraResultado - 0.5f)) / 0.5f);
            // la pantalla se oscurece de arriba hacia abajo, como si el estudio se hundiera
            float baja = Suave(t / 1.6f);
            for (int i = 0; i < 24; i++)
            {
                float y = i / 24f;
                float y0 = Mathf.Floor(i * H / 24f), y1 = Mathf.Floor((i + 1) * H / 24f); Estilos.Rect(new Rect(0, y0, W, y1 - y0), new Color(0.0f, 0.02f, 0.09f, Mathf.Lerp(0.82f, 0.42f, y) * baja * (1f - sale)));
            }
            if (!burbuja) burbuja = Resources.Load<Texture2D>("Escenario/burbuja");
            var antes = GUI.color;
            foreach (var p in piezas)
            {
                var c = p.color; c.a *= baja * (1f - sale); GUI.color = c;
                if (burbuja) GUI.DrawTexture(new Rect(p.x * W, p.y * H, p.ancho, p.alto), burbuja);
            }
            GUI.color = antes;
            // el letrero cae despacio y queda meciéndose
            float caida = Suave((t - 0.5f) / 1.5f);
            float yT = Mathf.Lerp(-140f, 250f, caida) + 9f * Mathf.Sin(t * 1.7f) * caida;
            var centro = new Vector2(W / 2, yT + 60) * (Screen.height / 1080f);
            var m = GUI.matrix;
            GUIUtility.RotateAroundPivot(3.5f * Mathf.Sin(t * 1.3f), centro);
            Texto(new Rect(0, yT, W, 120), "SIN TRATO", Estilos.Display, 112, Perfiles.Hex("#FF7A6B"), 1f - sale);
            GUI.matrix = m;
            float a2 = Suave((t - 2.4f) / 0.8f) * (1f - sale);
            Texto(new Rect(W / 2 - 600, 440, 1200, 50), "Esta vez " + empresa + " se fue al fondo…", Estilos.Semi, 34, Color.white, a2);
            Texto(new Rect(W / 2 - 600, 500, 1200, 90), "…pero ya sabes qué le faltó. Ajusta tu pitch y vuelve a nadar.", Estilos.Regular, 28, Perfiles.Hex("#C3D4F7"), Suave((t - 3.6f) / 0.8f) * (1f - sale));
        }

        void DibujarCierre(float W, float H)
        {
            float t = T;
            // cortinas con los colores del equipo que se cierran
            Color[] franjas = { Perfiles.Hex("#13B8F0"), Perfiles.Hex("#1060D0"), Perfiles.Hex("#4F1FD0"), Perfiles.Hex("#00165C") };
            for (int i = 0; i < franjas.Length; i++)
            {
                float k = Suave((t - i * 0.14f) / 0.75f);
                float ancho = W / 2f * k + 2f;
                Estilos.Rect(new Rect(0, 0, ancho, H), franjas[i]);
                Estilos.Rect(new Rect(W - ancho, 0, ancho, H), franjas[i]);
            }
            float a1 = Suave((t - 1.2f) / 0.6f), a2 = Suave((t - 2.0f) / 0.6f), a3 = Suave((t - 2.8f) / 0.6f);
            Texto(new Rect(0, 190, W, 44), "GRACIAS POR NADAR EN", Estilos.Semi, 32, Perfiles.Hex("#13B8F0"), a1);
            Texto(new Rect(0, 242, W, 150), Personas.Programa, Estilos.Display, Mathf.RoundToInt(Mathf.Lerp(136, 112, a1)), Color.white, a1);
            Texto(new Rect(0, 400, W, 44), "El episodio de hoy: " + empresa, Estilos.Regular, 30, Perfiles.Hex("#C3D4F7"), a2);
            var antes = GUI.color; GUI.color = new Color(1, 1, 1, a2);
            float x0 = W / 2 - panel.Length * 220f / 2f;
            for (int i = 0; i < panel.Length; i++)
            {
                var foto = Retratos.De(panel[i]); var persona = Personas.De(panel[i]);
                float sube = 26f * (1f - Rebote((t - 2.1f - i * 0.18f) / 0.5f));
                if (foto) GUI.DrawTexture(new Rect(x0 + i * 220 + 40, 480 + sube, 140, 140), foto);
                Texto(new Rect(x0 + i * 220, 628 + sube, 220, 32), persona != null ? persona.nombre : panel[i], Estilos.Semi, 20, Color.white, a2);
            }
            GUI.color = antes;
            Texto(new Rect(0, 736, W, 44), "Ahora, tu retroalimentación", Estilos.Semi, 28, Color.white, a3);
            Texto(new Rect(0, 786, W, 34), "Un programa de NeuralHub · Simulación educativa; no es asesoría financiera.", Estilos.Regular, 20, Perfiles.Hex("#C3D4F7"), a3);
        }
    }
}
