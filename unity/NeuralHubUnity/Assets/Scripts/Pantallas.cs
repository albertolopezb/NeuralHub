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
            GUILayout.BeginArea(new Rect(W / 2 - 430, 150, 860, 760), Estilos.Caja);
            GUILayout.Label("NeuralHub", Estilos.Titulo);
            GUILayout.Label("Simulador de panel de inversionistas con agentes de IA", Estilos.TextoChico);
            GUILayout.Space(20);
            GUILayout.Label("Practica tu pitch ante 3 tiburones elegidos de un catálogo de 6. Responde sus preguntas por escrito, mira cómo debaten entre ellos y recibe una decisión justificada con retroalimentación.", Estilos.Texto);
            GUILayout.Space(24);
            if (GUILayout.Button("Nueva sesión", Estilos.BotonPrimario, GUILayout.Height(64))) app.IrAFormulario();
            GUILayout.Space(10);
            if (GUILayout.Button("Historial de sesiones", Estilos.Boton, GUILayout.Height(54))) app.AbrirSesiones();
            GUILayout.Space(22);
            bool mock = GUILayout.Toggle(app.cliente.usarMock, "  Usar sesión de ejemplo (sin conexión)", GUILayout.Height(30));
            if (mock != app.cliente.usarMock) app.FijarMock(mock);
            if (!app.cliente.usarMock)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Servicio:", Estilos.Etiqueta, GUILayout.Width(90));
                app.cliente.urlBase = GUILayout.TextField(app.cliente.urlBase, Estilos.Campo, GUILayout.Width(420));
                if (GUILayout.Button("Probar", Estilos.BotonChico, GUILayout.Width(110))) app.ProbarConexion();
                GUILayout.EndHorizontal();
            }
            GUILayout.Space(10);
            var c = app.cliente.usarMock ? "Modo sin conexión: se reproduce una sesión real guardada." :
                    app.cliente.conectado ? "Conectado al servicio de agentes." : "Sin conexión con el servicio. Actívalo o usa la sesión de ejemplo.";
            var estilo = new GUIStyle(Estilos.TextoChico);
            estilo.normal.textColor = app.cliente.usarMock || app.cliente.conectado ? Estilos.Verde * 1.6f : Estilos.Ambar;
            GUILayout.Label(c, estilo);
            if (!string.IsNullOrEmpty(app.mensaje)) GUILayout.Label(app.mensaje, Estilos.TextoChico);
            GUILayout.FlexibleSpace();
            GUILayout.Label("Simulación educativa; no es asesoría financiera.", Estilos.TextoChico);
            GUILayout.EndArea();
        }
    }

    // ====================== Formulario del pitch ======================
    public class FormularioPitch
    {
        public PitchDto p = new PitchDto { sector = "tecnologia", etapa = "idea", equipo_dedicacion = "tiempo completo", propiedad_intelectual = "ninguna" };
        public string sFund = "1", sMonto = "", sPct = "", sVentas = "", sMargen = "", sGasto = "";
        Vector2 scroll;
        bool menuEjemplos;
        static readonly string[] Sectores = { "tecnologia", "alimentos_y_bebidas", "consumo_y_moda", "salud", "educacion", "sustentabilidad_y_energia", "servicios_profesionales" };
        static readonly string[] Etapas = { "idea", "prototipo", "primeras_ventas", "crecimiento" };
        const int Max = 300;

        static double Num(string s, double def = -1)
        {
            double v; return double.TryParse((s ?? "").Replace(",", "").Replace("$", "").Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out v) ? v : def;
        }

        string Texto(string etiqueta, string valor, bool area = false, float alto = 36)
        {
            GUILayout.Label(etiqueta, Estilos.Etiqueta);
            string nuevo = area ? GUILayout.TextArea(valor ?? "", Estilos.Area, GUILayout.Height(alto + 30)) : GUILayout.TextField(valor ?? "", Estilos.Campo, GUILayout.Height(alto));
            return nuevo.Length > Max ? nuevo.Substring(0, Max) : nuevo;
        }

        string Elegir(string etiqueta, string valor, string[] opciones)
        {
            GUILayout.Label(etiqueta, Estilos.Etiqueta);
            GUILayout.BeginHorizontal();
            foreach (var o in opciones)
                if (GUILayout.Toggle(valor == o, o.Replace('_', ' '), Estilos.Seleccion, GUILayout.Height(34))) valor = o;
            GUILayout.EndHorizontal();
            return valor;
        }

        public void Cargar(PitchDto d)
        {
            p = d;
            sFund = d.equipo_fundadores.ToString();
            sMonto = d.monto_mxn.ToString("0", CultureInfo.InvariantCulture); sPct = d.porcentaje.ToString("0.##", CultureInfo.InvariantCulture);
            sVentas = d.ventas_12m_mxn > 0 ? d.ventas_12m_mxn.ToString("0", CultureInfo.InvariantCulture) : "";
            sMargen = d.margen_bruto_pct > 0 ? d.margen_bruto_pct.ToString("0.##", CultureInfo.InvariantCulture) : "";
            sGasto = d.gasto_mensual_mxn > 0 ? d.gasto_mensual_mxn.ToString("0", CultureInfo.InvariantCulture) : "";
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
            return ClienteServicio.PitchAJson(p, p.ventas_12m_mxn >= 0 && sVentas != "", p.margen_bruto_pct >= 0 && sMargen != "", p.gasto_mensual_mxn >= 0 && sGasto != "");
        }

        public void Dibujar(AppNeuralHub app, float W, float H)
        {
            GUILayout.BeginArea(new Rect(W / 2 - 560, 30, 1120, H - 60), Estilos.Caja);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Tu pitch", Estilos.Subtitulo);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Cargar ejemplo ▾", Estilos.BotonChico, GUILayout.Width(220), GUILayout.Height(38))) menuEjemplos = !menuEjemplos;
            GUILayout.EndHorizontal();
            if (menuEjemplos && app.ejemplos != null)
            {
                foreach (var e in app.ejemplos)
                    if (GUILayout.Button(e.etiqueta, Estilos.Seleccion, GUILayout.Height(34))) { Cargar(e.pitch); menuEjemplos = false; }
            }
            scroll = GUILayout.BeginScrollView(scroll);
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
            GUILayout.Label("Equipo", Estilos.Subtitulo);
            sFund = Texto("Número de fundadores *", sFund);
            p.equipo_roles = Texto("Roles", p.equipo_roles);
            p.equipo_dedicacion = Elegir("Dedicación", p.equipo_dedicacion, new[] { "tiempo completo", "parcial" });
            p.equipo_experiencia = Texto("Experiencia relevante", p.equipo_experiencia);
            GUILayout.Label("Oferta", Estilos.Subtitulo);
            sMonto = Texto("Monto solicitado (MXN) *", sMonto);
            sPct = Texto("Porcentaje ofrecido (%) *", sPct);
            double monto = Num(sMonto), pct = Num(sPct);
            if (monto > 0 && pct > 0 && pct <= 100)
                GUILayout.Label("Valuación implícita: " + Perfiles.Mxn(monto / (pct / 100.0)), Estilos.Subtitulo);
            p.uso_fondos = Texto("Uso de los fondos *", p.uso_fondos);
            GUILayout.Label("Números clave (opcionales: déjalos vacíos si «Aún no lo sé»)", Estilos.Subtitulo);
            sVentas = Texto("Ventas de los últimos 12 meses (MXN)", sVentas);
            sMargen = Texto("Margen bruto (%)", sMargen);
            sGasto = Texto("Gasto mensual (MXN)", sGasto);
            GUILayout.EndScrollView();
            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Volver", Estilos.Boton, GUILayout.Width(180), GUILayout.Height(54))) app.Ir(Vista.Inicio);
            GUILayout.FlexibleSpace();
            GUILayout.Label(Completo() ? "" : "Completa los campos con *", Estilos.TextoChico, GUILayout.Width(300));
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
            GUILayout.BeginArea(new Rect(W / 2 - 700, 40, 1400, H - 80), Estilos.Caja);
            GUILayout.Label("Elige a tus 3 tiburones", Estilos.Subtitulo);
            GUILayout.Label("La app marcó a los 3 que más te servirían según los datos que te faltan. Puedes aceptarlos o cambiarlos (exactamente 3).", Estilos.TextoChico);
            if (app.recomendacion != null) GUILayout.Label("Valuación implícita de tu oferta: " + Perfiles.Mxn(app.recomendacion.valuacion_mxn), Estilos.Texto);
            GUILayout.Space(10);
            int col = 0;
            GUILayout.BeginHorizontal();
            var lista = app.recomendacion != null && app.recomendacion.ranking != null ? app.recomendacion.ranking : new RankingItem[0];
            foreach (var f in lista)
            {
                var info = app.Catalogo(f.tiburon_id);
                bool sel = elegidos.Contains(f.tiburon_id);
                GUILayout.BeginVertical(sel ? Estilos.Caja : Estilos.CajaClara, GUILayout.Width(440), GUILayout.Height(300));
                var c = Perfiles.Color(f.tiburon_id);
                Estilos.Rect(GUILayoutUtility.GetRect(10, 8, GUILayout.ExpandWidth(true)), c);
                GUILayout.Label((info != null ? info.nombre : f.tiburon_id) + (f.recomendado ? "   ★ recomendado" : ""), Estilos.Subtitulo);
                if (info != null) { GUILayout.Label(info.perfil, Estilos.Texto); GUILayout.Label("Criterio: " + info.criterio, Estilos.TextoChico); }
                GUILayout.Label("Por qué: " + f.motivo, Estilos.TextoChico);
                GUILayout.FlexibleSpace();
                bool nuevo = GUILayout.Toggle(sel, sel ? "  En mi panel" : "  Agregar al panel", Estilos.Seleccion, GUILayout.Height(36));
                if (nuevo && !sel && elegidos.Count < 3) elegidos.Add(f.tiburon_id);
                if (!nuevo && sel) elegidos.Remove(f.tiburon_id);
                GUILayout.EndVertical();
                col++;
                if (col % 3 == 0) { GUILayout.EndHorizontal(); GUILayout.Space(8); GUILayout.BeginHorizontal(); }
            }
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Volver", Estilos.Boton, GUILayout.Width(180), GUILayout.Height(54))) app.Ir(Vista.Formulario);
            GUILayout.FlexibleSpace();
            GUILayout.Label(elegidos.Count + " de 3 elegidos", Estilos.Texto, GUILayout.Width(220));
            GUI.enabled = elegidos.Count == 3;
            if (GUILayout.Button("Iniciar sesión", Estilos.BotonPrimario, GUILayout.Width(260), GUILayout.Height(54))) app.IniciarSesion(new List<string>(elegidos));
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }

    // ====================== Controles de reproducción ======================
    public class ControlReproduccion
    {
        public void Dibujar(AppNeuralHub app, float W)
        {
            var r = app.reproductor;
            GUILayout.BeginArea(new Rect(W / 2 - 560, 12, 1120, 56), Estilos.CajaClara);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("◀ Anterior", Estilos.BotonChico, GUILayout.Width(120), GUILayout.Height(36))) { app.Clic(); r.Anterior(); }
            if (GUILayout.Button(r.pausado ? "▶ Reanudar" : "❚❚ Pausa", Estilos.BotonChico, GUILayout.Width(130), GUILayout.Height(36))) { app.Clic(); r.AlternarPausa(); }
            if (GUILayout.Button("Siguiente ▶", Estilos.BotonChico, GUILayout.Width(130), GUILayout.Height(36))) { app.Clic(); r.Siguiente(); }
            if (GUILayout.Toggle(r.velocidad == 1f, "1x", Estilos.Seleccion, GUILayout.Width(50), GUILayout.Height(36))) r.FijarVelocidad(1f);
            if (GUILayout.Toggle(r.velocidad == 2f, "2x", Estilos.Seleccion, GUILayout.Width(50), GUILayout.Height(36))) r.FijarVelocidad(2f);
            GUILayout.Label("Turno " + r.Indice + "/" + r.Total, Estilos.Etiqueta, GUILayout.Width(120), GUILayout.Height(36));
            bool voz = GUILayout.Toggle(app.sonidos.vozActiva, "Voz", Estilos.Seleccion, GUILayout.Width(70), GUILayout.Height(36));
            if (voz != app.sonidos.vozActiva) { app.sonidos.vozActiva = voz; if (!voz) app.sonidos.DetenerVoz(); }
            bool sil = GUILayout.Toggle(app.sonidos.silencio, "Silencio", Estilos.Seleccion, GUILayout.Width(100), GUILayout.Height(36));
            if (sil != app.sonidos.silencio) app.sonidos.FijarSilencio(sil);
            if (GUILayout.Button("Historial", Estilos.BotonChico, GUILayout.Width(110), GUILayout.Height(36))) { app.Clic(); app.mostrarHistorial = !app.mostrarHistorial; }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Salir", Estilos.BotonChico, GUILayout.Width(80), GUILayout.Height(36))) app.Ir(Vista.Inicio);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }

    // ====================== Historial (turnos de la sesión) ======================
    public class PanelHistorial
    {
        Vector2 scroll;
        public void Dibujar(AppNeuralHub app, float W, float H)
        {
            GUILayout.BeginArea(new Rect(W - 760, 80, 730, H - 360), Estilos.Caja);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Conversación", Estilos.Subtitulo);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Cerrar ✕", Estilos.BotonChico, GUILayout.Width(110))) app.mostrarHistorial = false;
            GUILayout.EndHorizontal();
            GUILayout.Label("Pulsa «Ir» para volver a ver la escena desde ese turno.", Estilos.TextoChico);
            scroll = GUILayout.BeginScrollView(scroll);
            var recibidos = app.cliente.recibidos;
            int actual = app.reproductor.Indice - 1;
            for (int i = 0; i < recibidos.Count; i++)
            {
                var e = recibidos[i];
                if (e.tipo == "REPORTE") continue;
                bool visto = i <= actual;
                var c = Perfiles.Color(e.emisor);
                string etiqueta = (e.ronda > 0 ? "R" + e.ronda + " · " : "") + Perfiles.Corto(e.emisor) + " → " + Perfiles.Corto(e.destino) + " · " + e.tipo +
                                  (!string.IsNullOrEmpty(e.decision) ? " (" + e.decision + ")" : "");
                GUILayout.BeginHorizontal(i == actual ? Estilos.CajaClara : GUIStyle.none);
                var st = new GUIStyle(Estilos.Etiqueta); st.normal.textColor = visto ? Color.Lerp(c, Color.white, 0.5f) : new Color(1, 1, 1, 0.35f);
                GUILayout.Label((i == actual ? "▶ " : "") + etiqueta, st);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Ir", Estilos.BotonChico, GUILayout.Width(56), GUILayout.Height(28))) { app.Clic(); app.reproductor.IrA(i); }
                GUILayout.EndHorizontal();
                if (visto) GUILayout.Label(e.texto, Estilos.TextoChico);
                GUILayout.Space(4);
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }

    // ====================== Interfaz de la sesión (diálogo, ronda, marcador) ======================
    public class InterfazDebate
    {
        public bool mostrarPanel = true;

        public void Dibujar(AppNeuralHub app, float W, float H)
        {
            var r = app.reproductor;
            // Indicador de ronda
            string rt = r.actual == null ? "" : (r.ronda >= 4 ? "Negociación" : "Ronda " + Mathf.Max(1, r.ronda) + " de 3");
            if (r.actual != null && (r.actual.tipo == "PITCH" || r.actual.tipo == "PANEL")) rt = "Pitch";
            GUILayout.BeginArea(new Rect(24, 14, 330, 50), Estilos.CajaClara);
            GUILayout.Label(rt, Estilos.Subtitulo);
            GUILayout.EndArea();

            // Marcador de votos
            GUILayout.BeginArea(new Rect(W - 520, 76, 496, 130), Estilos.CajaClara);
            GUILayout.Label("Votos", Estilos.Etiqueta);
            GUILayout.BeginHorizontal();
            foreach (var id in r.panelIds)
            {
                string v; r.votos.TryGetValue(id, out v);
                var c = v == null ? new Color(1, 1, 1, 0.18f) : v == "invertir" ? Estilos.Verde : v == "contraoferta" ? Estilos.Ambar : Estilos.Rojo;
                var rect = GUILayoutUtility.GetRect(150, 70);
                Estilos.Rect(rect, c);
                GUI.Label(rect, Perfiles.Corto(id) + "\n" + (v ?? "—"), Estilos.Chip);
                GUILayout.Space(4);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            // Panel de tiburones con el motivo
            if (mostrarPanel && r.panelInfo.Length > 0)
            {
                GUILayout.BeginArea(new Rect(24, 76, 470, 420), Estilos.CajaClara);
                GUILayout.Label("Tu panel", Estilos.Etiqueta);
                foreach (var p in r.panelInfo)
                {
                    var st = new GUIStyle(Estilos.Texto) { fontStyle = FontStyle.Bold }; st.normal.textColor = Color.Lerp(Perfiles.Color(p.tiburon_id), Color.white, 0.4f);
                    GUILayout.Label(p.nombre, st);
                    GUILayout.Label(p.motivo, Estilos.TextoChico);
                    GUILayout.Space(4);
                }
                GUILayout.EndArea();
            }

            // Cuadro de diálogo con subtítulos
            if (r.actual != null && r.actual.tipo != "REPORTE")
            {
                var e = r.actual;
                GUILayout.BeginArea(new Rect(60, H - 250, W - 120, 220), Estilos.Caja);
                GUILayout.BeginHorizontal();
                var chip = new GUIStyle(Estilos.Chip) { alignment = TextAnchor.MiddleCenter };
                var rect = GUILayoutUtility.GetRect(260, 36);
                Estilos.Rect(rect, e.emisor == "emprendedor" ? Estilos.Azul : Perfiles.Color(e.emisor));
                GUI.Label(rect, Perfiles.Corto(e.emisor), chip);
                GUILayout.Label("  → " + Perfiles.Corto(e.destino) + "   ·   " + e.tipo + (!string.IsNullOrEmpty(e.postura) ? " (" + e.postura + ")" : ""), Estilos.Etiqueta);
                GUILayout.EndHorizontal();
                var sub = new GUIStyle(Estilos.Texto) { fontSize = 26 };
                GUILayout.Label(e.texto, sub);
                GUILayout.EndArea();
            }

            if (!string.IsNullOrEmpty(r.resultado))
            {
                bool trato = r.resultado == "TRATO";
                var st = new GUIStyle(Estilos.Grande); st.normal.textColor = trato ? Estilos.Verde * 1.7f : Estilos.Rojo * 1.5f;
                GUI.Label(new Rect(0, 230, W, 110), trato ? "TRATO" : "SIN TRATO", st);
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
            GUILayout.BeginArea(new Rect(W / 2 - 560, 70, 1120, H - 360), Estilos.Caja);
            GUILayout.Label("Ronda " + pend.ronda + " de 3: responde a los tiburones", Estilos.Subtitulo);
            scroll = GUILayout.BeginScrollView(scroll);
            bool todas = true;
            foreach (var q in pend.preguntas)
            {
                var st = new GUIStyle(Estilos.Etiqueta); st.normal.textColor = Color.Lerp(Perfiles.Color(q.tiburon_id), Color.white, 0.5f);
                GUILayout.Label(Perfiles.Corto(q.tiburon_id) + " pregunta:", st);
                GUILayout.Label(q.texto, Estilos.Texto);
                string v; texto.TryGetValue(q.tiburon_id, out v);
                v = GUILayout.TextArea(v ?? "", Estilos.Area, GUILayout.Height(90));
                if (v.Length > 600) v = v.Substring(0, 600);
                texto[q.tiburon_id] = v;
                GUILayout.BeginHorizontal();
                GUILayout.Label(v.Length + " / 600", Estilos.TextoChico, GUILayout.Width(120));
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
                GUILayout.Space(6);
            }
            GUILayout.EndScrollView();
            GUI.enabled = todas;
            if (GUILayout.Button("Enviar respuestas", Estilos.BotonPrimario, GUILayout.Height(52)))
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
            GUILayout.BeginArea(new Rect(W / 2 - 560, 90, 1120, H - 380), Estilos.Caja);
            if (pend.tipo == "CONDICIONES")
            {
                GUILayout.Label("¡Hay trato! Elige las condiciones finales", Estilos.Subtitulo);
                GUILayout.Label("Varios tiburones hicieron contraoferta. Tú decides con cuál te quedas.", Estilos.TextoChico);
                if (GUILayout.Button("Mantener mi oferta: " + Perfiles.Mxn(pend.original.monto_mxn) + " por el " + pend.original.porcentaje.ToString("0.##") + " %", Estilos.Boton, GUILayout.Height(52)))
                    app.StartCoroutine(app.cliente.EnviarCondicion("original"));
                foreach (var c in pend.contraofertas)
                    if (GUILayout.Button("Contraoferta de " + Perfiles.Corto(c.tiburon_id) + ": " + Perfiles.Mxn(c.monto_mxn) + " por el " + c.porcentaje.ToString("0.##") + " %", Estilos.Boton, GUILayout.Height(52)))
                        app.StartCoroutine(app.cliente.EnviarCondicion(c.tiburon_id));
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
                GUILayout.Space(8);
                foreach (var c in pend.contraofertas)
                    if (GUILayout.Button("Aceptar contraoferta de " + Perfiles.Corto(c.tiburon_id) + ": " + Perfiles.Mxn(c.monto_mxn) + " por el " + c.porcentaje.ToString("0.##") + " %", Estilos.Boton, GUILayout.Height(52)))
                    { app.StartCoroutine(app.cliente.EnviarNegociacion("aceptar_contraoferta", c.monto_mxn, c.porcentaje, c.tiburon_id)); iniciado = false; }
                GUILayout.Space(10);
                GUILayout.Label("Ajustar mi oferta", Estilos.Etiqueta);
                GUILayout.BeginHorizontal();
                GUILayout.Label("Monto (MXN)", Estilos.TextoChico, GUILayout.Width(120));
                sMonto = GUILayout.TextField(sMonto, Estilos.Campo, GUILayout.Width(220));
                GUILayout.Label("Porcentaje (%)", Estilos.TextoChico, GUILayout.Width(130));
                sPct = GUILayout.TextField(sPct, Estilos.Campo, GUILayout.Width(120));
                GUILayout.EndHorizontal();
                double m, p;
                bool ok = double.TryParse(sMonto, NumberStyles.Float, CultureInfo.InvariantCulture, out m) && double.TryParse(sPct, NumberStyles.Float, CultureInfo.InvariantCulture, out p) && m > 0 && p > 0 && p <= 100;
                p = 0; double.TryParse(sPct, NumberStyles.Float, CultureInfo.InvariantCulture, out p);
                GUI.enabled = ok;
                if (GUILayout.Button("Enviar mi oferta ajustada", Estilos.BotonPrimario, GUILayout.Height(52)))
                { app.StartCoroutine(app.cliente.EnviarNegociacion("ajustar", m, p, null)); iniciado = false; }
                GUI.enabled = true;
                if (GUILayout.Button("Mantener mi oferta", Estilos.Boton, GUILayout.Height(46)))
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
            GUILayout.BeginArea(new Rect(W / 2 - 640, 24, 1280, H - 48), Estilos.Caja);
            var st = new GUIStyle(Estilos.Grande) { fontSize = 60 }; st.normal.textColor = trato ? Estilos.Verde * 1.7f : Estilos.Rojo * 1.6f;
            GUILayout.Label(trato ? "TRATO" : "SIN TRATO", st);
            if (trato && r.decisionFinal != null && r.decisionFinal.condiciones != null)
                GUILayout.Label("Condiciones finales: " + Perfiles.Mxn(r.decisionFinal.condiciones.monto_mxn) + " por el " + r.decisionFinal.condiciones.porcentaje.ToString("0.##") + " % de la empresa.", new GUIStyle(Estilos.Subtitulo) { alignment = TextAnchor.MiddleCenter });
            scroll = GUILayout.BeginScrollView(scroll);
            GUILayout.Label("Voto y justificación de cada tiburón", Estilos.Subtitulo);
            foreach (var e in r.historial)
            {
                if (e.tipo != "VOTO") continue;
                var c = e.decision == "invertir" ? Estilos.Verde * 1.6f : e.decision == "contraoferta" ? Estilos.Ambar : Estilos.Rojo * 1.5f;
                var s = new GUIStyle(Estilos.Etiqueta); s.normal.textColor = c;
                GUILayout.Label(Perfiles.Corto(e.emisor) + " · " + e.decision.ToUpper() + (e.votacion == 2 ? " (votación final)" : ""), s);
                GUILayout.Label(e.texto, Estilos.TextoChico);
            }
            if (rep != null)
            {
                GUILayout.Space(10);
                GUILayout.Label("Retroalimentación por tiburón", Estilos.Subtitulo);
                foreach (var c in rep.por_tiburon)
                {
                    GUILayout.Label(Perfiles.Corto(c.tiburon_id) + ": " + new string('★', Mathf.Clamp(c.calificacion, 1, 5)) + new string('☆', 5 - Mathf.Clamp(c.calificacion, 1, 5)), Estilos.Etiqueta);
                    GUILayout.Label("«" + c.frase_clave + "»  Necesitaría: " + c.necesitaria, Estilos.TextoChico);
                }
                Lista("Fortalezas", rep.fortalezas); Lista("Alertas", rep.alertas);
                Lista("Preguntas que no supiste responder", rep.preguntas_sin_responder); Lista("Siguientes pasos", rep.siguientes_pasos);
                if (rep.criterios_no_evaluados != null && rep.criterios_no_evaluados.Length > 0)
                {
                    GUILayout.Label("Criterios que el panel no evaluó", Estilos.Subtitulo);
                    foreach (var c in rep.criterios_no_evaluados) GUILayout.Label("• " + c.nombre + ": " + c.criterio, Estilos.TextoChico);
                }
                GUILayout.Label(rep.aviso, Estilos.TextoChico);
            }
            GUILayout.EndScrollView();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Ver historial completo", Estilos.Boton, GUILayout.Height(52))) app.mostrarHistorial = !app.mostrarHistorial;
            if (GUILayout.Button("Repetir con este pitch", Estilos.Boton, GUILayout.Height(52))) app.Ir(Vista.Formulario);
            if (GUILayout.Button("Nueva sesión", Estilos.BotonPrimario, GUILayout.Height(52))) app.IrAFormulario();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        static void Lista(string titulo, string[] items)
        {
            if (items == null || items.Length == 0) return;
            GUILayout.Label(titulo, Estilos.Subtitulo);
            foreach (var i in items) GUILayout.Label("• " + i, Estilos.TextoChico);
        }
    }

    // ====================== Lista de sesiones pasadas ======================
    public class ListaSesiones
    {
        public SesionListada[] sesiones;
        public void Dibujar(AppNeuralHub app, float W, float H)
        {
            GUILayout.BeginArea(new Rect(W / 2 - 520, 120, 1040, H - 240), Estilos.Caja);
            GUILayout.Label("Historial de sesiones", Estilos.Subtitulo);
            if (sesiones == null || sesiones.Length == 0) GUILayout.Label("Todavía no hay sesiones guardadas.", Estilos.Texto);
            else foreach (var s in sesiones)
            {
                GUILayout.BeginHorizontal(Estilos.CajaClara);
                GUILayout.Label(s.negocio + "  ·  " + s.resultado + "  ·  " + s.estado, Estilos.Texto, GUILayout.Width(700));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Repetir sesión", Estilos.BotonChico, GUILayout.Width(190), GUILayout.Height(40))) app.RepetirSesion(s.sesion_id);
                GUILayout.EndHorizontal();
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Volver", Estilos.Boton, GUILayout.Width(180), GUILayout.Height(50))) app.Ir(Vista.Inicio);
            GUILayout.EndArea();
        }
    }
}
