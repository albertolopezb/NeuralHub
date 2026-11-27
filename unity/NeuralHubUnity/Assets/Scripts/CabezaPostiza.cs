using UnityEngine;

namespace NeuralHub
{
    /// <summary>
    /// Pone una cabeza de tiburón sobre el cuerpo humanoide de traje: encoge la cabeza humana (escala del hueso)
    /// y hace que la cabeza de tiburón siga la posición y el giro del hueso en cada cuadro.
    /// El desplazamiento y el giro se expresan en el espacio del personaje (z al frente, y arriba).
    /// </summary>
    public class CabezaPostiza : MonoBehaviour
    {
        public Transform raiz;                  // raíz del personaje
        public Transform hueso;                 // hueso Head del humanoide
        public Quaternion reposo = Quaternion.identity;   // giro del hueso respecto a la raíz en la pose de referencia
        public Vector3 desplazamiento;
        public Vector3 giro;
        public bool ocultarHumana = true;
        public bool soloOcultar;               // la cabeza de tiburón ya cuelga del hueso del cuello

        void LateUpdate() { Aplicar(); }

        public void Aplicar()
        {
            if (!hueso) return;
            if (ocultarHumana) hueso.localScale = Vector3.one * 0.001f;
            if (soloOcultar || !raiz) return;
            Quaternion delta = hueso.rotation * Quaternion.Inverse(raiz.rotation * reposo);
            float esc = raiz.lossyScale.y;
            transform.position = hueso.position + delta * (raiz.rotation * (desplazamiento * esc));
            transform.rotation = delta * raiz.rotation * Quaternion.Euler(giro);
        }
    }
}
