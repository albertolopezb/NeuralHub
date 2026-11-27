using System.Collections.Generic;
using UnityEngine;

namespace NeuralHub
{
    /// <summary>Pantallas laterales del estudio: datos de la oferta (con el ícono del sector) y marcador del panel con votos y calificaciones.</summary>
    public class PantallasDatos : MonoBehaviour
    {
        public TextMesh tituloOferta, textoOferta, tituloPanel;
        public TextMesh[] nombres = new TextMesh[3];
        public TextMesh[] estados = new TextMesh[3];
        public Transform[] barras = new Transform[3];
        public Renderer[] barrasRend = new Renderer[3];
        public Renderer icono;
        public readonly List<string> ids = new List<string>();
        readonly Dictionary<string, float> objetivo = new Dictionary<string, float>();

        static string Cifra(double v) { return v > 0 ? Perfiles.Mxn(v) : "Aún no lo sabe"; }

        public void MostrarOferta(PitchDto p, double valuacion)
        {
            if (p == null) return;
            if (tituloOferta) tituloOferta.text = p.nombre;
            if (textoOferta)
                textoOferta.text = "Pide: " + Perfiles.Mxn(p.monto_mxn) + "\nPor: " + p.porcentaje.ToString("0.##") + " %" +
                                   "\nValuación: " + Perfiles.Mxn(valuacion) + "\nVentas 12 m: " + Cifra(p.ventas_12m_mxn) +
                                   "\nMargen: " + (p.margen_bruto_pct > 0 ? p.margen_bruto_pct.ToString("0.#") + " %" : "Aún no lo sabe") +
                                   "\nSector: " + (p.sector ?? "").Replace('_', ' ').Replace("tecnologia", "tecnología").Replace("educacion", "educación").Replace("energia", "energía");
            if (icono)
            {
                var tex = Resources.Load<Texture2D>("Iconos/" + p.sector);
                icono.enabled = tex != null;
                if (tex) icono.material.mainTexture = tex;
            }
        }

        public void FijarPanel(PanelItem[] panel)
        {
            ids.Clear(); objetivo.Clear();
            for (int i = 0; i < 3; i++)
            {
                bool hay = panel != null && i < panel.Length;
                if (hay) ids.Add(panel[i].tiburon_id);
                if (nombres[i]) nombres[i].text = hay ? Perfiles.Corto(panel[i].tiburon_id) : "";
                if (estados[i]) estados[i].text = hay ? "escuchando" : "";
                if (barrasRend[i]) barrasRend[i].material.color = hay ? Perfiles.Color(panel[i].tiburon_id) : Color.gray;
                if (hay) objetivo[panel[i].tiburon_id] = 0.15f;
            }
        }

        public void Estado(string id, string texto)
        {
            int i = ids.IndexOf(id);
            if (i >= 0 && estados[i]) estados[i].text = texto;
        }

        public void Voto(string id, string decision)
        {
            int i = ids.IndexOf(id);
            if (i < 0) return;
            if (estados[i]) estados[i].text = Estilos.TextoVoto(decision);
            if (barrasRend[i]) barrasRend[i].material.color = Estilos.ColorVoto(decision);
            objetivo[id] = decision == "salir" ? 0.25f : 1f;
        }

        public void Calificaciones(ReporteDto r)
        {
            if (r == null || r.por_tiburon == null) return;
            foreach (var c in r.por_tiburon)
            {
                int i = ids.IndexOf(c.tiburon_id);
                if (i < 0) continue;
                objetivo[c.tiburon_id] = Mathf.Clamp(c.calificacion, 1, 5) / 5f;
                if (estados[i]) estados[i].text = estados[i].text + "  " + new string('*', Mathf.Clamp(c.calificacion, 1, 5)) + "/5";
            }
        }

        /// <summary>Coloca las barras en su valor final sin animación (capturas en el editor).</summary>
        public void Inmediato()
        {
            for (int i = 0; i < ids.Count && i < 3; i++)
            {
                float o; objetivo.TryGetValue(ids[i], out o);
                var s = barras[i].localScale; s.x = Mathf.Max(0.02f, o) * 3.2f; barras[i].localScale = s;
                barras[i].localPosition = new Vector3(-1.6f + s.x / 2f, barras[i].localPosition.y, barras[i].localPosition.z);
            }
        }

        void Update()
        {
            for (int i = 0; i < ids.Count && i < 3; i++)
            {
                if (!barras[i]) continue;
                float o; objetivo.TryGetValue(ids[i], out o);
                var s = barras[i].localScale;
                s.x = Mathf.Lerp(s.x, Mathf.Max(0.02f, o) * 3.2f, 4f * Time.deltaTime);
                barras[i].localScale = s;
                barras[i].localPosition = new Vector3(-1.6f + s.x / 2f, barras[i].localPosition.y, barras[i].localPosition.z);
            }
        }
    }
}
