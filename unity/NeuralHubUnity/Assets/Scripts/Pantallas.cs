using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace NeuralHub
{
    // ====================== Inicio ======================
    public class MenuInicio
    {
        public void Dibujar(AppNeuralHub app, float W, float H)
        {
            GUILayout.BeginArea(new Rect(W / 2 - 580, 50, 1160, H - 90), Estilos.Caja);
            GUILayout.Space(10);
            GUILayout.Label("NEURALHUB PRESENTA", new GUIStyle(Estilos.Quien) { alignment = TextAnchor.MiddleCenter });
            GUILayout.Label(Personas.Programa, new GUIStyle(Estilos.Titulo) { fontSize = 66 });
            GUILayout.Label(Personas.Lema + "  ·  Panel de inversionistas con agentes de IA", new GUIStyle(Estilos.TextoChico) { alignment = TextAnchor.MiddleCenter });
            GUILayout.Space(26);
            GUILayout.Label("Practica tu pitch ante 3 tiburones elegidos de un catálogo de 6. Responde sus preguntas por escrito, mira cómo debaten entre ellos y recibe una decisión justificada con retroalimentación.", Estilos.Texto);
            GUILayout.Space(28);
            GUILayout.Label("ELIGE UN EMPRENDEDOR PARA PRACTICAR", new GUIStyle(Estilos.Quien) { alignment = TextAnchor.MiddleCenter });
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            foreach (var n in Niveles.Ids) { TarjetaNivel(app, n); if (n != "dificil") GUILayout.Space(14); }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(14);
            if (GUILayout.Button("Escribir mi propio pitch", Estilos.BotonPrimario, GUILayout.Height(54))) app.IrAFormulario();
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Conoce a los tiburones", Estilos.Boton, GUILayout.Height(56))) { app.Clic(); app.conocer.Abrir(app, Vista.Inicio); }
            GUILayout.Space(12);
            if (GUILayout.Button("Historial de sesiones", Estilos.Boton, GUILayout.Height(56))) app.AbrirSesiones();
            GUILayout.EndHorizontal();
            GUILayout.Space(24);
            bool mock = GUILayout.Toggle(app.cliente.usarMock, "  Usar sesión de ejemplo (sin conexión)", GUILayout.Height(30));
            if (mock != app.cliente.usarMock) app.FijarMock(mock);
            if (!app.cliente.usarMock)
            {
                GUILayout.Space(6);
                GUILayout.BeginHorizontal();
                GUILayout.Label("Servicio", Estilos.Etiqueta, GUILayout.Width(90), GUILayout.Height(44));
                app.cliente.urlBase = GUILayout.TextField(app.cliente.urlBase, Estilos.Campo, GUILayout.Width(440), GUILayout.Height(44));
                GUILayout.Space(8);
                if (GUILayout.Button("Probar", Estilos.BotonChico, GUILayout.Width(120), GUILayout.Height(44))) app.ProbarConexion();
                GUILayout.EndHorizontal();
            }
            GUILayout.Space(12);
            var c = app.cliente.usarMock ? "Modo sin conexión: se reproduce una sesión real guardada." :
                    app.cliente.conectado ? "Conectado al servicio de agentes." :
                    app.cliente.probando ? "Conectando con el servicio…" : "Sin conexión con el servicio. Actívalo o usa la sesión de ejemplo.";
            GUILayout.Label(c, Estilos.Color_(Estilos.TextoChico, app.cliente.usarMock || app.cliente.conectado ? Estilos.Dentro : Estilos.Calido));
            if (!string.IsNullOrEmpty(app.mensaje)) GUILayout.Label(app.mensaje, Estilos.TextoChico);
            GUILayout.FlexibleSpace();
            GUILayout.Label("Simulación educativa; no es asesoría financiera.", new GUIStyle(Estilos.TextoChico) { alignment = TextAnchor.MiddleCenter });
            GUILayout.EndArea();
        }

        void TarjetaNivel(AppNeuralHub app, string n)
        {
            var ej = Niveles.Ejemplo(app.ejemplos, n);
            var r = GUILayoutUtility.GetRect(345, 206, GUILayout.Width(345), GUILayout.Height(206));
            bool encima = r.Contains(Event.current.mousePosition);
            GUI.Box(r, GUIContent.none, encima ? Estilos.CajaCalida : Estilos.CajaClara);
            Estilos.Rect(new Rect(r.x + 18, r.y + 14, r.width - 36, 6), Niveles.ColorDe(n));
            GUI.Label(new Rect(r.x + 18, r.y + 30, r.width - 36, 36), Niveles.Nombre(n), Estilos.Color_(new GUIStyle(Estilos.Subtitulo) { fontSize = 30 }, Niveles.ColorDe(n)));
            GUI.Label(new Rect(r.x + 18, r.y + 70, r.width - 36, 28), ej != null ? ej.pitch.nombre : "…", new GUIStyle(Estilos.Texto) { font = Estilos.Semi });
            GUI.Label(new Rect(r.x + 18, r.y + 102, r.width - 36, 96), Niveles.Descripcion(n), new GUIStyle(Estilos.TextoChico) { fontSize = 19 });
            GUI.enabled = ej != null;
            if (GUI.Button(r, GUIContent.none, GUIStyle.none)) { app.Clic(); app.ElegirNivel(n); }
            GUI.enabled = true;
        }
    }

    // ====================== Formulario del pitch ======================
    public class FormularioPitch
    {
        public PitchDto p = new PitchDto { sector = "tecnologia", etapa = "idea", equipo_dedicacion = "tiempo completo", propiedad_intelectual = "ninguna" };
        public string sFund = "1", sMonto = "", sPct = "", sVentas = "", sMargen = "", sGasto = "";
        string discursoEjemplo, huellaEjemplo;
        Vector2 scroll;
        bool menuEjemplos;
        static readonly string[] Sectores = { "tecnologia", "alimentos_y_bebidas", "consumo_y_moda", "salud", "educacion", "sustentabilidad_y_energia", "servicios_profesionales" };
        static readonly string[] Etapas = { "idea", "prototipo", "primeras_ventas", "crecimiento" };
        const int Max = 300;
        const float AnchoCampo = 1096f;

        // Autocompletar con un documento o con texto pegado
        bool menuAuto, extrayendo;
        string pegado = "", avisoAuto = "";
        Vector2 scrollPegado;

        void Extraer(AppNeuralHub app, byte[] datos, string nombreArchivo)
        {
            extrayendo = true; avisoAuto = "Leyendo " + (nombreArchivo == "pegado.txt" ? "tu texto" : nombreArchivo) + "…";
            app.StartCoroutine(app.cliente.ExtraerPitch(datos, nombreArchivo, (json, error) =>
            {
                extrayendo = false;
                if (json == null) { avisoAuto = "No se pudo autocompletar: " + error; return; }
                var r = JsonUtility.FromJson<ExtraccionDto>(json);
                var d = r.pitch;
                if (string.IsNullOrEmpty(d.sector)) d.sector = p.sector;
                if (string.IsNullOrEmpty(d.etapa)) d.etapa = p.etapa;
                if (string.IsNullOrEmpty(d.equipo_dedicacion)) d.equipo_dedicacion = p.equipo_dedicacion;
                if (string.IsNullOrEmpty(d.propiedad_intelectual)) d.propiedad_intelectual = "ninguna";
                Cargar(d);
                if (d.equipo_fundadores <= 0) sFund = "";
                if (d.monto_mxn <= 0) sMonto = "";
                if (d.porcentaje <= 0) sPct = "";
                discursoEjemplo = ""; huellaEjemplo = null;
                menuAuto = false; pegado = ""; scroll = Vector2.zero;
                avisoAuto = "Formulario llenado. REVÍSALO: puede que no todo se haya pasado bien." +
                    (r.faltan != null && r.faltan.Length > 0 ? "\nFALTA (obligatorio): " + string.Join(", ", r.faltan) + "." : "") +
                    (r.recomendados != null && r.recomendados.Length > 0 ? "\nNo venía y conviene llenarlo: " + string.Join(", ", r.recomendados) + "." : "");
            }));
        }

        // solo para la prueba de pantallas (-nh-ui)
        public bool Extrayendo { get { return extrayendo; } }
        public void PruebaDesplazar(float y) { scroll = new Vector2(0, y); }
        public void PruebaMenuAuto() { menuAuto = true; }
        public void PruebaExtraer(AppNeuralHub app, string ruta) { Extraer(app, System.IO.File.ReadAllBytes(ruta), System.IO.Path.GetFileName(ruta)); }

        void DibujarAutocompletar(AppNeuralHub app)
        {
            GUILayout.BeginVertical(Estilos.CajaClara);
            GUILayout.Label("Un agente copia a los campos lo que diga tu documento. Puede que no todo se pase bien: revisa cada campo antes de continuar.",
                            Estilos.Color_(new GUIStyle(Estilos.TextoChico) { wordWrap = true }, Estilos.Calido));
            GUILayout.Space(6);
            GUI.enabled = !extrayendo && !app.cliente.usarMock;
            if (GUILayout.Button("Subir documento (.docx, .pdf, .txt)", Estilos.BotonChico, GUILayout.Height(44)))
            {
                var ruta = DialogoArchivo.Elegir();
                if (!string.IsNullOrEmpty(ruta))
                {
                    try
                    {
                        if (new System.IO.FileInfo(ruta).Length > 5000000) avisoAuto = "El archivo pesa más de 5 MB.";
                        else Extraer(app, System.IO.File.ReadAllBytes(ruta), System.IO.Path.GetFileName(ruta));
                    }
                    catch (System.Exception ex) { avisoAuto = "No se pudo abrir el archivo (¿está abierto en otro programa?): " + ex.Message; }
                }
                GUIUtility.ExitGUI();
            }
            GUILayout.Space(6);
            GUILayout.Label("…o pega aquí tu pitch como texto:", Estilos.TextoChico);
            scrollPegado = GUILayout.BeginScrollView(scrollPegado, false, false, GUILayout.Height(130));
            pegado = GUILayout.TextArea(pegado, 20000, Estilos.Area, GUILayout.Width(AnchoCampo - 40), GUILayout.MinHeight(118));
            GUILayout.EndScrollView();
            GUI.enabled = !extrayendo && !app.cliente.usarMock && pegado.Trim().Length >= 80;
            if (GUILayout.Button(extrayendo ? "Leyendo…" : "Llenar el formulario con este texto", Estilos.BotonPrimario, GUILayout.Height(44)))
                Extraer(app, System.Text.Encoding.UTF8.GetBytes(pegado), "pegado.txt");
            GUI.enabled = true;
            if (app.cliente.usarMock) GUILayout.Label("Necesita el servicio conectado (no funciona con la sesión de ejemplo).", Estilos.TextoChico);
            GUILayout.EndVertical();
        }

        static double Num(string s, double def = -1)
        {
            double v; return double.TryParse((s ?? "").Replace(",", "").Replace("$", "").Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out v) ? v : def;
        }

        string Texto(string etiqueta, string valor, bool area = false, float alto = 44)
        {
            GUILayout.Space(10);
            GUILayout.Label(etiqueta, Estilos.Etiqueta);
            GUILayout.Space(4);
            // ancho fijo y ajuste de línea: el texto largo baja de renglón en vez de estirar la pantalla
            // la altura se calcula con el ancho real del campo (la automática de IMGUI no crecía)
            float necesaria = Estilos.Area.CalcHeight(new GUIContent((valor ?? "") + " "), AnchoCampo - 12f) + 4f;
            string nuevo = GUILayout.TextArea(valor ?? "", Estilos.Area, GUILayout.Width(AnchoCampo), GUILayout.Height(Mathf.Max(area ? alto + 30 : alto, necesaria)));
            nuevo = nuevo.Replace("\r", "").Replace('\n', ' ');
            return nuevo.Length > Max ? nuevo.Substring(0, Max) : nuevo;
        }

        string Elegir(string etiqueta, string valor, string[] opciones)
        {
            GUILayout.Space(10);
            GUILayout.Label(etiqueta, Estilos.Etiqueta);
            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            for (int i = 0; i < opciones.Length; i++)
            {
                if (i > 0 && i % 4 == 0) { GUILayout.EndHorizontal(); GUILayout.Space(6); GUILayout.BeginHorizontal(); }
                var o = opciones[i];
                if (GUILayout.Toggle(valor == o, o.Replace('_', ' '), Estilos.Seleccion, GUILayout.Height(42))) valor = o;
                GUILayout.Space(6);
            }
            GUILayout.EndHorizontal();
            return valor;
        }

        static void Seccion(string titulo)
        {
            GUILayout.Space(26);
            GUILayout.Label(titulo, Estilos.Quien);
        }

        public void Cargar(PitchDto d)
        {
            p = d;
            sFund = d.equipo_fundadores.ToString();
            sMonto = d.monto_mxn.ToString("0", CultureInfo.InvariantCulture); sPct = d.porcentaje.ToString("0.##", CultureInfo.InvariantCulture);
            sVentas = d.ventas_12m_mxn > 0 ? d.ventas_12m_mxn.ToString("0", CultureInfo.InvariantCulture) : "";
            sMargen = d.margen_bruto_pct > 0 ? d.margen_bruto_pct.ToString("0.##", CultureInfo.InvariantCulture) : "";
            sGasto = d.gasto_mensual_mxn > 0 ? d.gasto_mensual_mxn.ToString("0", CultureInfo.InvariantCulture) : "";
            discursoEjemplo = d.discurso; Json(); huellaEjemplo = p.Huella();
        }

        public bool Completo()
        {
            return !string.IsNullOrWhiteSpace(p.nombre) && !string.IsNullOrWhiteSpace(p.problema) && !string.IsNullOrWhiteSpace(p.solucion) &&
                   !string.IsNullOrWhiteSpace(p.mercado_objetivo) && !string.IsNullOrWhiteSpace(p.modelo_ingresos) && !string.IsNullOrWhiteSpace(p.uso_fondos) &&
                   Num(sMonto) > 0 && Num(sPct) > 0 && Num(sPct) <= 100 && Num(sFund) >= 1;
        }

        public string Json()
        {
            p.equipo_fundadores = (int)Num(sFund, 1); p.monto_mxn = Num(sMonto); p.porcentaje = Num(sPct);
            p.ventas_12m_mxn = Num(sVentas); p.margen_bruto_pct = Num(sMargen); p.gasto_mensual_mxn = Num(sGasto);
            p.discurso = huellaEjemplo != null && p.Huella() != huellaEjemplo ? "" : discursoEjemplo;
            return ClienteServicio.PitchAJson(p, p.ventas_12m_mxn >= 0 && sVentas != "", p.margen_bruto_pct >= 0 && sMargen != "", p.gasto_mensual_mxn >= 0 && sGasto != "");
        }

        public void Dibujar(AppNeuralHub app, float W, float H)
        {
            GUILayout.BeginArea(new Rect(W / 2 - 600, 30, 1200, H - 60), Estilos.Caja);
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            GUILayout.Label("Tu pitch", Estilos.Encabezado);
            GUILayout.Label("Los campos con * son obligatorios. Los tiburones preguntarán por lo que falte.", Estilos.TextoChico);
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(menuAuto ? "Cerrar" : "Autocompletar", Estilos.BotonPrimario, GUILayout.Width(220), GUILayout.Height(44))) { menuAuto = !menuAuto; menuEjemplos = false; }
            GUILayout.Space(8);
            if (GUILayout.Button(menuEjemplos ? "Cerrar ejemplos" : "Cargar ejemplo", Estilos.BotonChico, GUILayout.Width(220), GUILayout.Height(44))) { menuEjemplos = !menuEjemplos; menuAuto = false; }
            GUILayout.EndHorizontal();
            if (menuEjemplos && app.ejemplos != null)
            {
                foreach (var e in app.ejemplos)
                    if (GUILayout.Button(e.etiqueta, Estilos.BotonChico, GUILayout.Height(40))) { Cargar(e.pitch); menuEjemplos = false; }
            }
            if (menuAuto) DibujarAutocompletar(app);
            if (!string.IsNullOrEmpty(avisoAuto))
            {
                GUILayout.Space(6);
                GUILayout.Label(avisoAuto, Estilos.Color_(new GUIStyle(Estilos.TextoChico) { wordWrap = true }, Estilos.Calido), GUILayout.Width(1140));
            }
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Exigencia de los tiburones", Estilos.Etiqueta, GUILayout.Height(40));
            GUILayout.Space(12);
            foreach (var n in Niveles.Ids)
            {
                if (GUILayout.Toggle(app.nivel == n, Niveles.Nombre(n), Estilos.Seleccion, GUILayout.Width(130), GUILayout.Height(40))) app.nivel = n;
                GUILayout.Space(6);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            scroll = GUILayout.BeginScrollView(scroll, false, true, GUIStyle.none, GUI.skin.verticalScrollbar);
            p.nombre = Texto("Nombre del negocio *", p.nombre);
            p.sector = Elegir("Sector *", p.sector, Sectores);
            p.etapa = Elegir("Etapa *", p.etapa, Etapas);
            p.problema = Texto("Problema *", p.problema, true, 50);
            p.solucion = Texto("Solución *", p.solucion, true, 50);
            p.mercado_objetivo = Texto("Mercado objetivo *", p.mercado_objetivo);
            p.modelo_ingresos = Texto("Modelo de ingresos *", p.modelo_ingresos);
            p.competencia = Texto("Competencia", p.competencia);
            p.diferenciador = Texto("Diferenciador", p.diferenciador);
            p.validacion = Texto("Validación con clientes (clientes que ya pagan, pilotos, cartas de intención)", p.validacion);
            p.propiedad_intelectual = Elegir("Patente o marca registrada", p.propiedad_intelectual, new[] { "patente", "marca", "ninguna" });
            Seccion("EQUIPO");
            sFund = Texto("Número de fundadores *", sFund);
            p.equipo_roles = Texto("Roles", p.equipo_roles);
            p.equipo_dedicacion = Elegir("Dedicación", p.equipo_dedicacion, new[] { "tiempo completo", "parcial" });
            p.equipo_experiencia = Texto("Experiencia relevante", p.equipo_experiencia);
            Seccion("OFERTA");
            sMonto = Texto("Monto solicitado (MXN) *", sMonto);
            sPct = Texto("Porcentaje ofrecido (%) *", sPct);
            double monto = Num(sMonto), pct = Num(sPct);
            if (monto > 0 && pct > 0 && pct <= 100)
            { GUILayout.Space(8); GUILayout.Label("Valuación implícita: " + Perfiles.Mxn(monto / (pct / 100.0)), Estilos.Color_(Estilos.Subtitulo, Estilos.Calido)); }
            p.uso_fondos = Texto("Uso de los fondos *", p.uso_fondos);
            Seccion("NÚMEROS CLAVE");
            GUILayout.Label("Opcionales: déjalos vacíos si «Aún no lo sé».", Estilos.TextoChico);
            sVentas = Texto("Ventas de los últimos 12 meses (MXN)", sVentas);
            sMargen = Texto("Margen bruto (%)", sMargen);
            sGasto = Texto("Gasto mensual (MXN)", sGasto);
            GUILayout.EndScrollView();
            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Volver", Estilos.Boton, GUILayout.Width(180), GUILayout.Height(54))) app.Ir(Vista.Inicio);
            GUILayout.FlexibleSpace();
            GUILayout.Label(Completo() ? "" : "Completa los campos con *", Estilos.TextoChico, GUILayout.Width(300), GUILayout.Height(54));
            GUI.enabled = Completo();
            if (GUILayout.Button("Siguiente", Estilos.BotonPrimario, GUILayout.Width(240), GUILayout.Height(54))) app.IrAEleccionPanel(Json());
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }

    // ====================== Elección del panel ======================
    public class EleccionPanel
    {
        public readonly HashSet<string> elegidos = new HashSet<string>();

        public void PrecargarRecomendados(Recomendacion r)
        {
            elegidos.Clear();
            if (r == null || r.ranking == null) return;
            foreach (var f in r.ranking) if (f.recomendado) elegidos.Add(f.tiburon_id);
        }

        public void Dibujar(AppNeuralHub app, float W, float H)
        {
            GUILayout.BeginArea(new Rect(W / 2 - 720, 40, 1440, H - 80), Estilos.Caja);
            GUILayout.Label("Elige a tus 3 tiburones", Estilos.Encabezado);
            GUILayout.Label("La app marcó a los 3 que más te servirían según los datos que te faltan. Puedes aceptarlos o cambiarlos (exactamente 3).", Estilos.TextoChico);
            if (app.recomendacion != null)
                GUILayout.Label("Valuación implícita de tu oferta: " + Perfiles.Mxn(app.recomendacion.valuacion_mxn), Estilos.Color_(Estilos.Texto, Estilos.Calido));
            GUILayout.Space(16);
            int col = 0;
            GUILayout.BeginHorizontal();
            var lista = app.recomendacion != null && app.recomendacion.ranking != null ? app.recomendacion.ranking : new RankingItem[0];
            foreach (var f in lista)
            {
                var info = app.Catalogo(f.tiburon_id);
                bool sel = elegidos.Contains(f.tiburon_id);
                GUILayout.BeginVertical(sel ? Estilos.CajaCalida : Estilos.CajaClara, GUILayout.Width(450), GUILayout.Height(372));
                var persona = Personas.De(f.tiburon_id);
                GUILayout.BeginHorizontal();
                var hueco = GUILayoutUtility.GetRect(108, 108, GUILayout.Width(108), GUILayout.Height(108));
                var foto = Retratos.De(f.tiburon_id);
                if (foto) GUI.DrawTexture(hueco, foto);
                GUILayout.Space(14);
                GUILayout.BeginVertical();
                GUILayout.Label(f.recomendado ? "RECOMENDADO" : " ", Estilos.Quien);
                GUILayout.Label(persona != null ? persona.nombre : f.tiburon_id, new GUIStyle(Estilos.Subtitulo) { fontSize = 25 });
                GUILayout.Label(info != null ? info.nombre : "", Estilos.TextoChico);
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                GUILayout.Space(6);
                if (info != null)
                {
                    GUILayout.Label(info.perfil + " Se fija en " + info.criterio + ".", new GUIStyle(Estilos.Texto) { fontSize = 19 });
                }
                GUILayout.Label("Por qué: " + f.motivo, Estilos.TextoChico);
                GUILayout.FlexibleSpace();
                bool nuevo = GUILayout.Toggle(sel, sel ? "En mi panel" : "Agregar al panel", Estilos.Seleccion, GUILayout.Height(44));
                if (nuevo && !sel && elegidos.Count < 3) elegidos.Add(f.tiburon_id);
                if (!nuevo && sel) elegidos.Remove(f.tiburon_id);
                GUILayout.EndVertical();
                col++;
                if (col % 3 == 0) { GUILayout.EndHorizontal(); GUILayout.Space(12); GUILayout.BeginHorizontal(); }
                else GUILayout.Space(12);
            }
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Volver", Estilos.Boton, GUILayout.Width(180), GUILayout.Height(54))) app.Ir(Vista.Formulario);
            GUILayout.Space(12);
            if (GUILayout.Button("Conocer a los tiburones", Estilos.Boton, GUILayout.Width(320), GUILayout.Height(54))) { app.Clic(); app.conocer.Abrir(app, Vista.Panel); }
            GUILayout.FlexibleSpace();
            GUILayout.Label(elegidos.Count + " de 3 elegidos", Estilos.Color_(Estilos.Texto, elegidos.Count == 3 ? Estilos.Tinta : Estilos.Tenue), GUILayout.Width(220), GUILayout.Height(54));
            GUI.enabled = elegidos.Count == 3;
            if (GUILayout.Button("Siguiente", Estilos.BotonPrimario, GUILayout.Width(260), GUILayout.Height(54))) app.IrAModo(new List<string>(elegidos));
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }

    // ====================== Modo de simulación ======================
    public class SeleccionModo
    {
        GUIStyle tarjeta;

        /// <summary>Tarjeta seleccionable: un toggle con el fondo de la tarjeta (normal) o con borde cálido (elegida).</summary>
        bool Tarjeta(Rect r, bool elegida, string titulo, string texto)
        {
            if (tarjeta == null)
            {
                tarjeta = new GUIStyle { border = new RectOffset(16, 16, 16, 16) };
                tarjeta.normal.background = Estilos.CajaClara.normal.background;
                tarjeta.hover.background = Estilos.Redondeado(Estilos.PanelClaro, Estilos.Pendiente, 14);
                tarjeta.active.background = Estilos.CajaCalida.normal.background;
                tarjeta.onNormal.background = tarjeta.onHover.background = tarjeta.onActive.background = Estilos.CajaCalida.normal.background;
            }
            bool nuevo = GUI.Toggle(r, elegida, GUIContent.none, tarjeta);
            GUI.Label(new Rect(r.x + 28, r.y + 24, r.width - 56, 30), elegida ? "ELEGIDO" : " ", Estilos.Quien);
            GUI.Label(new Rect(r.x + 28, r.y + 54, r.width - 56, 40), titulo, new GUIStyle(Estilos.Subtitulo) { fontSize = 28 });
            GUI.Label(new Rect(r.x + 28, r.y + 102, r.width - 56, 120), texto, Estilos.Color_(Estilos.Texto, Estilos.Suave));
            return nuevo && !elegida;
        }

        public void Dibujar(AppNeuralHub app, float W, float H)
        {
            var area = new Rect(W / 2 - 640, 120, 1280, H - 240);
            GUI.Box(area, GUIContent.none, Estilos.Caja);
            float x = area.x + 40, y = area.y + 34, ancho = area.width - 80;
            GUI.Label(new Rect(x, y, ancho, 44), "¿Cómo quieres vivir la sesión?", Estilos.Encabezado);
            GUI.Label(new Rect(x, y + 50, ancho, 30), "Elige quién responde las preguntas de los tiburones.", Estilos.TextoChico);

            float tw = (ancho - 24) / 2, ty = y + 110, th = 250;
            if (Tarjeta(new Rect(x, ty, tw, th), !app.modoEnVivo, "Agente empresario",
                        "Tu agente empresario responde por ti con los datos de tu pitch. Tú observas cómo debate el panel y recibes la decisión y la retroalimentación."))
                app.modoEnVivo = false;
            if (Tarjeta(new Rect(x + tw + 24, ty, tw, th), app.modoEnVivo, "En vivo",
                        "Tú respondes cada pregunta en tiempo real, en cuanto el tiburón la hace. Así practicas tu pitch como si estuvieras frente al panel."))
                app.modoEnVivo = true;

            // Ayuda del agente empresario (solo en vivo)
            float ay = ty + th + 24;
            if (app.modoEnVivo)
            {
                var caja = new Rect(x + tw + 24, ay, tw, 196);
                GUI.Box(caja, GUIContent.none, Estilos.CajaClara);
                bool nueva = GUI.Toggle(new Rect(caja.x + 24, caja.y + 22, 260, 46), app.ayudaActiva,
                                        app.ayudaActiva ? "Ayuda activada" : "Ayuda desactivada", Estilos.Seleccion);
                if (nueva != app.ayudaActiva) app.ayudaActiva = nueva;
                GUI.Label(new Rect(caja.x + 24, caja.y + 80, caja.width - 48, 106),
                          app.ayudaActiva ? "Junto a tu respuesta podrás pedir una recomendación o, si ya escribiste algo, que el agente lo mejore."
                                          : "Las respuestas las escribes tú y nadie más.", Estilos.TextoChico);
            }
            if (app.cliente.usarMock)
                GUI.Label(new Rect(x, area.yMax - 150, ancho, 50), app.modoEnVivo
                    ? "Sin conexión: tus respuestas reemplazan a las de una sesión grabada, pero las reacciones del panel son las de la grabación. Conecta el servicio para una sesión real."
                    : "Sin conexión: se reproduce una sesión real guardada.", Estilos.Color_(Estilos.TextoChico, Estilos.Calido));

            if (GUI.Button(new Rect(x, area.yMax - 84, 180, 54), "Volver", Estilos.Boton)) app.Ir(Vista.Panel);
            if (GUI.Button(new Rect(area.xMax - 40 - 260, area.yMax - 84, 260, 54), "Iniciar sesión", Estilos.BotonPrimario)) app.IniciarSesionElegida();
        }
    }

    // ====================== Burbuja de respuesta (modo en vivo) ======================
    public class BurbujaRespuesta
    {
        public const float Alto = 270f;
        string turno, texto = "";
        bool cargando, sugerida, enfocar, grabando, transcribiendo;
        string avisoVoz = "";

        /// <summary>Escribe una respuesta en la burbuja (la usa la prueba automática -nh-vivo).</summary>
        public void Escribir(string turnoActual, string t) { turno = turnoActual; texto = t ?? ""; cargando = false; sugerida = false; enfocar = false; }

        public void Dibujar(AppNeuralHub app, float W, float H)
        {
            var r = app.reproductor;
            if (r.turnoUsuario != turno)
            {
                if (grabando) { GrabadorVoz.Detener(); grabando = false; }
                turno = r.turnoUsuario; texto = ""; cargando = false; sugerida = false; transcribiendo = false; enfocar = true; avisoVoz = "";
            }
            bool ayuda = app.cliente.ayuda;
            float ancho = Mathf.Min(1360, W - 80), x = W / 2 - ancho / 2, y = H - 120 - Alto;
            var caja = new Rect(x, y, ancho, Alto);
            GUI.Box(caja, GUIContent.none, Estilos.Caja);

            float derecha = 330f;   // columna de botones
            float anchoTexto = ancho - 56 - derecha - 20;
            GUI.Label(new Rect(x + 28, y + 20, 600, 28), "TÚ  ·  RESPUESTA A " + Perfiles.Corto(turno).ToUpper(), Estilos.Color_(Estilos.Quien, Estilos.Suave));
            if (sugerida) GUI.Label(new Rect(x + 28 + 360, y + 20, anchoTexto - 360, 28), "Sugerencia del agente empresario: puedes editarla", new GUIStyle(Estilos.Etiqueta) { alignment = TextAnchor.UpperRight });

            // Ctrl + Enter envía
            var ev = Event.current;
            bool enviar = false;
            if (ev.type == EventType.KeyDown && (ev.keyCode == KeyCode.Return || ev.keyCode == KeyCode.KeypadEnter) && (ev.control || ev.command) && !string.IsNullOrWhiteSpace(texto))
            { enviar = true; ev.Use(); }

            GUI.SetNextControlName("respuesta_en_vivo");
            var area = new Rect(x + 28, y + 56, anchoTexto, Alto - 56 - 46);
            string nuevo = GUI.TextArea(area, texto, 600, Estilos.Area);
            if (nuevo != texto) { texto = nuevo; }
            if (enfocar && ev.type == EventType.Repaint) { GUI.FocusControl("respuesta_en_vivo"); enfocar = false; }
            GUI.Label(new Rect(x + 28, area.yMax + 8, anchoTexto, 26), texto.Length + " / 600   ·   Ctrl + Enter para responder", Estilos.Color_(Estilos.TextoChico, Estilos.Tenue));

            float bx = caja.xMax - 28 - derecha;
            if (ayuda)
            {
                GUI.enabled = !cargando;
                var estBoton = new GUIStyle(Estilos.Boton) { wordWrap = true, fontSize = 18 };
                bool hayBorrador = !string.IsNullOrWhiteSpace(texto) && !sugerida;
                if (GUI.Button(new Rect(bx, y + 56, derecha, 70), cargando ? "El agente empresario está pensando…" : hayBorrador ? "Mejorar lo que escribí (agente empresario)" : "Recomendación del agente empresario", estBoton))
                {
                    cargando = true;
                    string pedido = turno;
                    app.PedirSugerencia(turno, hayBorrador ? texto.Trim() : "", s =>
                    {
                        if (pedido != turno) return;   // ya cambió el turno
                        cargando = false;
                        if (!string.IsNullOrEmpty(s)) { texto = s.Length > 600 ? s.Substring(0, 600) : s; sugerida = true; }
                        else avisoVoz = "El agente no pudo ayudar (hay una ayuda por pregunta). Edita tu respuesta y envíala.";
                    });
                }
                GUI.enabled = true;
            }
            // Dictado por voz (se transcribe con Whisper en el servicio y se agrega al texto)
            var rVoz = new Rect(bx, ayuda ? y + 132 : y + 56, derecha, 40);
            bool detener = false;
            if (transcribiendo) GUI.Label(rVoz, "Transcribiendo… no cierres esta ventana", new GUIStyle(Estilos.Etiqueta) { alignment = TextAnchor.MiddleCenter, fontSize = 17 });
            else if (!GrabadorVoz.HayMicrofono) GUI.Label(rVoz, "Sin micrófono para dictar", new GUIStyle(Estilos.TextoChico) { fontSize = 16, alignment = TextAnchor.MiddleCenter });
            else if (GUI.Button(rVoz, grabando ? "■ Detener  " + GrabadorVoz.Segundos.ToString("0") + " / " + GrabadorVoz.MaxSegundos + " s" : "● Dictar con voz",
                                grabando ? Estilos.BotonPrimario : Estilos.BotonChico))
            {
                app.Clic();
                if (grabando) detener = true;
                else { grabando = GrabadorVoz.Iniciar(); avisoVoz = ""; }
            }
            if (grabando && ev.type == EventType.Repaint && GrabadorVoz.LlegoAlLimite) detener = true;
            if (detener)
            {
                grabando = false;
                var wav = GrabadorVoz.Detener();
                if (wav == null) avisoVoz = "No se grabó audio. Revisa el micrófono.";
                else if (GrabadorVoz.UltimoPico < 0.01f) avisoVoz = "Casi no se oyó: acércate al micrófono o revisa cuál está activo.";
                else
                {
                    transcribiendo = true; string pedido = turno;
                    app.StartCoroutine(app.cliente.Transcribir(wav, t =>
                    {
                        if (pedido != turno) return;
                        transcribiendo = false;
                        if (!string.IsNullOrEmpty(t)) { texto = (texto + " " + t).Trim(); sugerida = false; if (texto.Length > 600) { texto = texto.Substring(0, 600); avisoVoz = "Se recortó a 600 caracteres."; } else avisoVoz = "Revisa el texto antes de enviarlo."; }
                        else avisoVoz = "No se entendió el audio. Habla más cerca, en voz normal o alta, e inténtalo de nuevo.";
                    }));
                }
            }
            if (GrabadorVoz.HayMicrofono)
                GUI.Label(new Rect(bx, rVoz.yMax + 2, derecha, ayuda ? 44 : 70), !string.IsNullOrEmpty(avisoVoz) ? avisoVoz : "Hasta 2 min. Habla claro, cerca del micrófono y sin ruido.",
                          new GUIStyle(Estilos.TextoChico) { fontSize = 15, wordWrap = true, alignment = TextAnchor.UpperCenter });

            GUI.enabled = !string.IsNullOrWhiteSpace(texto) && !grabando;
            if (GUI.Button(new Rect(bx, caja.yMax - 22 - 60, derecha, 60), "Responder", Estilos.BotonPrimario)) { app.Clic(); enviar = true; }
            GUI.enabled = true;
            if (enviar && !string.IsNullOrWhiteSpace(texto)) { r.Responder(texto.Trim()); GUI.FocusControl(null); }
        }
    }

    // ====================== Controles de reproducción (abajo, al centro) ======================
    public class ControlReproduccion
    {
        public void Dibujar(AppNeuralHub app, float W, float H)
        {
            var r = app.reproductor;
            const float alto = 50f, sep = 10f;
            var botones = new[] { "◀ Anterior", r.pausado ? "Reanudar" : "Pausa", "Siguiente ▶", "1x", "2x", "Turno", "Voz", "Silencio", "Historial", "Tu panel", "Salir" };
            var anchos = new[] { 136f, 120f, 140f, 60f, 60f, 130f, 76f, 110f, 120f, 120f, 90f };
            float total = 0; foreach (var a in anchos) total += a + sep; total -= sep;
            float x = W / 2 - total / 2, y = H - alto - 34;
            var sonidos = app.sonidos;
            GUI.Box(new Rect(W / 2 - total / 2 - 18, y - 10, total + 36, H - y + 6), GUIContent.none, Estilos.CajaClara);
            for (int i = 0; i < botones.Length; i++)
            {
                var rect = new Rect(x, y, anchos[i], alto);
                switch (i)
                {
                    case 0: if (GUI.Button(rect, botones[i], Estilos.BotonChico)) { app.Clic(); r.Anterior(); } break;
                    case 1: if (GUI.Button(rect, botones[i], Estilos.BotonChico)) { app.Clic(); r.AlternarPausa(); } break;
                    case 2: if (GUI.Button(rect, botones[i], Estilos.BotonChico)) { app.Clic(); r.Siguiente(); } break;
                    case 3: if (GUI.Toggle(rect, r.velocidad == 1f, "1x", Estilos.Seleccion) && r.velocidad != 1f) r.FijarVelocidad(1f); break;
                    case 4: if (GUI.Toggle(rect, r.velocidad == 2f, "2x", Estilos.Seleccion) && r.velocidad != 2f) r.FijarVelocidad(2f); break;
                    case 5: GUI.Label(rect, "Turno " + r.Indice + "/" + r.Total, new GUIStyle(Estilos.Etiqueta) { alignment = TextAnchor.MiddleCenter }); break;
                    case 6:
                        if (!sonidos) break;
                        bool voz = GUI.Toggle(rect, sonidos.vozActiva, botones[i], Estilos.Seleccion);
                        if (voz != sonidos.vozActiva) { sonidos.vozActiva = voz; if (!voz) sonidos.DetenerVoz(); }
                        break;
                    case 7:
                        if (!sonidos) break;
                        bool sil = GUI.Toggle(rect, sonidos.silencio, botones[i], Estilos.Seleccion);
                        if (sil != sonidos.silencio) sonidos.FijarSilencio(sil);
                        break;
                    case 8: if (GUI.Toggle(rect, app.mostrarHistorial, botones[i], Estilos.Seleccion) != app.mostrarHistorial) { app.Clic(); app.mostrarHistorial = !app.mostrarHistorial; } break;
                    case 9: if (GUI.Toggle(rect, app.interfaz.mostrarPanel, botones[i], Estilos.Seleccion) != app.interfaz.mostrarPanel) { app.Clic(); app.interfaz.mostrarPanel = !app.interfaz.mostrarPanel; } break;
                    case 10: if (GUI.Button(rect, botones[i], Estilos.BotonChico)) { app.Clic(); app.Ir(Vista.Inicio); } break;
                }
                x += anchos[i] + sep;
            }
            GUI.Label(new Rect(0, H - 30, W, 24), "Simulación educativa; no es asesoría financiera.", Estilos.Color_(new GUIStyle(Estilos.TextoChico) { fontSize = 16, alignment = TextAnchor.MiddleCenter }, Estilos.Suave));
        }
    }

    // ====================== Historial (turnos de la sesión) ======================
    public class PanelHistorial
    {
        Vector2 scroll;
        public void Dibujar(AppNeuralHub app, float W, float H)
        {
            GUILayout.BeginArea(new Rect(W - 780, 110, 740, H - 400), Estilos.Caja);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Conversación", Estilos.Subtitulo);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Cerrar", Estilos.BotonChico, GUILayout.Width(110), GUILayout.Height(40))) app.mostrarHistorial = false;
            GUILayout.EndHorizontal();
            GUILayout.Label("Pulsa «Ir» para volver a ver la escena desde ese turno.", Estilos.Color_(Estilos.TextoChico, Estilos.Tenue));
            GUILayout.Space(6);
            scroll = GUILayout.BeginScrollView(scroll);
            var recibidos = app.cliente.recibidos;
            int actual = app.reproductor.Indice - 1;
            for (int i = 0; i < recibidos.Count; i++)
            {
                var e = recibidos[i];
                if (e.tipo == "REPORTE") continue;
                bool visto = i <= actual;
                string etiqueta = (e.ronda > 0 ? "R" + e.ronda + " · " : "") + Perfiles.Corto(e.emisor) + " → " + Perfiles.Corto(e.destino) + " · " + e.tipo +
                                  (!string.IsNullOrEmpty(e.decision) ? " (" + Estilos.TextoVoto(e.decision).ToLower() + ")" : "");
                var c = e.tipo == "VOTO" ? Estilos.ColorVoto(e.decision) : e.emisor == "emprendedor" ? Estilos.Suave : Estilos.Calido;
                if (!visto) c = Estilos.Pendiente;
                GUILayout.BeginHorizontal(i == actual ? Estilos.CajaClara : GUIStyle.none);
                GUILayout.Label((i == actual ? "▶ " : "") + etiqueta.ToUpper(), Estilos.Color_(Estilos.Etiqueta, c));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Ir", Estilos.BotonChico, GUILayout.Width(56), GUILayout.Height(30))) { app.Clic(); app.reproductor.IrA(i); }
                GUILayout.EndHorizontal();
                if (visto) GUILayout.Label(e.texto, new GUIStyle(Estilos.TextoChico) { normal = { textColor = Estilos.Letra(Estilos.Suave) } });
                GUILayout.Space(10);
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }

    // ====================== HUD de la sesión (título, chips de votos, subtítulo) ======================
    public class InterfazDebate
    {
        public bool mostrarPanel;
        readonly Dictionary<string, GUIStyle> chips = new Dictionary<string, GUIStyle>();
        readonly Dictionary<string, GUIStyle> insignias = new Dictionary<string, GUIStyle>();

        GUIStyle EstiloChip(Color borde, bool ahora)
        {
            string k = ColorUtility.ToHtmlStringRGB(borde) + ahora;
            GUIStyle s;
            if (!chips.TryGetValue(k, out s))
            {
                s = new GUIStyle { normal = { background = Estilos.Redondeado(Estilos.PanelClaro, borde, 20, ahora ? 3f : 1.5f) }, border = new RectOffset(22, 22, 22, 22) };
                chips[k] = s;
            }
            return s;
        }

        GUIStyle EstiloInsignia(Color c)
        {
            string k = ColorUtility.ToHtmlStringRGB(c);
            GUIStyle s;
            if (!insignias.TryGetValue(k, out s))
            {
                s = new GUIStyle(Estilos.Insignia) { border = new RectOffset(14, 14, 14, 14) };
                s.normal.background = Estilos.Redondeado(Color.Lerp(Estilos.Fondo, c, 0.16f), c, 12, 3f);
                s.normal.textColor = Estilos.Letra(Color.Lerp(c, Color.white, 0.6f));
                insignias[k] = s;
            }
            return s;
        }

        static string NombreLargo(AppNeuralHub app, string id)
        {
            if (id == "emprendedor") return "Tú";
            var persona = Personas.De(id);
            if (persona != null) return persona.nombre + "  ·  " + (id == "tiburona_consumo" ? "Tiburona de " : "Tiburón ") + Perfiles.Corto(id).ToLower();
            foreach (var p in app.reproductor.panelInfo) if (p.tiburon_id == id && !string.IsNullOrEmpty(p.nombre)) return p.nombre;
            var info = app.Catalogo(id);
            return info != null ? info.nombre : Perfiles.Corto(id);
        }

        static void Titulos(ReproductorSesion r, out string titulo, out string sub)
        {
            var e = r.actual;
            string ronda = "Preguntas · ronda " + Mathf.Clamp(r.ronda, 1, 3);
            titulo = "Preparando sesión"; sub = "";
            if (e == null) return;
            switch (e.tipo)
            {
                case "PITCH": case "PANEL": titulo = "Tu pitch"; sub = "Los tiburones escuchan"; break;
                case "PREGUNTA": titulo = ronda; sub = r.turnoUsuario != null ? "Tu turno: responde a " + Perfiles.Corto(r.turnoUsuario) : "Pregunta de " + Perfiles.Corto(e.emisor); break;
                case "RESPUESTA": titulo = ronda; sub = "Tu respuesta a " + Perfiles.Corto(e.destino); break;
                case "REPLICA": titulo = ronda; sub = Perfiles.Corto(e.emisor) + " replica a " + Perfiles.Corto(e.destino); break;
                case "VOTO": titulo = e.votacion == 2 ? "Votación final" : "Veredicto"; sub = "Habla " + Mathf.Max(1, r.votos.Count) + " de " + Mathf.Max(1, r.panelIds.Length); break;
                case "NEGOCIACION": case "OFERTA": titulo = "Negociación"; sub = "Ajusta tu oferta"; break;
                case "DECISION": titulo = "Decisión del panel"; sub = ""; break;
                case "REPORTE": titulo = "Decisión del panel"; sub = "Preparando tu reporte"; break;
                default: titulo = r.ronda >= 4 ? "Negociación" : ronda; break;
            }
        }

        static string textoPaginado;
        static int anchoPaginado;
        static List<string> paginasCache;

        /// <summary>Parte un texto largo en páginas de pocas líneas, cortando en frases cuando se puede.</summary>
        static List<string> Paginas(string texto, int maximo)
        {
            if (texto == textoPaginado && maximo == anchoPaginado) return paginasCache;
            var paginas = new List<string>();
            var resto = texto.Trim();
            // páginas parejas: se calcula cuántas hacen falta y cada una se corta cerca de su parte,
            // de preferencia al final de una frase, para que nunca quede una palabra suelta
            while (resto.Length > maximo)
            {
                int faltan = Mathf.CeilToInt(resto.Length / (float)maximo);
                int meta = Mathf.CeilToInt(resto.Length / (float)faltan);
                int desde = Mathf.Max(1, meta - 50), hasta = Mathf.Min(maximo, meta + 30);
                int corte = -1, mejor = int.MaxValue;
                for (int i = desde; i <= hasta && i < resto.Length; i++)
                {
                    if (resto[i] != ' ') continue;
                    int costo = Mathf.Abs(i - meta) + (".?!:;,".IndexOf(resto[i - 1]) >= 0 ? 0 : 40);
                    if (costo < mejor) { mejor = costo; corte = i; }
                }
                if (corte < 0) corte = resto.LastIndexOf(' ', maximo);
                if (corte <= 0) break;
                paginas.Add(resto.Substring(0, corte)); resto = resto.Substring(corte + 1);
            }
            if (resto.Length > 0) paginas.Add(resto);
            if (paginas.Count == 0) paginas.Add(texto);
            textoPaginado = texto; anchoPaginado = maximo; paginasCache = paginas;
            return paginas;
        }

        static int PaginaActual(List<string> paginas, float avance)
        {
            int total = 0; foreach (var p in paginas) total += p.Length;
            float visto = avance * total; int suma = 0;
            for (int i = 0; i < paginas.Count; i++) { suma += paginas[i].Length; if (visto < suma) return i; }
            return paginas.Count - 1;
        }

        public void Dibujar(AppNeuralHub app, float W, float H)
        {
            var r = app.reproductor;

            // Título y ronda (arriba a la izquierda)
            string titulo, sub; Titulos(r, out titulo, out sub);
            // fondo propio: sin él, el texto se encima con las pantallas del estudio
            float anchoTitulo = Mathf.Max(Estilos.Encabezado.CalcSize(new GUIContent(titulo)).x,
                string.IsNullOrEmpty(sub) ? 0f : Estilos.TextoChico.CalcSize(new GUIContent(sub)).x);
            GUI.Box(new Rect(24, 14, Mathf.Max(240f, anchoTitulo + 44f), 152), GUIContent.none, Estilos.Caja);
            GUI.Label(new Rect(42, 24, 360, 28), "NIVEL " + Niveles.Nombre(app.nivel).ToUpper(), Estilos.Color_(Estilos.Quien, Niveles.ColorDe(app.nivel)));
            GUI.Label(new Rect(40, 82, 900, 44), titulo, Estilos.Encabezado);
            if (!string.IsNullOrEmpty(sub)) GUI.Label(new Rect(42, 128, 900, 28), sub, Estilos.TextoChico);

            // Chips de votos (arriba a la derecha): punto + nombre · estado
            float x = W - 40;
            for (int i = r.panelIds.Length - 1; i >= 0; i--)
            {
                var id = r.panelIds[i];
                string v; r.votos.TryGetValue(id, out v);
                bool ahora = r.actual != null && r.actual.emisor == id;
                var c = Estilos.ColorVoto(v);
                string txt = Perfiles.Corto(id) + " · " + Estilos.TextoVoto(v);
                float ancho = Estilos.Chip.CalcSize(new GUIContent(txt)).x + 62;
                var rect = new Rect(x - ancho, 82, ancho, 46);
                GUI.Box(rect, GUIContent.none, EstiloChip(v == null ? (ahora ? Estilos.Calido : Estilos.Linea) : c, ahora));
                Estilos.Punto(new Rect(rect.x + 20, rect.y + 17, 12, 12), c);
                GUI.Label(new Rect(rect.x + 42, rect.y, ancho - 50, rect.height), txt, Estilos.Color_(Estilos.Chip, v == null ? Estilos.Tenue : Color.Lerp(c, Color.white, 0.55f)));
                x -= ancho + 10;
            }

            // Tarjeta del tema: la misma lámina de la pantalla grande, siempre visible aunque la cámara esté en un tiburón
            var pant = app.estudio ? app.estudio.pantalla : null;
            bool enCerca = app.camara && app.camara.planoActual != Plano.General && app.camara.planoActual != Plano.Pantalla;
            if (!mostrarPanel && !app.mostrarHistorial && enCerca && pant && pant.Imagen && r.actual != null)
            {
                var estCuerpo = new GUIStyle(Estilos.TextoChico) { fontSize = 18, wordWrap = true };
                float altoCuerpo = estCuerpo.CalcHeight(new GUIContent(pant.Cuerpo ?? ""), 334);
                var caja = new Rect(W - 560, 146, 520, Mathf.Max(176, 92 + altoCuerpo));
                GUI.Box(caja, GUIContent.none, Estilos.CajaClara);
                GUI.DrawTexture(new Rect(caja.x + 14, caja.y + 18, 140, 140), pant.Imagen, ScaleMode.ScaleToFit);
                GUI.Label(new Rect(caja.x + 172, caja.y + 16, 330, 24), (pant.Sobre ?? "").ToUpper(), new GUIStyle(Estilos.Quien) { fontSize = 17 });
                GUI.Label(new Rect(caja.x + 172, caja.y + 42, 330, 36), pant.Encabezado, new GUIStyle(Estilos.Subtitulo) { fontSize = 27, wordWrap = false, clipping = TextClipping.Clip });
                GUI.Label(new Rect(caja.x + 172, caja.y + 80, 334, altoCuerpo + 6), pant.Cuerpo ?? "", estCuerpo);
            }

            // Tu panel (se abre con el botón «Tu panel»)
            if (mostrarPanel && r.panelInfo.Length > 0)
            {
                GUILayout.BeginArea(new Rect(40, 176, 480, 440), Estilos.CajaClara);
                GUILayout.Label("TU PANEL", Estilos.Quien);
                foreach (var p in r.panelInfo)
                {
                    GUILayout.Space(6);
                    GUILayout.Label(p.nombre, new GUIStyle(Estilos.Texto) { font = Estilos.Semi });
                    GUILayout.Label(p.motivo, Estilos.TextoChico);
                }
                GUILayout.EndArea();
            }

            // Resultado grande
            if (!string.IsNullOrEmpty(r.resultado))
            {
                bool trato = r.resultado == "TRATO";
                GUI.Label(new Rect(0, 170, W, 110), trato ? "TRATO" : "SIN TRATO", Estilos.Color_(Estilos.Grande, trato ? Estilos.Dentro : Estilos.Fuera));
            }

            // Subtítulo de quien habla (abajo), oculto mientras el usuario responde. Los textos largos avanzan por frases
            // al ritmo de la voz para que siempre se lean completos y con letra grande.
            bool modal = r.EnEspera && app.cliente.pendiente != null;
            if (r.actual != null && r.actual.tipo != "REPORTE" && !modal && !string.IsNullOrEmpty(r.actual.texto))
            {
                var e = r.actual;
                bool habla = e.emisor != null && e.emisor.StartsWith("tibur");
                bool insignia = e.tipo == "VOTO" && !string.IsNullOrEmpty(e.decision);
                float ancho = Mathf.Min(1440, W - 80), xCaja = W / 2 - ancho / 2;
                var cuerpo = new GUIStyle(Estilos.Texto) { fontSize = 29 };
                float anchoTexto = ancho - 56 - (insignia ? 280 : 0);
                bool respondiendo = r.turnoUsuario != null;
                var paginas = respondiendo ? new List<string> { e.texto } : Paginas(e.texto, insignia ? 190 : 240);
                int pagina = respondiendo ? 0 : PaginaActual(paginas, r.Progreso);
                string visible = paginas[pagina];
                float altoTexto = Mathf.Max(80, cuerpo.CalcHeight(new GUIContent(visible), anchoTexto));
                float alto = Mathf.Max(insignia ? 130 : 0, 22 + 28 + 10 + altoTexto + 22);
                float fondo = H - 120 - (r.turnoUsuario != null ? BurbujaRespuesta.Alto + 16 : 0);
                var caja = new Rect(xCaja, fondo - alto, ancho, alto);
                GUI.Box(caja, GUIContent.none, habla ? Estilos.CajaCalida : Estilos.Caja);
                if (paginas.Count > 1)
                    for (int i = 0; i < paginas.Count; i++)
                        Estilos.Punto(new Rect(caja.xMax - 30 - (paginas.Count - i) * 18, caja.yMax - 20, 10, 10), i == pagina ? Estilos.Calido : new Color(1, 1, 1, 0.28f));
                string quien = NombreLargo(app, e.emisor).ToUpper();
                string extra = e.tipo == "PREGUNTA" ? "  ·  pregunta" : e.tipo == "REPLICA" ? "  ·  réplica" + (!string.IsNullOrEmpty(e.postura) ? " (" + e.postura + ")" : "") :
                               e.tipo == "VOTO" ? "  ·  voto" : e.tipo == "RESPUESTA" ? "  ·  respuesta" : "";
                var estQuien = Estilos.Color_(Estilos.Quien, e.emisor == "emprendedor" ? Estilos.Suave : Estilos.Calido);
                float anchoQuien = estQuien.CalcSize(new GUIContent(quien)).x;
                GUI.Label(new Rect(caja.x + 28, caja.y + 22, anchoQuien, 28), quien, estQuien);
                GUI.Label(new Rect(caja.x + 28 + anchoQuien, caja.y + 22, 400, 28), extra, Estilos.Etiqueta);
                GUI.Label(new Rect(caja.x + 28, caja.y + 22 + 28 + 10, anchoTexto, altoTexto), visible, cuerpo);
                if (insignia)
                {
                    var c = Estilos.ColorVoto(e.decision);
                    string t = e.decision == "invertir" ? "Estoy dentro" : e.decision == "contraoferta" ? "Contraoferta" : "Estoy fuera";
                    GUI.Label(new Rect(caja.xMax - 28 - 240, caja.y + 22, 240, 60), t, EstiloInsignia(c));
                }
            }
        }
    }

    // ====================== Respuestas del usuario ======================
    public class PanelRespuestas
    {
        public readonly Dictionary<string, string> texto = new Dictionary<string, string>();
        int rondaActual = -1;
        Vector2 scroll;
        string grabandoPara, transcribiendo;

        public void Dibujar(AppNeuralHub app, Pendiente pend, float W, float H)
        {
            if (pend.ronda != rondaActual) { texto.Clear(); rondaActual = pend.ronda; }
            GUILayout.BeginArea(new Rect(W / 2 - 620, 130, 1240, H - 330), Estilos.Caja);
            GUILayout.Label("Ronda " + pend.ronda + " de 3: responde a los tiburones", Estilos.Subtitulo);
            GUILayout.Label("Escribe con tus propios datos. Si no sabes algo, dilo: también cuenta.", Estilos.TextoChico);
            GUILayout.Space(10);
            scroll = GUILayout.BeginScrollView(scroll);
            bool todas = true;
            foreach (var q in pend.preguntas)
            {
                GUILayout.Space(8);
                GUILayout.Label(Perfiles.Corto(q.tiburon_id).ToUpper() + " PREGUNTA", Estilos.Quien);
                GUILayout.Label("«" + q.texto + "»", Estilos.Texto);
                GUILayout.Space(6);
                string v; texto.TryGetValue(q.tiburon_id, out v);
                v = GUILayout.TextArea(v ?? "", Estilos.Area, GUILayout.Height(96));
                if (v.Length > 600) v = v.Substring(0, 600);
                texto[q.tiburon_id] = v;
                GUILayout.BeginHorizontal();
                GUILayout.Label(v.Length + " / 600", new GUIStyle(Estilos.TextoChico) { fontSize = 16 }, GUILayout.Width(120));
                GUILayout.FlexibleSpace();
                bool este = grabandoPara == q.tiburon_id;
                if (transcribiendo == q.tiburon_id) GUILayout.Label("Transcribiendo tu voz…", Estilos.Etiqueta);
                else if (!GrabadorVoz.HayMicrofono) GUILayout.Label("Sin micrófono", Estilos.TextoChico);
                else if (GUILayout.Button(este ? "■ Detener (" + (Time.realtimeSinceStartup - GrabadorVoz.inicio).ToString("0") + " s)" : "● Dictar con voz",
                                          este ? Estilos.BotonPrimario : Estilos.BotonChico, GUILayout.Width(230), GUILayout.Height(34)))
                {
                    if (este)
                    {
                        var wav = GrabadorVoz.Detener(); grabandoPara = null;
                        string id = q.tiburon_id; transcribiendo = id;
                        if (wav != null)
                            app.StartCoroutine(app.cliente.Transcribir(wav, t =>
                            {
                                transcribiendo = null;
                                if (!string.IsNullOrEmpty(t)) { string prev; texto.TryGetValue(id, out prev); texto[id] = ((prev ?? "") + " " + t).Trim(); }
                            }));
                        else transcribiendo = null;
                    }
                    else if (grabandoPara == null && GrabadorVoz.Iniciar()) grabandoPara = q.tiburon_id;
                }
                GUILayout.EndHorizontal();
                if (string.IsNullOrWhiteSpace(v)) todas = false;
            }
            GUILayout.EndScrollView();
            GUILayout.Space(8);
            GUI.enabled = todas;
            if (GUILayout.Button("Enviar respuestas", Estilos.BotonPrimario, GUILayout.Height(56)))
                app.StartCoroutine(app.cliente.EnviarRespuestas(new Dictionary<string, string>(texto)));
            GUI.enabled = true;
            GUILayout.EndArea();
        }
    }

    // ====================== Negociación y condiciones ======================
    public class PanelNegociacion
    {
        string sMonto = "", sPct = "";
        bool iniciado;

        public void Dibujar(AppNeuralHub app, Pendiente pend, float W, float H)
        {
            GUILayout.BeginArea(new Rect(W / 2 - 600, 140, 1200, H - 360), Estilos.Caja);
            if (pend.tipo == "CONDICIONES")
            {
                GUILayout.Label(pend.puede_original ? "¡Hay trato! Elige las condiciones finales" : "Tienes contraofertas: tú decides", Estilos.Subtitulo);
                GUILayout.Label(pend.puede_original
                    ? "Dos o más tiburones aceptan tu oferta. Puedes mantenerla o tomar una contraoferta."
                    : "Tu oferta original no tiene mayoría: acepta una contraoferta o sal sin trato.", Estilos.TextoChico);
                GUILayout.Space(14);
                if (pend.puede_original && GUILayout.Button("Mantener mi oferta: " + Perfiles.Mxn(pend.original.monto_mxn) + " por el " + pend.original.porcentaje.ToString("0.##") + " %", Estilos.Boton, GUILayout.Height(56)))
                    app.StartCoroutine(app.cliente.EnviarCondicion("original"));
                foreach (var c in pend.contraofertas)
                {
                    GUILayout.Space(8);
                    if (GUILayout.Button("Contraoferta de " + Perfiles.Corto(c.tiburon_id) + ": " + Perfiles.Mxn(c.monto_mxn) + " por el " + c.porcentaje.ToString("0.##") + " %", Estilos.Boton, GUILayout.Height(56)))
                        app.StartCoroutine(app.cliente.EnviarCondicion(c.tiburon_id));
                }
                if (!pend.puede_original)
                {
                    GUILayout.Space(16);
                    if (GUILayout.Button("Rechazar las contraofertas (salir sin trato)", Estilos.BotonChico, GUILayout.Height(50)))
                        app.StartCoroutine(app.cliente.EnviarCondicion("rechazar"));
                }
            }
            else
            {
                if (!iniciado && pend.oferta_actual != null)
                {
                    sMonto = pend.oferta_actual.monto_mxn.ToString("0", CultureInfo.InvariantCulture);
                    sPct = pend.oferta_actual.porcentaje.ToString("0.##", CultureInfo.InvariantCulture); iniciado = true;
                }
                GUILayout.Label("Negociación: solo 1 tiburón votó a favor", Estilos.Subtitulo);
                GUILayout.Label("Ajusta tu oferta o acepta una contraoferta. Los 3 tiburones votarán una última vez.", Estilos.TextoChico);
                GUILayout.Space(14);
                foreach (var c in pend.contraofertas)
                {
                    if (GUILayout.Button("Aceptar contraoferta de " + Perfiles.Corto(c.tiburon_id) + ": " + Perfiles.Mxn(c.monto_mxn) + " por el " + c.porcentaje.ToString("0.##") + " %", Estilos.Boton, GUILayout.Height(56)))
                    { app.StartCoroutine(app.cliente.EnviarNegociacion("aceptar_contraoferta", c.monto_mxn, c.porcentaje, c.tiburon_id)); iniciado = false; }
                    GUILayout.Space(8);
                }
                GUILayout.Space(10);
                GUILayout.Label("AJUSTAR MI OFERTA", Estilos.Quien);
                GUILayout.Space(6);
                GUILayout.BeginHorizontal();
                GUILayout.Label("Monto (MXN)", Estilos.Etiqueta, GUILayout.Width(130), GUILayout.Height(44));
                sMonto = GUILayout.TextField(sMonto, Estilos.Campo, GUILayout.Width(240), GUILayout.Height(44));
                GUILayout.Space(20);
                GUILayout.Label("Porcentaje (%)", Estilos.Etiqueta, GUILayout.Width(140), GUILayout.Height(44));
                sPct = GUILayout.TextField(sPct, Estilos.Campo, GUILayout.Width(120), GUILayout.Height(44));
                GUILayout.EndHorizontal();
                double m, p;
                bool ok = double.TryParse(sMonto, NumberStyles.Float, CultureInfo.InvariantCulture, out m) && double.TryParse(sPct, NumberStyles.Float, CultureInfo.InvariantCulture, out p) && m > 0 && p > 0 && p <= 100;
                p = 0; double.TryParse(sPct, NumberStyles.Float, CultureInfo.InvariantCulture, out p);
                GUILayout.Space(14);
                GUI.enabled = ok;
                if (GUILayout.Button("Enviar mi oferta ajustada", Estilos.BotonPrimario, GUILayout.Height(56)))
                { app.StartCoroutine(app.cliente.EnviarNegociacion("ajustar", m, p, null)); iniciado = false; }
                GUI.enabled = true;
                GUILayout.Space(8);
                if (GUILayout.Button("Mantener mi oferta", Estilos.Boton, GUILayout.Height(50)))
                { app.StartCoroutine(app.cliente.EnviarNegociacion("mantener", 0, 0, null)); iniciado = false; }
            }
            GUILayout.EndArea();
        }
    }

    // ====================== Resultado y reporte ======================
    public class PantallaResultado
    {
        Vector2 scroll;
        public void Dibujar(AppNeuralHub app, float W, float H)
        {
            var r = app.reproductor;
            var rep = r.reporte != null ? r.reporte.reporte : null;
            bool trato = r.resultado == "TRATO";
            GUILayout.BeginArea(new Rect(W / 2 - 660, 24, 1320, H - 48), Estilos.Caja);
            GUILayout.Label(trato ? "TRATO" : "SIN TRATO", Estilos.Color_(new GUIStyle(Estilos.Grande) { fontSize = 64 }, trato ? Estilos.Dentro : Estilos.Fuera));
            GUILayout.Label("Nivel " + Niveles.Nombre(app.nivel), Estilos.Color_(new GUIStyle(Estilos.TextoChico) { alignment = TextAnchor.MiddleCenter }, Niveles.ColorDe(app.nivel)));
            string modo = !r.enVivo ? "Modo agente empresario: tu agente respondió por ti." :
                          !app.cliente.ayuda ? "Modo en vivo: respondiste tú, sin ayuda." :
                          "Modo en vivo con ayuda: pediste " + app.sugerenciasPedidas + (app.sugerenciasPedidas == 1 ? " recomendación" : " recomendaciones") + " al agente empresario.";
            GUILayout.Label(modo, new GUIStyle(Estilos.TextoChico) { alignment = TextAnchor.MiddleCenter });
            if (trato && r.decisionFinal != null && r.decisionFinal.condiciones != null)
                GUILayout.Label("Condiciones finales: " + Perfiles.Mxn(r.decisionFinal.condiciones.monto_mxn) + " por el " + r.decisionFinal.condiciones.porcentaje.ToString("0.##") + " % de la empresa.",
                                new GUIStyle(Estilos.Texto) { alignment = TextAnchor.MiddleCenter });
            GUILayout.Space(10);
            scroll = GUILayout.BeginScrollView(scroll);
            Seccion("VOTO Y JUSTIFICACIÓN DE CADA TIBURÓN");
            foreach (var e in r.historial)
            {
                if (e.tipo != "VOTO") continue;
                GUILayout.Space(6);
                GUILayout.Label(Perfiles.Corto(e.emisor) + " · " + Estilos.TextoVoto(e.decision) + (e.votacion == 2 ? " (votación final)" : ""),
                                Estilos.Color_(new GUIStyle(Estilos.Texto) { font = Estilos.Semi }, Estilos.ColorVoto(e.decision)));
                GUILayout.Label(e.texto, Estilos.Color_(Estilos.TextoChico, Estilos.Suave));
            }
            if (rep != null)
            {
                Seccion("RETROALIMENTACIÓN POR TIBURÓN");
                foreach (var c in rep.por_tiburon)
                {
                    int n = Mathf.Clamp(c.calificacion, 1, 5);
                    GUILayout.Space(6);
                    GUILayout.Label(Perfiles.Corto(c.tiburon_id) + "   " + n + " de 5", new GUIStyle(Estilos.Texto) { font = Estilos.Semi });
                    GUILayout.Label("«" + c.frase_clave + "»  Necesitaría: " + c.necesitaria, Estilos.Color_(Estilos.TextoChico, Estilos.Suave));
                }
                Lista("FORTALEZAS", rep.fortalezas); Lista("ALERTAS", rep.alertas);
                Lista("PREGUNTAS QUE NO SUPISTE RESPONDER", rep.preguntas_sin_responder); Lista("SIGUIENTES PASOS", rep.siguientes_pasos);
                if (rep.criterios_no_evaluados != null && rep.criterios_no_evaluados.Length > 0)
                {
                    Seccion("CRITERIOS QUE EL PANEL NO EVALUÓ");
                    foreach (var c in rep.criterios_no_evaluados) GUILayout.Label("•  " + c.nombre + ": " + c.criterio, Estilos.Color_(Estilos.TextoChico, Estilos.Suave));
                }
                GUILayout.Space(14);
                GUILayout.Label(rep.aviso, Estilos.TextoChico);
            }
            GUILayout.EndScrollView();
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Ver historial completo", Estilos.Boton, GUILayout.Height(54))) app.mostrarHistorial = !app.mostrarHistorial;
            GUILayout.Space(10);
            if (GUILayout.Button("Repetir con este pitch", Estilos.Boton, GUILayout.Height(54))) app.Ir(Vista.Formulario);
            GUILayout.Space(10);
            if (GUILayout.Button("Nueva sesión", Estilos.BotonPrimario, GUILayout.Height(54))) app.IrAFormulario();
            GUILayout.Space(10);
            if (GUILayout.Button("Menú principal", Estilos.Boton, GUILayout.Height(54))) app.Ir(Vista.Inicio);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        static void Seccion(string titulo)
        {
            GUILayout.Space(18);
            GUILayout.Label(titulo, Estilos.Quien);
        }

        static void Lista(string titulo, string[] items)
        {
            if (items == null || items.Length == 0) return;
            Seccion(titulo);
            foreach (var i in items) GUILayout.Label("•  " + i, Estilos.Color_(Estilos.TextoChico, Estilos.Suave));
        }
    }

    // ====================== Lista de sesiones pasadas ======================
    public class ListaSesiones
    {
        public SesionListada[] sesiones;
        Vector2 scroll;
        public void Dibujar(AppNeuralHub app, float W, float H)
        {
            GUILayout.BeginArea(new Rect(W / 2 - 540, 120, 1080, H - 240), Estilos.Caja);
            GUILayout.Label("Historial de sesiones", Estilos.Encabezado);
            GUILayout.Space(12);
            scroll = GUILayout.BeginScrollView(scroll);
            if (sesiones == null || sesiones.Length == 0) GUILayout.Label("Todavía no hay sesiones guardadas.", Estilos.Texto);
            else foreach (var s in sesiones)
            {
                GUILayout.BeginHorizontal(Estilos.CajaClara);
                GUILayout.Label(s.negocio, new GUIStyle(Estilos.Texto) { font = Estilos.Semi }, GUILayout.Width(420), GUILayout.Height(44));
                GUILayout.Label(s.resultado + "  ·  " + s.estado, Estilos.Color_(Estilos.Texto, s.resultado == "TRATO" ? Estilos.Dentro : Estilos.Tenue), GUILayout.Width(300), GUILayout.Height(44));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Repetir sesión", Estilos.BotonChico, GUILayout.Width(190), GUILayout.Height(44))) app.RepetirSesion(s.sesion_id);
                GUILayout.EndHorizontal();
                GUILayout.Space(8);
            }
            GUILayout.EndScrollView();
            GUILayout.Space(10);
            if (GUILayout.Button("Volver", Estilos.Boton, GUILayout.Width(180), GUILayout.Height(54))) app.Ir(Vista.Inicio);
            GUILayout.EndArea();
        }
    }
}
