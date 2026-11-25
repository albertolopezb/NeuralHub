using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace NeuralHub
{
    /// <summary>
    /// Habla con el servicio de agentes por HTTP (UnityWebRequest) y guarda los eventos en una cola.
    /// Con usarMock lee StreamingAssets/mock_sesion.json y no necesita el servicio.
    /// </summary>
    public class ClienteServicio : MonoBehaviour
    {
        public string urlBase = "http://localhost:8000";
        public bool usarMock;
        public bool conectado;
        public string ultimoError;

        // Estado de la sesión en curso
        public string sesionId;
        public string estadoSesion = "";
        public Pendiente pendiente;
        public readonly List<Evento> recibidos = new List<Evento>();
        public Action<string> alError;

        Coroutine sondeo;
        int desde;

        // ---------- utilidades ----------
        public static string Esc(string s)
        {
            if (s == null) return "";
            var sb = new StringBuilder();
            foreach (var ch in s)
            {
                switch (ch)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': break;
                    case '\t': sb.Append("\\t"); break;
                    default: sb.Append(ch); break;
                }
            }
            return sb.ToString();
        }

        static string Num(double d) { return d.ToString(System.Globalization.CultureInfo.InvariantCulture); }

        static string Campo(string k, string v, bool coma = true)
        {
            return (v == null || v.Trim() == "") ? "" : "\"" + k + "\":\"" + Esc(v) + "\"" + (coma ? "," : "");
        }

        /// <summary>Convierte el formulario a JSON; los campos vacíos se omiten (= «Aún no lo sé»).</summary>
        public static string PitchAJson(PitchDto p, bool tieneVentas, bool tieneMargen, bool tieneGasto)
        {
            var sb = new StringBuilder("{");
            sb.Append(Campo("nombre", p.nombre)).Append(Campo("sector", p.sector)).Append(Campo("etapa", p.etapa));
            sb.Append(Campo("problema", p.problema)).Append(Campo("solucion", p.solucion));
            sb.Append(Campo("mercado_objetivo", p.mercado_objetivo)).Append(Campo("modelo_ingresos", p.modelo_ingresos));
            sb.Append(Campo("competencia", p.competencia)).Append(Campo("diferenciador", p.diferenciador));
            sb.Append(Campo("validacion", p.validacion)).Append(Campo("propiedad_intelectual", p.propiedad_intelectual));
            sb.Append(Campo("equipo_roles", p.equipo_roles)).Append(Campo("equipo_dedicacion", p.equipo_dedicacion));
            sb.Append(Campo("equipo_experiencia", p.equipo_experiencia)).Append(Campo("uso_fondos", p.uso_fondos));
            sb.Append("\"equipo_fundadores\":" + p.equipo_fundadores + ",");
            if (tieneVentas) sb.Append("\"ventas_12m_mxn\":" + Num(p.ventas_12m_mxn) + ",");
            if (tieneMargen) sb.Append("\"margen_bruto_pct\":" + Num(p.margen_bruto_pct) + ",");
            if (tieneGasto) sb.Append("\"gasto_mensual_mxn\":" + Num(p.gasto_mensual_mxn) + ",");
            sb.Append("\"monto_mxn\":" + Num(p.monto_mxn) + ",\"porcentaje\":" + Num(p.porcentaje) + "}");
            return sb.ToString();
        }

        // ---------- catálogo y panel ----------
        public IEnumerator ObtenerTiburones(Action<TiburonInfo[]> listo)
        {
            string txt = null;
            if (usarMock) txt = LeerStreaming("tiburones.json");
            else yield return Get("/tiburones", t => txt = t);
            if (txt != null) listo(JsonUtility.FromJson<TiburonesLista>("{\"items\":" + txt + "}").items);
        }

        public IEnumerator ObtenerEjemplos(Action<EjemploItem[]> listo)
        {
            string txt = null;
            if (usarMock)
            {
                txt = LeerStreaming("ejemplos.json");
            }
            else
            {
                yield return Get("/ejemplos", t => txt = t);
            }
            if (txt != null) listo(JsonUtility.FromJson<EjemplosLista>("{\"items\":" + txt + "}").items);
        }

        public IEnumerator Recomendar(string pitchJson, Action<Recomendacion> listo)
        {
            string txt = null;
            if (usarMock) txt = LeerStreaming("recomendacion_ejemplo.json");
            else yield return Post("/panel/recomendacion", pitchJson, t => txt = t);
            if (txt != null) listo(JsonUtility.FromJson<Recomendacion>(txt));
        }

        // ---------- sesión ----------
        public IEnumerator CrearSesion(string pitchJson, string[] panel, Action<bool> listo)
        {
            Reiniciar();
            if (usarMock)
            {
                var g = JsonUtility.FromJson<SesionGuardada>(LeerStreaming("mock_sesion.json"));
                sesionId = "mock";
                recibidos.AddRange(g.eventos);
                estadoSesion = "terminada";
                conectado = true;
                listo(true);
                yield break;
            }
            var ids = new List<string>();
            foreach (var p in panel) ids.Add("\"" + p + "\"");
            string cuerpo = "{\"pitch\":" + pitchJson + ",\"panel\":[" + string.Join(",", ids.ToArray()) + "],\"modo\":\"humano\"}";
            string txt = null;
            yield return Post("/sesiones", cuerpo, t => txt = t);
            if (txt == null) { listo(false); yield break; }
            sesionId = JsonUtility.FromJson<CrearRespuesta>(txt).sesion_id;
            estadoSesion = "corriendo";
            sondeo = StartCoroutine(Sondear());
            listo(true);
        }

        /// <summary>Vuelve a cargar una sesión pasada (historial): trae todos sus eventos para repetirlos en la escena.</summary>
        public IEnumerator CargarSesionPasada(string id, Action<bool> listo)
        {
            Reiniciar();
            if (usarMock)
            {
                var g = JsonUtility.FromJson<SesionGuardada>(LeerStreaming("mock_sesion.json"));
                sesionId = "mock"; recibidos.AddRange(g.eventos); estadoSesion = "terminada"; listo(true); yield break;
            }
            string txt = null;
            yield return Get("/sesiones/" + id + "/eventos?desde=0", t => txt = t);
            if (txt == null) { listo(false); yield break; }
            var e = JsonUtility.FromJson<EstadoSesion>(txt);
            sesionId = id; recibidos.AddRange(e.eventos); estadoSesion = "terminada";
            listo(true);
        }

        public IEnumerator ListarSesiones(Action<SesionListada[]> listo)
        {
            if (usarMock)
            {
                var g = JsonUtility.FromJson<SesionGuardada>(LeerStreaming("mock_sesion.json"));
                listo(new[] { new SesionListada { sesion_id = "mock", negocio = g.pitch.nombre, panel = g.panel, estado = "terminada", resultado = "(ejemplo)" } });
                yield break;
            }
            string txt = null;
            yield return Get("/sesiones", t => txt = t);
            if (txt != null) listo(JsonUtility.FromJson<SesionesLista>("{\"items\":" + txt + "}").items);
        }

        void Reiniciar()
        {
            if (sondeo != null) StopCoroutine(sondeo);
            sondeo = null; desde = 0; recibidos.Clear(); pendiente = null; estadoSesion = ""; sesionId = null; ultimoError = null;
        }

        IEnumerator Sondear()
        {
            while (true)
            {
                string txt = null;
                yield return Get("/sesiones/" + sesionId + "/eventos?desde=" + desde, t => txt = t);
                if (txt != null)
                {
                    var e = JsonUtility.FromJson<EstadoSesion>(txt);
                    if (e.eventos != null) { recibidos.AddRange(e.eventos); desde += e.eventos.Length; }
                    estadoSesion = e.estado;
                    pendiente = e.estado == "esperando" ? e.pendiente : null;
                    if (e.estado == "error") { Error("Error en el servicio: " + e.error); yield break; }
                    if (e.estado == "terminada") yield break;
                }
                yield return new WaitForSeconds(1f);   // polling de 1 s
            }
        }

        public IEnumerator EnviarRespuestas(Dictionary<string, string> r)
        {
            var partes = new List<string>();
            foreach (var kv in r) partes.Add("\"" + kv.Key + "\":\"" + Esc(kv.Value) + "\"");
            pendiente = null; estadoSesion = "corriendo";
            yield return Post("/sesiones/" + sesionId + "/respuestas", "{\"respuestas\":{" + string.Join(",", partes.ToArray()) + "}}", null);
        }

        public IEnumerator EnviarNegociacion(string accion, double monto, double pct, string tiburonId)
        {
            string c = "{\"accion\":\"" + accion + "\",\"monto_mxn\":" + Num(monto) + ",\"porcentaje\":" + Num(pct) +
                       (tiburonId != null ? ",\"tiburon_id\":\"" + tiburonId + "\"" : "") + "}";
            pendiente = null; estadoSesion = "corriendo";
            yield return Post("/sesiones/" + sesionId + "/negociacion", c, null);
        }

        public IEnumerator EnviarCondicion(string opcion)
        {
            pendiente = null; estadoSesion = "corriendo";
            yield return Post("/sesiones/" + sesionId + "/condiciones", "{\"opcion\":\"" + opcion + "\"}", null);
        }

        // ---------- voz ----------
        /// <summary>Audio (WAV) de la línea del evento n: del servicio en vivo o de StreamingAssets/audio en modo sin conexión.</summary>
        public IEnumerator ObtenerAudio(int n, Action<AudioClip> listo)
        {
            string url = (usarMock || sesionId == "mock")
                ? new Uri(Path.Combine(Application.streamingAssetsPath, "audio", n + ".wav")).AbsoluteUri
                : urlBase + "/sesiones/" + sesionId + "/audio/" + n;
            using (var req = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV))
            {
                req.timeout = 20;
                yield return req.SendWebRequest();
                listo(req.result == UnityWebRequest.Result.Success ? DownloadHandlerAudioClip.GetContent(req) : null);
            }
        }

        [Serializable] class Transcripcion { public string texto; }

        /// <summary>Envía la voz grabada (WAV) al servicio, que la transcribe con Whisper.</summary>
        public IEnumerator Transcribir(byte[] wav, Action<string> listo)
        {
            using (var req = new UnityWebRequest(urlBase + "/voz/transcribir", "POST"))
            {
                req.uploadHandler = new UploadHandlerRaw(wav);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "audio/wav");
                req.timeout = 60;
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success) listo(JsonUtility.FromJson<Transcripcion>(req.downloadHandler.text).texto);
                else { Error("No se pudo transcribir la voz: " + req.error); listo(null); }
            }
        }

        // ---------- HTTP ----------
        IEnumerator Get(string ruta, Action<string> ok)
        {
            using (var req = UnityWebRequest.Get(urlBase + ruta))
            {
                req.timeout = 20;
                yield return req.SendWebRequest();
                Resolver(req, ok);
            }
        }

        IEnumerator Post(string ruta, string json, Action<string> ok)
        {
            using (var req = new UnityWebRequest(urlBase + ruta, "POST"))
            {
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.timeout = 20;
                yield return req.SendWebRequest();
                Resolver(req, ok);
            }
        }

        void Resolver(UnityWebRequest req, Action<string> ok)
        {
            if (req.result == UnityWebRequest.Result.Success)
            {
                conectado = true;
                if (ok != null) ok(req.downloadHandler.text);
            }
            else
            {
                conectado = req.result != UnityWebRequest.Result.ConnectionError;
                Error(req.result == UnityWebRequest.Result.ConnectionError
                    ? "No hay conexión con el servicio (" + urlBase + "). Puedes usar la sesión de ejemplo."
                    : "El servicio respondió " + req.responseCode + ": " + req.downloadHandler.text);
            }
        }

        void Error(string m) { ultimoError = m; if (alError != null) alError(m); Debug.LogWarning(m); }

        public IEnumerator ProbarConexion()
        {
            if (usarMock) { conectado = true; yield break; }
            string t = null;
            yield return Get("/salud", x => t = x);
            conectado = t != null;
        }

        static string LeerStreaming(string archivo)
        {
            string ruta = Path.Combine(Application.streamingAssetsPath, archivo);
            return File.Exists(ruta) ? File.ReadAllText(ruta, Encoding.UTF8) : null;
        }
    }
}
