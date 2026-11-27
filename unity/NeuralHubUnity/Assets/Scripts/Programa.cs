using System.Collections.Generic;
using UnityEngine;

namespace NeuralHub
{
    /// <summary>Retratos de los tiburones (Resources/Retratos, renderizados desde sus modelos).</summary>
    public static class Retratos
    {
        static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();
        public static string Especialidad(string id)
        {
            return id == "tiburona_consumo" || id == "tiburon_consumo" ? "Tiburona de consumo" : "Tiburón " + Perfiles.Corto(id).ToLower();
        }
        public static Texture2D De(string id)
        {
            Texture2D t;
            if (!cache.TryGetValue(id, out t)) { t = Resources.Load<Texture2D>("Retratos/" + id); cache[id] = t; }
            return t;
        }
    }

    /// <summary>
    /// Entrada del programa, como en televisión: cortinilla con el nombre del programa y después «en este episodio»
    /// con la empresa que se presenta y el panel, mientras la cámara recorre el estudio. La sesión empieza al terminar.
    /// </summary>
    public class IntroPrograma
    {
        public bool activa;
        public const float Duracion = 10.5f;
        const float Portada = 4.2f;
        float desde;
        string empresa, sector, frase;
        string[] panel = new string[0];

        public void Iniciar(AppNeuralHub app, string nombre, string sectorId, string problema, string[] ids)
        {
            activa = true; desde = Reloj.Ahora;
            empresa = nombre; sector = sectorId; frase = problema; panel = ids ?? new string[0];
            app.reproductor.pausado = true;
            if (app.sonidos) app.sonidos.Cortinilla();
            if (app.estudio && app.estudio.pantalla)
                app.estudio.pantalla.MostrarLamina("sector_" + sector, "En este episodio", empresa, problema);
            if (app.camara) app.camara.Vuelo(0f, true);
        }

        public void Terminar(AppNeuralHub app)
        {
            if (!activa) return;
            activa = false;
            app.reproductor.pausado = false;
            if (app.camara) app.camara.Cambiar(Plano.General, null);
        }

        public void Actualizar(AppNeuralHub app)
        {
            if (!activa) return;
            float t = Reloj.Ahora - desde;
            if (app.camara) app.camara.Vuelo(Mathf.Clamp01((t - Portada + 0.6f) / (Duracion - Portada)), false);
            if (t >= Duracion) Terminar(app);
        }

        static float Suave(float x) { x = Mathf.Clamp01(x); return 1f - Mathf.Pow(1f - x, 3f); }

