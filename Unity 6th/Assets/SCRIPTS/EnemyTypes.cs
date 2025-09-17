// Archivo: EnemyTypes.cs
// CREA ESTE ARCHIVO PRIMERO para resolver todos los errores de EnemyType

public enum ObjectType
{
    Enemy,
    Innocent
}

public enum EnemyType
{
    Normal,
    Fast,
    Jumper,
    Valuable,
    Innocent
}

// Interface IPoolable para reciclaje eficiente
public interface IPoolable
{
    void OnSpawnFromPool();
    void OnReturnToPool();
}

// Interface para objetos que pueden ser disparados
public interface IShootable
{
    void OnHit(ObjectType type, int value);
}