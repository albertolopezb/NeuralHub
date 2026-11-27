namespace NeuralHub
{
    /// <summary>Quién es cada tiburón en el programa: nombre propio, cómo se presenta y cuándo conviene elegirlo.</summary>
    public class Persona
    {
        public string id, nombre, apodo, presentacion, usalo, lamina;
    }

    public static class Personas
    {
        public const string Programa = "AGUAS PROFUNDAS";
        public const string Lema = "El panel donde tu idea nada… o se hunde";

        public static readonly Persona[] Todas =
        {
            new Persona { id = "tiburon_financiero", nombre = "Ramiro Cifuentes", apodo = "El de las cuentas claras", lamina = "tema_numeros",
                presentacion = "Soy Ramiro. Fui director de finanzas veinte años. A mí no me enamora una idea: me convencen los números. Si tus cuentas cierran, puedo ser tu mejor socio.",
                usalo = "Elígelo si ya vendes y quieres poner a prueba tus márgenes, tus gastos y la valuación que pides." },
            new Persona { id = "tiburon_estratega", nombre = "Estela Marín", apodo = "La que ve diez años adelante", lamina = "tema_mercado",
                presentacion = "Soy Estela. He llevado empresas mexicanas a otros países. Quiero saber qué tan grande es tu océano, quién más nada en él y por qué tú vas a ganar.",
                usalo = "Elígela si quieres practicar cómo explicar tu mercado, tu competencia y tu ventaja." },
            new Persona { id = "tiburon_tecnologico", nombre = "Teo Navarro", apodo = "El fan de lo nuevo", lamina = "tema_ventaja",
                presentacion = "¡Qué tal! Soy Teo. Programé mi primera empresa a los diecinueve. Me encantan las ideas que rompen reglas, pero quiero ver qué hace especial a tu tecnología y cómo la proteges.",
                usalo = "Elígelo si tu producto usa tecnología, software o algo que podrías patentar." },
            new Persona { id = "tiburona_consumo", nombre = "Coral Medina", apodo = "La voz de tu cliente", lamina = "sector_consumo_y_moda",
                presentacion = "Hola, soy Coral. Construí tres marcas que seguro has visto en el súper. Yo pienso como tu cliente: ¿por qué te compraría, dónde te encontraría y volvería a comprarte?",
                usalo = "Elígela si vendes a personas: alimentos, moda, productos del hogar o servicios para el consumidor." },
            new Persona { id = "tiburon_mentor", nombre = "Aurelio Paredes", apodo = "El que invierte en personas", lamina = "tema_equipo",
                presentacion = "Soy Aurelio. He acompañado a más de cincuenta fundadores. Los planes cambian, las personas son las que sacan adelante un negocio. Cuéntame quién está contigo.",
                usalo = "Elígelo si estás empezando o todavía no tienes muchas cifras, y quieres practicar tu historia y la de tu equipo." },
            new Persona { id = "tiburon_negociador", nombre = "Bruno Garza", apodo = "El del trato duro", lamina = "tema_oferta",
                presentacion = "Bruno Garza. Cierro tratos antes del desayuno. Si me interesas, te voy a hacer una oferta… y casi nunca va a ser la que tú pediste. Prepárate para negociar.",
                usalo = "Elígelo si quieres practicar cuánto pides, qué porcentaje cedes y cómo responder una contraoferta." },
        };

        public static Persona De(string id)
        {
            foreach (var p in Todas) if (p.id == id) return p;
            return null;
        }
    }
}
