// Sierpinski triangle

using UnityEngine;

public class ProceduralFractal : MonoBehaviour
{

    [SerializeField, Range(1, 8)]
    private int _depth = 4;

    [SerializeField, Range(0.0f, 360.0f)]
    private float _rotationSpeed = 30.0f;

    [SerializeField]
    private Mesh _mesh;

    [SerializeField]
    private Material _material;

    private struct FractalPart
    {
        public Vector3 direction, worldPosition;
        public Quaternion rotation, worldRotation;
        public float spinAngle;
    }

    private FractalPart[][] _parts;
    private Matrix4x4[][] _matrices;
    private ComputeBuffer[] _matricesBuffers;
    private static readonly int _matricesID = Shader.PropertyToID("_Matrices");
    private static MaterialPropertyBlock _propertyBlock;

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

    private void OnEnable()
    {
        // Each level gets its own array
        _parts = new FractalPart[_depth][];
        _matrices = new Matrix4x4[_depth][];
        _matricesBuffers = new ComputeBuffer[_depth];
        int stride = /*Mat4x4=*/16 * sizeof(float);

        // At the top level [0], there is only 1 fractal part, then each subsequent level gets 5 children per fractal part
        // Length is the number of fractals at the current level i
        for (int i = 0, length = 1; i < _parts.Length; i++, length *= 5)
        {
            _parts[i] = new FractalPart[length];
            _matrices[i] = new Matrix4x4[length];
            _matricesBuffers[i] = new ComputeBuffer(length, stride);
        }

        _parts[0][0] = CreatePart(0);

        // Iterate thru each level of fractals
        for (int levelIndex = 1; levelIndex < _parts.Length; levelIndex++)
        {

            // Get the fractals at this level and iterate thru each
            FractalPart[] levelParts = _parts[levelIndex];
            for (int fractalPartIndex = 0; fractalPartIndex < levelParts.Length; fractalPartIndex+=5)
            {

                // Create 5 children for this fractal
                for (int childIndex = 0; childIndex < 5; childIndex++)
                {
                    levelParts[fractalPartIndex + childIndex] = CreatePart(childIndex);
                }

            }
        }

        _propertyBlock ??= new MaterialPropertyBlock();
    }

    private void OnDisable()
    {
        for (int i = 0; i < _matricesBuffers.Length; i++)
            _matricesBuffers[i].Release();

        _parts = null;
        _matrices = null;
        _matricesBuffers = null;
    }

    // When the fractal component is changed in the editor, hot reload
    private void OnValidate()
    {
        if (_parts != null && enabled)
        {
            OnDisable();
            OnEnable();
        }
    }

    private FractalPart CreatePart(int childIndex) => new FractalPart
    {
        direction = directions[childIndex],
        rotation = rotations[childIndex]
    };

    private void Update()
    {
        // For animation
        float deltaSpinAngle = _rotationSpeed * Time.deltaTime;

        // Initialize first fractal part
        FractalPart rootPart = _parts[0][0];
        rootPart.spinAngle += deltaSpinAngle;
        rootPart.worldRotation = transform.rotation * (rootPart.rotation * Quaternion.Euler(0f, rootPart.spinAngle, 0f));
        rootPart.worldPosition = transform.position;
        _parts[0][0] = rootPart;

        // Transformation Matrix: Translation-Rotation-Scale
        float objectScale = transform.lossyScale.x;
        _matrices[0][0] = Matrix4x4.TRS(
            rootPart.worldPosition, rootPart.worldRotation, objectScale * Vector3.one
        );

        float scale = objectScale;
        // Don't move the root, so start at level=1 rather than =0
        // For each level
        for (int levelIndex = 1; levelIndex < _parts.Length; levelIndex++)
        {
            scale *= 0.5f;

            // Fractal array at previous level is parents to the fractals at this level
            FractalPart[] parentParts = _parts[levelIndex-1];
            FractalPart[] levelParts = _parts[levelIndex];
            Matrix4x4[] levelMatrices = _matrices[levelIndex];

            // For each fractal at this level
            for (int fractalPartIndex = 0; fractalPartIndex < levelParts.Length; fractalPartIndex++)
            {

                // Index parent for this fractal
                FractalPart parent = parentParts[fractalPartIndex/5];
                FractalPart part = levelParts[fractalPartIndex];

                // Animate
                part.spinAngle += deltaSpinAngle;

                // Upscale location by 1.5x because the pos is relative to the root rather than local
                part.worldRotation = parent.worldRotation * (part.rotation * Quaternion.Euler(0f, part.spinAngle, 0f));
                part.worldPosition = parent.worldPosition + parent.worldRotation * (1.5f * scale * part.direction);
                
                levelParts[fractalPartIndex] = part;

                levelMatrices[fractalPartIndex] = Matrix4x4.TRS(
                    part.worldPosition, part.worldRotation, scale * Vector3.one
                );
            }
        }

        // Due to the halving, fractal size converges and is guaranteed to fit in BB with size 3
        float edgeLength = 3.0f; 
        Bounds bounds = new Bounds(rootPart.worldPosition, edgeLength  * objectScale * Vector3.one);

        // Send matrix data from CPU to compute buffers on GPU and draw it procedurally
        for (int i = 0; i < _matricesBuffers.Length; i++)
        {
            ComputeBuffer buffer = _matricesBuffers[i];
            buffer.SetData(_matrices[i]);
            _propertyBlock.SetBuffer(_matricesID, buffer);
            Graphics.DrawMeshInstancedProcedural(_mesh, 0, _material, bounds, buffer.count, _propertyBlock);
        }
    }
}