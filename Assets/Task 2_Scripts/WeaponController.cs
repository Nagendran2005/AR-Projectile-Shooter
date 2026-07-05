using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera arCamera;
    [SerializeField] private GameObject projectilePrefab;

    [Header("Spawn Point")]
    [SerializeField] private Transform firePoint;

    [Header("Effects")]
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip fireSFX;

    [Header("Settings")]
    [SerializeField] private float projectileSpeed = 15f;

    public void Fire()
    {
        // Is camera looking at a target?
        if (CameraTargetDetector.Instance == null)
            return;

        TargetController target = CameraTargetDetector.Instance.CurrentTarget;

        if (target == null)
        {
            Debug.Log("No Target Selected");
            return;
        }

        // Fire Position
        Vector3 spawnPos = firePoint != null ?
            firePoint.position :
            arCamera.transform.position;

        // Calculate direction to target
        Vector3 direction =
            (target.transform.position - spawnPos).normalized;

        Quaternion rotation = Quaternion.LookRotation(direction);

        // Spawn Bullet
        GameObject bullet =
            Instantiate(projectilePrefab, spawnPos, rotation);

        // Move Bullet
        Projectile projectile = bullet.GetComponent<Projectile>();

        if (projectile != null)
        {
            projectile.Initialize(target.transform, projectileSpeed);
        }

        // Play Muzzle Flash
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }

        // Play Fire Sound
        if (audioSource != null && fireSFX != null)
        {
            audioSource.PlayOneShot(fireSFX);
        }
    }
}