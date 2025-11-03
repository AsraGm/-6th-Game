using UnityEngine;

namespace ShootingRange
{
    [System.Serializable]
    public class StarThresholds
    {
        [Header("Umbrales de Dinero")]
        [Tooltip("Dinero mínimo para 1 estrella")]
        public int oneStar = 100;

        [Tooltip("Dinero mínimo para 2 estrellas")]
        public int twoStars = 250;

        [Tooltip("Dinero mínimo para 3 estrellas")]
        public int threeStars = 400;

        // Calcular estrellas según dinero ganado
        public int CalculateStars(int moneyEarned)
        {
            if (moneyEarned >= threeStars)
                return 3;
            else if (moneyEarned >= twoStars)
                return 2;
            else if (moneyEarned >= oneStar)
                return 1;
            else
                return 0;
        }
    }
}
