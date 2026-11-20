using UnityEngine;

namespace NeuralHub
{
    /// <summary>Pez de fondo que nada en círculos alrededor de un centro.</summary>
    public class Nadador : MonoBehaviour
    {
        public Vector3 centro; public float radio = 6f, velocidad = 0.4f, fase, ondaVertical = 0.4f;
        void Update()
        {
            float a = Time.time * velocidad + fase;
            Vector3 p = centro + new Vector3(Mathf.Cos(a) * radio, Mathf.Sin(a * 1.7f) * ondaVertical, Mathf.Sin(a) * radio * 0.4f);
            Vector3 d = p - transform.position;
            transform.position = p;
            if (d.sqrMagnitude > 1e-8f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), 5f * Time.deltaTime);
        }
    }
}
