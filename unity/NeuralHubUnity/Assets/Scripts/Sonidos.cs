using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeuralHub
{
    /// <summary>Audio de la escena: ambiente submarino, efectos (clic, turno, voto, trato) y la voz de cada personaje.</summary>
    public class Sonidos : MonoBehaviour
    {
        AudioSource ambiente, efectos, voz, musica;
        public bool musicaActiva = true;
        public bool vozActiva = true;
        public bool silencio;

        // Registro para mezclar el audio al grabar video (fotograma|tipo|dato)
        public List<string> registro;
        public Func<int> fotograma;

        void Awake()
        {
            ambiente = gameObject.AddComponent<AudioSource>();
            ambiente.loop = true; ambiente.volume = 0.16f; ambiente.clip = Resources.Load<AudioClip>("Sonidos/ambiente");
            if (ambiente.clip) ambiente.Play();
            efectos = gameObject.AddComponent<AudioSource>();
            voz = gameObject.AddComponent<AudioSource>(); voz.volume = 1f;
            musica = gameObject.AddComponent<AudioSource>(); musica.loop = true; musica.volume = 0f;
            musica.clip = Resources.Load<AudioClip>("Sonidos/musica");
            if (musica.clip) musica.Play();
        }

        float enCortinilla;

        /// <summary>Pieza musical del programa (fanfarria, hundimiento o cierre): la música de fondo calla mientras suena.</summary>
        public void Musical(string nombre, float dura) { Efecto(nombre, 0.8f); enCortinilla = dura; }

        /// <summary>Cortinilla de entrada del programa; la música de fondo baja mientras suena.</summary>
        public void Cortinilla()
        {
            Efecto("cortinilla", 0.8f); enCortinilla = 7.2f;
        }

        void Update()
        {
            if (!musica) return;
            enCortinilla -= Time.unscaledDeltaTime;
            // Con voz la música queda muy abajo (más aún mientras alguien habla); sin voz sube para que no se sienta vacío
            float objetivo = !musicaActiva || enCortinilla > 0f ? 0f : voz.isPlaying ? 0.05f : vozActiva ? 0.16f : 0.34f;
            musica.volume = Mathf.MoveTowards(musica.volume, objetivo, Time.unscaledDeltaTime * 0.5f);
        }

        void Anotar(string tipo, string dato)
        {
            if (registro != null && fotograma != null) registro.Add(fotograma() + "|" + tipo + "|" + dato);
        }

        public void Efecto(string nombre, float volumen = 0.6f)
        {
            Anotar("efecto", nombre);
            if (silencio) return;
            var c = Resources.Load<AudioClip>("Sonidos/" + nombre);
            if (c) efectos.PlayOneShot(c, volumen);
        }

        public void Voz(AudioClip clip, float tono, int n)
        {
            Anotar("voz", n.ToString());
            voz.Stop(); vozEnPausa = false;
            if (silencio || clip == null) return;
            voz.clip = clip; voz.pitch = tono; voz.Play();
        }

        bool vozEnPausa;
        /// <summary>Hay una línea sonando (o en pausa): la sesión no avanza hasta que termine.</summary>
        public bool Hablando { get { return voz && voz.clip && (voz.isPlaying || vozEnPausa); } }
        /// <summary>Avance real (0 a 1) del audio en curso, para sincronizar los subtítulos.</summary>
        public float AvanceVoz { get { return voz && voz.clip && voz.clip.length > 0 ? Mathf.Clamp01(voz.time / voz.clip.length) : 0f; } }

        public void PausarVoz(bool pausa)
        {
            if (!voz) return;
            if (pausa && voz.isPlaying) { voz.Pause(); vozEnPausa = true; }
            else if (!pausa && vozEnPausa) { voz.UnPause(); vozEnPausa = false; }
        }

        public void DetenerVoz() { vozEnPausa = false; if (voz) voz.Stop(); }

        public void FijarSilencio(bool s) { silencio = s; AudioListener.volume = s ? 0f : 1f; }
    }
}
