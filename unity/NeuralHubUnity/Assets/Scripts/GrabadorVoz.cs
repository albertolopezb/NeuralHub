using System;
using System.IO;
using UnityEngine;

namespace NeuralHub
{
    /// <summary>Graba la voz del usuario con el micrófono (16 kHz, mono) y la convierte a WAV para el servicio (Whisper).</summary>
    public static class GrabadorVoz
    {
        static AudioClip clip;
        static string dispositivo;
        public static float inicio;
        public const int MaxSegundos = 120;
        public static float UltimoPico;         // volumen de la última grabación (0 a 1)
        public static float Segundos { get { return Time.realtimeSinceStartup - inicio; } }
        public static bool LlegoAlLimite { get { return clip != null && Segundos >= MaxSegundos - 1; } }

        public static bool HayMicrofono { get { return Microphone.devices.Length > 0; } }
        public static bool Grabando { get { return clip != null && Microphone.IsRecording(dispositivo); } }

        public static bool Iniciar()
        {
            if (!HayMicrofono) return false;
            dispositivo = Microphone.devices[0];
            clip = Microphone.Start(dispositivo, false, MaxSegundos, 16000);
            inicio = Time.realtimeSinceStartup;
            return clip != null;
        }

        public static byte[] Detener()
        {
            if (clip == null) return null;
            // si el micrófono ya se detuvo solo, GetPosition vale 0: se usa lo que duró la grabación para no perderla
            int pos = Microphone.IsRecording(dispositivo) ? Microphone.GetPosition(dispositivo) : 0;
            if (pos <= 0) pos = Mathf.Min(clip.samples, (int)(Segundos * clip.frequency));
            Microphone.End(dispositivo);
            if (pos <= 0) { clip = null; return null; }
            var datos = new float[pos * clip.channels];
            clip.GetData(datos, 0);
            int canales = clip.channels, frecuencia = clip.frequency;
            clip = null;
            UltimoPico = 0f; foreach (var m in datos) UltimoPico = Mathf.Max(UltimoPico, Mathf.Abs(m));
            return Wav(datos, frecuencia, canales);
        }

        static byte[] Wav(float[] muestras, int frecuencia, int canales)
        {
            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms))
            {
                int bytesDatos = muestras.Length * 2;
                w.Write(new[] { 'R', 'I', 'F', 'F' }); w.Write(36 + bytesDatos); w.Write(new[] { 'W', 'A', 'V', 'E' });
                w.Write(new[] { 'f', 'm', 't', ' ' }); w.Write(16); w.Write((short)1); w.Write((short)canales);
                w.Write(frecuencia); w.Write(frecuencia * canales * 2); w.Write((short)(canales * 2)); w.Write((short)16);
                w.Write(new[] { 'd', 'a', 't', 'a' }); w.Write(bytesDatos);
                foreach (var m in muestras) w.Write((short)(Mathf.Clamp(m, -1f, 1f) * 32767));
                return ms.ToArray();
            }
        }
    }
}
