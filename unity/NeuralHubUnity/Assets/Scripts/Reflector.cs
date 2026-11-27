using UnityEngine;
using System.Collections.Generic;

namespace NeuralHub
{
    /// <summary>
    /// Spotlight cálido que cae desde arriba sobre el tiburón en turno, con haz visible y círculo de luz en el piso.
    /// Se enciende y se apaga con un desvanecido.
    /// </summary>
    public class Reflector : MonoBehaviour
    {
        public Light luz;
        public Renderer haz;        // cono aditivo (hijo de la luz)
        public Renderer charco;     // círculo de luz en el piso
        public float intensidad = 2.6f, alturaLuz = 4.6f;
        public float alfaHaz = 0.016f, alfaCharco = 0.16f;
        public Color colorLuz = new Color(1f, 0.835f, 0.6f);

        Vector3 objetivo;
        bool encendido;
        float nivel;   // 0 apagado .. 1 encendido
        Material matHaz, matCharco;
        static readonly int Tinte = Shader.PropertyToID("_TintColor");

        void Awake()
        {
            if (!luz) luz = GetComponent<Light>();
            if (haz) matHaz = haz.material;
            if (charco) matCharco = charco.material;
            AplicarNivel();
        }

        public void Iluminar(Vector3 punto)
        {
            bool salto = nivel < 0.05f;
            objetivo = punto; encendido = true;
            if (luz) luz.enabled = true;
            if (salto) Colocar(1f);
        }

        public void Apagar() { encendido = false; }

        void Colocar(float k)
        {
            var pos = new Vector3(objetivo.x, alturaLuz, objetivo.z - 0.2f);
            transform.position = Vector3.Lerp(transform.position, pos, k);
            var mira = new Vector3(objetivo.x, 0.6f, objetivo.z) - transform.position;
            if (mira.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(mira), k);
            if (charco) charco.transform.position = Vector3.Lerp(charco.transform.position, new Vector3(objetivo.x, 0.09f, objetivo.z - 0.15f), k);
        }

        void AplicarNivel()
        {
            if (luz) { luz.intensity = intensidad * nivel; luz.color = colorLuz; luz.enabled = nivel > 0.01f; }
            if (matHaz) matHaz.SetColor(Tinte, new Color(colorLuz.r, colorLuz.g, colorLuz.b, alfaHaz * nivel));
            if (matCharco) matCharco.SetColor(Tinte, new Color(colorLuz.r, colorLuz.g, colorLuz.b, alfaCharco * nivel));
            if (haz) haz.enabled = nivel > 0.01f;
            if (charco) charco.enabled = nivel > 0.01f;
        }

        void Update()
        {
            nivel = Mathf.MoveTowards(nivel, encendido ? 1f : 0f, Time.deltaTime * 2.2f);
            if (encendido) Colocar(1f - Mathf.Exp(-5f * Time.deltaTime));
            AplicarNivel();
        }
    }
}
