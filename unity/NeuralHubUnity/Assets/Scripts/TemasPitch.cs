using System.Globalization;
using System.Text;

namespace NeuralHub
{
    /// <summary>
    /// Decide de qué tema trata una pregunta (por palabras clave, sin llamar al modelo) y arma la lámina del pitch
    /// que se muestra en la pantalla grande: así lo que se ve siempre corresponde a lo que se está hablando.
    /// </summary>
    public static class TemasPitch
    {
        static readonly string[] Temas = { "numeros", "oferta", "equipo", "ventaja", "mercado" };
        static readonly string[][] Claves =
        {
            new[] { "venta", "margen", "ingreso", "costo", "gasto", "cac", "rentab", "utilidad", "adquisi", "precio", "flujo", "ganancia" },
            new[] { "valuaci", "porcentaje", "capital", "fondos", "inversi", "contraoferta", "monto", "oferta", "acciones" },
            new[] { "equipo", "fundador", "experiencia", "roles", "dedica", "socio", "compromiso" },
            new[] { "patente", "marca registrada", "tecnolog", "propiedad", "diferenci", "copia", "competidor", "competencia", "ventaja", "algoritmo", "modelo de ia" },
            new[] { "mercado", "cliente", "demanda", "canal", "crec", "tamano", "validaci", "piloto", "usuario", "distribu" },
        };

        static string SinAcentos(string s)
        {
            var sb = new StringBuilder();
            foreach (var c in (s ?? "").ToLowerInvariant().Normalize(NormalizationForm.FormD))
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
            return sb.ToString();
        }

        public static string Detectar(string pregunta, string perfil)
        {
            var t = SinAcentos(pregunta);
            int mejor = -1, puntos = 0;
            for (int i = 0; i < Temas.Length; i++)
            {
                int n = 0;
                foreach (var k in Claves[i]) if (t.Contains(k)) n++;
                if (n > puntos) { puntos = n; mejor = i; }
            }
            if (mejor >= 0) return Temas[mejor];
            var p = perfil ?? "";
            return p.Contains("financiero") ? "numeros" : p.Contains("tecnolog") ? "ventaja" : p.Contains("negociador") ? "oferta" : p.Contains("mentor") ? "equipo" : "mercado";
        }

        static string O(string s) { return string.IsNullOrWhiteSpace(s) ? "Aún no lo sabe" : s; }
        static string Dinero(double v) { return v > 0 ? Perfiles.Mxn(v) : "Aún no lo sabe"; }

        public static void Mostrar(Pantalla pantalla, PitchDto p, double valuacion, string tema, string sobre)
        {
            if (!pantalla || p == null) return;
            switch (tema)
            {
                case "numeros":
                    pantalla.MostrarLamina("tema_numeros", sobre, "Números clave",
                        "Ventas 12 meses: " + Dinero(p.ventas_12m_mxn) + "\nMargen bruto: " + (p.margen_bruto_pct > 0 ? p.margen_bruto_pct.ToString("0.#") + " %" : "Aún no lo sabe") +
                        "\nGasto mensual: " + Dinero(p.gasto_mensual_mxn) + "\nModelo: " + O(p.modelo_ingresos)); break;
                case "oferta":
                    pantalla.MostrarLamina("tema_oferta", sobre, "La oferta",
                        "Pide " + Perfiles.Mxn(p.monto_mxn) + " por el " + p.porcentaje.ToString("0.##") + " %\nValuación: " + Perfiles.Mxn(valuacion) +
                        "\nUso: " + O(p.uso_fondos)); break;
                case "equipo":
                    pantalla.MostrarLamina("tema_equipo", sobre, "El equipo",
                        p.equipo_fundadores + (p.equipo_fundadores == 1 ? " fundador" : " fundadores") + (string.IsNullOrEmpty(p.equipo_dedicacion) ? "" : " · " + p.equipo_dedicacion) +
                        "\n" + O(p.equipo_roles) + "\n" + O(p.equipo_experiencia)); break;
                case "ventaja":
                    pantalla.MostrarLamina("tema_ventaja", sobre, "Ventaja competitiva",
                        O(p.diferenciador) + "\nRegistro: " + O(p.propiedad_intelectual) + "\nCompetencia: " + O(p.competencia)); break;
                case "mercado":
                    pantalla.MostrarLamina("tema_mercado", sobre, "Mercado",
                        O(p.mercado_objetivo) + "\nValidación: " + O(p.validacion)); break;
                case "solucion":
                    pantalla.MostrarLamina("sector_" + p.sector, sobre, p.nombre, p.solucion); break;
                default:
                    pantalla.MostrarLamina("sector_" + p.sector, sobre, p.nombre,
                        "Pide " + Perfiles.Mxn(p.monto_mxn) + " por el " + p.porcentaje.ToString("0.##") + " %\nValuación: " + Perfiles.Mxn(valuacion) + "\n\n" + p.problema); break;
            }
        }
    }
}
