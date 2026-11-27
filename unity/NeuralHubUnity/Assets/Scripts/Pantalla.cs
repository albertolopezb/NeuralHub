using UnityEngine;
using System.Collections.Generic;

namespace NeuralHub
{
    /// <summary>Pantalla grande del fondo: negocio, monto, porcentaje, valuación e ícono del sector. Se atenúa cuando habla un tiburón.</summary>
    public class Pantalla : MonoBehaviour
    {
        public TextMesh titulo, detalle, etiqueta;
        public Renderer fondo;
        public Renderer icono;  // ícono del sector del pitch (Resources/Iconos)
        public Light brillo;    // luz azul que emite la pantalla

        float nivel = 1f, nivelObj = 1f, brilloBase = -1f;
        readonly Dictionary<TextMesh, Color> colores = new Dictionary<TextMesh, Color>();
        Material matFondo, matIcono; Color colorFondo, emisionFondo, colorIcono;

        void Awake()
        {
            foreach (var t in new[] { titulo, detalle, etiqueta }) if (t) colores[t] = t.color;
            if (fondo) { matFondo = fondo.material; colorFondo = matFondo.color; emisionFondo = matFondo.GetColor("_EmissionColor"); }
            if (icono && icono.sharedMaterial && icono.sharedMaterial.HasProperty("_Color")) { matIcono = icono.material; colorIcono = matIcono.color; }
            if (brillo) brilloBase = brillo.intensity;
        }

        public void MostrarOferta(string negocio, double monto, double pct, double valuacion, string sector = null)
        {
            if (etiqueta) etiqueta.text = "TU NEGOCIO";
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

        public void MostrarTexto(string t, string d)
        {
            if (etiqueta) etiqueta.text = "";
            if (titulo) titulo.text = t; if (detalle) detalle.text = d;
            MostrarIcono(null);
        }

        /// <summary>Baja la pantalla para que no compita con el tiburón que habla.</summary>
        public void Atenuar(bool atenuar) { nivelObj = atenuar ? 0.3f : 1f; }

        void Update()
        {
            if (Mathf.Approximately(nivel, nivelObj)) return;
            nivel = Mathf.MoveTowards(nivel, nivelObj, Time.deltaTime * 1.6f);
            foreach (var kv in colores) { var c = kv.Value * nivel; c.a = kv.Value.a; kv.Key.color = c; }
            if (matFondo) { matFondo.color = colorFondo * nivel; matFondo.SetColor("_EmissionColor", emisionFondo * nivel); }
            if (matIcono) { var c = colorIcono * nivel; c.a = colorIcono.a * nivel; matIcono.color = c; }
            if (brillo && brilloBase >= 0) brillo.intensity = brilloBase * Mathf.Lerp(0.3f, 1f, (nivel - 0.3f) / 0.7f);
        }
    }
}
