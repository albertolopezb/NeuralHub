using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace NeuralHub
{
    /// <summary>
    /// Pantalla grande del estudio: funciona como la presentación del pitch. Cada lámina tiene una ilustración
    /// (Resources/Laminas), un antetítulo, un título y texto del pitch, y cambia según el tema que se está hablando.
    /// </summary>
    public class Pantalla : MonoBehaviour
    {
        public TextMesh antetitulo, titulo, detalle;
        public Renderer ilustracion;

        // La lámina en curso, para repetirla como tarjeta en la interfaz cuando la cámara no ve la pantalla
        public Texture2D Imagen { get; private set; }
        public string Sobre { get; private set; }
        public string Encabezado { get; private set; }
        public string Cuerpo { get; private set; }

        Vector3 escalaBase = Vector3.zero;
        float tamBase;
        float entrada = 1f;
        string actual;

        public void MostrarLamina(string lamina, string sobre, string encabezado, string cuerpo)
        {
            if (antetitulo) antetitulo.text = (sobre ?? "").ToUpper();
            if (titulo) titulo.text = Ajustar(encabezado, 20, 2);
            if (detalle)
            {
                // El texto completo siempre cabe: si es largo, se reduce la letra en lugar de cortarlo
                if (tamBase <= 0f) tamBase = detalle.characterSize;
                int[] anchos = { 36, 42, 49, 57, 66 }; int[] renglones = { 7, 8, 10, 12, 14 }; float[] escalas = { 1f, 0.86f, 0.74f, 0.63f, 0.54f };
                int k = 0;
                while (k < anchos.Length - 1 && Ajustar(cuerpo, anchos[k], 99).Split('\n').Length > renglones[k]) k++;
                detalle.characterSize = tamBase * escalas[k];
                detalle.text = Ajustar(cuerpo, anchos[k], renglones[k]);
            }
            var tex = Resources.Load<Texture2D>("Laminas/" + lamina);
            Imagen = tex; Sobre = sobre; Encabezado = encabezado; Cuerpo = cuerpo;
            if (!ilustracion) return;
            ilustracion.enabled = tex != null;
            if (!tex) return;
            var b = new MaterialPropertyBlock(); b.SetTexture("_MainTex", tex); ilustracion.SetPropertyBlock(b);
            if (lamina != actual && Application.isPlaying)
            {
                if (escalaBase == Vector3.zero) escalaBase = ilustracion.transform.localScale;
                entrada = 0f;
            }
            actual = lamina;
        }

        public void MostrarTexto(string t, string d) { MostrarLamina("tema_votacion", "NeuralHub", t, d); }

        void Update()
        {
            if (entrada >= 1f || !ilustracion) return;
            entrada = Mathf.Min(1f, entrada + Time.deltaTime * 2.5f);
            float k = 1f + 2.2f * Mathf.Pow(entrada - 1f, 3f) + 1.2f * Mathf.Pow(entrada - 1f, 2f);   // rebote suave
            ilustracion.transform.localScale = escalaBase * Mathf.Max(0.05f, k);
        }

        /// <summary>Corta el texto en renglones de ancho máximo (TextMesh no ajusta solo).</summary>
        public static string Ajustar(string texto, int ancho, int maxRenglones)
        {
            if (string.IsNullOrEmpty(texto)) return "";
            var renglones = new List<string>();
            foreach (var parrafo in texto.Split('\n'))
            {
                var linea = new StringBuilder();
                foreach (var palabra in parrafo.Split(' '))
                {
                    if (linea.Length > 0 && linea.Length + 1 + palabra.Length > ancho) { renglones.Add(linea.ToString()); linea.Length = 0; }
                    if (linea.Length > 0) linea.Append(' ');
                    linea.Append(palabra);
                }
                renglones.Add(linea.ToString());
            }
            if (renglones.Count > maxRenglones)
            {
                renglones.RemoveRange(maxRenglones, renglones.Count - maxRenglones);
                renglones[maxRenglones - 1] = renglones[maxRenglones - 1].TrimEnd('.', ',', ' ') + "…";
            }
            return string.Join("\n", renglones.ToArray());
        }
    }
}
