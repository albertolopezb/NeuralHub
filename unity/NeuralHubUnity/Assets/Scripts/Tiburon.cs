using UnityEngine;

namespace NeuralHub
{
    public enum EstadoTiburon { FueraDelPanel, Esperando, Escuchando, Preguntando, Evaluando, Replicando, Votando, Reaccionando }

    /// <summary>
    /// Un tiburón del panel. Su estado (máquina de estados del diagrama) decide la animación del modelo:
    /// saluda al pedir la palabra, asiente o niega al replicar y votar, y celebra o se encoge con la decisión.
    /// </summary>
    public class Tiburon : MonoBehaviour
    {
        public string perfilId;
        public string nombre;
        public EstadoTiburon estado = EstadoTiburon.FueraDelPanel;

        [System.Serializable]
        public class Modelo { public GameObject raiz; public Animator animador; public Transform cabeza; }

        [System.Serializable]
        public class Juego { public string id; public GameObject[] piezas; }

        public Modelo[] modelos;                // dos diseños de tiburón; cada perfil usa uno
        public Juego[] juegos;                  // accesorio, objeto del podio y emblema de cada perfil
        public GameObject[] corbatas;           // la de cada modelo
        public Transform giro;                  // solo gira el cuerpo; el letrero del podio se queda quieto
        public Transform paleta;
        public TextMesh letrero;
        public Renderer[] pintables;            // corbata y aro de luz con el color del perfil
        public Color color = Color.gray;

        Modelo activo;
        string decisionVoto, postura, animActual;
        float tiempoEstado, habla;
        bool aFavor;
        Transform mirarA;
        Quaternion giroBase;

        void Awake() { if (paleta) paleta.gameObject.SetActive(false); }

        public static int Variante(string id)
        {
            return id == "tiburon_financiero" || id == "tiburona_consumo" || id == "tiburon_consumo" || id == "tiburon_mentor" ? 1 : 0;
        }

        public void Asignar(string id, string nom)
        {
            perfilId = id; nombre = nom; color = Perfiles.Color(id);
            if (letrero) letrero.text = Perfiles.Corto(id).ToUpper();
            int v = Variante(id);
            for (int i = 0; i < modelos.Length; i++) modelos[i].raiz.SetActive(i == v);
            activo = modelos[v];
            // Estela y Coral van sin corbata: que se vea su collar
            if (corbatas != null) for (int i = 0; i < corbatas.Length; i++) if (corbatas[i]) corbatas[i].SetActive(id != "tiburon_estratega" && id != "tiburona_consumo" && id != "tiburon_consumo");
            if (juegos != null)
                foreach (var j in juegos)
                    foreach (var pieza in j.piezas) if (pieza) pieza.SetActive(j.id == id);
            foreach (var r in pintables)
            {
                if (!r) continue;
                var bloque = new MaterialPropertyBlock();
                bloque.SetColor("_Color", color);
                bloque.SetColor("_EmissionColor", color.linear * 1.8f);
                r.SetPropertyBlock(bloque);
            }
            giroBase = giro ? giro.localRotation : Quaternion.identity;
            gameObject.SetActive(true);
            CambiarEstado(EstadoTiburon.Esperando);
        }

        public void Retirar() { CambiarEstado(EstadoTiburon.FueraDelPanel); gameObject.SetActive(false); }

        public void MirarA(Transform t) { mirarA = t; }

        public void CambiarEstado(EstadoTiburon nuevo)
        {
            estado = nuevo; tiempoEstado = 0f; animActual = null;
            if (paleta && nuevo != EstadoTiburon.Votando && nuevo != EstadoTiburon.Reaccionando) paleta.gameObject.SetActive(false);
        }

        // Acciones del modelo de la clase (escuchar, preguntar, replicar, votar, reaccionar)
        public void Escuchar() { if (estado != EstadoTiburon.Escuchando) CambiarEstado(EstadoTiburon.Escuchando); }
        public void Preguntar() { CambiarEstado(EstadoTiburon.Preguntando); }
        public void Replicar(string postura = null) { this.postura = postura; CambiarEstado(EstadoTiburon.Replicando); }
        public void Evaluar() { CambiarEstado(EstadoTiburon.Evaluando); }

