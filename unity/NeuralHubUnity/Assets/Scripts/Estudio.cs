using UnityEngine;
using System.Collections.Generic;

namespace NeuralHub
{
    /// <summary>
    /// El estudio: coloca a los 3 tiburones elegidos en los sillones y aplica cada evento a la escena.
    /// Mientras habla el emprendedor, los tiburones quedan en penumbra (siluetas); cuando habla uno, la cámara
    /// se le acerca, el spotlight lo ilumina y la pantalla del fondo se atenúa.
    /// </summary>
    public class Estudio : MonoBehaviour
    {
        public Tiburon prefabTiburon;
        public Transform[] sillones;            // 3 sillones fijos (izquierda, centro, derecha)
        public Emprendedor emprendedor;
        public CamaraEstudio camara;
        public Reflector reflector;
        public Pantalla pantalla;
        public TextMesh letreroResultado;
        public Transform podio;
        public string[] panelInicial = { "tiburon_financiero", "tiburon_mentor", "tiburon_estratega" };

        public readonly List<Tiburon> panel = new List<Tiburon>();

        public void Preparar() { if (pantalla) pantalla.MostrarTexto("NeuralHub", "Preparando sesión..."); }

        void Start()
        {
            // Siluetas de fondo para el menú (se reemplazan por el panel real al empezar la sesión)
            if (panel.Count == 0 && panelInicial != null && panelInicial.Length > 0)
            {
                Colocar(panelInicial, null);
                if (pantalla) pantalla.MostrarTexto("NeuralHub", "Practica tu pitch ante el panel");
            }
        }

        void Colocar(string[] ids, string[] nombres)
        {
            // Si ya están sentados los mismos tiburones, no se vuelven a crear
            if (ids.Length > 0 && ids.Length == panel.Count)
            {
                bool iguales = true;
                for (int i = 0; i < ids.Length; i++) if (!panel[i] || panel[i].perfilId != ids[i]) iguales = false;
                if (iguales) { foreach (var t in panel) t.CambiarEstado(EstadoTiburon.Esperando); return; }
            }
            foreach (var t in panel) if (t) Destroy(t.gameObject);
            panel.Clear();
            for (int i = 0; i < sillones.Length && i < ids.Length; i++)
            {
                var t = Instantiate(prefabTiburon, sillones[i].position, sillones[i].rotation, transform);
                t.pose = i;
                t.Asignar(ids[i], nombres != null && i < nombres.Length ? nombres[i] : ids[i]);
                t.MirarA(emprendedor ? emprendedor.transform : null);
                panel.Add(t);
            }
        }

        public void IniciarSesion(string[] ids, string[] nombres)
        {
            Colocar(ids, nombres);
            if (letreroResultado) letreroResultado.text = "";
            if (pantalla) { if (ids.Length == 0) pantalla.MostrarTexto("NeuralHub", "Preparando sesión..."); pantalla.Atenuar(false); }
            camara.Cambiar(Plano.General, null);
            reflector.Apagar();
        }

        public Tiburon Buscar(string id)
        {
            foreach (var t in panel) if (t && t.perfilId == id) return t;
            return null;
        }

        void Enfocar(Tiburon t)
        {
            camara.Cambiar(Plano.Tiburon, t.PuntoCabeza());
            reflector.Iluminar(t.transform.position + Vector3.up * 1.2f);
            if (pantalla) pantalla.Atenuar(true);
        }

        void VistaPodio()
        {
            camara.Cambiar(Plano.Emprendedor, emprendedor ? emprendedor.PuntoCabeza() : (Vector3?)null);
            reflector.Apagar();
            if (pantalla) pantalla.Atenuar(false);
        }

        /// <summary>Aplica un evento a la escena (animación, cámara y reflector). El texto y el marcador los dibuja InterfazDebate.</summary>
        public void Aplicar(Evento e)
        {
            switch (e.tipo)
            {
                case "PITCH":
                    if (e.pitch != null && pantalla) pantalla.MostrarOferta(e.pitch.nombre, e.pitch.monto_mxn, e.pitch.porcentaje, e.valuacion_mxn, e.pitch.sector);
                    emprendedor.Presentar();
                    foreach (var t in panel) t.Escuchar();
                    VistaPodio();
                    break;
                case "PANEL":
                    VistaPodio();
                    break;
                case "PREGUNTA":
                {
                    var t = Buscar(e.emisor);
                    foreach (var o in panel) if (o != t) o.Escuchar();
                    emprendedor.Escuchar();
                    if (t) { t.Preguntar(); Enfocar(t); }
                    break;
                }
                case "RESPUESTA":
                {
                    emprendedor.Responder();
                    foreach (var t in panel) { if (t.perfilId == e.destino) t.Escuchar(); else t.Evaluar(); }
                    VistaPodio();
                    break;
                }
                case "REPLICA":
                {
                    var t = Buscar(e.emisor);
                    emprendedor.Escuchar();
                    foreach (var o in panel) if (o != t) o.Escuchar();
                    var dest = Buscar(e.destino);
                    if (t) { t.Replicar(); t.MirarA(dest ? dest.transform : emprendedor.transform); Enfocar(t); }
                    break;
                }
                case "VOTO":
                {
                    var t = Buscar(e.emisor);
                    if (t) { t.MirarA(emprendedor.transform); t.Votar(e.decision); Enfocar(t); }
                    break;
                }
                case "NEGOCIACION":
                case "OFERTA":
                    emprendedor.Negociar();
                    foreach (var t in panel) t.Evaluar();
                    VistaPodio();
                    break;
                case "DECISION":
                {
                    bool trato = e.resultado == "TRATO";
                    foreach (var t in panel) t.Reaccionar(trato);
                    emprendedor.RecibirResultado(trato);
                    if (letreroResultado) { letreroResultado.text = trato ? "TRATO" : "SIN TRATO"; letreroResultado.color = trato ? Estilos.Dentro : Estilos.Fuera; }
                    if (pantalla && trato && e.condiciones != null) pantalla.MostrarTexto("TRATO", Perfiles.Mxn(e.condiciones.monto_mxn) + " por el " + e.condiciones.porcentaje.ToString("0.##") + " %");
                    else if (pantalla) pantalla.MostrarTexto("SIN TRATO", "El panel no invirtió");
                    camara.Cambiar(Plano.General, null);
                    reflector.Apagar();
                    if (pantalla) pantalla.Atenuar(false);
                    break;
                }
                case "REPORTE":
                    camara.Cambiar(Plano.General, null);
                    break;
            }
        }
    }
}
