using UnityEngine;

namespace NeuralHub
{
    /// <summary>Hace que algas y medusas se mezan suavemente (ambiente de pecera).</summary>
    public class Ondular : MonoBehaviour
    {
        public float amplitud = 6f, velocidad = 1.2f, fase;
        public bool vertical;
        Quaternion baseRot; Vector3 basePos;
        void Start() { baseRot = transform.localRotation; basePos = transform.localPosition; fase = Random.value * 6.28f; }
        void Update()
        {
            float t = Time.time * velocidad + fase;
            transform.localRotation = baseRot * Quaternion.Euler(amplitud * Mathf.Sin(t), 0, amplitud * 0.7f * Mathf.Cos(t * 0.8f));
            if (vertical) transform.localPosition = basePos + new Vector3(0, 0.25f * Mathf.Sin(t), 0);
        }
    }
}