        static void Texto(Rect r, string s, Font f, int tam, Color c, float alfa)
        {
            var e = new GUIStyle { font = f, fontSize = tam, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            c.a = alfa; e.normal.textColor = Estilos.Letra(c);
            GUI.Label(r, s, e);
        }

        public void Dibujar(AppNeuralHub app, float W, float H)
        {
            float t = Reloj.Ahora - desde;
            if (t < Portada)
            {
                // Portada del programa: franjas con los colores del equipo que barren la pantalla
                float salida = Suave((t - (Portada - 0.6f)) / 0.6f);
                var fondo = Estilos.Navy; fondo.a = 1f - salida;
                Estilos.Rect(new Rect(0, 0, W, H), fondo);
                Color[] franjas = { Perfiles.Hex("#4F1FD0"), Perfiles.Hex("#1060D0"), Perfiles.Hex("#13B8F0") };
                for (int i = 0; i < 3; i++)
                {
                    float k = Suave((t - 0.1f - i * 0.16f) / 0.7f);
                    var c = franjas[i]; c.a = 1f - salida;
                    Estilos.Rect(new Rect(-W + W * k + salida * W, H * 0.63f + i * 30, W, 18), c);
                    Estilos.Rect(new Rect(W - W * k - salida * W, H * 0.27f - i * 30, W, 18), c);
                }
                float a1 = Suave((t - 0.5f) / 0.6f) * (1f - salida), a2 = Suave((t - 1.5f) / 0.5f) * (1f - salida), a3 = Suave((t - 2.2f) / 0.6f) * (1f - salida);
                Texto(new Rect(0, H * 0.34f, W, 40), "NEURALHUB PRESENTA", Estilos.Semi, 30, Perfiles.Hex("#13B8F0"), a1);
                int tam = Mathf.RoundToInt(Mathf.Lerp(150f, 116f, Suave((t - 1.5f) / 0.5f)));
                Texto(new Rect(0, H * 0.40f, W, 160), Personas.Programa, Estilos.Display, tam, Color.white, a2);
                Texto(new Rect(0, H * 0.55f, W, 44), Personas.Lema, Estilos.Regular, 30, Perfiles.Hex("#C3D4F7"), a3);
            }
            else
            {
                // «En este episodio»: la empresa y su panel, sobre el estudio
                float u = t - Portada;
                float entra = Suave(u / 0.6f), sale = Suave((t - (Duracion - 0.5f)) / 0.5f), a = entra * (1f - sale);
                var antes = GUI.color; GUI.color = new Color(1, 1, 1, a);
                float x = Mathf.Lerp(-900, 70, entra);
                var caja = new Rect(x, H - 400, 1060, 300);
                GUI.Box(caja, GUIContent.none, Estilos.CajaCalida);
                var lamina = Resources.Load<Texture2D>("Laminas/sector_" + sector);
                if (lamina) GUI.DrawTexture(new Rect(caja.x + 30, caja.y + 34, 230, 230), lamina, ScaleMode.ScaleToFit);
                GUI.Label(new Rect(caja.x + 290, caja.y + 34, 740, 30), "EN ESTE EPISODIO PRESENTAMOS", Estilos.Quien);
                GUI.Label(new Rect(caja.x + 290, caja.y + 66, 740, 80), empresa, Estilos.Color_(new GUIStyle(Estilos.Grande) { fontSize = 62, alignment = TextAnchor.MiddleLeft }, Color.white));
                GUI.Label(new Rect(caja.x + 290, caja.y + 160, 740, 120), Pantalla.Ajustar(frase, 62, 3), Estilos.Texto);
                if (u > 2.3f)
                {
                    float k = Suave((u - 2.3f) / 0.6f);
                    float px = Mathf.Lerp(W + 40, W - 640, k);
                    GUI.Box(new Rect(px, 90, 570, 90 + panel.Length * 118), GUIContent.none, Estilos.Caja);
                    GUI.Label(new Rect(px + 30, 112, 520, 30), "HOY NADAN CONTIGO", Estilos.Quien);
                    for (int i = 0; i < panel.Length; i++)
                    {
                        var persona = Personas.De(panel[i]);
                        float y = 160 + i * 118;
                        var foto = Retratos.De(panel[i]);
                        if (foto) GUI.DrawTexture(new Rect(px + 30, y, 100, 100), foto);
                        GUI.Label(new Rect(px + 150, y + 14, 400, 40), persona != null ? persona.nombre : panel[i], Estilos.Subtitulo);
                        GUI.Label(new Rect(px + 150, y + 56, 400, 30), Retratos.Especialidad(panel[i]), Estilos.TextoChico);
                    }
                }
                GUI.color = antes;
            }
            if (GUI.Button(new Rect(W - 250, H - 80, 210, 50), "Saltar intro ▶▶", Estilos.BotonChico)) { app.Clic(); Terminar(app); }
        }
    }

    /// <summary>«Conoce a los tiburones»: cada uno aparece en el estudio, se presenta y dice cuándo conviene elegirlo.</summary>
    public class ConocerTiburones
    {
        public int indice;
        public Vista volverA = Vista.Inicio;
        Tiburon enEscena;
        string mostrado;

        public void Abrir(AppNeuralHub app, Vista desde)
        {
            volverA = desde; mostrado = null;
            app.Ir(Vista.Conocer);
            Mostrar(app, indice);
        }

