using UnityEngine;
using System.Collections.Generic;

namespace NeuralHub
{
    /// <summary>Pantalla grande del fondo: negocio, monto y porcentaje.</summary>
    public class Pantalla : MonoBehaviour
    {
        public TextMesh titulo, detalle;
        public void MostrarOferta(string negocio, double monto, double pct, double valuacion)
        {
            if (titulo) titulo.text = negocio;
            if (detalle) detalle.text = "Pide " + Perfiles.Mxn(monto) + " por el " + pct.ToString("0.##") + " %\nValuación: " + Perfiles.Mxn(valuacion);
        }
        public void MostrarTexto(string t, string d) { if (titulo) titulo.text = t; if (detalle) detalle.text = d; }
    }
}