        public void Votar(string decision)
        {
            aFavor = decision != "salir"; decisionVoto = decision;
            if (paleta)
            {
                paleta.gameObject.SetActive(true);
                var c = Estilos.ColorVoto(decision);
                foreach (var r in paleta.GetComponentsInChildren<Renderer>())
                {
                    if (r.GetComponent<TextMesh>()) continue;
                    var b = new MaterialPropertyBlock(); b.SetColor("_Color", c); b.SetColor("_EmissionColor", c.linear * 0.8f); r.SetPropertyBlock(b);
                }
                var t = paleta.GetComponentInChildren<TextMesh>();
                if (t) t.text = decision == "invertir" ? "DENTRO" : decision == "salir" ? "FUERA" : "OFERTA";
            }
            CambiarEstado(EstadoTiburon.Votando);
        }

        public void Reaccionar(bool trato)
        {
            // Celebra si hubo trato y votó a favor; se encoge si no hubo trato.
            aFavor = trato && aFavor;
            CambiarEstado(EstadoTiburon.Reaccionando);
        }

        /// <summary>Animación según el estado: primero el gesto y después reposo.</summary>
        string Animacion()
        {
            switch (estado)
            {
                case EstadoTiburon.Preguntando: return tiempoEstado < 1.6f ? "Wave" : "Idle";
                case EstadoTiburon.Replicando: return tiempoEstado < 1.7f ? (postura == "desacuerdo" ? "No" : "Yes") : "Idle";
                case EstadoTiburon.Votando: return decisionVoto == "salir" ? "No" : decisionVoto == "invertir" ? "Yes" : "Wave";
                case EstadoTiburon.Reaccionando: return aFavor ? "Jump" : (tiempoEstado < 1.7f ? "Duck" : "Idle");
                default: return "Idle";
            }
        }

        void Update()
        {
            if (estado == EstadoTiburon.FueraDelPanel || activo == null) return;
            tiempoEstado += Time.deltaTime;
            string a = Animacion();
            if (a != animActual && activo.animador && activo.animador.isActiveAndEnabled) { activo.animador.CrossFadeInFixedTime(a, 0.25f); animActual = a; }
            if (giro)
            {
                var objetivo = giroBase;
                if (mirarA)
                {
                    Vector3 d = mirarA.position - transform.position; d.y = 0;
                    if (d.sqrMagnitude > 0.01f)
                    {
                        float ang = Mathf.Clamp(Vector3.SignedAngle(transform.forward, d, Vector3.up), -55f, 55f);
                        objetivo = Quaternion.Euler(0, ang, 0) * giroBase;
                    }
                }
                giro.localRotation = Quaternion.Slerp(giro.localRotation, objetivo, 4f * Time.deltaTime);
            }
        }

        // Cabeceo al hablar y cabeza ladeada al evaluar (se suma encima de la animación)
        void LateUpdate()
        {
            if (activo == null || !activo.cabeza) return;
            bool hablando = estado == EstadoTiburon.Preguntando || estado == EstadoTiburon.Replicando;
            habla = Mathf.MoveTowards(habla, hablando ? 1f : 0f, 4f * Time.deltaTime);
            float t = Time.time;
            float cabeceo = habla * (5f * Mathf.Sin(t * 9f) + 3f * Mathf.Sin(t * 5.3f));
            float ladeo = estado == EstadoTiburon.Evaluando ? 9f : 0f;
            if (Mathf.Abs(cabeceo) > 0.01f || ladeo > 0f)
                activo.cabeza.rotation = Quaternion.AngleAxis(cabeceo, transform.right) * Quaternion.AngleAxis(ladeo, transform.forward) * activo.cabeza.rotation;
        }

        public Vector3 PuntoCabeza() { return transform.position + Vector3.up * 2.35f; }
    }
}
