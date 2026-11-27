using UnityEngine;

namespace NeuralHub
{
    /// <summary>
    /// Barra de progreso de la sesión (Pitch → Ronda 1 → Ronda 2 → Ronda 3 → Veredicto → Negociación) y letrero
    /// grande al empezar cada etapa, para que siempre quede claro en qué ronda va el usuario y qué pasa en ella.
    /// </summary>
    public class BarraRondas
    {
        static readonly string[] Pasos = { "Pitch", "Ronda 1", "Ronda 2", "Ronda 3", "Veredicto", "Negociación" };
        static readonly string[] Explica =
        {
            "Presentas tu negocio. El panel escucha.",
            "Cada tiburón hace su primera pregunta según su criterio.",
            "Profundizan en lo que tus respuestas dejaron abierto.",
            "Última ronda de preguntas: es tu momento de convencerlos.",
            "Cada tiburón vota en secreto y explica por qué.",
            "Alguien quiere invertir: ajusta tu oferta y vuelven a votar.",
        };
        static readonly string[] Grande = { "Tu pitch", "Ronda 1 de 3", "Ronda 2 de 3", "Ronda 3 de 3", "Hora de votar", "A negociar" };
        const float Dura = 3.2f;

        int etapaAnterior = -1;
        float desde;
        GUIStyle pastilla, pastillaActual, pastillaHecha, banda;

        public static int Etapa(ReproductorSesion r)
        {
            var e = r.actual;
            if (e == null) return 0;
            switch (e.tipo)
            {
                case "PITCH": case "PANEL": return 0;
                case "VOTO": return e.votacion == 2 ? 5 : 4;
                case "NEGOCIACION": case "OFERTA": return 5;
                case "DECISION": case "REPORTE": return r.ronda >= 4 ? 5 : 4;
                default: return Mathf.Clamp(r.ronda, 1, 3);
            }
        }

        void Preparar()
        {
            if (pastilla != null) return;
            pastilla = Estilo(new Color(0f, 0.05f, 0.2f, 0.94f), Estilos.Linea, Estilos.Tenue);
            pastillaHecha = Estilo(new Color(0.05f, 0.2f, 0.2f, 0.88f), new Color(0.24f, 0.86f, 0.59f, 0.75f), Estilos.Suave);
            pastillaActual = Estilo(Estilos.Calido, Estilos.Calido, Estilos.SobreCalido);
            banda = new GUIStyle { normal = { background = Estilos.Redondeado(new Color(0f, 0.07f, 0.28f, 0.97f), Estilos.Calido, 28, 3f) }, border = new RectOffset(30, 30, 30, 30) };
        }

        static GUIStyle Estilo(Color fondo, Color borde, Color texto)
        {
            return new GUIStyle
            {
                font = Estilos.Semi, fontSize = 17, alignment = TextAnchor.MiddleCenter, border = new RectOffset(20, 20, 20, 20),
                normal = { background = Estilos.Redondeado(fondo, borde, 18, 1.5f), textColor = Estilos.Letra(texto) },
            };
        }

        public void Dibujar(ReproductorSesion r, float W, bool conLetrero)
        {
            Preparar();
            int etapa = Etapa(r);
            const float ancho = 140f, sep = 8f;
            int n = etapa == 5 ? 6 : 5;     // la negociación solo aparece si ocurre
            float x = W / 2 - (n * ancho + (n - 1) * sep) / 2;
            for (int i = 0; i < n; i++)
            {
                var rect = new Rect(x + i * (ancho + sep), 20, ancho, 40);
                if (i < etapa) GUI.Box(rect, "✓ " + Pasos[i], pastillaHecha);
                else GUI.Box(rect, Pasos[i], i == etapa ? pastillaActual : pastilla);
            }

            if (r.actual == null || !conLetrero) return;   // con el historial abierto, el letrero lo taparía
            if (etapa != etapaAnterior) { etapaAnterior = etapa; desde = Reloj.Ahora; }
            float t = Reloj.Ahora - desde;
            if (t > Dura) return;
            float a = Mathf.Clamp01(t / 0.25f) * Mathf.Clamp01((Dura - t) / 0.4f);
            float crece = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / 0.35f), 3f);
            var antes = GUI.color; GUI.color = new Color(1, 1, 1, a);
            float bw = Mathf.Lerp(560, 860, crece);
            var caja = new Rect(W / 2 - bw / 2, 190, bw, 176);
            GUI.Box(caja, GUIContent.none, banda);
            GUI.Label(new Rect(caja.x, caja.y + 20, caja.width, 80), Grande[etapa], Estilos.Color_(new GUIStyle(Estilos.Grande) { fontSize = 62 }, Estilos.Calido));
            GUI.Label(new Rect(caja.x + 30, caja.y + 112, caja.width - 60, 44), Explica[etapa], new GUIStyle(Estilos.Texto) { alignment = TextAnchor.MiddleCenter });
            GUI.color = antes;
        }

        public void Reiniciar() { etapaAnterior = -1; }
    }
}