        public void Mostrar(AppNeuralHub app, int i)
        {
            indice = i;
            var p = Personas.Todas[i];
            if (mostrado == p.id) return;
            mostrado = p.id;
            Quitar(app);
            var e = app.estudio;
            e.RetirarPanel();
            var base_ = e.sillones[1];
            enEscena = Object.Instantiate(e.prefabTiburon, base_.position, base_.rotation, e.transform);
            enEscena.Asignar(p.id, p.nombre);
            enEscena.Preguntar();
            app.camara.Cambiar(Plano.Conocer, enEscena.PuntoCabeza());
            if (e.pantalla) e.pantalla.MostrarLamina(p.lamina, "Conoce a los tiburones", p.nombre, p.apodo);
            if (app.sonidos) app.sonidos.DetenerVoz();
        }

        void Quitar(AppNeuralHub app) { if (enEscena) Object.Destroy(enEscena.gameObject); enEscena = null; }

        public void Cerrar(AppNeuralHub app)
        {
            Quitar(app); mostrado = null;
            if (app.sonidos) app.sonidos.DetenerVoz();
            app.Ir(volverA);
        }

        public void Dibujar(AppNeuralHub app, float W, float H)
        {
            GUI.Box(new Rect(30, 30, 430, H - 60), GUIContent.none, Estilos.Caja);
            GUI.Label(new Rect(58, 52, 390, 44), "Conoce a los tiburones", new GUIStyle(Estilos.Subtitulo) { fontSize = 30 });
            for (int i = 0; i < Personas.Todas.Length; i++)
            {
                var p = Personas.Todas[i];
                var r = new Rect(52, 112 + i * 134, 386, 122);
                GUI.Box(r, GUIContent.none, i == indice ? Estilos.CajaCalida : Estilos.CajaClara);
                var foto = Retratos.De(p.id);
                if (foto) GUI.DrawTexture(new Rect(r.x + 12, r.y + 12, 98, 98), foto);
                GUI.Label(new Rect(r.x + 126, r.y + 30, 250, 34), p.nombre, new GUIStyle(Estilos.Texto) { font = Estilos.Semi, wordWrap = false });
                GUI.Label(new Rect(r.x + 126, r.y + 66, 250, 30), Retratos.Especialidad(p.id), Estilos.TextoChico);
                if (GUI.Button(r, GUIContent.none, GUIStyle.none)) { app.Clic(); Mostrar(app, i); }
            }

            var sel = Personas.Todas[indice];
            var info = app.Catalogo(sel.id);
            var caja = new Rect(W - 640, 30, 610, H - 60);
            GUILayout.BeginArea(caja, Estilos.Caja);
            GUILayout.Label(sel.apodo.ToUpper(), Estilos.Quien);
            GUILayout.Label(sel.nombre, new GUIStyle(Estilos.Encabezado) { fontSize = 40 });
            GUILayout.Label(Retratos.Especialidad(sel.id), Estilos.TextoChico);
            GUILayout.Space(18);
            GUILayout.Label("«" + sel.presentacion + "»", new GUIStyle(Estilos.Texto) { fontSize = 26 });
            GUILayout.Space(14);
            if (GUILayout.Button("▶  Escuchar su presentación", Estilos.BotonPrimario, GUILayout.Height(54))) { app.Clic(); app.EscucharPresentacion(sel.id); if (enEscena) enEscena.Preguntar(); }
            GUILayout.Space(22);
            GUILayout.Label("CUÁNDO ELEGIRLO", Estilos.Quien);
            GUILayout.Label(sel.usalo, Estilos.Texto);
            GUILayout.Space(16);
            GUILayout.Label("EN QUÉ SE FIJA", Estilos.Quien);
            GUILayout.Label(info != null ? info.criterio : "", Estilos.Texto);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Volver", Estilos.Boton, GUILayout.Height(54))) Cerrar(app);
            GUILayout.EndArea();
        }
    }
}
