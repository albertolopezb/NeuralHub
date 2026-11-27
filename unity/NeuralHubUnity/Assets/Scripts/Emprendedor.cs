using UnityEngine;
using System.Collections.Generic;

namespace NeuralHub
{
    /// <summary>El emprendedor (avatar del usuario) de pie junto al podio.</summary>
    public class Emprendedor : MonoBehaviour
    {
        public enum Estado { Esperando, Presentando, Escuchando, Respondiendo, Negociando, RecibiendoResultado }
        public Estado estado = Estado.Esperando;
        public Transform cuerpo, cabeza;
        Vector3 escala, posCabeza;
        bool trato;

        void Awake() { if (cuerpo) escala = cuerpo.localScale; if (cabeza) posCabeza = cabeza.localPosition; }

        public void Presentar() { estado = Estado.Presentando; }
        public void Responder() { estado = Estado.Respondiendo; }
        public void Negociar() { estado = Estado.Negociando; }
        public void Escuchar() { estado = Estado.Escuchando; }
        public void RecibirResultado(bool esTrato) { trato = esTrato; estado = Estado.RecibiendoResultado; }

        // ---------- personaje animado (cuerpo de traje + animaciones de Mixamo) ----------
        public Animator animador;
        public Transform puntoCabeza;
        string animActual;
        float enEstado;
        Estado anterior;

        string Animacion()
        {
            switch (estado)
            {
                case Estado.Presentando: return enEstado < 4.5f ? "Saludo" : "Hablando";
                case Estado.Respondiendo: return "PreguntaDosManos";
                case Estado.Negociando: return "GestoFeliz";
                case Estado.RecibiendoResultado: return trato ? "Victoria" : "Decepcionado";
                default: return "ReposoFeliz";
            }
        }

        void Update()
        {
            if (animador)
            {
                if (estado != anterior) { anterior = estado; enEstado = 0; }
                enEstado += Time.deltaTime;
                string a = Animacion();
                if (a != animActual) { animador.CrossFadeInFixedTime(a, 0.35f); animActual = a; }
                return;
            }
            if (!cuerpo) return;
            float t = Time.time;
            Vector3 pc = posCabeza; Vector3 esc = escala;
            switch (estado)
            {
                case Estado.Presentando: pc += new Vector3(0, 0.04f * Mathf.Abs(Mathf.Sin(t * 4f)), 0); break;
                case Estado.Respondiendo: pc += new Vector3(0.04f * Mathf.Sin(t * 5f), 0, 0); break;
                case Estado.Negociando: esc *= 1.03f; break;
                case Estado.RecibiendoResultado:
                    if (trato) pc += new Vector3(0, 0.14f * Mathf.Abs(Mathf.Sin(t * 7f)), 0); else esc = new Vector3(escala.x, escala.y * 0.94f, escala.z);
                    break;
                default: esc *= 1f + 0.01f * Mathf.Sin(t * 1.5f); break;
            }
            cuerpo.localScale = Vector3.Lerp(cuerpo.localScale, esc, 8f * Time.deltaTime);
            if (cabeza) cabeza.localPosition = Vector3.Lerp(cabeza.localPosition, pc, 8f * Time.deltaTime);
        }

        public Vector3 PuntoCabeza()
        {
            if (puntoCabeza) return puntoCabeza.position;
            return cabeza ? cabeza.position : transform.position + Vector3.up * 1.6f;
        }
    }
}
