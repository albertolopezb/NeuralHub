using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeuralHub
{
    /// <summary>Audio de la escena: ambiente submarino, efectos (clic, turno, voto, trato) y la voz de cada personaje.</summary>
    public class Sonidos : MonoBehaviour
    {
        AudioSource ambiente, efectos, voz;
        public bool vozActiva = true;
        public bool silencio;

        // Registro para mezclar el audio al grabar video (fotograma|tipo|dato)
        public List<string> registro;
        public Func<int> fotograma;

        void Awake()
        {
            ambiente = gameObject.AddComponent<AudioSource>();
            ambiente.loop = true; ambiente.volume = 0.32f; ambiente.clip = Resources.Load<AudioClip>("Sonidos/ambiente");
            if (ambiente.clip) ambiente.Play();
            efectos = gameObject.AddComponent<AudioSource>();
            voz = gameObject.AddComponent<AudioSource>(); voz.volume = 1f;
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
