using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ShootingRange
{
    public class LevelButton : MonoBehaviour
    {
        [Header("Level Data")]
        [SerializeField] private SOLevelData levelData;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI levelNameText;
        [SerializeField] private TextMeshProUGUI bestMoneyText;
        [SerializeField] private GameObject[] starIcons; // 3 estrellas

        [Header("Star Visuals")]
        [SerializeField] private Color earnedStarColor = Color.yellow;
        [SerializeField] private Color unearnedStarColor = Color.gray;

        private void Start()
        {
            UpdateDisplay();
        }

        public void Initialize(SOLevelData data)
        {
            levelData = data;
            UpdateDisplay();
        }

        public void UpdateDisplay()
        {
            if (levelData == null) return;

            // Mostrar nombre del nivel
            if (levelNameText != null)
                levelNameText.text = levelData.levelName;

            // Obtener mejor puntuación guardada
            int bestStars = SaveSystem.Instance.LoadLevelStars(levelData.levelID);
            int bestMoney = SaveSystem.Instance.LoadLevelBestScore(levelData.levelID);

            // Mostrar mejor dinero
            if (bestMoneyText != null)
                bestMoneyText.text = bestMoney > 0 ? $"${bestMoney}" : "-";

            // Actualizar estrellas
            UpdateStars(bestStars);
        }

        private void UpdateStars(int earnedStars)
        {
            for (int i = 0; i < starIcons.Length; i++)
            {
                if (starIcons[i] != null)
                {
                    bool earned = i < earnedStars;

                    Image starImage = starIcons[i].GetComponent<Image>();
                    if (starImage != null)
                    {
                        starImage.color = earned ? earnedStarColor : unearnedStarColor;
                    }
                    else
                    {
                        starIcons[i].SetActive(earned);
                    }
                }
            }
        }

        // Método para cuando se hace clic en el botón
        public void OnButtonClicked()
        {
            // ✅ CORREGIDO: Usar LevelHelper en lugar de LevelManager.Instance
            if (LevelHelper.Instance != null && levelData != null)
            {
                int levelIndex = levelData.levelNumber - 1;
                LevelHelper.Instance.LoadLevel(levelIndex);
            }
        }
    }
}