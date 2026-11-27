using System.Collections.Generic;
using UnityEngine;

namespace NeuralHub
{
    /// <summary>
    /// Reproduce los eventos de la cola de ClienteServicio de uno en uno (ControlReproduccion + reproducirSiguiente).
    /// Pausa, velocidad 1x/2x, saltar, navegar a cualquier turno (anterior, siguiente, ir a) y voz de cada personaje.
    /// En modo en vivo se detiene en cada pregunta hasta que el usuario responde (turnoUsuario).
    /// </summary>
    public class ReproductorSesion : MonoBehaviour
    {
        public ClienteServicio cliente;
        public Estudio estudio;
        public Sonidos sonidos;
        public PantallasDatos datos;

        public bool pausado;
        public float velocidad = 1f;
        public float factorExtra = 1f;          // lo usan los modos de capturas y video
        public readonly List<Evento> historial = new List<Evento>();
        public Evento actual;
        public int ronda;
        public readonly Dictionary<string, string> votos = new Dictionary<string, string>();
        public string resultado = "";
        public bool reporteListo;
        public Evento reporte;
        public Evento decisionFinal;
        public string[] panelIds = new string[0];
        public PanelItem[] panelInfo = new PanelItem[0];
        public PitchDto pitchActual;
        public float ultimoEventoRealtime;      // para medir la latencia en vivo

        // Modo en vivo: el usuario responde cada pregunta en cuanto el tiburón la hace
        public bool enVivo;
        public string turnoUsuario;              // tiburón al que el usuario debe responder ahora (null si no es su turno)
        public string preguntaUsuario;
        public int rondaRespuestas = -1;
        public string enviada;                   // clave de las preguntas ya enviadas al servicio (ronda|tiburón)
        public float Duracion_ { get; private set; }
        /// <summary>Avance (0 a 1) del turno en curso: la interfaz lo usa para pasar los subtítulos largos por frases.</summary>
        public float Progreso
        {
            get
            {
                if (sonidos && sonidos.Hablando) return sonidos.AvanceVoz;
                return Duracion_ > 0.01f ? Mathf.Clamp01(1f - restante / Duracion_) : 1f;
            }
        }
        public readonly Dictionary<string, string> respuestasRonda = new Dictionary<string, string>();

        int siguiente;
        float restante;
        bool activo;
        bool esperandoAudio;
        float esperaAudio;

        public int Indice { get { return siguiente; } }
        public int Total { get { return cliente.recibidos.Count; } }

        public void Iniciar()
        {
            historial.Clear(); votos.Clear(); siguiente = 0; restante = 0; actual = null; ronda = 0; resultado = "";
            reporteListo = false; reporte = null; decisionFinal = null; activo = true; pausado = false; esperandoAudio = false;
            turnoUsuario = null; preguntaUsuario = null; rondaRespuestas = -1; enviada = null; respuestasRonda.Clear();
            if (sonidos) sonidos.DetenerVoz();
        }

        /// <summary>El usuario respondió la pregunta en turno (modo en vivo): se guarda y la sesión continúa.</summary>
        public void Responder(string texto)
        {
            if (turnoUsuario == null) return;
            respuestasRonda[turnoUsuario] = texto;
            turnoUsuario = null; preguntaUsuario = null;
            restante = Mathf.Max(restante, 0.4f);
        }

        public void Detener() { activo = false; if (sonidos) sonidos.DetenerVoz(); }
        public bool ColaVacia { get { return siguiente >= cliente.recibidos.Count; } }
        public bool Ocupado { get { return restante > 0f || esperandoAudio || (sonidos && sonidos.Hablando); } }
        public bool EnEspera { get { return activo && ColaVacia && !Ocupado && turnoUsuario == null; } }

        public void SaltarAnimacion() { restante = 0f; esperandoAudio = false; if (sonidos) sonidos.DetenerVoz(); }
        public void AlternarPausa() { pausado = !pausado; if (sonidos) sonidos.PausarVoz(pausado); }
        public void FijarVelocidad(float v) { velocidad = v; }

        /// <summary>Vuelve a un turno anterior o salta a uno posterior ya recibido: reconstruye la escena hasta ese punto y sigue desde ahí.</summary>
        public void IrA(int indice)
        {
            indice = Mathf.Clamp(indice, 0, Mathf.Max(0, cliente.recibidos.Count - 1));
            historial.Clear(); votos.Clear(); ronda = 0; resultado = ""; reporteListo = false; reporte = null; decisionFinal = null;
            restante = 0; esperandoAudio = false; activo = true; turnoUsuario = null; preguntaUsuario = null;
            if (sonidos) sonidos.DetenerVoz();
            for (int i = 0; i < indice; i++) Aplicar(cliente.recibidos[i], false);
            siguiente = indice;
        }

        public void Anterior() { IrA(Mathf.Max(0, siguiente - 2)); }
        public void Siguiente() { SaltarAnimacion(); }

