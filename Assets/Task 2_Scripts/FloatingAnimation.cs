using UnityEngine;

public class FloatingAnimation : MonoBehaviour
{
    [Header("Floating")]
    [Tooltip("How high the object moves up and down.")]
    [SerializeField] private float amplitude = 0.15f;

    [Tooltip("Floating speed.")]
    [SerializeField] private float speed = 2f;

    [Header("Rotation (Optional)")]
    [SerializeField] private bool rotate = true;

    [SerializeField] private float rotationSpeed = 45f;

    private Vector3 startPosition;
    private float randomOffset;

    private void Start()
    {
        startPosition = transform.localPosition;

        // Gives each target a different floating phase
        randomOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        // Floating Motion
        Vector3 pos = startPosition;
        pos.y += Mathf.Sin((Time.time * speed) + randomOffset) * amplitude;
        transform.localPosition = pos;

        // Optional Rotation
        if (rotate)
        {
            transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.Self);
        }
    }
}