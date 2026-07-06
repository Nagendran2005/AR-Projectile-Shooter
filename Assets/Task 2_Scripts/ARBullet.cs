using UnityEngine;

public class ARBullet : MonoBehaviour
{
    [SerializeField] private float speed = 35f; // Zips fast across distance
    [SerializeField] private float lifeTime = 2f; // Auto recycle timeout

    private System.Action<GameObject> returnToPoolAction;
    private float currentLifeTime;
    private bool isMoving = false;

    public void Launch(Vector3 position, Quaternion rotation, System.Action<GameObject> onReturnToPool)
    {
        transform.position = position;
        transform.rotation = rotation;
        returnToPoolAction = onReturnToPool;
        currentLifeTime = 0f;
        isMoving = true;

        gameObject.SetActive(true);
    }

    private void Update()
    {
        if (!isMoving) return;

        // Trace forward cleanly
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        currentLifeTime += Time.deltaTime;
        if (currentLifeTime >= lifeTime)
        {
            DeactivateAndReturn();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // If it overlaps any collider (the target birds), instantly recycle it back to pool safely
        DeactivateAndReturn();
    }

    private void DeactivateAndReturn()
    {
        isMoving = false;
        gameObject.SetActive(false);
        returnToPoolAction?.Invoke(gameObject);
    }
}   