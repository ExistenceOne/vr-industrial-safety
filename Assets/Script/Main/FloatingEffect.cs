using UnityEngine;

public class FloatingEffect : MonoBehaviour
{
    public float floatHeight = 0.05f;
    public float floatSpeed = 2f;
    public float rotateSpeed = 0f;

    private Vector3 startLocalPosition;

    void Start()
    {
        startLocalPosition = transform.localPosition;
    }

    void Update()
    {
        float y = Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        transform.localPosition = startLocalPosition + new Vector3(0f, y, 0f);

        if (rotateSpeed != 0f)
        {
            transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f);
        }
    }
}