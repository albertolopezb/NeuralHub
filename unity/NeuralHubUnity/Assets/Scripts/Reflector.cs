using UnityEngine;
using System.Collections.Generic;

namespace NeuralHub
{
    /// <summary>Spotlight que ilumina a quien tiene el turno.</summary>
    public class Reflector : MonoBehaviour
    {
        public Light luz;
        Vector3 objetivo;
        void Awake() { if (!luz) luz = GetComponent<Light>(); }
        public void Iluminar(Vector3 punto) { objetivo = punto; if (luz) luz.enabled = true; }
        public void Apagar() { if (luz) luz.enabled = false; }
        void Update()
        {
            if (!luz || !luz.enabled) return;
            Vector3 deseado = objetivo + new Vector3(0, 5.5f, -1.2f);
            transform.position = Vector3.Lerp(transform.position, deseado, 5f * Time.deltaTime);
            var rot = Quaternion.LookRotation(objetivo - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, 6f * Time.deltaTime);
        }
    }
}
