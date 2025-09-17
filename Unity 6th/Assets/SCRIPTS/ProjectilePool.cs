// Archivo: ProjectilePool.cs
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

// Sistema de Pool para proyectiles visuales
public class ProjectilePool
{
    private Queue<GameObject> projectileQueue;
    private GameObject projectilePrefab;
    private Transform parentTransform;
    private int poolSize;
    
    public ProjectilePool(GameObject prefab, int size, Transform parent)
    {
        projectilePrefab = prefab;
        poolSize = size;
        parentTransform = parent;
        projectileQueue = new Queue<GameObject>();
        
        InitializePool();
    }
    
    private void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject projectile = Object.Instantiate(projectilePrefab, parentTransform);
            projectile.SetActive(false);
            projectileQueue.Enqueue(projectile);
        }
    }
    
    public GameObject GetProjectile()
    {
        if (projectileQueue.Count > 0)
        {
            GameObject projectile = projectileQueue.Dequeue();
            projectile.SetActive(true);
            return projectile;
        }
        
        // Si no hay proyectiles disponibles, crear uno nuevo (fallback)
        return Object.Instantiate(projectilePrefab, parentTransform);
    }
    
    public void ReturnProjectile(GameObject projectile)
    {
        projectile.SetActive(false);
        projectileQueue.Enqueue(projectile);
    }
}