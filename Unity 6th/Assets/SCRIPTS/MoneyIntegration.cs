using UnityEngine;

// ARCHIVO: MoneyIntegration.cs
// Conecta el sistema de dinero con detección de objetivos y otros sistemas

namespace ShootingRange
{
    public class MoneyIntegration : MonoBehaviour
    {
        [Header("Referencias de Sistemas")]
        [Tooltip("ARRASTRA AQUÍ tu MoneySystem")]
        public MoneySystem moneySystem;
        
        [Tooltip("ARRASTRA AQUÍ tu TargetDetectionSystem")]
        public TargetDetectionSystem targetDetectionSystem;
        
        [Tooltip("ARRASTRA AQUÍ tu ScoreSystem (opcional)")]
        public ScoreSystem scoreSystem;
        
        [Header("Configuración")]
        [Tooltip("Conectar automáticamente al iniciar")]
        public bool autoConnect = true;
        
        void Start()
        {
            if (autoConnect)
            {
                ConnectSystems();
            }
        }
        
        // Conectar todos los sistemas
        public void ConnectSystems()
        {
            // Buscar sistemas automáticamente si no están asignados
            FindSystems();
            
            // Conectar eventos
            ConnectTargetDetection();
            ConnectScoreSystem();
            
            Debug.Log("Sistemas de dinero conectados");
        }
        
        void FindSystems()
        {
            if (moneySystem == null)
            {
                moneySystem = FindObjectOfType<MoneySystem>();
                if (moneySystem == null)
                {
                    Debug.LogError("MoneySystem no encontrado. Asegúrate de tenerlo en la escena.");
                }
            }
            
            if (targetDetectionSystem == null)
            {
                targetDetectionSystem = FindObjectOfType<TargetDetectionSystem>();
            }
            
            if (scoreSystem == null)
            {
                scoreSystem = FindObjectOfType<ScoreSystem>();
            }
        }
        
        void ConnectTargetDetection()
        {
            if (targetDetectionSystem != null && moneySystem != null)
            {
                // Suscribirse al evento de target hit
                // Nota: Necesitamos modificar TargetDetectionSystem para que tenga este evento
                Debug.Log("TargetDetectionSystem conectado con MoneySystem");
            }
        }
        
        void ConnectScoreSystem()
        {
            if (scoreSystem != null && moneySystem != null)
            {
                // Conectar eventos entre sistemas de puntuación y dinero
                scoreSystem.OnTargetHit += HandleTargetHit;
                Debug.Log("ScoreSystem conectado con MoneySystem");
            }
        }
        
        // Manejar cuando un objetivo es golpeado
        void HandleTargetHit(ObjectType objectType, EnemyType enemyType, int scoreValue)
        {
            if (moneySystem != null)
            {
                // Dar dinero basado en el tipo de enemigo
                moneySystem.AddMoneyForEnemy(enemyType);
            }
        }
        
        void OnDestroy()
        {
            // Desconectar eventos para evitar errores
            if (scoreSystem != null)
            {
                scoreSystem.OnTargetHit -= HandleTargetHit;
            }
        }
    }
}