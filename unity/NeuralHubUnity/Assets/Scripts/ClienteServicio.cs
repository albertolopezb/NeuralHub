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
    /// Modos: agente empresario (autopiloto: el agente responde) o en vivo (humano: el usuario responde, con ayuda opcional).
    /// Sin conexión, el modo en vivo pausa la sesión grabada en cada ronda para que el usuario responda; la recomendación
    /// es la respuesta que dio el agente empresario en esa grabación.
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
        public bool enVivo, ayuda;

        // Sesión grabada que se reproduce sin conexión en modo en vivo
        List<Evento> guionMock;
        int posMock;
        readonly Dictionary<string, string> respuestasMock = new Dictionary<string, string>();

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
        /// <param name="modoEnVivo">true: el usuario responde (modo humano). false: responde el agente empresario (autopiloto).</param>
        /// <param name="conAyuda">Solo en vivo: el agente empresario puede sugerir respuestas.</param>
        public IEnumerator CrearSesion(string pitchJson, string[] panel, bool modoEnVivo, bool conAyuda, string nivel, Action<bool> listo)
        {
            Reiniciar();
            enVivo = modoEnVivo; ayuda = modoEnVivo && conAyuda;
            if (usarMock)
            {
                var g = JsonUtility.FromJson<SesionGuardada>(LeerStreaming("mock_sesion.json"));
                sesionId = "mock";
                conectado = true;
                if (enVivo) { guionMock = new List<Evento>(g.eventos); posMock = 0; AvanzarMock(); }
                else { recibidos.AddRange(g.eventos); estadoSesion = "terminada"; }
                listo(true);
                yield break;
            }
            var ids = new List<string>();
            foreach (var p in panel) ids.Add("\"" + p + "\"");
            string cuerpo = "{\"pitch\":" + pitchJson + ",\"panel\":[" + string.Join(",", ids.ToArray()) + "],\"modo\":\"" +
                            (enVivo ? "humano" : "autopiloto") + "\",\"ayuda\":" + (ayuda ? "true" : "false") + ",\"nivel\":\"" + nivel + "\"}";
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
            enVivo = false; ayuda = false;
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
            guionMock = null; posMock = 0; respuestasMock.Clear();
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

        /// <summary>Sin conexión, en vivo: agrega eventos de la grabación hasta llegar a respuestas que todavía no da el usuario.</summary>
        void AvanzarMock()
        {
            while (guionMock != null && posMock < guionMock.Count)
            {
                var e = guionMock[posMock];
                if (e.tipo == "RESPUESTA")
                {
                    string k = e.ronda + "|" + e.destino;
                    if (!respuestasMock.ContainsKey(k))
                    {
                        var preguntas = new List<PreguntaPendiente>();
                        foreach (var q in guionMock)
                            if (q.tipo == "PREGUNTA" && q.ronda == e.ronda) preguntas.Add(new PreguntaPendiente { tiburon_id = q.emisor, texto = q.texto });
                        pendiente = new Pendiente { tipo = "RESPUESTAS", ronda = e.ronda, preguntas = preguntas.ToArray() };
                        estadoSesion = "esperando";
                        return;
                    }
                    e = JsonUtility.FromJson<Evento>(JsonUtility.ToJson(e));
                    e.texto = respuestasMock[k];
                }
                recibidos.Add(e);
                posMock++;
            }
            pendiente = null; estadoSesion = "terminada";
        }

        /// <summary>Modo en vivo con ayuda: el agente empresario sugiere una respuesta a la última pregunta del tiburón.</summary>
        public IEnumerator PedirSugerencia(string tiburonId, Action<string> listo)
        {
            if (usarMock)
            {
                int ronda = 0;
                foreach (var e in recibidos) if (e.tipo == "PREGUNTA" && e.emisor == tiburonId) ronda = e.ronda;
                string grabada = null;
                if (guionMock != null)
                    foreach (var e in guionMock) if (e.tipo == "RESPUESTA" && e.destino == tiburonId && e.ronda == ronda) grabada = e.texto;
                yield return new WaitForSeconds(0.6f);
                listo(grabada);
                yield break;
            }
            string txt = null;
            yield return Post("/sesiones/" + sesionId + "/sugerencia", "{\"tiburon_id\":\"" + tiburonId + "\"}", t => txt = t);
            listo(txt != null ? JsonUtility.FromJson<SugerenciaDto>(txt).texto : null);
        }

        public IEnumerator EnviarRespuestas(Dictionary<string, string> r)
        {
            if (usarMock && guionMock != null)
            {
                int ronda = pendiente != null ? pendiente.ronda : 0;
                foreach (var kv in r) respuestasMock[ronda + "|" + kv.Key] = kv.Value;
                pendiente = null;
                AvanzarMock();
                yield break;
            }
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
