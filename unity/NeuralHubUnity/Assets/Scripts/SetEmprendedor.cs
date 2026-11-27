using UnityEngine;

namespace NeuralHub
{
    /// <summary>
    /// Escenografía detrás del emprendedor (letrero del programa, emblema del sector y nombre de la empresa). Solo se
    /// muestra cuando la cámara lo enfoca, para no tapar las tomas generales.
    /// </summary>
    public class SetEmprendedor : MonoBehaviour
    {
        public GameObject contenido;
        public TextMesh empresa;
        public Renderer logo;
        CamaraEstudio camara;

        public void Vestir(string nombre, string sector)
        {
            if (empresa) empresa.text = Pantalla.Ajustar(nombre, 14, 2);
            if (!logo) return;
            var tex = Resources.Load<Texture2D>("Laminas/sector_" + sector);
            if (!tex) return;
            var b = new MaterialPropertyBlock(); b.SetTexture("_MainTex", tex); logo.SetPropertyBlock(b);
        }

        void LateUpdate()
        {
            if (!camara) camara = FindFirstObjectByType<CamaraEstudio>();
            if (!camara || !contenido) return;
            bool ver = camara.planoActual == Plano.Emprendedor;
            if (contenido.activeSelf != ver) contenido.SetActive(ver);
        }
    }
}
