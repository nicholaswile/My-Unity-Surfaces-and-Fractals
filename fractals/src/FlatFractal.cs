// Sierpinski triangle

using UnityEngine;

public class FlatFractal : MonoBehaviour
{

    [SerializeField, Range(0, 8)]
    private int _depth = 4;

    [SerializeField, Range(0.0f, 360.0f)]
    private float _rotationSpeed = 30.0f;

    [SerializeField]
    private Mesh _mesh;

    [SerializeField]
    private Material _material;

    private struct FractalPart
    {
        public Vector3 direction;
        public Quaternion rotation;
        public Transform transform;
    }

    private FractalPart[][] _parts;

    public static Vector3[] directions =
    {
        Vector3.up, Vector3.right, Vector3.left, Vector3.forward, Vector3.back
    };

    public static Quaternion[] rotations =
    {
        Quaternion.identity, 
        Quaternion.Euler(     0f,   0f, -90f),
        Quaternion.Euler(     0f,   0f,  90f),
        Quaternion.Euler(    90f,   0f,   0f),
        Quaternion.Euler(   -90f,   0f,   0f),
    };

    private void Awake()
    {
        // Each level gets its own array
        _parts = new FractalPart[_depth][];

        // At the top level [0], there is only 1 fractal part, then each subsequent level gets 5 children per fractal part
        for (int i = 0, length = 1; i < _parts.Length; i++, length *= 5) 
            _parts[i] = new FractalPart[length];

        float scale = 1.0f;
        _parts[0][0] = CreatePart(0, 0, scale);

        // Iterate thru each level of fractals
        for (int levelIndex = 1; levelIndex < _parts.Length; levelIndex++)
        {
            // Child is half the size as parent
            scale *= 0.5f;

            // Get the fractals at this level and iterate thru each
            FractalPart[] levelParts = _parts[levelIndex];
            for (int fractalPartIndex = 0; fractalPartIndex < levelParts.Length; fractalPartIndex+=5)
            {

                // Create 5 children for this fractal
                for (int childIndex = 0; childIndex < 5; childIndex++)
                {
                    levelParts[fractalPartIndex + childIndex] = CreatePart(levelIndex, childIndex, scale);
                }

            }
        }
    }

    private FractalPart CreatePart(int levelIndex, int childIndex, float scale)
    {
        GameObject obj = new GameObject($"Fractal Part L{levelIndex} C{childIndex}");
        obj.transform.SetParent(transform, false);
        obj.transform.localScale = scale * Vector3.one;
        obj.AddComponent<MeshFilter>().mesh = _mesh;
        obj.AddComponent<MeshRenderer>().material = _material;

        return new FractalPart
        {
            direction = directions[childIndex],
            rotation = rotations[childIndex],
            transform = obj.transform
        };
    }

    private void Update()
    {
        // For animation
        Quaternion deltaRotation = Quaternion.Euler(0.0f, _rotationSpeed * Time.deltaTime, 0.0f);

        // Initialize first fractal part
        FractalPart rootPart = _parts[0][0];
        rootPart.rotation *= deltaRotation;
        rootPart.transform.localRotation = rootPart.rotation;
        _parts[0][0] = rootPart;

        // Don't move the root, so start at level=1 rather than =0
        // For each level
        for (int levelIndex = 1; levelIndex < _parts.Length; levelIndex++)
        {
            // Fractal array at previous level is parents to the fractals at this level
            FractalPart[] parentParts = _parts[levelIndex-1];
            FractalPart[] levelParts = _parts[levelIndex];

            // For each fractal at this level
            for (int fractalPartIndex = 0; fractalPartIndex < levelParts.Length; fractalPartIndex++)
            {

                // Index parent for this fractal
                Transform parentTransform = parentParts[fractalPartIndex/5].transform;
                FractalPart part = levelParts[fractalPartIndex];

                // Animate
                part.rotation *= deltaRotation;

                // Upscale location by 1.5x because the pos is relative to the root rather than local
                part.transform.SetLocalPositionAndRotation(parentTransform.localPosition + parentTransform.localRotation * (1.5f * part.transform.localScale.x * part.direction), parentTransform.localRotation * part.rotation);
                levelParts[fractalPartIndex] = part;
            }
        }
    }
}