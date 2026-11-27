using UnityEngine;
using System.Collections.Generic;

namespace NeuralHub
{
    public enum EstadoTiburon { FueraDelPanel, Esperando, Escuchando, Preguntando, Evaluando, Replicando, Votando, Reaccionando }

    /// <summary>
    /// Un tiburón del panel: inversionista low-poly sentado en su sillón. Su estado (máquina de estados del diagrama)
    /// decide la animación, que se hace con giros sutiles de cabeza y la respiración del torso.
    /// </summary>
    public class Tiburon : MonoBehaviour
    {
        public string perfilId;
        public string nombre;
        public int pose;                      // 0, 1 o 2: depende del sillón
        public EstadoTiburon estado = EstadoTiburon.FueraDelPanel;
        public Transform cuerpo, cabeza, aleta, paleta;   // aleta y paleta ya no se usan (se conservan por compatibilidad)
        public TextMesh letrero;
        public Renderer[] pintables;
        public MaterialesFigura materiales = new MaterialesFigura();
        public Color color = Color.gray;

        Transform figura;
        Vector3 escalaCuerpo = Vector3.one;
        Quaternion giroBase = Quaternion.identity;
        float tiempoEstado, fase;
        bool aFavor;
        Transform mirarA;

        void Awake() { fase = Random.value * 6.28f; if (paleta) paleta.gameObject.SetActive(false); }

        /// <summary>Crea (o vuelve a crear) la figura con la pose de su sillón y el peinado de su perfil.</summary>
        public void Construir()
        {
            if (figura) { if (Application.isPlaying) Destroy(figura.gameObject); else DestroyImmediate(figura.gameObject); }
            var partes = FiguraInversionista.Construir(transform, pose, perfilId, materiales);
            figura = partes.raiz; cuerpo = partes.torso; cabeza = partes.cabeza;
            escalaCuerpo = cuerpo.localScale; giroBase = cabeza.localRotation;
        }

        public void Asignar(string id, string nom)
        {
            perfilId = id; nombre = nom; color = Perfiles.Color(id);
            Construir();
            if (letrero) letrero.text = Perfiles.Corto(id).ToUpper();
            gameObject.SetActive(true);
            CambiarEstado(EstadoTiburon.Esperando);
        }

        public void Retirar() { CambiarEstado(EstadoTiburon.FueraDelPanel); gameObject.SetActive(false); }

        /// <summary>Gira la cabeza hacia el objetivo (sin mover el sillón).</summary>
        public void MirarA(Transform t) { mirarA = t; }

        public void CambiarEstado(EstadoTiburon nuevo) { estado = nuevo; tiempoEstado = 0f; }

        // Acciones del modelo de la clase (escuchar, preguntar, replicar, votar, reaccionar)
        public void Escuchar() { CambiarEstado(EstadoTiburon.Escuchando); }
        public void Preguntar() { CambiarEstado(EstadoTiburon.Preguntando); }
        public void Replicar() { CambiarEstado(EstadoTiburon.Replicando); }
        public void Evaluar() { CambiarEstado(EstadoTiburon.Evaluando); }

        public void Votar(string decision)
        {
            aFavor = decision != "salir";
            CambiarEstado(EstadoTiburon.Votando);
        }

        public void Reaccionar(bool trato)
        {
            // Asiente si hubo trato y votó a favor; baja la mirada si no.
            aFavor = trato && aFavor;
            CambiarEstado(EstadoTiburon.Reaccionando);
        }

        void Update()
        {
            if (estado == EstadoTiburon.FueraDelPanel || cuerpo == null || cabeza == null) return;
            tiempoEstado += Time.deltaTime;
            float t = Time.time + fase;
            float respira = 1f + 0.008f * Mathf.Sin(t * 1.4f);
            Vector3 giro = Vector3.zero; // grados: x = asentir (positivo baja la cabeza), y = voltear, z = ladear
            switch (estado)
            {
                case EstadoTiburon.Escuchando: giro = new Vector3(0, 0, 3f * Mathf.Sin(t * 0.9f)); break;
                case EstadoTiburon.Preguntando: giro = new Vector3(-4f + 2.5f * Mathf.Sin(t * 5f), 0, 0); respira *= 1.006f; break;
                case EstadoTiburon.Replicando: giro = new Vector3(0, 6f * Mathf.Sin(t * 2.2f), 0); break;
                case EstadoTiburon.Evaluando: giro = new Vector3(7f, 0, 0); break;
                case EstadoTiburon.Votando: giro = new Vector3(tiempoEstado < 1.2f ? 8f * Mathf.Abs(Mathf.Sin(tiempoEstado * 5f)) : 0, 0, 0); break;
                case EstadoTiburon.Reaccionando:
                    giro = aFavor ? new Vector3(tiempoEstado < 1.6f ? 10f * Mathf.Abs(Mathf.Sin(tiempoEstado * 6f)) : 0, 0, 0) : new Vector3(14f, 0, 0);
                    break;
            }
            if (mirarA && estado == EstadoTiburon.Replicando)
            {
                Vector3 d = transform.InverseTransformPoint(mirarA.position); d.y = 0;
                if (d.sqrMagnitude > 0.01f) giro.y += Mathf.Clamp(Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, -35f, 35f);
            }
            cuerpo.localScale = Vector3.Lerp(cuerpo.localScale, new Vector3(escalaCuerpo.x, escalaCuerpo.y * respira, escalaCuerpo.z), 8f * Time.deltaTime);
            cabeza.localRotation = Quaternion.Slerp(cabeza.localRotation, giroBase * Quaternion.Euler(giro), 5f * Time.deltaTime);
        }

        public Vector3 PuntoCabeza() { return cabeza ? cabeza.position : transform.position + Vector3.up * 1.3f; }
    }
}
