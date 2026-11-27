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
            voz.Stop();
            if (silencio || clip == null) return;
            voz.clip = clip; voz.pitch = tono; voz.Play();
        }

        public void DetenerVoz() { if (voz) voz.Stop(); }

        public void FijarSilencio(bool s) { silencio = s; AudioListener.volume = s ? 0f : 1f; }
    }
}