        void Update()
        {
            if (!activo || pausado) return;
            float dt = Time.deltaTime;
            if (esperandoAudio) { esperaAudio += Time.unscaledDeltaTime; if (esperaAudio < 25f) return; esperandoAudio = false; }
            if (turnoUsuario != null) return;   // en vivo: espera a que el usuario responda
            if (restante > 0f) { restante -= dt * velocidad * factorExtra; if (restante > 0f) return; }
            if (sonidos && sonidos.Hablando) return;      // nadie interrumpe: el siguiente turno espera a que acabe la voz
            if (!ColaVacia) ReproducirSiguiente();
        }

        public void ReproducirSiguiente()
        {
            var e = cliente.recibidos[siguiente++];
            Aplicar(e, true);
            restante = Duracion(e);
            Duracion_ = restante;
            ultimoEventoRealtime = Time.realtimeSinceStartup;
            if (enVivo && e.tipo == "PREGUNTA" && e.emisor != null && e.emisor.StartsWith("tibur"))
            {
                if (e.ronda != rondaRespuestas) { respuestasRonda.Clear(); rondaRespuestas = e.ronda; }
                if (!respuestasRonda.ContainsKey(e.emisor)) { turnoUsuario = e.emisor; preguntaUsuario = e.texto; restante = 0f; }
            }
            // En vivo, la respuesta ya la escribió el usuario: solo se muestra un momento y sin voz
            bool respuestaPropia = enVivo && e.tipo == "RESPUESTA";
            if (respuestaPropia) restante = 1.6f;
            if (sonidos && sonidos.vozActiva && TieneVoz(e) && !respuestaPropia)
            {
                esperandoAudio = true; esperaAudio = 0;
                var evento = e;
                StartCoroutine(cliente.ObtenerAudio(e.n, clip =>
                {
                    esperandoAudio = false;
                    if (clip == null || actual != evento) return;
                    float tono = Mathf.Clamp(velocidad * factorExtra, 1f, 1.5f);
                    sonidos.Voz(clip, tono, evento.n);
                    restante = clip.length / tono + 0.35f;
                    Duracion_ = restante;
                }));
            }
        }

        static bool TieneVoz(Evento e) { return e.tipo != "REPORTE" && e.tipo != "PANEL" && !string.IsNullOrEmpty(e.texto); }

        void Aplicar(Evento e, bool conEfectos)
        {
            actual = e;
            historial.Add(e);
            switch (e.tipo)
            {
                case "PITCH":
                    if (e.pitch != null) pitchActual = e.pitch;
                    if (datos) datos.MostrarOferta(e.pitch, e.valuacion_mxn);
                    break;
                case "PANEL":
                    panelInfo = e.panel ?? new PanelItem[0];
                    var ids = new List<string>(); var nombres = new List<string>();
                    foreach (var p in panelInfo) { ids.Add(p.tiburon_id); nombres.Add(p.nombre); }
                    panelIds = ids.ToArray();
                    estudio.IniciarSesion(panelIds, nombres.ToArray());
                    if (datos) datos.FijarPanel(panelInfo);
                    break;
                case "PREGUNTA":
                    if (datos) datos.Estado(e.emisor, "preguntando");
                    if (conEfectos && sonidos) sonidos.Efecto("turno", 0.35f);
                    break;
                case "REPLICA":
                    if (datos) datos.Estado(e.emisor, "replicando");
                    if (conEfectos && sonidos) sonidos.Efecto("burbujas", 0.4f);
                    break;
                case "VOTO":
                    votos[e.emisor] = e.decision;
                    if (datos) datos.Voto(e.emisor, e.decision);
                    if (conEfectos && sonidos) sonidos.Efecto("voto", 0.5f);
                    break;
                case "NEGOCIACION": votos.Clear(); break;
                case "DECISION":
                    resultado = e.resultado; decisionFinal = e;
                    // la fanfarria o el hundimiento los pone FinalPrograma
                    break;
                case "REPORTE":
                    reporte = e; reporteListo = true;
                    if (datos) datos.Calificaciones(e.reporte);
                    break;
            }
            if (e.ronda > 0 && e.tipo != "REPORTE") ronda = e.ronda;
            estudio.Aplicar(e);
        }

        static float Duracion(Evento e)
        {
            int largo = e.texto != null ? e.texto.Length : 0;
            switch (e.tipo)
            {
                case "PANEL": return 3.5f;
                case "DECISION": return 7.5f;
                case "REPORTE": return 1.5f;
                case "VOTO": return Mathf.Clamp(largo * 0.055f, 3f, 14f);
                case "PITCH": return Mathf.Clamp(largo * 0.058f, 6f, 45f);
                default: return Mathf.Clamp(largo * 0.055f, 3f, 14f);
            }
        }

        public void Repetir() { Iniciar(); }
    }
}
