using UnityEngine;
using System.Collections.Generic;

namespace NeuralHub
{
    public enum EstadoTiburon { FueraDelPanel, Esperando, Escuchando, Preguntando, Evaluando, Replicando, Votando, Reaccionando }

    /// <summary>Un tiburón del panel. Su estado (máquina de estados del diagrama) decide la animación.</summary>
    public class Tiburon : MonoBehaviour
    {
        public string perfilId;
        public string nombre;
        public EstadoTiburon estado = EstadoTiburon.FueraDelPanel;
        public Transform cuerpo, cabeza, aleta, paleta;
        public TextMesh letrero;
        public Renderer[] pintables;
        public Color color = Color.gray;

        Vector3 escalaCuerpo, posCabeza;
        float tiempoEstado;
        Color colorVoto = Color.gray;
        bool aFavor;
        Transform mirarA;

        void Awake()
        {
            if (cuerpo) escalaCuerpo = cuerpo.localScale;
            if (cabeza) posCabeza = cabeza.localPosition;
            if (paleta) paleta.gameObject.SetActive(false);
        }

        public void Asignar(string id, string nom)
        {
            perfilId = id; nombre = nom; color = Perfiles.Color(id);
            if (letrero) letrero.text = Perfiles.Corto(id);
            foreach (var r in pintables) if (r) r.material.color = color;
            gameObject.SetActive(true);
            CambiarEstado(EstadoTiburon.Esperando);
        }

        public void Retirar() { CambiarEstado(EstadoTiburon.FueraDelPanel); gameObject.SetActive(false); }

        public void MirarA(Transform t) { mirarA = t; }

        public void CambiarEstado(EstadoTiburon nuevo)
        {
            estado = nuevo; tiempoEstado = 0f;
            if (paleta && nuevo != EstadoTiburon.Votando && nuevo != EstadoTiburon.Reaccionando) paleta.gameObject.SetActive(false);
        }

        // Acciones del modelo de la clase (escuchar, preguntar, replicar, votar, reaccionar)
        public void Escuchar() { CambiarEstado(EstadoTiburon.Escuchando); }
        public void Preguntar() { CambiarEstado(EstadoTiburon.Preguntando); }
        public void Replicar() { CambiarEstado(EstadoTiburon.Replicando); }
        public void Evaluar() { CambiarEstado(EstadoTiburon.Evaluando); }

        public void Votar(string decision)
        {
            aFavor = decision != "salir";
            colorVoto = decision == "invertir" ? Perfiles.Hex("#2F9E44") : decision == "contraoferta" ? Perfiles.Hex("#F59F00") : Perfiles.Hex("#C92A2A");
            if (paleta) { paleta.gameObject.SetActive(true); paleta.GetComponent<Renderer>().material.color = colorVoto; }
            CambiarEstado(EstadoTiburon.Votando);
        }

        public void Reaccionar(bool trato)
        {
            // Celebra si hubo trato y votó a favor; se desploma si no hubo trato.
            aFavor = trato && aFavor;
            CambiarEstado(EstadoTiburon.Reaccionando);
        }

        void Update()
        {
            if (estado == EstadoTiburon.FueraDelPanel || cuerpo == null) return;
            tiempoEstado += Time.deltaTime;
            float t = Time.time;
            Vector3 esc = escalaCuerpo;
            Vector3 pc = posCabeza;
            Quaternion giroCabeza = Quaternion.identity;
            switch (estado)
            {
                case EstadoTiburon.Esperando: esc *= 1f + 0.01f * Mathf.Sin(t * 1.5f); break;
                case EstadoTiburon.Escuchando: giroCabeza = Quaternion.Euler(0, 0, 6f * Mathf.Sin(t * 1.2f)); break;
                case EstadoTiburon.Preguntando: pc += new Vector3(0, 0.05f * Mathf.Abs(Mathf.Sin(t * 6f)), 0.08f); esc *= 1.04f; break;
                case EstadoTiburon.Replicando: pc += new Vector3(0.06f * Mathf.Sin(t * 5f), 0.03f, 0.05f); giroCabeza = Quaternion.Euler(0, 15f * Mathf.Sin(t * 3f), 0); break;
                case EstadoTiburon.Evaluando: giroCabeza = Quaternion.Euler(10f, 0, 0); break;
                case EstadoTiburon.Votando: pc += new Vector3(0, 0.04f, 0); break;
                case EstadoTiburon.Reaccionando:
                    if (aFavor) pc += new Vector3(0, 0.12f * Mathf.Abs(Mathf.Sin(t * 7f)), 0);          // salta de gusto
                    else { esc = new Vector3(escalaCuerpo.x, escalaCuerpo.y * 0.93f, escalaCuerpo.z); giroCabeza = Quaternion.Euler(25f, 0, 0); }
                    break;
            }
            cuerpo.localScale = Vector3.Lerp(cuerpo.localScale, esc, 8f * Time.deltaTime);
            if (cabeza)
            {
                cabeza.localPosition = Vector3.Lerp(cabeza.localPosition, pc, 8f * Time.deltaTime);
                cabeza.localRotation = Quaternion.Slerp(cabeza.localRotation, giroCabeza, 8f * Time.deltaTime);
            }
            if (mirarA)
            {
                Vector3 d = mirarA.position - transform.position; d.y = 0;
                if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), 4f * Time.deltaTime);
            }
        }

        public Vector3 PuntoCabeza() { return cabeza ? cabeza.position : transform.position + Vector3.up * 1.6f; }
    }
}
