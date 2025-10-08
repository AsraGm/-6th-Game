using UnityEngine;

// ARCHIVO: TargetTypes.cs
// Contiene todos los enums e interfaces del sistema de detección

namespace ShootingRange
{
    // ENUM PRINCIPAL: Tipos de objetivos para puntuación
    public enum ObjectType
    {
        Enemy,      // Objetivos buenos para disparar
        Innocent    // Objetivos que NO se deben disparar (penalización)
    }
    
    // ENUM EXTENDIDO: Tipos específicos de enemigos (CONEXIÓN CON LISTA B1)
    public enum EnemyType
    {
        Normal,     // Enemigo estándar
        Static,     // Enemigo estático
        ZigZag,       // Enemigo rápido
        Jumper,     // Enemigo que salta
        Valuable,   // Enemigo de alto valor
        Innocent    // Civil (no disparar)
    }
    
    // INTERFACE PRINCIPAL: Para objetos que pueden ser disparados
    public interface IShootable
    {
        void OnHit(ObjectType objectType, int scoreValue);
        EnemyType GetEnemyType(); // CONEXIÓN CON LISTA B1
        string GetThemeID();      // CONEXIÓN CON LISTA C2
    }
    
    // INTERFACE PARA POOLING (CONEXIÓN CON LISTA B1)
    public interface IPoolable
    {
        void OnSpawnFromPool();
        void OnReturnToPool();
        bool IsActiveInPool { get; set; }
    }
}