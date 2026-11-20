using UnityEngine;

namespace NeuralHub
{
    /// <summary>Estilos de la interfaz con los colores del equipo: navy #00165C, violeta #4F1FD0, azul #1060D0, cian #13B8F0.</summary>
    public static class Estilos
    {
        public static readonly Color Navy = Perfiles.Hex("#00165C");
        public static readonly Color Violeta = Perfiles.Hex("#4F1FD0");
        public static readonly Color Azul = Perfiles.Hex("#1060D0");
        public static readonly Color Cian = Perfiles.Hex("#13B8F0");
        public static readonly Color Blanco = Color.white;
        public static readonly Color Verde = Perfiles.Hex("#2F9E44");
        public static readonly Color Rojo = Perfiles.Hex("#C92A2A");
        public static readonly Color Ambar = Perfiles.Hex("#F59F00");

        public static GUIStyle Titulo, Subtitulo, Texto, TextoChico, Boton, BotonChico, BotonPrimario, Etiqueta, Campo, Area, Caja, CajaClara, Chip, Grande, Seleccion;
        static bool listo;

        public static Texture2D Tex(Color c)
        {
            var t = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            t.SetPixel(0, 0, c); t.Apply(); return t;
        }

        public static void Iniciar()
        {
            if (listo) return;
            listo = true;
            var fondoNavy = Tex(new Color(Navy.r, Navy.g, Navy.b, 0.92f));
            var fondoClaro = Tex(new Color(1, 1, 1, 0.07f));
            Caja = new GUIStyle { normal = { background = fondoNavy }, padding = new RectOffset(18, 18, 14, 14) };
            CajaClara = new GUIStyle { normal = { background = fondoClaro }, padding = new RectOffset(12, 12, 8, 8) };
            Titulo = new GUIStyle { fontSize = 46, fontStyle = FontStyle.Bold, normal = { textColor = Blanco }, alignment = TextAnchor.MiddleCenter };
            Grande = new GUIStyle { fontSize = 72, fontStyle = FontStyle.Bold, normal = { textColor = Blanco }, alignment = TextAnchor.MiddleCenter };
            Subtitulo = new GUIStyle { fontSize = 26, fontStyle = FontStyle.Bold, normal = { textColor = Cian }, wordWrap = true };
            Texto = new GUIStyle { fontSize = 22, normal = { textColor = Blanco }, wordWrap = true };
            TextoChico = new GUIStyle { fontSize = 18, normal = { textColor = new Color(0.85f, 0.9f, 1f) }, wordWrap = true };
            Etiqueta = new GUIStyle { fontSize = 18, fontStyle = FontStyle.Bold, normal = { textColor = Cian } };
            Chip = new GUIStyle { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = Blanco } };

            Boton = Crear(Azul, new Color(0.2f, 0.45f, 0.9f), 22);
            BotonChico = Crear(Azul, new Color(0.2f, 0.45f, 0.9f), 18);
            BotonPrimario = Crear(Violeta, new Color(0.38f, 0.22f, 0.85f), 24);
            Seleccion = Crear(new Color(1, 1, 1, 0.12f), new Color(1, 1, 1, 0.22f), 18);
            Seleccion.onNormal.background = Tex(Violeta); Seleccion.onHover.background = Tex(Violeta);
            Seleccion.onNormal.textColor = Blanco; Seleccion.onHover.textColor = Blanco;

            Campo = new GUIStyle(GUI.skin.textField) { fontSize = 20, padding = new RectOffset(8, 8, 6, 6) };
            Campo.normal.background = Tex(new Color(1, 1, 1, 0.95f)); Campo.focused.background = Tex(Color.white);
            Campo.normal.textColor = Color.black; Campo.focused.textColor = Color.black;
            Area = new GUIStyle(Campo) { wordWrap = true };
        }

        static GUIStyle Crear(Color normal, Color hover, int tam)
        {
            var s = new GUIStyle
            {
                fontSize = tam, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter,
                normal = { background = Tex(normal), textColor = Blanco },
                hover = { background = Tex(hover), textColor = Blanco },
                active = { background = Tex(Cian), textColor = Blanco },
                padding = new RectOffset(14, 14, 8, 8),
            };
            return s;
        }

        public static void Rect(Rect r, Color c)
        {
            var old = GUI.color; GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = old;
        }
    }
}
