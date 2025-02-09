// Sierpinski triangle

using UnityEngine;

public class RecursiveFractal : MonoBehaviour
{

    [SerializeField, Range(0, 8)]
    private int _depth = 4;

    [SerializeField, Range(0.0f, 360.0f)]
    private float _rotationSpeed = 30.0f;

    // Awake is called before the first frame
    // If we instantiate here, all objects would try to be spawned at the same time
    private void Awake()
    {
        name = $"Fractal {_depth}";
    }

    // Start is called after the first frame
    // This allows objects a frame between spawning
    private void Start()
    {
        if (_depth <= 1)
            return;

        RecursiveFractal child1 = CreateChild(Vector3.right, Quaternion.identity);
        RecursiveFractal child2 = CreateChild(Vector3.up, Quaternion.Euler(0f, 0f, -90f));
        RecursiveFractal child3 = CreateChild(Vector3.left, Quaternion.Euler(0f, 0f, 90f));
        RecursiveFractal child4 = CreateChild(Vector3.forward, Quaternion.Euler(90f, 0f, 0f));
        RecursiveFractal child5 = CreateChild(Vector3.back, Quaternion.Euler(-90f, 0f, 0f));

        child1.transform.SetParent(transform, false);
        child2.transform.SetParent(transform, false);
        child3.transform.SetParent(transform, false);
        child4.transform.SetParent(transform, false);
        child5.transform.SetParent(transform, false);
    }

    private RecursiveFractal CreateChild(Vector3 offset, Quaternion rotation)
    {
        RecursiveFractal child = Instantiate(this);
        child._depth --;
        // 0.75 (0.5+0.25) positions parent (1.0 scale, radius=0.5) next to child (0.5 scale, radius=0.25)
        child.transform.SetLocalPositionAndRotation(0.75f * offset, rotation);
        // Each child half the size of the parent
        child.transform.localScale = 0.5f * Vector3.one;
        return child;
    }

    private void Update()
    {
        transform.Rotate(0f, _rotationSpeed * Time.deltaTime, 0f);
    }
}
