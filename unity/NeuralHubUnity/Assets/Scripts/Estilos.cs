using UnityEngine;

namespace NeuralHub
{
    /// <summary>
    /// Estilos de la interfaz (IMGUI) con el look del estudio oscuro: paneles casi negros con borde fino,
    /// un solo acento cálido (#F5C26B, el color del spotlight) y verde/rojo solo para los veredictos.
    /// Fuentes: Inter (texto) y Space Grotesk (títulos), en Assets/Resources/Fuentes.
    /// </summary>
    public static class Estilos
    {
        // Paleta del concepto
        public static readonly Color Fondo = Perfiles.Hex("#05070B");
        public static readonly Color Panel = new Color(8 / 255f, 12 / 255f, 20 / 255f, 0.88f);
        public static readonly Color PanelClaro = new Color(15 / 255f, 23 / 255f, 38 / 255f, 0.84f);
        public static readonly Color Linea = Perfiles.Hex("#24344F");
        public static readonly Color LineaFuerte = Perfiles.Hex("#3A4A66");
        public static readonly Color Tinta = Perfiles.Hex("#E8EDF5");
        public static readonly Color Suave = Perfiles.Hex("#C9D6E8");
        public static readonly Color Tenue = Perfiles.Hex("#AEBDD3");
        public static readonly Color Calido = Perfiles.Hex("#F5C26B");
        public static readonly Color SobreCalido = Perfiles.Hex("#1A1206");
        public static readonly Color Dentro = Perfiles.Hex("#3DDC97");
        public static readonly Color Fuera = Perfiles.Hex("#FF7A6B");
        public static readonly Color Pendiente = Perfiles.Hex("#5B6B84");

        // Nombres anteriores (se conservan para no romper otros scripts)
        public static readonly Color Navy = Perfiles.Hex("#0A0E16");
        public static readonly Color Violeta = Calido;
        public static readonly Color Azul = Perfiles.Hex("#1A2436");
        public static readonly Color Cian = Calido;
        public static readonly Color Blanco = Color.white;
        public static readonly Color Verde = Dentro;
        public static readonly Color Rojo = Fuera;
        public static readonly Color Ambar = Calido;

        public static GUIStyle Titulo, Encabezado, Subtitulo, Texto, TextoChico, Boton, BotonChico, BotonPrimario, Etiqueta, Quien, Campo, Area,
                               Caja, CajaClara, CajaCalida, Chip, Grande, Seleccion, Insignia;
        public static Font Regular, Semi, Display;
        static bool listo;

        public static Texture2D Tex(Color c)
        {
            var t = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            t.SetPixel(0, 0, c); t.Apply(); return t;
        }

