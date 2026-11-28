using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NeuralHub
{
    public enum Vista { Inicio, Formulario, Panel, Modo, Sesion, Resultado, Sesiones, Conocer }

    /// <summary>Controlador de la aplicación: navega entre pantallas y conecta el servicio, el reproductor y el estudio.</summary>
    public class AppNeuralHub : MonoBehaviour
    {
        public ClienteServicio cliente;
        public ReproductorSesion reproductor;
        public Estudio estudio;
        public CamaraEstudio camara;
        public Sonidos sonidos;
        public PantallasDatos datos;
        bool vivoAuto;
        readonly List<string> latencias = new List<string>();

        public void Clic() { if (sonidos) sonidos.Efecto("clic", 0.35f); }

        static bool Arg(string a) { return Array.IndexOf(Environment.GetCommandLineArgs(), a) >= 0; }

        public Vista vista = Vista.Inicio;
        public bool mostrarHistorial;
        public bool ocultarUI;
        public string mensaje = "";
        public EjemploItem[] ejemplos;
        public string nivel = "medio";     // facil | medio | dificil
        public readonly BarraRondas barra = new BarraRondas();
        public readonly IntroPrograma intro = new IntroPrograma();
        public readonly FinalPrograma final_ = new FinalPrograma();
        Evento decisionVista;
        public readonly ConocerTiburones conocer = new ConocerTiburones();
        public TiburonInfo[] catalogo;
        public Recomendacion recomendacion;

        // Modo de simulación: agente empresario (autopiloto) o en vivo (el usuario responde, con ayuda opcional)
        public bool modoEnVivo;
        public bool ayudaActiva = true;
        public int sugerenciasPedidas;
        List<string> panelElegido;

        public readonly MenuInicio menu = new MenuInicio();
        public readonly FormularioPitch formulario = new FormularioPitch();
        public readonly EleccionPanel eleccion = new EleccionPanel();
        public readonly ControlReproduccion controles = new ControlReproduccion();
        public readonly PanelHistorial panelHistorial = new PanelHistorial();
        public readonly InterfazDebate interfaz = new InterfazDebate();
        public readonly PanelRespuestas respuestas = new PanelRespuestas();
        public readonly PanelNegociacion negociacion = new PanelNegociacion();
        public readonly PantallaResultado resultado = new PantallaResultado();
        public readonly ListaSesiones listaSesiones = new ListaSesiones();
        public readonly SeleccionModo seleccionModo = new SeleccionModo();
        public readonly BurbujaRespuesta burbuja = new BurbujaRespuesta();

        string pitchJson;
        float esperaResultado;

        void Start()
        {
            // 30 cuadros por segundo: sin límite, la gráfica integrada trabaja al máximo y al grabar la pantalla la computadora se traba
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 30;
            cliente.alError = m => mensaje = m;
            foreach (var a in Environment.GetCommandLineArgs())
                if (a == "-nh-mock" || a == "-nh-capturar" || (a == "-nh-video" && !Arg("-nh-vivo"))) cliente.usarMock = true;
            for (int i = 0; i < Environment.GetCommandLineArgs().Length - 1; i++)
                if (Environment.GetCommandLineArgs()[i] == "-nh-url") cliente.urlBase = Environment.GetCommandLineArgs()[i + 1];
            ProbarConexion();
            CargarCatalogos();
            foreach (var a in Environment.GetCommandLineArgs()) if (a == "-nh-capturar") { cliente.usarMock = true; StartCoroutine(Capturas()); }
            if (Arg("-nh-vivo")) { vivoAuto = true; modoEnVivo = true; ayudaActiva = true; StartCoroutine(RespondedorAutomatico()); }
            if (Arg("-nh-video")) StartCoroutine(Video());
            if (Arg("-nh-final")) { cliente.usarMock = true; StartCoroutine(PruebaFinal()); }
            if (Arg("-nh-ui")) StartCoroutine(PruebaPantallas());
        }

        // ---------- navegación ----------
        /// <summary>Voz de la presentación de un tiburón (audios incluidos en la aplicación).</summary>
        int pedidoVoz;      // si cambias de tiburón antes de que cargue su audio, el anterior ya no suena
        public void EscucharPresentacion(string id) { StartCoroutine(ReproducirArchivo("presentaciones/" + id + ".wav", ++pedidoVoz)); }
        public void CallarPresentacion() { pedidoVoz++; if (sonidos) sonidos.DetenerVoz(); }

        IEnumerator ReproducirArchivo(string relativo, int pedido)
        {
            var uri = new Uri(System.IO.Path.Combine(Application.streamingAssetsPath, "audio/" + relativo)).AbsoluteUri;
            using (var req = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.WAV))
            {
                yield return req.SendWebRequest();
                if (pedido == pedidoVoz && req.result == UnityEngine.Networking.UnityWebRequest.Result.Success && sonidos)
                    sonidos.Voz(UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(req), 1f, 0);
            }
        }

        public string NombreEmpresa()
        {
            return reproductor.pitchActual != null && !string.IsNullOrEmpty(reproductor.pitchActual.nombre) ? reproductor.pitchActual.nombre : "tu empresa";
        }

        public void Ir(Vista v)
        {
            if (vista == Vista.Sesion && v != Vista.Sesion && sonidos) sonidos.Callar();   // salir de la sesión también calla
            if (v != Vista.Sesion) { intro.activa = false; final_.Saltar(); }
            vista = v; mostrarHistorial = false;
            if (v != Vista.Sesion && v != Vista.Resultado)
            {
                reproductor.Detener(); camara.Cambiar(Plano.General, null);
                if (estudio && estudio.letreroResultado) estudio.letreroResultado.text = "";
            }
        }

        public void FijarMock(bool mock) { cliente.usarMock = mock; mensaje = ""; ProbarConexion(); CargarCatalogos(); }

        public void ProbarConexion() { StartCoroutine(cliente.ProbarConexion()); }

        public void CargarCatalogos()
        {
            StartCoroutine(cliente.ObtenerTiburones(t => catalogo = t));
            StartCoroutine(cliente.ObtenerEjemplos(e => ejemplos = e));
        }

        public TiburonInfo Catalogo(string id)
        {
            if (catalogo == null) return null;
            foreach (var t in catalogo) if (t.id == id) return t;
            return null;
        }

        public void IrAFormulario() { mensaje = ""; if (ejemplos == null || catalogo == null) CargarCatalogos(); Ir(Vista.Formulario); }

        public void IrAEleccionPanel(string json)
        {
            pitchJson = json; mensaje = "";
            StartCoroutine(cliente.Recomendar(json, r => { recomendacion = r; eleccion.PrecargarRecomendados(r); Ir(Vista.Panel); }));
        }

        /// <summary>Después de elegir el panel: pantalla para elegir el modo de simulación.</summary>
        /// <summary>Desde el inicio: practica con el emprendedor de ejemplo del nivel elegido.</summary>
        public void ElegirNivel(string n)
        {
            nivel = n;
            var e = Niveles.Ejemplo(ejemplos, n);
            if (e != null) formulario.Cargar(e.pitch);
            IrAFormulario();
        }

        public void IrAModo(List<string> ids) { panelElegido = new List<string>(ids); mensaje = ""; Ir(Vista.Modo); }

        public void IniciarSesionElegida() { if (panelElegido != null) IniciarSesion(panelElegido); }

        public bool iniciando;      // se pidió la sesión y el servicio aún no contesta

        public void IniciarSesion(List<string> ids)
        {
            if (iniciando) return;
            iniciando = true;
            mensaje = "";
            bool enVivo = modoEnVivo, ayuda = modoEnVivo && ayudaActiva;
            // Mismo orden del catálogo para que el panel quede estable
            var panel = new List<string>();
            foreach (var f in recomendacion.ranking) if (ids.Contains(f.tiburon_id)) panel.Add(f.tiburon_id);
            StartCoroutine(cliente.CrearSesion(pitchJson, panel.ToArray(), enVivo, ayuda, nivel, ok =>
            {
                iniciando = false;
                if (!ok) return;
                sugerenciasPedidas = 0;
                barra.Reiniciar();
                final_.Reiniciar();
                var datosPitch = formulario.p;
                // Los 3 tiburones elegidos ya se sientan (en penumbra) mientras el emprendedor presenta su pitch
                var nombres = new List<string>();
                foreach (var id in panel) { var info = Catalogo(id); nombres.Add(info != null ? info.nombre : id); }
                reproductor.Iniciar(); reproductor.enVivo = enVivo;
                estudio.IniciarSesion(panel.ToArray(), nombres.ToArray()); Ir(Vista.Sesion);
                intro.Iniciar(this, datosPitch.nombre, datosPitch.sector, datosPitch.problema, panel.ToArray());
            }));
        }

        /// <summary>Modo en vivo con ayuda: pide al agente empresario una respuesta sugerida para el tiburón en turno.</summary>
        public void PedirSugerencia(string tiburonId, string borrador, Action<string> listo)
        {
            sugerenciasPedidas++;
            StartCoroutine(cliente.PedirSugerencia(tiburonId, borrador, listo));
        }

        public void AbrirSesiones()
        {
            StartCoroutine(cliente.ListarSesiones(s => { listaSesiones.sesiones = s; Ir(Vista.Sesiones); }));
        }

        public void RepetirSesion(string id)
        {
            StartCoroutine(cliente.CargarSesionPasada(id, ok => { if (ok) { reproductor.Iniciar(); reproductor.enVivo = false; estudio.IniciarSesion(new string[0], null); estudio.Preparar(); Ir(Vista.Sesion); } }));
        }

        void Update()
        {
            intro.Actualizar(this);
            final_.Actualizar();
            // Al llegar la decisión: celebración o hundimiento
            var actual = reproductor.actual;
            if (vista == Vista.Sesion && actual != null && actual.tipo == "DECISION" && actual != decisionVista)
            {
                decisionVista = actual;
                bool trato = actual.resultado == "TRATO";
                string cond = trato && actual.condiciones != null
                    ? Perfiles.Mxn(actual.condiciones.monto_mxn) + " por el " + actual.condiciones.porcentaje.ToString("0.##") + " % de la empresa" : "";
                final_.Resultado(this, trato, NombreEmpresa(), cond);
            }
            // En vivo: cuando el usuario ya respondió las preguntas que el servicio espera, se envían juntas
            var p = cliente.pendiente;
            if (vista == Vista.Sesion && reproductor.enVivo && p != null && p.tipo == "RESPUESTAS" && p.preguntas != null &&
                reproductor.rondaRespuestas == p.ronda)
            {
                var envio = new Dictionary<string, string>();
                string clave = p.ronda + "|";
                foreach (var q in p.preguntas)
                {
                    clave += q.tiburon_id + ",";
                    string txt; if (reproductor.respuestasRonda.TryGetValue(q.tiburon_id, out txt)) envio[q.tiburon_id] = txt;
                }
                if (envio.Count == p.preguntas.Length && clave != reproductor.enviada)
                {
                    reproductor.enviada = clave;
                    StartCoroutine(cliente.EnviarRespuestas(envio));
                }
            }

            if (vista == Vista.Sesion && reproductor.reporteListo && reproductor.EnEspera && !final_.cierreVisto && !final_.Activo)
                final_.Cerrar(this, NombreEmpresa());
            if (vista == Vista.Sesion && reproductor.reporteListo && reproductor.EnEspera && !final_.Activo)
            {
                esperaResultado += Time.deltaTime;
                if (esperaResultado > 1.2f) { esperaResultado = 0; Ir(Vista.Resultado); }
            }
            else esperaResultado = 0;
        }

        // ---------- dibujo ----------
        void OnGUI()
        {
            Estilos.Iniciar();
            float s = Screen.height / 1080f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1));
            float W = Screen.width / s, H = 1080f;
            if (ocultarUI) return;

            if (vista == Vista.Inicio || vista == Vista.Formulario || vista == Vista.Panel || vista == Vista.Modo || vista == Vista.Sesiones)
                Estilos.Rect(new Rect(0, 0, W, H), new Color(Estilos.Fondo.r, Estilos.Fondo.g, Estilos.Fondo.b, 0.55f));

            switch (vista)
            {
                case Vista.Inicio: menu.Dibujar(this, W, H); break;
                case Vista.Formulario: formulario.Dibujar(this, W, H); break;
                case Vista.Panel: eleccion.Dibujar(this, W, H); break;
                case Vista.Modo: seleccionModo.Dibujar(this, W, H); break;
                case Vista.Sesiones: listaSesiones.Dibujar(this, W, H); break;
                case Vista.Conocer: conocer.Dibujar(this, W, H); break;
                case Vista.Sesion:
                    if (intro.activa) { intro.Dibujar(this, W, H); break; }
                    if (final_.Activo) { final_.Dibujar(this, W, H); break; }
                    interfaz.Dibujar(this, W, H);
                    barra.Dibujar(reproductor, W, !mostrarHistorial);
                    controles.Dibujar(this, W, H);
                    if (reproductor.turnoUsuario != null) burbuja.Dibujar(this, W, H);
                    else if (reproductor.EnEspera)
                    {
                        var p = cliente.pendiente;
                        if (p != null && p.tipo == "RESPUESTAS" && !reproductor.enVivo) respuestas.Dibujar(this, p, W, H);
                        else if (p != null && (p.tipo == "NEGOCIACION" || p.tipo == "CONDICIONES")) negociacion.Dibujar(this, p, W, H);
                        else if (cliente.estadoSesion == "corriendo" || cliente.estadoSesion == "esperando")
                        {
                            GUILayout.BeginArea(new Rect(W / 2 - 320, 130, 640, 84), Estilos.Caja);
                            GUILayout.Label("Los tiburones están pensando…", new GUIStyle(Estilos.Subtitulo) { alignment = TextAnchor.MiddleCenter });
                            GUILayout.EndArea();
                        }
                        if (cliente.estadoSesion == "error") { GUI.Label(new Rect(W / 2 - 400, H / 2, 800, 80), mensaje, Estilos.Color_(Estilos.Texto, Estilos.Fuera)); }
                    }
                    break;
                case Vista.Resultado: resultado.Dibujar(this, W, H); break;
            }
            if (mostrarHistorial && (vista == Vista.Sesion || vista == Vista.Resultado)) panelHistorial.Dibujar(this, W, H);
            if (vista != Vista.Inicio && !string.IsNullOrEmpty(mensaje)) GUI.Label(new Rect(24, H - 40, W - 48, 34), mensaje, Estilos.TextoChico);
        }



        // ---------- usuario automático (pruebas en vivo con el servicio real) ----------
        static readonly string[][] RespuestasDemo =
        {
            new[] { "Hoy tenemos 50 clientes que pagan y crecemos 15 % al mes; el costo de adquirir un cliente todavía lo estamos midiendo, estimamos unos 3,000 pesos.",
                    "Competimos con Power BI y con consultores; nuestra ventaja es que no requiere configuración y entrega el análisis en lenguaje natural.",
                    "Usamos modelos de lenguaje con conectores propios para hojas de cálculo; no tenemos patente, sí marca registrada." },
            new[] { "Nuestro ingreso recurrente es de unos 22,500 pesos al mes y el margen bruto es de 72 %; con la inversión esperamos duplicar clientes en 9 meses.",
                    "El mercado son PyMEs de comercio y servicios con 10 a 100 empleados; empezamos en Monterrey y luego Guadalajara.",
                    "Para proteger la tecnología acumulamos datos anonimizados de uso que mejoran las alertas; evaluaremos una patente del método." },
            new[] { "La valuación de 3.3 millones se basa en el crecimiento de 15 % mensual; estamos abiertos a negociar el porcentaje si el socio aporta ventas.",
                    "Somos cuatro fundadores de tiempo completo con experiencia en datos, producto, ventas y operaciones.",
                    "El siguiente paso técnico es integrar facturación electrónica; ya tenemos un piloto con dos clientes." },
        };

        bool HayEventoDeAgente(int desde)
        {
            for (int i = desde; i < cliente.recibidos.Count; i++)
                if (cliente.recibidos[i].emisor != "emprendedor") return true;
            return false;
        }

        /// <summary>Prueba automática: mantiene su oferta si puede; si no, toma la contraoferta que menos le cuesta.</summary>
        static string MejorCondicion(Pendiente p)
        {
            if (p.puede_original || p.contraofertas == null || p.contraofertas.Length == 0) return "original";
            var mejor = p.contraofertas[0];
            foreach (var c in p.contraofertas) if (c.monto_mxn / c.porcentaje > mejor.monto_mxn / mejor.porcentaje) mejor = c;
            return mejor.tiburon_id;
        }

        IEnumerator RespondedorAutomatico()
        {
            while (true)
            {
                yield return null;
                if (vista != Vista.Sesion) continue;
                // Modo en vivo: responde en la burbuja cada pregunta en cuanto el tiburón la hace
                if (reproductor.turnoUsuario != null)
                {
                    string turno = reproductor.turnoUsuario;
                    // contesta lo que se le preguntó: pide la respuesta al agente empresario de esta misma sesión
                    string txt = null; bool llego = false;
                    PedirSugerencia(turno, "", sugerida => { txt = sugerida; llego = true; });
                    float limite = Time.realtimeSinceStartup + 30f;
                    while (!llego && Time.realtimeSinceStartup < limite) yield return null;
                    if (reproductor.turnoUsuario != turno) continue;
                    if (string.IsNullOrEmpty(txt)) txt = "Ese dato todavía no lo tengo medido; puedo enviarlo después de esta sesión.";
                    burbuja.Escribir(turno, txt);                                 // se ve escrita en pantalla
                    yield return new WaitForSecondsRealtime(2.5f);
                    if (reproductor.turnoUsuario != turno) continue;
                    reproductor.Responder(txt);
                    var pr = cliente.pendiente;
                    if (pr != null && pr.tipo == "RESPUESTAS")
                        yield return MedirLatencia("RESPUESTAS", Time.realtimeSinceStartup, cliente.recibidos.Count);
                    continue;
                }
                if (!reproductor.EnEspera || cliente.pendiente == null) continue;
                var p = cliente.pendiente;
                float t0 = Time.realtimeSinceStartup; int antes = cliente.recibidos.Count;
                if (p.tipo == "RESPUESTAS" && !reproductor.enVivo)
                {
                    var r = new Dictionary<string, string>();
                    var lista = RespuestasDemo[Mathf.Clamp(p.ronda - 1, 0, 2)];
                    for (int i = 0; i < p.preguntas.Length; i++) r[p.preguntas[i].tiburon_id] = lista[i % lista.Length];
                    foreach (var kv in r) respuestas.texto[kv.Key] = kv.Value;   // se ven escritas en pantalla
                    yield return new WaitForSecondsRealtime(2.5f);
                    t0 = Time.realtimeSinceStartup;
                    yield return cliente.EnviarRespuestas(r);
                }
                else if (p.tipo == "CONDICIONES") yield return cliente.EnviarCondicion(MejorCondicion(p));
                else if (p.tipo == "NEGOCIACION")
                {
                    if (p.contraofertas != null && p.contraofertas.Length > 0)
                        yield return cliente.EnviarNegociacion("aceptar_contraoferta", p.contraofertas[0].monto_mxn, p.contraofertas[0].porcentaje, p.contraofertas[0].tiburon_id);
                    else yield return cliente.EnviarNegociacion("ajustar", p.oferta_actual.monto_mxn, Math.Min(100, p.oferta_actual.porcentaje + 5), null);
                }
                else continue;
                yield return MedirLatencia(p.tipo, t0, antes);
            }
        }

        // latencia: tiempo hasta que llega la siguiente intervención de un agente
        IEnumerator MedirLatencia(string tipo, float t0, int antes)
        {
            while (cliente.estadoSesion != "error" && !HayEventoDeAgente(antes)) yield return null;
            latencias.Add(tipo + "|" + (Time.realtimeSinceStartup - t0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture));
        }

        // ---------- grabación de video (fotogramas a velocidad fija; se unen con ffmpeg) ----------
        int fotograma;
        const int FPS = 12;

        IEnumerator Grabar(string carpeta, float segundos)
        {
            int total = Mathf.Max(1, (int)(segundos * FPS));
            for (int i = 0; i < total; i++)
            {
                yield return new WaitForEndOfFrame();
                if (Arg("-nh-sin-cuadros")) { fotograma++; continue; }
                ScreenCapture.CaptureScreenshot(Path.Combine(carpeta, "f" + (fotograma++).ToString("D5") + ".png"));
            }
        }

        IEnumerator GrabarHasta(string carpeta, Func<bool> cond, float maxSeg)
        {
            int max = (int)(maxSeg * FPS), i = 0;
            while (!cond() && i++ < max)
            {
                yield return Grabar(carpeta, 1f / FPS);
                // mientras los agentes piensan en el servicio, se graba solo un cuadro cada 0.4 s reales (el video no se alarga)
                if (vista == Vista.Sesion && reproductor.EnEspera && cliente.pendiente == null && cliente.estadoSesion == "corriendo")
                    yield return new WaitForSecondsRealtime(0.4f);
            }
        }

        IEnumerator Video()
        {
            string carpeta = ArgValor("-nh-salida", Path.Combine(Application.persistentDataPath, "video"));
            Directory.CreateDirectory(carpeta);
            Time.captureFramerate = FPS;
            if (sonidos) { sonidos.registro = new List<string>(); sonidos.fotograma = () => fotograma; }
            yield return Grabar(carpeta, 2.5f);
            IrAFormulario();
            yield return GrabarHasta(carpeta, () => ejemplos != null, 10);
            int k = ejemplos.Length - 1; int.TryParse(ArgValor("-nh-ejemplo", k.ToString()), out k);
            var ek = ejemplos[Mathf.Clamp(k, 0, ejemplos.Length - 1)]; formulario.Cargar(ek.pitch); nivel = ek.nivel;
            yield return Grabar(carpeta, 3.5f);
            IrAEleccionPanel(formulario.Json());
            yield return GrabarHasta(carpeta, () => vista == Vista.Panel, 10);
            yield return Grabar(carpeta, 3.5f);
            IniciarSesion(new List<string>(eleccion.elegidos));
            reproductor.factorExtra = 1f;       // el video lleva las voces completas, a velocidad normal
            yield return GrabarHasta(carpeta, () => vista == Vista.Resultado, 900);
            yield return Grabar(carpeta, 6f);
            mostrarHistorial = true;
            yield return Grabar(carpeta, 3f);
            if (sonidos && sonidos.registro != null) File.WriteAllLines(Path.Combine(carpeta, "audio_log.txt"), sonidos.registro.ToArray());
            File.WriteAllLines(Path.Combine(carpeta, "latencias.txt"), latencias.ToArray());
            File.WriteAllText(Path.Combine(carpeta, "sesion.txt"), cliente.sesionId ?? "");
            File.WriteAllText(Path.Combine(carpeta, "listo.txt"), fotograma.ToString());
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        // ---------- capturas automáticas (para la documentación) ----------
        string ArgValor(string clave, string def)
        {
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == clave) return a[i + 1];
            return def;
        }

        IEnumerator Cap(string carpeta, string nombre)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(carpeta, nombre + ".png"));
            yield return new WaitForSeconds(0.4f);
        }

        IEnumerator Hasta(Func<bool> cond, float max = 150f)
        {
            float t = 0;
            while (!cond() && t < max) { t += Time.deltaTime; yield return null; }
        }

        /// <summary>Prueba corta (-nh-final): captura la animación de «sin trato» sin reproducir toda una sesión.</summary>
        IEnumerator PruebaFinal()
        {
            string carpeta = ArgValor("-nh-salida", Path.Combine(Application.persistentDataPath, "capturas"));
            Directory.CreateDirectory(carpeta);
            yield return new WaitForSeconds(2f);
            ocultarUI = false; vista = Vista.Sesion;
            final_.Resultado(this, false, "GreenPlate", "");
            yield return new WaitForSeconds(5f);
            yield return Cap(carpeta, "17_sin_trato");
            Application.Quit();
        }

        /// <summary>Prueba corta (-nh-ui): formulario con textos largos, autocompletado y pantalla de modo.</summary>
        IEnumerator PruebaPantallas()
        {
            string carpeta = ArgValor("-nh-salida", Path.Combine(Application.persistentDataPath, "capturas"));
            Directory.CreateDirectory(carpeta);
            yield return new WaitForSeconds(2f);
            IrAFormulario();
            yield return Hasta(() => ejemplos != null);
            formulario.Cargar(ejemplos[0].pitch);
            formulario.p.diferenciador = "Nosotros nos hacemos cargo de los autos porque nuestro sistema de monitoreo es bastante avanzado y no gastamos en sensores por cajón, además de que la plaza no tiene que cambiar nada de lo que ya tiene instalado y el conductor no descarga ninguna aplicación.";
            yield return new WaitForSeconds(0.5f);
            formulario.PruebaDesplazar(420f);
            yield return new WaitForSeconds(0.5f);
            yield return Cap(carpeta, "ui_campo_largo");
            formulario.PruebaDesplazar(0f);
            var documento = ArgValor("-nh-doc", "");
            if (documento != "") { formulario.PruebaExtraer(this, documento); yield return new WaitForSeconds(0.2f); yield return Hasta(() => !formulario.Extrayendo, 40f); }
            yield return new WaitForSeconds(0.6f);
            yield return Cap(carpeta, "ui_autocompletado");
            formulario.PruebaMenuAuto();
            yield return new WaitForSeconds(0.5f);
            yield return Cap(carpeta, "ui_menu_auto");
            IrAEleccionPanel(formulario.Json());
            yield return Hasta(() => vista == Vista.Panel, 15f);
            yield return new WaitForSeconds(0.5f);
            yield return Cap(carpeta, "ui_panel");
            conocer.Abrir(this, Vista.Panel);
            conocer.Mostrar(this, 2); yield return new WaitForSeconds(1.2f);
            yield return Cap(carpeta, "12_conocer");
            conocer.Cerrar(this); yield return new WaitForSeconds(0.4f);
            modoEnVivo = true; ayudaActiva = true; Ir(Vista.Modo);
            yield return new WaitForSeconds(0.5f);
            yield return Cap(carpeta, "ui_modo");
            iniciando = true;
            yield return new WaitForSeconds(0.4f);
            yield return Cap(carpeta, "ui_iniciando");
            iniciando = false;
            Ir(Vista.Sesion); cliente.ayuda = true;
            reproductor.turnoUsuario = "tiburon_tecnologico";
            burbuja.Escribir("tiburon_tecnologico", "Tenemos un demo funcionando que lee las placas con las cámaras de la plaza.");
            burbuja.Avisar("Revisa el texto antes de enviarlo.");
            yield return new WaitForSeconds(0.6f);
            yield return Cap(carpeta, "ui_respuesta");
            Application.Quit();
        }

        IEnumerator Capturas()
        {
            string carpeta = ArgValor("-nh-salida", Path.Combine(Application.persistentDataPath, "capturas"));
            Directory.CreateDirectory(carpeta);
            yield return new WaitForSeconds(2f);
            yield return Cap(carpeta, "01_inicio");
            IrAFormulario();
            yield return Hasta(() => ejemplos != null);
            formulario.Cargar(ejemplos[ejemplos.Length - 1].pitch); nivel = ejemplos[ejemplos.Length - 1].nivel;
            yield return new WaitForSeconds(0.5f);
            yield return Cap(carpeta, "02_formulario");
            IrAEleccionPanel(formulario.Json());
            yield return Hasta(() => vista == Vista.Panel);
            yield return new WaitForSeconds(0.5f);
            yield return Cap(carpeta, "03_eleccion_panel");
            conocer.Abrir(this, Vista.Panel);
            conocer.Mostrar(this, 2); yield return new WaitForSeconds(1.2f);
            yield return Cap(carpeta, "12_conocer");
            conocer.Cerrar(this); yield return new WaitForSeconds(0.4f);
            IniciarSesion(new List<string>(eleccion.elegidos));
            reproductor.factorExtra = 2.2f;
            yield return Hasta(() => intro.activa);
            yield return new WaitForSeconds(3.0f);
            yield return Cap(carpeta, "13_intro_portada");
            yield return new WaitForSeconds(5.0f);
            yield return Cap(carpeta, "14_intro_episodio");
            yield return Hasta(() => reproductor.actual != null && reproductor.actual.tipo == "PITCH");
            yield return new WaitForSeconds(1.6f);
            yield return Cap(carpeta, "15_pitch");
            yield return Hasta(() => reproductor.actual != null && reproductor.actual.tipo == "PANEL");
            yield return new WaitForSeconds(2.2f);
            ocultarUI = true; yield return new WaitForSeconds(0.3f);
            yield return Cap(carpeta, "04_estudio_general");
            ocultarUI = false;
            yield return Hasta(() => reproductor.actual != null && reproductor.actual.tipo == "PREGUNTA");
            yield return new WaitForSeconds(1.8f);
            yield return Cap(carpeta, "05_pregunta");
            yield return Hasta(() => reproductor.actual != null && reproductor.actual.tipo == "REPLICA");
            yield return new WaitForSeconds(1.8f);
            yield return Cap(carpeta, "06_replica");
            yield return Hasta(() => reproductor.actual != null && reproductor.actual.tipo == "VOTO");
            yield return new WaitForSeconds(1.5f);
            mostrarHistorial = true; yield return new WaitForSeconds(0.4f);
            yield return Cap(carpeta, "07_historial");
            mostrarHistorial = false;
            yield return Hasta(() => reproductor.votos.Count >= 3);
            yield return new WaitForSeconds(1.5f);
            yield return Cap(carpeta, "08_votos");
            yield return Hasta(() => reproductor.actual != null && reproductor.actual.tipo == "DECISION");
            yield return new WaitForSeconds(3.4f);
            yield return Cap(carpeta, "09_decision");
            yield return Hasta(() => final_.EnCierre);
            yield return new WaitForSeconds(4.2f);
            yield return Cap(carpeta, "16_cierre");
            yield return Hasta(() => vista == Vista.Resultado);
            yield return new WaitForSeconds(0.6f);
            yield return Cap(carpeta, "10_resultado");
            // Vista de arriba (sin interfaz) para comparar con el boceto
            Ir(Vista.Sesion); ocultarUI = true;
            estudio.camara.instantaneo = true; estudio.camara.Cambiar(Plano.Arriba, null);
            yield return new WaitForSeconds(0.8f);
            yield return Cap(carpeta, "11_vista_arriba");
            estudio.camara.instantaneo = false;
            File.WriteAllText(Path.Combine(carpeta, "listo.txt"), "ok");
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
