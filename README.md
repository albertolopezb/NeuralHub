# NeuralHub

Simulador de un panel de inversionistas tipo Shark Tank con agentes de IA generativa (LangGraph + Azure OpenAI) y una escena 3D en Unity.
Equipo NeuralHub, TC2008B Gpo 305, Tec de Monterrey.

- `servicio/`: servicio de agentes (LangGraph, FastAPI), pruebas y evaluación de 15 sesiones.
- `prototipo_un_agente/`: prototipo de un tiburón resolviendo el problema.
- `unity/NeuralHubUnity/`: proyecto de Unity 6 (6000.3). Menú NeuralHub → Construir estudio / Compilar para Windows.
- `docs/`: documento del primer incremento, figuras y hoja de gestión.

## Servicio
```
cd servicio
python -m venv .venv && .venv\Scripts\activate
pip install -r requirements.txt
copy .env.example .env        # y poner AZURE_API_KEY (nunca se sube a git)
pytest                        # 53 pruebas (modelo simulado; la de voz usa SAPI y Whisper locales)
python ejecutar_sesion.py --ejemplo 0
python evaluar.py             # 15 sesiones reales
python comparar_niveles.py    # mismo pitch en los tres niveles
python prueba_ciega.py        # juez automático del objetivo 2
uvicorn neuralhub.api:app --port 8000   # NEURALHUB_MOCK=1 para no usar el modelo
```

Sesión en vivo automatizada (con el servicio encendido; `-nh-ejemplo` 0, 1 o 2 elige el negocio de ejemplo):
```
unity\NeuralHubUnity\Build\NeuralHub.exe -nh-vivo -nh-url http://127.0.0.1:8000 -nh-ejemplo 0 -nh-salida <carpeta>
```

## El programa «Aguas Profundas»
- **Intro de episodio**, pitch hablado como en un programa de inversionistas (la petición va al final), 3 rondas de
  preguntas **una por una**, veredicto, negociación, celebración o hundimiento y cierre.
- **Niveles** Fácil, Medio y Difícil: cambian cómo preguntan y cómo votan los tiburones.
  `python servicio/comparar_niveles.py` los compara con el mismo pitch usando el modelo real.
- **Conoce a los tiburones**: cada uno se presenta y explica cuándo conviene elegirlo.
- La pantalla del estudio muestra una lámina del tema de cada pregunta con los datos del pitch.

Regrabar la sesión de ejemplo (modo sin conexión) y los audios, desde `servicio/`:
```bash
python generar_mock.py 2 facil        # llama al modelo
python generar_mock.py --solo-audio   # solo voces
python generar_presentaciones.py
```

## Modos de simulación
- **Agente empresario** (`modo: "autopiloto"`): el agente empresario responde las preguntas con los datos del pitch.
- **En vivo** (`modo: "humano"`): el usuario responde cada pregunta en cuanto el tiburón la hace. Con `ayuda: true`, el botón
  «Recomendación del agente empresario» llama a `POST /sesiones/{id}/sugerencia` (`{"tiburon_id": ...}`) y el usuario puede
  editar la respuesta sugerida. Cada sugerencia es 1 llamada extra al modelo; las métricas cuentan cuántas se pidieron.
- Sin conexión (sesión de ejemplo), el modo en vivo pausa la sesión grabada en cada ronda y la recomendación es la respuesta
  que dio el agente empresario en esa grabación.
