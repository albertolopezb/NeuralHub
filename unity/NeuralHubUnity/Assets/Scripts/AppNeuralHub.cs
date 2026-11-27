using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NeuralHub
{
    public enum Vista { Inicio, Formulario, Panel, Modo, Sesion, Resultado, Sesiones }

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
            cliente.alError = m => mensaje = m;
            foreach (var a in Environment.GetCommandLineArgs())
                if (a == "-nh-mock" || a == "-nh-capturar" || (a == "-nh-video" && !Arg("-nh-vivo"))) cliente.usarMock = true;
            for (int i = 0; i < Environment.GetCommandLineArgs().Length - 1; i++)
                if (Environment.GetCommandLineArgs()[i] == "-nh-url") cliente.urlBase = Environment.GetCommandLineArgs()[i + 1];
            ProbarConexion();
            CargarCatalogos();
            foreach (var a in Environment.GetCommandLineArgs()) if (a == "-nh-capturar") { cliente.usarMock = true; StartCoroutine(Capturas()); }
            if (Arg("-nh-vivo")) { vivoAuto = true; modoEnVivo = true; ayudaActiva = false; StartCoroutine(RespondedorAutomatico()); }
            if (Arg("-nh-video")) StartCoroutine(Video());
        }

        // ---------- navegación ----------
        public void Ir(Vista v)
        {
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

        public void IniciarSesion(List<string> ids)
        {
            mensaje = "";
            bool enVivo = modoEnVivo, ayuda = modoEnVivo && ayudaActiva;
            // Mismo orden del catálogo para que el panel quede estable
            var panel = new List<string>();
            foreach (var f in recomendacion.ranking) if (ids.Contains(f.tiburon_id)) panel.Add(f.tiburon_id);
            StartCoroutine(cliente.CrearSesion(pitchJson, panel.ToArray(), enVivo, ayuda, nivel, ok =>
            {
                if (!ok) return;
                sugerenciasPedidas = 0;
                barra.Reiniciar();
                // Los 3 tiburones elegidos ya se sientan (en penumbra) mientras el emprendedor presenta su pitch
                var nombres = new List<string>();
                foreach (var id in panel) { var info = Catalogo(id); nombres.Add(info != null ? info.nombre : id); }
                reproductor.Iniciar(); reproductor.enVivo = enVivo;
                estudio.IniciarSesion(panel.ToArray(), nombres.ToArray()); Ir(Vista.Sesion);
            }));
        }

        /// <summary>Modo en vivo con ayuda: pide al agente empresario una respuesta sugerida para el tiburón en turno.</summary>
        public void PedirSugerencia(string tiburonId, Action<string> listo)
        {
            sugerenciasPedidas++;
            StartCoroutine(cliente.PedirSugerencia(tiburonId, listo));
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
            // En vivo: cuando el usuario ya respondió las preguntas que el servicio espera, se envían juntas
            var p = cliente.pendiente;
            if (vista == Vista.Sesion && reproductor.enVivo && p != null && p.tipo == "RESPUESTAS" && p.preguntas != null &&
                p.ronda != reproductor.enviadaRonda && reproductor.rondaRespuestas == p.ronda)
            {
                bool todas = true;
                foreach (var q in p.preguntas) if (!reproductor.respuestasRonda.ContainsKey(q.tiburon_id)) todas = false;
                if (todas)
                {
                    reproductor.enviadaRonda = p.ronda;
                    StartCoroutine(cliente.EnviarRespuestas(new Dictionary<string, string>(reproductor.respuestasRonda)));
                }
            }

            if (vista == Vista.Sesion && reproductor.reporteListo && reproductor.EnEspera)
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
                case Vista.Sesion:
                    interfaz.Dibujar(this, W, H);
                    barra.Dibujar(reproductor, W);
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
                    var lista = RespuestasDemo[Mathf.Clamp(reproductor.rondaRespuestas - 1, 0, 2)];
                    string txt = lista[reproductor.respuestasRonda.Count % lista.Length];
                    burbuja.Escribir(turno, txt);                                 // se ve escrita en pantalla
                    yield return new WaitForSecondsRealtime(2.5f);
                    if (reproductor.turnoUsuario != turno) continue;
                    reproductor.Responder(txt);
                    var pr = cliente.pendiente;
                    if (pr != null && pr.tipo == "RESPUESTAS" && pr.preguntas != null && reproductor.respuestasRonda.Count >= pr.preguntas.Length)
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
                else if (p.tipo == "CONDICIONES") yield return cliente.EnviarCondicion("original");
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
            reproductor.factorExtra = 2.2f;
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
            IniciarSesion(new List<string>(eleccion.elegidos));
            reproductor.factorExtra = 2.2f;
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
            yield return new WaitForSeconds(2.0f);
            yield return Cap(carpeta, "09_decision");
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
