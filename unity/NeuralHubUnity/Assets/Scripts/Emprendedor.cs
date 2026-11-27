using UnityEngine;

namespace NeuralHub
{
    /// <summary>El emprendedor (avatar del usuario) en el círculo de presentación.</summary>
    public class Emprendedor : MonoBehaviour
    {
        public enum Estado { Esperando, Presentando, Escuchando, Respondiendo, Negociando, RecibiendoResultado }
        public Estado estado = Estado.Esperando;
        public Animator animador;

        string animActual;
        float enEstado;
        Estado anterior;
        bool trato;

        public void Presentar() { estado = Estado.Presentando; }
        public void Responder() { estado = Estado.Respondiendo; }
        public void Negociar() { estado = Estado.Negociando; }
        public void Escuchar() { estado = Estado.Escuchando; }
        public void RecibirResultado(bool esTrato) { trato = esTrato; estado = Estado.RecibiendoResultado; }

        string Animacion()
        {
            switch (estado)
            {
                case Estado.Presentando: return enEstado < 2.2f ? "Wave" : "Interact";
                case Estado.Respondiendo: return "Interact";
                case Estado.Negociando: return "Interact";
                case Estado.RecibiendoResultado: return trato ? "Wave" : (enEstado < 1.2f ? "HitRecieve" : "Idle_Neutral");
                case Estado.Escuchando: return "Idle_Neutral";
                default: return "Idle";
            }
        }

        void Update()
        {
            if (!animador) return;
            if (estado != anterior) { anterior = estado; enEstado = 0; animActual = null; }
            enEstado += Time.deltaTime;
            string a = Animacion();
            if (a != animActual) { animador.CrossFadeInFixedTime(a, 0.3f); animActual = a; }
        }

        public Vector3 PuntoCabeza() { return transform.position + Vector3.up * 1.75f; }
    }
}
