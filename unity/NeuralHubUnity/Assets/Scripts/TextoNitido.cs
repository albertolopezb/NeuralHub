using UnityEngine;

namespace NeuralHub
{
    /// <summary>
    /// Mantiene la textura de la fuente en el material del texto 3D: las fuentes dinámicas reconstruyen su atlas
    /// al aparecer letras nuevas y el material propio (con profundidad) debe seguirlo.
    /// </summary>
    [ExecuteAlways]
    public class TextoNitido : MonoBehaviour
    {
        public Font fuente;
        Renderer render;

        void OnEnable() { render = GetComponent<Renderer>(); Font.textureRebuilt += AlReconstruir; Aplicar(); }
        void OnDisable() { Font.textureRebuilt -= AlReconstruir; }
        void AlReconstruir(Font f) { if (f == fuente) Aplicar(); }

        public void Aplicar()
        {
            if (!render || !fuente || !fuente.material) return;
            var b = new MaterialPropertyBlock();
            b.SetTexture("_MainTex", fuente.material.mainTexture);
            render.SetPropertyBlock(b);
        }

        void LateUpdate() { Aplicar(); }
    }
}
