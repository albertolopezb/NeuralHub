using UnityEngine;

namespace NeuralHub
{
    /// <summary>Mueve lentamente la luz con textura de cáusticas para simular la luz que atraviesa el agua.</summary>
    public class Caustica : MonoBehaviour
    {
        public float velocidad = 4f;
        Quaternion baseRot;
        void Start() { baseRot = transform.rotation; }
        void Update()
        {
            float t = Time.time;
            transform.rotation = baseRot * Quaternion.Euler(2.5f * Mathf.Sin(t * 0.3f), t * velocidad, 2.5f * Mathf.Cos(t * 0.23f));
        }
    }
}
