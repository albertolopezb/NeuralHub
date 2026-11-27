using System;
using UnityEngine;

namespace NeuralHub
{
    /// <summary>Niveles de práctica: cada uno trae un emprendedor de ejemplo y cambia la exigencia de los tiburones.</summary>
    public static class Niveles
    {
        public static readonly string[] Ids = { "facil", "medio", "dificil" };

        public static string Nombre(string n) { return n == "facil" ? "Fácil" : n == "dificil" ? "Difícil" : "Medio"; }

        public static Color ColorDe(string n) { return n == "facil" ? Estilos.Dentro : n == "dificil" ? Estilos.Fuera : Estilos.Calido; }

        public static string Descripcion(string n)
        {
            return n == "facil" ? "Datos completos y buen margen. Los tiburones preguntan con calma y perdonan huecos pequeños."
                 : n == "dificil" ? "Faltan cifras clave. Los tiburones son muy exigentes: piden números y negocian duro."
                 : "Ya vende, pero tiene puntos débiles. Los tiburones son exigentes pero justos.";
        }

        public static EjemploItem Ejemplo(EjemploItem[] ejemplos, string nivel)
        {
            return ejemplos == null ? null : Array.Find(ejemplos, e => e.nivel == nivel);
        }
    }
}
