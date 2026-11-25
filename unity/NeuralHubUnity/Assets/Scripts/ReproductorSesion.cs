using System.Collections.Generic;
using UnityEngine;

namespace NeuralHub
{
    /// <summary>
    /// Reproduce los eventos de la cola de ClienteServicio de uno en uno (ControlReproduccion + reproducirSiguiente).
    /// Pausa, velocidad 1x/2x, saltar, navegar a cualquier turno (anterior, siguiente, ir a) y voz de cada personaje.
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
            if (sonidos) sonidos.DetenerVoz();
        }

        public void Detener() { activo = false; if (sonidos) sonidos.DetenerVoz(); }
        public bool ColaVacia { get { return siguiente >= cliente.recibidos.Count; } }
        public bool Ocupado { get { return restante > 0f || esperandoAudio; } }
        public bool EnEspera { get { return activo && ColaVacia && !Ocupado; } }

        public void SaltarAnimacion() { restante = 0f; esperandoAudio = false; if (sonidos) sonidos.DetenerVoz(); }
        public void AlternarPausa() { pausado = !pausado; if (pausado && sonidos) sonidos.DetenerVoz(); }
        public void FijarVelocidad(float v) { velocidad = v; }

        /// <summary>Vuelve a un turno anterior o salta a uno posterior ya recibido: reconstruye la escena hasta ese punto y sigue desde ahí.</summary>
        public void IrA(int indice)
        {
            indice = Mathf.Clamp(indice, 0, Mathf.Max(0, cliente.recibidos.Count - 1));
            historial.Clear(); votos.Clear(); ronda = 0; resultado = ""; reporteListo = false; reporte = null; decisionFinal = null;
            restante = 0; esperandoAudio = false; activo = true;
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
            if (esperandoAudio) { esperaAudio += Time.unscaledDeltaTime; if (esperaAudio < 6f) return; esperandoAudio = false; }
            if (restante > 0f) { restante -= dt * velocidad * factorExtra; if (restante > 0f) return; }
            if (!ColaVacia) ReproducirSiguiente();
        }

        public void ReproducirSiguiente()
        {
            var e = cliente.recibidos[siguiente++];
            Aplicar(e, true);
            restante = Duracion(e);
            ultimoEventoRealtime = Time.realtimeSinceStartup;
            if (sonidos && sonidos.vozActiva && TieneVoz(e))
            {
                esperandoAudio = true; esperaAudio = 0;
                var evento = e;
                StartCoroutine(cliente.ObtenerAudio(e.n, clip =>
                {
                    esperandoAudio = false;
                    if (clip == null || actual != evento) return;
                    float tono = Mathf.Clamp(velocidad * factorExtra, 1f, 1.5f);
                    sonidos.Voz(clip, tono, evento.n);
                    restante = Mathf.Max(restante, clip.length / tono + 0.35f) ;
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
                    if (conEfectos && sonidos) sonidos.Efecto(e.resultado == "TRATO" ? "trato" : "sin_trato", 0.7f);
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
                case "DECISION": return 5f;
                case "REPORTE": return 1.5f;
                case "VOTO": return Mathf.Clamp(largo * 0.03f, 2.5f, 7f);
                default: return Mathf.Clamp(largo * 0.035f, 2.5f, 8f);
            }
        }

        public void Repetir() { Iniciar(); }
    }
}
