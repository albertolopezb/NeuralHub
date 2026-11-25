using UnityEngine;

namespace NeuralHub
{
    /// <summary>Pantalla grande del fondo: negocio, monto, porcentaje, valuación e ícono del sector del pitch.</summary>
    public class Pantalla : MonoBehaviour
    {
        public TextMesh titulo, detalle;
        public Renderer icono;

        public void MostrarOferta(string negocio, double monto, double pct, double valuacion, string sector = null)
        {
            if (titulo) titulo.text = negocio;
            if (detalle) detalle.text = "Pide " + Perfiles.Mxn(monto) + " por el " + pct.ToString("0.##") + " %\nValuación: " + Perfiles.Mxn(valuacion);
            MostrarIcono(sector);
        }

        public void MostrarIcono(string sector)
        {
            if (!icono) return;
            var tex = string.IsNullOrEmpty(sector) ? null : Resources.Load<Texture2D>("Iconos/" + sector);
            icono.enabled = tex != null;
            if (tex) icono.material.mainTexture = tex;
        }

        public void MostrarTexto(string t, string d) { if (titulo) titulo.text = t; if (detalle) detalle.text = d; MostrarIcono(null); }
    }
}
