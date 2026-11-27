using UnityEngine;
using System.Collections.Generic;

namespace NeuralHub
{
    /// <summary>
    /// El emprendedor es el usuario: la escena se ve en primera persona desde su podio, así que no tiene cuerpo visible.
    /// Se conserva su máquina de estados (presentando, escuchando, respondiendo...) para el resto del sistema.
    /// </summary>
    public class Emprendedor : MonoBehaviour
    {
        public enum Estado { Esperando, Presentando, Escuchando, Respondiendo, Negociando, RecibiendoResultado }
        public Estado estado = Estado.Esperando;
        public Transform cuerpo, cabeza;      // sin uso en primera persona (se conservan por compatibilidad)
        public float alturaOjos = 1.62f;
        bool trato;

        public void Presentar() { estado = Estado.Presentando; }
        public void Responder() { estado = Estado.Respondiendo; }
        public void Negociar() { estado = Estado.Negociando; }
        public void Escuchar() { estado = Estado.Escuchando; }
        public void RecibirResultado(bool esTrato) { trato = esTrato; estado = Estado.RecibiendoResultado; }

        public Vector3 PuntoCabeza() { return transform.position + Vector3.up * alturaOjos; }
    }
}
