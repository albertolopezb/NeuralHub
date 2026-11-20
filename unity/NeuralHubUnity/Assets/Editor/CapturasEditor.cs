using UnityEditor;
using UnityEditor.SceneManagement;

namespace NeuralHub.Editor
{
    /// <summary>Corre la aplicación en modo Play dentro del editor y guarda las capturas de pantalla (para la documentación).</summary>
    [InitializeOnLoad]
    public static class CapturasEditor
    {
        static CapturasEditor()
        {
            EditorApplication.playModeStateChanged += s =>
            {
                if (SessionState.GetBool("nh_cap", false) && s == PlayModeStateChange.EnteredEditMode)
                {
                    SessionState.SetBool("nh_cap", false);
                    EditorApplication.Exit(0);
                }
            };
        }

        public static void Ejecutar()
        {
            SessionState.SetBool("nh_cap", true);
            EditorSceneManager.OpenScene("Assets/Scenes/Estudio.unity");
            EditorApplication.EnterPlaymode();
        }
    }
}
