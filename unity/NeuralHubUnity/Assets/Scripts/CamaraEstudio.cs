using UnityEngine;
using System.Collections.Generic;

namespace NeuralHub
{
    public enum Plano { General, Emprendedor, Tiburon, Pantalla, Arriba }

    /// <summary>Cámara principal que cambia de plano para enfocar a quien habla.</summary>
    public class CamaraEstudio : MonoBehaviour
    {
        public Transform centro;
        Vector3 posObj; Quaternion rotObj;
        public Vector3 posGeneral = new Vector3(0, 3.6f, -10.5f);
        public Vector3 miraGeneral = new Vector3(0, 1.8f, 3f);
        public Plano planoActual = Plano.General;
        public bool instantaneo;

        void Start() { Cambiar(Plano.General, null); transform.position = posObj; transform.rotation = rotObj; }

        public void Cambiar(Plano plano, Vector3? punto)
        {
            planoActual = plano;
            switch (plano)
            {
                case Plano.General:
                    posObj = posGeneral; rotObj = Quaternion.LookRotation(miraGeneral - posGeneral); break;
                case Plano.Arriba:
                    posObj = new Vector3(0, 17f, 1f); rotObj = Quaternion.Euler(90, 0, 0); break;
                case Plano.Pantalla:
                    posObj = new Vector3(0, 3.8f, -3f); rotObj = Quaternion.LookRotation(new Vector3(0, 4f, 9.5f) - posObj); break;
                case Plano.Emprendedor:
                {
                    var p = punto ?? new Vector3(0, 1.7f, -2.5f);
                    posObj = p + new Vector3(2.2f, 0.4f, 4.2f); rotObj = Quaternion.LookRotation(p - posObj); break;
                }
                case Plano.Tiburon:
                {
                    var p = punto ?? Vector3.zero;
                    Vector3 haciaCentro = (new Vector3(0, 0, -2.5f) - new Vector3(p.x, 0, p.z)).normalized;
                    Vector3 lado = Vector3.Cross(Vector3.up, haciaCentro);
                    posObj = p + haciaCentro * 4.5f + lado * 1.2f + Vector3.up * 0.25f;
                    rotObj = Quaternion.LookRotation(p + Vector3.down * 0.62f - posObj); break;
                }
            }
            if (instantaneo) { transform.position = posObj; transform.rotation = rotObj; }
        }

        void LateUpdate()
        {
            transform.position = Vector3.Lerp(transform.position, posObj, 3.2f * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotObj, 3.2f * Time.deltaTime);
        }
    }
}
