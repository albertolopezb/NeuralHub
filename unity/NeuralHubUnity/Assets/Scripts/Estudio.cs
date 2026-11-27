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
        public SetEmprendedor setEmprendedor;     // escenografía detrás del emprendedor
        public Transform podio;

        public readonly List<Tiburon> panel = new List<Tiburon>();
        PitchDto pitch;                          // el pitch en curso, para las láminas de la pantalla
        double valuacion;

        public void Preparar() { if (pantalla) pantalla.MostrarTexto("NeuralHub", "Preparando sesión…"); }

        public void IniciarSesion(string[] ids, string[] nombres)
        {
            foreach (var t in panel) if (t) Destroy(t.gameObject);
            panel.Clear();
            for (int i = 0; i < sillones.Length && i < ids.Length; i++)
            {
                var t = Instantiate(prefabTiburon, sillones[i].position, Quaternion.identity, transform);
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

        public void RetirarPanel()
        {
            foreach (var t in panel) if (t) Destroy(t.gameObject);
            panel.Clear();
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
                    if (e.pitch != null) { pitch = e.pitch; valuacion = e.valuacion_mxn; TemasPitch.Mostrar(pantalla, pitch, valuacion, "portada", "Tu pitch"); if (setEmprendedor) setEmprendedor.Vestir(pitch.nombre, pitch.sector); }
                    emprendedor.Presentar();
                    foreach (var t in panel) t.Escuchar();
                    camara.Cambiar(Plano.Emprendedor, emprendedor.PuntoCabeza());
                    reflector.Iluminar(emprendedor.transform.position + Vector3.up * 1.2f);
                    break;
                case "PANEL":
                    TemasPitch.Mostrar(pantalla, pitch, valuacion, "solucion", "La solución");
                    camara.Cambiar(Plano.General, null);
                    reflector.Apagar();
                    break;
                case "PREGUNTA":
                {
                    var t = Buscar(e.emisor);
                    foreach (var o in panel) if (o != t) o.Escuchar();
                    emprendedor.Escuchar();
                    if (t) { t.Preguntar(); camara.Cambiar(Plano.Tiburon, t.PuntoCabeza()); reflector.Iluminar(t.transform.position + Vector3.up * 1.2f); }
                    TemasPitch.Mostrar(pantalla, pitch, valuacion, TemasPitch.Detectar(e.texto, e.emisor), "Pregunta de " + Perfiles.Corto(e.emisor));
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
                    if (t) { t.Replicar(e.postura); if (dest) t.MirarA(dest.transform); camara.Cambiar(Plano.Tiburon, t.PuntoCabeza()); reflector.Iluminar(t.transform.position + Vector3.up * 1.2f); }
                    break;
                }
                case "VOTO":
                {
                    var t = Buscar(e.emisor);
                    if (pantalla) pantalla.MostrarLamina("tema_votacion", e.votacion == 2 ? "Votación final" : "Veredicto", Perfiles.Corto(e.emisor) + ": " + Estilos.TextoVoto(e.decision),
                        pitch != null ? "Cada tiburón explica su decisión sobre " + pitch.nombre + "." : "");
                    if (t) { t.MirarA(emprendedor.transform); t.Votar(e.decision); camara.Cambiar(Plano.Tiburon, t.PuntoCabeza()); reflector.Iluminar(t.transform.position + Vector3.up * 1.2f); }
                    break;
                }
                case "NEGOCIACION":
                case "OFERTA":
                    emprendedor.Negociar();
                    foreach (var t in panel) t.Evaluar();
                    camara.Cambiar(Plano.Emprendedor, emprendedor.PuntoCabeza());
                    reflector.Iluminar(emprendedor.transform.position + Vector3.up * 1.2f);
                    if (pantalla) pantalla.MostrarLamina("tema_oferta", "Negociación", e.tipo == "OFERTA" ? "Nueva oferta" : "¿Negociamos?", e.texto);
                    break;
                case "DECISION":
                {
                    bool trato = e.resultado == "TRATO";
                    foreach (var t in panel) t.Reaccionar(trato);
                    emprendedor.RecibirResultado(trato);
                    if (letreroResultado) letreroResultado.text = "";   // el resultado ya aparece en la pantalla y en la interfaz
                    if (pantalla && trato && e.condiciones != null) pantalla.MostrarLamina("tema_trato", "Decisión del panel", "¡Trato hecho!", Perfiles.Mxn(e.condiciones.monto_mxn) + " por el " + e.condiciones.porcentaje.ToString("0.##") + " % de la empresa");
                    else if (pantalla) pantalla.MostrarLamina("tema_sin_trato", "Decisión del panel", "Sin trato", "El panel no invirtió esta vez. Revisa la retroalimentación y vuelve a intentarlo.");
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
