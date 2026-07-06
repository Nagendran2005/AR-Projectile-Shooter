using UnityEngine;

public class ARTarget : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int maxHits = 3;
    [SerializeField] private GameObject explosionEffect;

    private int currentHits = 0;
    private ARExperienceManager manager;

    private void Start()
    {
        // Automatically link up with the manager in your scene
        manager = FindFirstObjectByType<ARExperienceManager>();
    }

    public void TakeDamage()
    {
        currentHits++;

        if (currentHits >= maxHits)
        {
            DestroyTarget();
        }
    }

    private void DestroyTarget()
    {
        // Spawn particle blast at the bird's exact position
        if (explosionEffect != null)
        {
            GameObject fx = Instantiate(explosionEffect, transform.position, transform.rotation);
            Destroy(fx, 1.5f);
        }

        // Award +10 points to the tracker
        if (manager != null)
        {
            manager.AddScore(10);
        }

        // Vaporize target
        Destroy(gameObject);
    }
}