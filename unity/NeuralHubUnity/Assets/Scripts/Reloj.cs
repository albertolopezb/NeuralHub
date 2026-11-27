using UnityEngine;

namespace NeuralHub
{
    /// <summary>
    /// Reloj de las animaciones de la interfaz: tiempo real normalmente y tiempo del video cuando se graba cuadro por
    /// cuadro (captureFramerate), para que la intro, los letreros y los finales salgan a su velocidad.
    /// </summary>
    public static class Reloj
    {
        public static bool Grabando { get { return Time.captureFramerate > 0; } }
        public static float Ahora { get { return Grabando ? Time.time : Time.unscaledTime; } }
        public static float Delta { get { return Grabando ? Time.deltaTime : Time.unscaledDeltaTime; } }
    }
}
