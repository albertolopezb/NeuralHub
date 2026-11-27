using UnityEngine;
using System.Collections.Generic;

namespace NeuralHub
{
    public enum Plano { General, Emprendedor, Tiburon, Pantalla, Arriba }

    /// <summary>
    /// Cámara en primera persona desde el podio del emprendedor. Cuando un tiburón habla, se acerca un poco a él
    /// (y cierra el campo de visión); cuando habla el emprendedor, vuelve a la vista del podio.
    /// </summary>
    public class CamaraEstudio : MonoBehaviour
    {
        public Transform centro;
        public Vector3 posGeneral = new Vector3(0, 1.62f, -4.35f);   // ojos del emprendedor
        public Vector3 miraGeneral = new Vector3(0, 0.95f, 0.3f);
        public float fovGeneral = 50f, fovTiburon = 40f;
        public float distanciaTiburon = 3.1f;
        public Plano planoActual = Plano.General;
        public bool instantaneo;

        Vector3 posObj; Quaternion rotObj; float fovObj = 50f;
        Camera cam;

        void Awake() { cam = GetComponent<Camera>(); }
        void Start() { Cambiar(Plano.General, null); Aplicar(); }

        /// <summary>Posición, rotación y campo de visión de un plano (también lo usa RenderEscena).</summary>
        public void Calcular(Plano plano, Vector3? punto, out Vector3 pos, out Quaternion rot, out float fov)
        {
            fov = fovGeneral;
            switch (plano)
            {
                case Plano.Arriba:
                    pos = new Vector3(0, 9f, 0.1f); rot = Quaternion.Euler(90, 0, 0); fov = 55f; break;
                case Plano.Pantalla:
                    pos = new Vector3(0, 1.9f, -1.2f); rot = Quaternion.LookRotation(new Vector3(0, 2.45f, 3.36f) - pos); fov = 45f; break;
                case Plano.Tiburon:
                {
                    var p = punto ?? new Vector3(0, 1.3f, 0.75f);
                    var mira = p + Vector3.down * 0.42f;
                    var hacia = posGeneral - mira; hacia.y = 0;
                    pos = mira + hacia.normalized * distanciaTiburon; pos.y = 1.42f;
                    rot = Quaternion.LookRotation(mira - pos); fov = fovTiburon; break;
                }
                default: // General y Emprendedor: primera persona desde el podio
                    pos = posGeneral; rot = Quaternion.LookRotation(miraGeneral - posGeneral); break;
            }
        }

        public void Cambiar(Plano plano, Vector3? punto)
        {
            planoActual = plano;
            Calcular(plano, punto, out posObj, out rotObj, out fovObj);
            if (instantaneo) Aplicar();
        }

        void Aplicar()
        {
            transform.position = posObj; transform.rotation = rotObj;
            if (cam) cam.fieldOfView = fovObj;
        }

        void LateUpdate()
        {
            float k = 1f - Mathf.Exp(-2.6f * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, posObj, k);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotObj, k);
            if (cam) cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, fovObj, k);
        }
    }
}
