using UnityEngine;
using System.Collections.Generic;

namespace NeuralHub
{
    /// <summary>El estudio: coloca los 3 tiburones elegidos en los sillones y aplica cada evento a la escena.</summary>
    public class Estudio : MonoBehaviour
    {
        public Tiburon prefabTiburon;
        public Transform[] sillones;            // 3 sillones fijos
        public Emprendedor emprendedor;
        public CamaraEstudio camara;
        public Reflector reflector;
        public Pantalla pantalla;
        public TextMesh letreroResultado;
        public Transform podio;

        public readonly List<Tiburon> panel = new List<Tiburon>();

        public void Preparar() { if (pantalla) pantalla.MostrarTexto("NeuralHub", "Preparando sesión..."); }

        public void IniciarSesion(string[] ids, string[] nombres)
        {
            foreach (var t in panel) if (t) Destroy(t.gameObject);
            panel.Clear();
            for (int i = 0; i < sillones.Length && i < ids.Length; i++)
            {
                var t = Instantiate(prefabTiburon, sillones[i].position - sillones[i].forward * 0.05f, Quaternion.identity, transform);
                Vector3 haciaCentro = emprendedor ? emprendedor.transform.position - t.transform.position : Vector3.back;
                haciaCentro.y = 0;
                t.transform.rotation = Quaternion.LookRotation(haciaCentro);
                t.Asignar(ids[i], nombres != null && i < nombres.Length ? nombres[i] : ids[i]);
                t.MirarA(emprendedor ? emprendedor.transform : null);
                panel.Add(t);
            }
            if (letreroResultado) letreroResultado.text = "";
            camara.Cambiar(Plano.General, null);
            reflector.Apagar();
        }

        public Tiburon Buscar(string id)
        {
            foreach (var t in panel) if (t && t.perfilId == id) return t;
            return null;
        }

        void TodosEscuchan(string excepto)
        {
            foreach (var t in panel) if (t.perfilId != excepto && t.estado != EstadoTiburon.Votando) t.Escuchar();
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
                    camara.Cambiar(Plano.Emprendedor, emprendedor.PuntoCabeza());
                    reflector.Iluminar(emprendedor.transform.position + Vector3.up * 1.2f);
                    break;
                case "PANEL":
                    camara.Cambiar(Plano.General, null);
                    reflector.Apagar();
                    break;
                case "PREGUNTA":
                {
                    var t = Buscar(e.emisor);
                    foreach (var o in panel) if (o != t) o.Escuchar();
                    emprendedor.Escuchar();
                    if (t) { t.Preguntar(); camara.Cambiar(Plano.Tiburon, t.PuntoCabeza()); reflector.Iluminar(t.transform.position + Vector3.up * 1.2f); }
                    break;
                }
                case "RESPUESTA":
                {
                    emprendedor.Responder();
                    foreach (var t in panel) { if (t.perfilId == e.destino) t.Escuchar(); else t.Evaluar(); }
                    camara.Cambiar(Plano.Emprendedor, emprendedor.PuntoCabeza());
                    reflector.Iluminar(emprendedor.transform.position + Vector3.up * 1.2f);
                    break;
                }
                case "REPLICA":
                {
                    var t = Buscar(e.emisor);
                    emprendedor.Escuchar();
                    foreach (var o in panel) if (o != t) o.Escuchar();
                    var dest = Buscar(e.destino);
                    if (t) { t.Replicar(); if (dest) t.MirarA(dest.transform); camara.Cambiar(Plano.Tiburon, t.PuntoCabeza()); reflector.Iluminar(t.transform.position + Vector3.up * 1.2f); }
                    break;
                }
                case "VOTO":
                {
                    var t = Buscar(e.emisor);
                    if (t) { t.MirarA(emprendedor.transform); t.Votar(e.decision); camara.Cambiar(Plano.Tiburon, t.PuntoCabeza()); reflector.Iluminar(t.transform.position + Vector3.up * 1.2f); }
                    break;
                }
                case "NEGOCIACION":
                case "OFERTA":
                    emprendedor.Negociar();
                    foreach (var t in panel) t.Evaluar();
                    camara.Cambiar(Plano.Emprendedor, emprendedor.PuntoCabeza());
                    reflector.Iluminar(emprendedor.transform.position + Vector3.up * 1.2f);
                    if (e.tipo == "OFERTA" && pantalla && e.pitch == null) { /* la pantalla se actualiza con el texto de la oferta en la interfaz */ }
                    break;
                case "DECISION":
                {
                    bool trato = e.resultado == "TRATO";
                    foreach (var t in panel) t.Reaccionar(trato);
                    emprendedor.RecibirResultado(trato);
                    if (letreroResultado) { letreroResultado.text = trato ? "TRATO" : "SIN TRATO"; letreroResultado.color = trato ? Perfiles.Hex("#2F9E44") : Perfiles.Hex("#C92A2A"); }
                    if (pantalla && trato && e.condiciones != null) pantalla.MostrarTexto("TRATO", Perfiles.Mxn(e.condiciones.monto_mxn) + " por el " + e.condiciones.porcentaje.ToString("0.##") + " %");
                    else if (pantalla) pantalla.MostrarTexto("SIN TRATO", "El panel no invirtió");
                    camara.Cambiar(Plano.General, null);
                    reflector.Apagar();
                    break;
                }
                case "REPORTE":
                    camara.Cambiar(Plano.General, null);
                    break;
            }
        }
    }
}