        /// <summary>Textura de rectángulo redondeado con borde (se estira con 9-slice usando style.border).</summary>
        public static Texture2D Redondeado(Color relleno, Color borde, int radio = 10, float grosor = 1.5f, int extra = 8)
        {
            int n = radio * 2 + extra;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    // distancia con signo al borde del rectángulo redondeado (negativa adentro)
                    float cx = Mathf.Clamp(x + 0.5f, radio, n - radio), cy = Mathf.Clamp(y + 0.5f, radio, n - radio);
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy)) - radio;
                    float dentro = Mathf.Clamp01(0.5f - d);
                    float enBorde = borde.a > 0 ? Mathf.Clamp01(d + grosor + 0.5f) : 0f;
                    var c = Color.Lerp(relleno, borde, enBorde);
                    c.a *= dentro;
                    px[y * n + x] = c;
                }
            t.SetPixels(px); t.Apply();
            return t;
        }

        /// <summary>El proyecto usa espacio de color lineal y ahí IMGUI oscurece el texto: se compensa el color.</summary>
        public static Color Letra(Color c)
        {
            if (QualitySettings.activeColorSpace != ColorSpace.Linear) return c;
            var g = c.gamma; g.a = c.a; return g;
        }

        static Font Fuente(string nombre)
        {
            var f = Resources.Load<Font>("Fuentes/" + nombre);
            return f != null ? f : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        static GUIStyle Texto_(Font f, int tam, Color c, bool ajustar = true, TextAnchor ancla = TextAnchor.UpperLeft)
        {
            return new GUIStyle { font = f, fontSize = tam, wordWrap = ajustar, alignment = ancla, normal = { textColor = Letra(c) }, richText = false };
        }

        public static void Iniciar()
        {
            if (listo) return;
            listo = true;
            Regular = Fuente("Inter-Regular"); Semi = Fuente("Inter-SemiBold"); Display = Fuente("SpaceGrotesk-Bold");

            Caja = new GUIStyle { normal = { background = Redondeado(Panel, Linea, 14) }, border = new RectOffset(16, 16, 16, 16), padding = new RectOffset(28, 28, 22, 22) };
            CajaClara = new GUIStyle { normal = { background = Redondeado(PanelClaro, Linea, 12) }, border = new RectOffset(14, 14, 14, 14), padding = new RectOffset(16, 16, 12, 12) };
            CajaCalida = new GUIStyle(Caja) { normal = { background = Redondeado(Panel, Calido, 14, 2.5f) } };

            Titulo = Texto_(Display, 52, Tinta, false, TextAnchor.MiddleCenter);
            Grande = Texto_(Display, 80, Tinta, false, TextAnchor.MiddleCenter);
            Encabezado = Texto_(Display, 33, Tinta, false);
            Subtitulo = Texto_(Display, 27, Tinta);
            Texto = Texto_(Regular, 22, Tinta);
            TextoChico = Texto_(Regular, 19, Tenue);
            Etiqueta = Texto_(Semi, 18, Tenue, false);
            Quien = Texto_(Semi, 18, Calido, false);
            Chip = Texto_(Semi, 19, Suave, false, TextAnchor.MiddleLeft);
            Insignia = Texto_(Display, 25, Tinta, false, TextAnchor.MiddleCenter);
            Insignia.padding = new RectOffset(20, 20, 10, 10);

            Boton = Crear(new Color(10 / 255f, 15 / 255f, 25 / 255f, 0.9f), LineaFuerte, Pendiente, Suave, 20);
            BotonChico = Crear(new Color(10 / 255f, 15 / 255f, 25 / 255f, 0.9f), LineaFuerte, Pendiente, Suave, 17);
            BotonPrimario = Crear(Calido, Calido, Perfiles.Hex("#FFD58F"), SobreCalido, 21);
            BotonPrimario.hover.background = Redondeado(Perfiles.Hex("#FFD58F"), Perfiles.Hex("#FFD58F"), 10);
            BotonPrimario.hover.textColor = Letra(SobreCalido);
            Seleccion = Crear(new Color(10 / 255f, 15 / 255f, 25 / 255f, 0.9f), LineaFuerte, Pendiente, Suave, 17);
            var on = Redondeado(Calido, Calido, 10);
            Seleccion.onNormal.background = on; Seleccion.onHover.background = on; Seleccion.onActive.background = on;
            Seleccion.onNormal.textColor = Seleccion.onHover.textColor = Seleccion.onActive.textColor = Letra(SobreCalido);

            Campo = new GUIStyle(GUI.skin.textField) { font = Regular, fontSize = 20, padding = new RectOffset(14, 14, 8, 8), border = new RectOffset(12, 12, 12, 12) };
            Campo.normal.background = Redondeado(Perfiles.Hex("#0D1320"), Linea, 10);
            Campo.hover.background = Redondeado(Perfiles.Hex("#0D1320"), LineaFuerte, 10);
            Campo.focused.background = Redondeado(Perfiles.Hex("#0D1320"), Calido, 10, 2f);
            Campo.normal.textColor = Campo.hover.textColor = Campo.focused.textColor = Letra(Tinta);
            Area = new GUIStyle(Campo) { wordWrap = true };

            // Piel por defecto: cursor, selección, casillas y barras de desplazamiento con la misma paleta
            var skin = GUI.skin;
            skin.font = Regular;
            skin.settings.cursorColor = Calido;
            skin.settings.selectionColor = new Color(Calido.r, Calido.g, Calido.b, 0.35f);
            skin.label.normal.textColor = Letra(Tinta);
            skin.toggle.font = Regular; skin.toggle.fontSize = 18;
            skin.toggle.normal.textColor = Letra(Suave); skin.toggle.hover.textColor = skin.toggle.onNormal.textColor = skin.toggle.onHover.textColor = Letra(Tinta);
            skin.verticalScrollbar.normal.background = Tex(new Color(1, 1, 1, 0.04f));
            skin.verticalScrollbar.fixedWidth = 10; skin.verticalScrollbar.border = new RectOffset(0, 0, 0, 0);
            skin.verticalScrollbarThumb.normal.background = Redondeado(LineaFuerte, LineaFuerte, 4);
            skin.verticalScrollbarThumb.border = new RectOffset(5, 5, 5, 5); skin.verticalScrollbarThumb.fixedWidth = 10;
            skin.verticalScrollbarUpButton.fixedHeight = 0; skin.verticalScrollbarDownButton.fixedHeight = 0;
        }

        static GUIStyle Crear(Color relleno, Color borde, Color bordeHover, Color texto, int tam)
        {
            var s = new GUIStyle
            {
                font = Semi, fontSize = tam, alignment = TextAnchor.MiddleCenter, border = new RectOffset(12, 12, 12, 12),
                normal = { background = Redondeado(relleno, borde, 10), textColor = Letra(texto) },
                hover = { background = Redondeado(relleno, bordeHover, 10), textColor = Color.white },
                active = { background = Redondeado(Calido, Calido, 10), textColor = Letra(SobreCalido) },
                padding = new RectOffset(18, 18, 8, 8),
            };
            return s;
        }

        /// <summary>Estilo de texto con otro color (para no tocar los estilos base).</summary>
        public static GUIStyle Color_(GUIStyle baseStyle, Color c)
        {
            var s = new GUIStyle(baseStyle); s.normal.textColor = Letra(c); return s;
        }

        public static Color ColorVoto(string decision)
        {
            return decision == "invertir" ? Dentro : decision == "contraoferta" ? Calido : decision == "salir" ? Fuera : Pendiente;
        }

        public static string TextoVoto(string decision)
        {
            return decision == "invertir" ? "Dentro" : decision == "contraoferta" ? "Contraoferta" : decision == "salir" ? "Fuera" : "Pendiente";
        }

        public static void Rect(Rect r, Color c)
        {
            var old = GUI.color; GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = old;
        }

        /// <summary>Círculo pequeño (punto de estado de los chips).</summary>
        static Texture2D punto;
        public static void Punto(Rect r, Color c)
        {
            if (punto == null) punto = Redondeado(Color.white, new Color(0, 0, 0, 0), 16, 0f, 0);
            var old = GUI.color; GUI.color = c; GUI.DrawTexture(r, punto); GUI.color = old;
        }
    }
}
