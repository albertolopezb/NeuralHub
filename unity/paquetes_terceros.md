# Assets de la escena

La escena se construye con `NeuralHub > Construir estudio`. **No hace falta instalar nada de la Asset Store**: todo lo
necesario está en el repositorio. El paquete oficial *Post Processing* de Unity se instala solo desde `Packages/manifest.json`.

## Modelos de terceros incluidos (CC0, dominio público)
| Autor y paquete | Uso | Carpeta |
|---|---|---|
| Quaternius — Pirate Kit | tiburones inversionistas (Sharky y Mako) y tiburón que nada | `Assets/Terceros/Quaternius` |
| Quaternius — Animated Fish Pack | peces, mantarraya y delfín del ventanal | `Assets/Terceros/Quaternius/Peces` |
| Quaternius — Ultimate Modular Men | emprendedor animado | `Assets/Terceros/Quaternius` |

Cambios nuestros: se quitaron las armas, se tiñó el pantalón de los tiburones (`Atlas_Tiburon.png`) y se agregaron
corbata, cuello, accesorios y objetos de podio hechos por código.

## Hecho por el equipo (código reproducible)
- Escenario (plataforma, ventanal, podios, pantallas, rocas y algas): `Assets/Editor/ConstruirEstudio.cs` y `Mallas.cs`.
- Accesorios de cada tiburón, retratos y set del emprendedor: `Assets/Editor/ConstruirCaracter.cs`.
- Láminas de la pantalla: `docs/figuras/generar_ilustraciones.py`; fondo del océano: `generar_fondo_oceano.py`.
- Música de fondo, cortinilla, fanfarria, hundimiento y cierre (originales): `docs/figuras/generar_musica.py`.
- Voces: voces de Windows en español de México (Raúl y Sabina) personalizadas por personaje en `servicio/neuralhub/voz.py`.

## Otros incluidos con licencia libre
Fuentes Inter y Space Grotesk (SIL OFL) y Poppins (OFL) en `Assets/Resources/Fuentes`; Kenney (CC0) en `Assets/Terceros`.

## Opcional
Si se importa el paquete gratuito **Underwater Ambient** (Dmitriy Sirosh, Asset Store) como `Assets/Underwater Ambient.wav`,
el estudio lo usa como sonido ambiente. Su licencia no permite publicarlo en el repositorio.
