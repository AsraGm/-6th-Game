using UnityEngine;

// ARCHIVO: ScoreSystem.cs
// Sistema básico de puntuación (se expandirá en otras listas)

namespace ShootingRange
{
    public class ScoreSystem : MonoBehaviour
    {
        [Header("Puntuación Actual")]
        [Tooltip("Puntuación actual del jugador")]
        public int currentScore = 0;
        
        [Tooltip("Mejor puntuación alcanzada")]
        public int highScore = 0;
        
        [Header("Estadísticas")]
        [Tooltip("Total de enemigos disparados correctamente")]
        public int totalEnemiesHit = 0;
        
        [Tooltip("Total de inocentes disparados por error")]
        public int innocentsHit = 0;
        
        [Tooltip("Porcentaje de precisión del jugador")]
        public float accuracy = 0f;
        
        // Eventos para notificar cambios en la UI
        public event System.Action<int> OnScoreChanged;
        public event System.Action<ObjectType, EnemyType, int> OnTargetHit;
        public event System.Action<float> OnAccuracyChanged;
        
        void Start()
        {
            // Cargar high score desde PlayerPrefs
            highScore = PlayerPrefs.GetInt("HighScore", 0);
        }
        
        public void AddScore(int points, ObjectType objectType, EnemyType enemyType)
        {
            currentScore += points;
            
            // Actualizar estadísticas
            if (objectType == ObjectType.Enemy)
                totalEnemiesHit++;
            else
                innocentsHit++;
                
            // Calcular precisión
            UpdateAccuracy();
            
            // Actualizar high score
            if (currentScore > highScore)
            {
                highScore = currentScore;
                PlayerPrefs.SetInt("HighScore", highScore);
                PlayerPrefs.Save();
            }
            
            // Notificar cambios
            OnScoreChanged?.Invoke(currentScore);
            OnTargetHit?.Invoke(objectType, enemyType, points);
            
            Debug.Log($"Score: {currentScore} | Accuracy: {accuracy:F1}% | Hit: {enemyType} ({points} pts)");
        }
        
        void UpdateAccuracy()
        {
            int totalShots = totalEnemiesHit + innocentsHit;
            if (totalShots > 0)
            {
                accuracy = (float)totalEnemiesHit / totalShots * 100f;
                OnAccuracyChanged?.Invoke(accuracy);
            }
        }
        
        public void ResetScore()
        {
            currentScore = 0;
            totalEnemiesHit = 0;
            innocentsHit = 0;
            accuracy = 0f;
            
            OnScoreChanged?.Invoke(currentScore);
            OnAccuracyChanged?.Invoke(accuracy);
            
            Debug.Log("Score reseteado");
        }
        
        // Getters para UI
        public int GetCurrentScore() => currentScore;
        public int GetHighScore() => highScore;
        public float GetAccuracy() => accuracy;
        public int GetEnemiesHit() => totalEnemiesHit;
        public int GetInnocentsHit() => innocentsHit;
    }
}