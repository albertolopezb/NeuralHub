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

        public static bool HayMicrofono { get { return Microphone.devices.Length > 0; } }
        public static bool Grabando { get { return clip != null && Microphone.IsRecording(dispositivo); } }

        public static bool Iniciar()
        {
            if (!HayMicrofono) return false;
            dispositivo = Microphone.devices[0];
            clip = Microphone.Start(dispositivo, false, 30, 16000);
            inicio = Time.realtimeSinceStartup;
            return clip != null;
        }

        public static byte[] Detener()
        {
            if (clip == null) return null;
            int pos = Microphone.GetPosition(dispositivo);
            Microphone.End(dispositivo);
            if (pos <= 0) { clip = null; return null; }
            var datos = new float[pos * clip.channels];
            clip.GetData(datos, 0);
            int canales = clip.channels, frecuencia = clip.frequency;
            clip = null;
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
