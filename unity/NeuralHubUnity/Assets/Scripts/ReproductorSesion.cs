using System.Collections.Generic;
using UnityEngine;

namespace NeuralHub
{
    /// <summary>
    /// Reproduce los eventos de la cola de ClienteServicio de uno en uno (ControlReproduccion + reproducirSiguiente).
    /// Pausa, velocidad 1x/2x, saltar animación e historial de lo ya reproducido.
    /// </summary>
    public class ReproductorSesion : MonoBehaviour
    {
        public ClienteServicio cliente;
        public Estudio estudio;

        public bool pausado;
        public float velocidad = 1f;
        public float factorExtra = 1f;          // lo usa el modo de capturas
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

        int siguiente;
        float restante;
        bool activo;

        public void Iniciar()
        {
            historial.Clear(); votos.Clear(); siguiente = 0; restante = 0; actual = null; ronda = 0; resultado = "";
            reporteListo = false; reporte = null; decisionFinal = null; activo = true; pausado = false;
        }

        public void Detener() { activo = false; }
        public bool ColaVacia { get { return siguiente >= cliente.recibidos.Count; } }
        public bool Ocupado { get { return restante > 0f; } }
        public bool EnEspera { get { return activo && ColaVacia && !Ocupado; } }

        public void SaltarAnimacion() { restante = 0f; }
        public void AlternarPausa() { pausado = !pausado; }
        public void FijarVelocidad(float v) { velocidad = v; }

        void Update()
        {
            if (!activo || pausado) return;
            if (restante > 0f) { restante -= Time.deltaTime * velocidad * factorExtra; if (restante > 0f) return; }
            if (!ColaVacia) ReproducirSiguiente();
        }

        public void ReproducirSiguiente()
        {
            var e = cliente.recibidos[siguiente++];
            actual = e;
            historial.Add(e);
            switch (e.tipo)
            {
                case "PITCH": if (e.pitch != null) pitchActual = e.pitch; break;
                case "PANEL":
                    panelInfo = e.panel ?? new PanelItem[0];
                    var ids = new List<string>(); var nombres = new List<string>();
                    foreach (var p in panelInfo) { ids.Add(p.tiburon_id); nombres.Add(p.nombre); }
                    panelIds = ids.ToArray();
                    estudio.IniciarSesion(panelIds, nombres.ToArray());
                    break;
                case "VOTO": votos[e.emisor] = e.decision; break;
                case "NEGOCIACION": votos.Clear(); break;
                case "DECISION": resultado = e.resultado; decisionFinal = e; break;
                case "REPORTE": reporte = e; reporteListo = true; break;
            }
            if (e.ronda > 0 && e.tipo != "REPORTE") ronda = e.ronda;
            estudio.Aplicar(e);
            restante = Duracion(e);
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

        /// <summary>Reemplaza (o reinicia) la lista de eventos desde otra sesión (repetir sesión del historial).</summary>
        public void Repetir() { Iniciar(); }
    }
}
