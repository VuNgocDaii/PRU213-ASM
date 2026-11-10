using System.Collections.Generic;
using UnityEngine;

public class ProjectilePool : MonoBehaviour
{
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private int initialCapacity = 20;

    private readonly Queue<Projectile> pool = new Queue<Projectile>();

    private void Awake()
    {
        for (int i = 0; i < initialCapacity; i++)
        {
            CreateNewInstance();
        }
    }

    private Projectile CreateNewInstance()
    {
        Projectile p = Instantiate(projectilePrefab, transform);
        p.gameObject.SetActive(false);
        pool.Enqueue(p);
        return p;
    }

    public Projectile Get()
    {
        Projectile p = pool.Count > 0 ? pool.Dequeue() : CreateNewInstance();
        return p;
    }

    public void Release(Projectile p)
    {
        p.gameObject.SetActive(false);
        p.transform.SetParent(transform);
        pool.Enqueue(p);
    }
}