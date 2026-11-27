using System;
using UnityEngine;

namespace NeuralHub
{
    /// <summary>Evento que produce el servicio de agentes y que Unity reproduce (clase Evento del diagrama de clases).</summary>
    [Serializable]
    public class Evento
    {
        public int n;
        public int ronda;
        public string tipo;      // PITCH, PANEL, PREGUNTA, RESPUESTA, REPLICA, VOTO, NEGOCIACION, OFERTA, DECISION, REPORTE
        public string emisor;
        public string destino;
        public string texto;
        public string estado;    // estado de animación del emisor
        public string decision;  // VOTO: invertir | contraoferta | salir
        public string postura;   // REPLICA: acuerdo | desacuerdo
        public int votacion;
        public string resultado; // DECISION: TRATO | SIN_TRATO
        public double valuacion_mxn;
        public double contraoferta_monto_mxn;
        public double contraoferta_porcentaje;
        public PitchDto pitch;
        public PanelItem[] panel;
        public Condiciones condiciones;
        public ReporteDto reporte;
    }

    [Serializable]
    public class PanelItem { public string tiburon_id; public string nombre; public string motivo; }

    [Serializable]
    public class Condiciones { public double monto_mxn; public double porcentaje; public string origen; }

    [Serializable]
    public class CalificacionDto { public string tiburon_id; public int calificacion; public string frase_clave; public string necesitaria; }

    [Serializable]
    public class CriterioDto { public string tiburon_id; public string nombre; public string criterio; }

    [Serializable]
    public class ReporteDto
    {
        public CalificacionDto[] por_tiburon;
        public string[] fortalezas;
        public string[] alertas;
        public string[] preguntas_sin_responder;
        public string[] siguientes_pasos;
        public CriterioDto[] criterios_no_evaluados;
        public string resultado;
        public double valuacion_mxn;
        public Condiciones condiciones;
        public string aviso;
    }

    /// <summary>Campos del formulario del pitch (los opcionales usan -1 / vacío para «Aún no lo sé»).</summary>
    [Serializable]
    public class PitchDto
    {
        public string nombre, sector, etapa, problema, solucion, mercado_objetivo, modelo_ingresos;
        public string competencia, diferenciador, validacion, propiedad_intelectual;
        public string equipo_roles, equipo_dedicacion, equipo_experiencia, uso_fondos;
        public int equipo_fundadores;
        public double monto_mxn, porcentaje, ventas_12m_mxn, margen_bruto_pct, gasto_mensual_mxn;
    }

    [Serializable]
    public class PreguntaPendiente { public string tiburon_id; public string texto; }

    /// <summary>Respuesta sugerida por el agente empresario (modo en vivo con ayuda).</summary>
    [Serializable]
    public class SugerenciaDto { public string tiburon_id; public string texto; }

    [Serializable]
    public class OfertaDto { public double monto_mxn; public double porcentaje; }

    [Serializable]
    public class ContraofertaDto { public string tiburon_id; public double monto_mxn; public double porcentaje; }

    [Serializable]
    public class Pendiente
    {
        public string tipo;      // RESPUESTAS | NEGOCIACION | CONDICIONES
        public int ronda;
        public PreguntaPendiente[] preguntas;
        public OfertaDto oferta_actual;
        public OfertaDto original;
        public ContraofertaDto[] contraofertas;
    }

    [Serializable]
    public class EstadoSesion
    {
        public string sesion_id;
        public string estado;    // creada | corriendo | esperando | terminada | error
        public Pendiente pendiente;
        public Evento[] eventos;
        public int total_eventos;
        public string error;
    }

    [Serializable]
    public class TiburonInfo { public string id; public string nombre; public string perfil; public string criterio; public string color; public string[] campos; }

    [Serializable]
    public class TiburonesLista { public TiburonInfo[] items; }

    [Serializable]
    public class RankingItem { public string tiburon_id; public double puntaje; public string motivo; public bool recomendado; public string[] huecos; }

    [Serializable]
    public class Recomendacion { public double valuacion_mxn; public RankingItem[] ranking; }

    [Serializable]
    public class CrearRespuesta { public string sesion_id; }

    [Serializable]
    public class EjemploItem { public string etiqueta; public string nivel; public PitchDto pitch; }

    [Serializable]
    public class EjemplosLista { public EjemploItem[] items; }

    [Serializable]
    public class SesionListada { public string sesion_id; public string negocio; public string[] panel; public string estado; public string resultado; }

    [Serializable]
    public class SesionesLista { public SesionListada[] items; }

    /// <summary>Archivo de sesión guardado por el servicio (se usa como sesión de ejemplo sin conexión).</summary>
    [Serializable]
    public class SesionGuardada { public string id; public string[] panel; public PitchDto pitch; public Evento[] eventos; }

    public static class Perfiles
    {
        public static string Corto(string id)
        {
            switch (id)
            {
                case "tiburon_financiero": return "Financiero";
                case "tiburon_estratega": return "Estratega";
                case "tiburon_tecnologico": return "Tecnológico";
                case "tiburona_consumo": return "Consumo";
                case "tiburon_mentor": return "Mentor";
                case "tiburon_negociador": return "Negociador";
                case "emprendedor": return "Emprendedor";
                case "moderador": return "Moderador";
                case "nodo_votacion": return "Votación";
                default: return id;
            }
        }

        public static Color Color(string id)
        {
            switch (id)
            {
                case "tiburon_financiero": return Hex("#1060D0");
                case "tiburon_estratega": return Hex("#4F1FD0");
                case "tiburon_tecnologico": return Hex("#13B8F0");
                case "tiburona_consumo": return Hex("#E8590C");
                case "tiburon_mentor": return Hex("#2F9E44");
                case "tiburon_negociador": return Hex("#C92A2A");
                default: return Hex("#8A93B0");
            }
        }

        public static Color Hex(string h)
        {
            Color c;
            return ColorUtility.TryParseHtmlString(h, out c) ? c : UnityEngine.Color.white;
        }

        public static string Mxn(double x) { return "$" + x.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + " MXN"; }
    }
}
