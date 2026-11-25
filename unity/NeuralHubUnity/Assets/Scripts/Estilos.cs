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

        /// <summary>Textura 32x32 con esquinas redondeadas y borde suavizado; se usa como fondo 9-slice.</summary>
        public static Texture2D Red(Color c, int radio = 12, Color? borde = null)
        {
            const int n = 32;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Max(radio - x - 0.5f, x + 0.5f - (n - radio)));
                    float dy = Mathf.Max(0, Mathf.Max(radio - y - 0.5f, y + 0.5f - (n - radio)));
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(radio - d + 0.5f);
                    var col = c;
                    if (borde.HasValue && d > radio - 2f) col = borde.Value;
                    col.a *= a;
                    t.SetPixel(x, y, col);
                }
            t.Apply();
            return t;
        }

        static readonly RectOffset Borde = new RectOffset(13, 13, 13, 13);
        public static Font Fuente, FuenteFuerte;

        public static void Iniciar()
        {
            if (listo) return;
            listo = true;
            Fuente = Resources.Load<Font>("Fuentes/Poppins-Regular");
            FuenteFuerte = Resources.Load<Font>("Fuentes/Poppins-SemiBold") ?? Fuente;
            var fondoNavy = Red(new Color(Navy.r, Navy.g, Navy.b, 0.9f), 14, new Color(Cian.r, Cian.g, Cian.b, 0.55f));
            var fondoClaro = Red(new Color(0.02f, 0.10f, 0.28f, 0.62f), 12, new Color(1, 1, 1, 0.18f));
            Caja = new GUIStyle { normal = { background = fondoNavy }, border = Borde, padding = new RectOffset(20, 20, 16, 16) };
            CajaClara = new GUIStyle { normal = { background = fondoClaro }, border = Borde, padding = new RectOffset(12, 12, 8, 8) };
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
            Seleccion.onNormal.background = Red(Violeta, 12); Seleccion.onHover.background = Red(Violeta, 12, Cian);
            Seleccion.onNormal.textColor = Blanco; Seleccion.onHover.textColor = Blanco;

            Campo = new GUIStyle(GUI.skin.textField) { fontSize = 20, padding = new RectOffset(8, 8, 6, 6) };
            Campo.normal.background = Tex(new Color(1, 1, 1, 0.95f)); Campo.focused.background = Tex(Color.white);
            Campo.normal.textColor = Color.black; Campo.focused.textColor = Color.black;
            Area = new GUIStyle(Campo) { wordWrap = true };
            Campo.normal.background = Red(new Color(1, 1, 1, 0.96f), 8); Campo.focused.background = Red(Color.white, 8, Cian); Campo.border = new RectOffset(9, 9, 9, 9);
            Area.normal.background = Campo.normal.background; Area.focused.background = Campo.focused.background; Area.border = Campo.border;
            foreach (var st in new[] { Texto, TextoChico, Campo, Area }) if (Fuente) st.font = Fuente;
            foreach (var st in new[] { Titulo, Grande, Subtitulo, Etiqueta, Chip, Boton, BotonChico, BotonPrimario, Seleccion }) if (FuenteFuerte) st.font = FuenteFuerte;
        }

        static GUIStyle Crear(Color normal, Color hover, int tam)
        {
            var s = new GUIStyle
            {
                fontSize = tam, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter,
                normal = { background = Red(normal, 12), textColor = Blanco },
                hover = { background = Red(hover, 12, Cian), textColor = Blanco },
                active = { background = Red(Cian, 12), textColor = Blanco },
                border = Borde,
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
