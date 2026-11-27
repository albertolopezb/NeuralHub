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
pip install langgraph langchain-openai python-dotenv fastapi uvicorn pytest httpx
copy .env.example .env        # y poner AZURE_API_KEY (nunca se sube a git)
pytest                        # 21 pruebas con modelo simulado
python ejecutar_sesion.py --ejemplo 0
python evaluar.py             # 15 sesiones reales
uvicorn neuralhub.api:app --port 8000   # NEURALHUB_MOCK=1 para no usar el modelo
```

## Modos de simulación
- **Agente empresario** (`modo: "autopiloto"`): el agente empresario responde las preguntas con los datos del pitch.
- **En vivo** (`modo: "humano"`): el usuario responde cada pregunta en cuanto el tiburón la hace. Con `ayuda: true`, el botón
  «Recomendación del agente empresario» llama a `POST /sesiones/{id}/sugerencia` (`{"tiburon_id": ...}`) y el usuario puede
  editar la respuesta sugerida. Cada sugerencia es 1 llamada extra al modelo; las métricas cuentan cuántas se pidieron.
- Sin conexión (sesión de ejemplo), el modo en vivo pausa la sesión grabada en cada ronda y la recomendación es la respuesta
  que dio el agente empresario en esa grabación.
