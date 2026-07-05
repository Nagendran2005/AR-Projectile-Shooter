using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float hitDistance = 0.1f;
    [SerializeField] private float destroyAfter = 5f;

    private Transform target;

    public void Initialize(Transform targetTransform, float projectileSpeed)
    {
        target = targetTransform;
        speed = projectileSpeed;

        Destroy(gameObject, destroyAfter);
    }

    private void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        // Move toward target
        transform.position = Vector3.MoveTowards(
            transform.position,
            target.position,
            speed * Time.deltaTime);

        // Face target
        transform.LookAt(target);

        // Check hit
        if (Vector3.Distance(transform.position, target.position) <= hitDistance)
        {
            HitTarget();
        }
    }

    private void HitTarget()
    {
        TargetController targetController = target.GetComponent<TargetController>();

        if (targetController != null)
        {
            targetController.DestroyTarget();
        }

        Destroy(gameObject);
    }
}