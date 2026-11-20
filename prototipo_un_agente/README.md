# Prototipo NeuralHub: un tiburón evaluando un pitch (LangGraph + Azure OpenAI)

## Cómo correrlo en Windows (PowerShell)

```powershell
cd prototipo
python -m venv .venv
.venv\Scripts\activate
pip install -r requirements.txt
copy .env.example .env      # luego abre .env y pon la llave AZURE_API_KEY del reto
python prototipo_un_tiburon.py
```

Otras pruebas:

```powershell
python prototipo_un_tiburon.py --idea greenplate --tiburon tiburona_consumo
python prototipo_un_tiburon.py --mock     # sin LLM, solo para probar el flujo
```

Cada corrida imprime la conversación, las métricas (llamadas al modelo, rondas, tiempo
y si la justificación menciona el criterio del tiburón) y guarda los eventos en `sesiones/`.

**Nunca subas `.env` a GitHub.** Ya está en `.gitignore`.
