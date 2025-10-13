using UnityEngine;

namespace ShootingRange
{
    public class LevelInitializer : MonoBehaviour
    {
        [Header("Referencias del Nivel")]
        [Tooltip("Background SpriteRenderer de este nivel")]
        public SpriteRenderer backgroundRenderer;

        [Tooltip("Curtain SpriteRenderer de este nivel")]
        public SpriteRenderer curtainRenderer;

        void Start()
        {
            InitializeLevel();
        }

        void InitializeLevel()
        {
            // Buscar automáticamente si no están asignados
            if (backgroundRenderer == null)
            {
                GameObject bg = GameObject.FindWithTag("Background");
                if (bg != null)
                    backgroundRenderer = bg.GetComponent<SpriteRenderer>();
            }

            if (curtainRenderer == null)
            {
                GameObject curtain = GameObject.FindWithTag("Curtain");
                if (curtain != null)
                    curtainRenderer = curtain.GetComponent<SpriteRenderer>();
            }

            // Conectar con ThemeManager persistente
            if (ThemeManager.Instance != null)
            {
                ThemeManager.Instance.backgroundRenderer = backgroundRenderer;
                ThemeManager.Instance.curtainRenderer = curtainRenderer;

                // Aplicar tema actual
                ThemeManager.Instance.ApplyCurrentTheme();

                Debug.Log("Nivel inicializado con tema actual");
            }
            else
            {
                Debug.LogWarning("ThemeManager no encontrado. ¿Empezaste desde MainMenu?");
            }
        }
    }
}