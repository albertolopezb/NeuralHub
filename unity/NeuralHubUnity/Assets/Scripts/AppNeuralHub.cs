using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NeuralHub
{
    public enum Vista { Inicio, Formulario, Panel, Sesion, Resultado, Sesiones }

    /// <summary>Controlador de la aplicación: navega entre pantallas y conecta el servicio, el reproductor y el estudio.</summary>
    public class AppNeuralHub : MonoBehaviour
    {
        public ClienteServicio cliente;
        public ReproductorSesion reproductor;
        public Estudio estudio;
        public CamaraEstudio camara;

        public Vista vista = Vista.Inicio;
        public bool mostrarHistorial;
        public bool ocultarUI;
        public string mensaje = "";
        public EjemploItem[] ejemplos;
        public TiburonInfo[] catalogo;
        public Recomendacion recomendacion;

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

        string pitchJson;
        float esperaResultado;

        void Start()
        {
            cliente.alError = m => mensaje = m;
            foreach (var a in Environment.GetCommandLineArgs())
                if (a == "-nh-mock" || a == "-nh-capturar") cliente.usarMock = true;
            for (int i = 0; i < Environment.GetCommandLineArgs().Length - 1; i++)
                if (Environment.GetCommandLineArgs()[i] == "-nh-url") cliente.urlBase = Environment.GetCommandLineArgs()[i + 1];
            ProbarConexion();
            CargarCatalogos();
            foreach (var a in Environment.GetCommandLineArgs()) if (a == "-nh-capturar") { cliente.usarMock = true; StartCoroutine(Capturas()); }
        }

        // ---------- navegación ----------
        public void Ir(Vista v) { vista = v; mostrarHistorial = false; if (v != Vista.Sesion && v != Vista.Resultado) { reproductor.Detener(); camara.Cambiar(Plano.General, null); } }

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

        public void IniciarSesion(List<string> ids)
        {
            mensaje = "";
            // Mismo orden del catálogo para que el panel quede estable
            var panel = new List<string>();
            foreach (var f in recomendacion.ranking) if (ids.Contains(f.tiburon_id)) panel.Add(f.tiburon_id);
            StartCoroutine(cliente.CrearSesion(pitchJson, panel.ToArray(), ok =>
            {
                if (!ok) return;
                reproductor.Iniciar(); estudio.IniciarSesion(new string[0], null); Ir(Vista.Sesion);
            }));
        }

        public void AbrirSesiones()
        {
            StartCoroutine(cliente.ListarSesiones(s => { listaSesiones.sesiones = s; Ir(Vista.Sesiones); }));
        }

        public void RepetirSesion(string id)
        {
            StartCoroutine(cliente.CargarSesionPasada(id, ok => { if (ok) { reproductor.Iniciar(); estudio.IniciarSesion(new string[0], null); Ir(Vista.Sesion); } }));
        }

        void Update()
        {
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

            if (vista == Vista.Inicio || vista == Vista.Formulario || vista == Vista.Panel || vista == Vista.Sesiones)
                Estilos.Rect(new Rect(0, 0, W, H), new Color(Estilos.Navy.r, Estilos.Navy.g, Estilos.Navy.b, 0.62f));

            switch (vista)
            {
                case Vista.Inicio: menu.Dibujar(this, W, H); break;
                case Vista.Formulario: formulario.Dibujar(this, W, H); break;
                case Vista.Panel: eleccion.Dibujar(this, W, H); break;
                case Vista.Sesiones: listaSesiones.Dibujar(this, W, H); break;
                case Vista.Sesion:
                    interfaz.Dibujar(this, W, H);
                    controles.Dibujar(this, W);
                    if (reproductor.EnEspera)
                    {
                        var p = cliente.pendiente;
                        if (p != null && p.tipo == "RESPUESTAS") respuestas.Dibujar(this, p, W, H);
                        else if (p != null && (p.tipo == "NEGOCIACION" || p.tipo == "CONDICIONES")) negociacion.Dibujar(this, p, W, H);
                        else if (cliente.estadoSesion == "corriendo" || cliente.estadoSesion == "esperando")
                        {
                            GUILayout.BeginArea(new Rect(W / 2 - 300, H / 2 - 40, 600, 80), Estilos.Caja);
                            GUILayout.Label("Los tiburones están pensando…", Estilos.Subtitulo);
                            GUILayout.EndArea();
                        }
                        if (cliente.estadoSesion == "error") { GUI.Label(new Rect(W / 2 - 400, H / 2, 800, 80), mensaje, Estilos.Texto); }
                    }
                    break;
                case Vista.Resultado: resultado.Dibujar(this, W, H); break;
            }
            if (mostrarHistorial && (vista == Vista.Sesion || vista == Vista.Resultado)) panelHistorial.Dibujar(this, W, H);
            if (vista != Vista.Inicio && !string.IsNullOrEmpty(mensaje)) GUI.Label(new Rect(24, H - 40, W - 48, 34), mensaje, Estilos.TextoChico);
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
            formulario.Cargar(ejemplos[ejemplos.Length - 1].pitch);
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
