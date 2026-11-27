# Paquetes de terceros (gratuitos)

Estos assets se usan en la escena, pero **no están en el repositorio**: la licencia de la Unity Asset Store (EULA estándar)
y los términos de Mixamo no permiten publicarlos. El script `NeuralHub > Construir estudio` los usa si están importados.
Si faltan, construye la escena con los personajes y la decoración de primitivas.

## Unity Asset Store (todos con precio $0, licencia EULA estándar de la Asset Store)
Agrégalos a tu cuenta («Add to My Assets»). Luego, en Unity, ve a **Window > Package Management > My Assets** y pulsa **Download** e **Import**.

| Paquete | Autor | Uso en NeuralHub | Enlace |
|---|---|---|---|
| Suit Character Pack – Generic | BlocBros Studio | cuerpo humanoide de traje de los tiburones y del emprendedor | https://assetstore.unity.com/packages/3d/characters/humanoids/suit-character-pack-generic-16772 |
| Fish – PolyPack | Alstra Infinite | cabeza de tiburón, peces y tiburón que nadan al fondo | https://assetstore.unity.com/packages/3d/characters/animals/fish/fish-polypack-202232 |
| Fishes And Underwater Environment Set | Marcin's Assets | arrecife de coral, cofre y cristales | https://assetstore.unity.com/packages/package/123678 |
| Low-Poly Office Set #1 | VNB Productions | silla de oficina de cada tiburón | https://assetstore.unity.com/packages/package/327126 |
| Bubbles VFX | Vefects | burbujas | https://assetstore.unity.com/packages/package/389450 |
| Underwater Ambient | Dmitriy Sirosh | sonido ambiente en bucle | https://assetstore.unity.com/packages/audio/ambient/underwater-ambient-66498 |
| 3D shark – Character Free | G.Park | (alternativa de tiburón animado) | https://assetstore.unity.com/packages/3d/characters/animals/fish/3d-shark-character-free-190081 |
| Simple Boids | Nick Veselov | (alternativa de cardumen) | https://assetstore.unity.com/packages/package/164188 |
| Sci-fi GUI skin | 3drina | (texturas de interfaz) | https://assetstore.unity.com/packages/2d/gui/sci-fi-gui-skin-15606 |

## Mixamo (Adobe, gratis con cuenta)
Usa **FBX Binary**, **With Skin**, **30 FPS** y **Keyframe Reduction: none**. Guarda los archivos en
`unity/NeuralHubUnity/Assets/Terceros/Mixamo/` con estos nombres.

- Tiburones (sentados):
  - `T_SentadoReposo` (Sitting Idle)
  - `T_SentadoReposoManos` (Sitting Idle, hands on thighs)
  - `T_SentadoHablando` (Sitting Talking)
  - `T_SentadoHablando2` (Talking, male seated)
  - `T_SentadoAplauso` (Sitting Clap)
  - `T_SentadoDesaprueba` (Sitting Disapproval)
  - `T_SentadoPulgarArriba` (Sitting Thumbs Up)
  - `T_SentadoIncredulo` (Sitting Disbelief)
  - `T_SentadoSenala` (Sitting And Pointing)
  - `T_SentadoPensando` (Male Sitting Pose, elbows on legs)
  - `T_SentadoVictoria` (Sitting Victory)
- Emprendedor (de pie):
  - `E_Saludo` (Standing Greeting)
  - `E_Hablando` (Talking, general conversation)
  - `E_PreguntaDosManos` (Talking, asking with two hands)
  - `E_ReposoFeliz` (Happy Idle)
  - `E_GestoFeliz` (Happy Hand Gesture)
  - `E_Reverencia` (Quick Formal Bow)
  - `E_Decepcionado` (Disappointed)
  - `E_Victoria` (Victory)

## Incluidos en el repositorio (licencia libre)
- Kenney Furniture Kit (CC0): laptop, lámpara, libros y plantas (`Assets/Terceros/Kenney_FurnitureKit`).
- Fuentes Poppins, Inter y Space Grotesk (SIL OFL) en `Assets/Resources/Fuentes`.
- Kenney UI Pack (CC0) en `unity/descargas/ui`. Está descargado, pero todavía no se usa.

## Cómo se usan
- `Editor/ConstruirPersonajes.cs` configura el traje y los FBX de Mixamo como **Humanoid** y crea los controladores
  `Animaciones/Tiburon.controller` y `Animaciones/Emprendedor.controller`, con un estado por animación. Arma también
  el tiburón empresario: la cabeza de tiburón cuelga del hueso del cuello y la cabeza humana se oculta (`CabezaPostiza`).
- `Tiburon.cs` y `Emprendedor.cs` traducen cada estado de la máquina de estados del agente en una animación
  (por ejemplo, Preguntando → hablar sentado, voto «invertir» → pulgar arriba, «salir» → desaprobar).
- `Editor/ConstruirAmbiente.cs` coloca el coral, los peces que nadan, las burbujas y el sonido ambiente.
