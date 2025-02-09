// Sierpinski triangle

// Burst is a high performance C# compiler using LLVM to translate .NET bytecode to optimized native code
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;
using float4x4 = Unity.Mathematics.float4x4;
using float3x4 = Unity.Mathematics.float3x4;
using quaternion = Unity.Mathematics.quaternion;

public class BurstFractal : MonoBehaviour
{
    [BurstCompile (FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    // A job interface used for functionality running inside for loops
    struct UpdateFractalLevelJob : IJobFor {

        public float deltaSpinAngle;
        public float scale;

        [ReadOnly]
        public NativeArray<FractalPart> parents;

        public NativeArray<FractalPart> parts;

        [WriteOnly]
        public NativeArray<float3x4> matrices;
        // public NativeArray<float4x4> matrices;

        // i is iterator of for loop
        // this method replaces the code of the innermost loop in our update method
        public void Execute (int i) {
            // Index parent for this fractal
            FractalPart parent = parents[i / 5];
            FractalPart part = parts[i];

            // Animate
            part.spinAngle += deltaSpinAngle;

            // Upscale location by 1.5x because the pos is relative to the root rather than local
            part.worldRotation = mul(parent.worldRotation, mul(part.rotation, quaternion.RotateY(part.spinAngle)));
            part.worldPosition = parent.worldPosition + mul(parent.worldRotation, (1.5f * scale * part.direction));
            parts[i] = part;

            float3x3 r = float3x3(part.worldRotation) * scale;
            matrices[i] = float3x4(r.c0, r.c1, r.c2, part.worldPosition);
            // matrices[i] = float4x4.TRS(part.worldPosition, part.worldRotation, scale);
        }
    }

    [SerializeField, Range(1, 8)]
    private int _depth = 8;

    [SerializeField, Range(0.0f, 360.0f)]
    private float _rotationSpeed = 30.0f;

    [SerializeField]
    private Mesh _mesh;

    [SerializeField]
    private Material _material;

    private const int BATCH_COUNT = 5;

    private struct FractalPart
    {
        public float3 direction, worldPosition;
        public quaternion rotation, worldRotation;
        public float spinAngle;
    }

    private ComputeBuffer[] _matricesBuffers;
    private static readonly int _matricesID = Shader.PropertyToID("_Matrices");
    private static MaterialPropertyBlock _propertyBlock;

    // Native code
    NativeArray<FractalPart>[] _parts;
    // Since the last row is always (0, 0, 0, 1), we optimize by instead creating a 3x4 then constructing the last row
    NativeArray<float3x4>[] _matrices;
    // NativeArray<float4x4>[] _matrices;

    public static float3[] directions =
    {
        up(), right(), left(), forward(), back()
    };

    public static quaternion[] rotations =
    {
        quaternion.identity, 
        quaternion.RotateZ(-.5f * PI),
        quaternion.RotateZ( .5f * PI),
        quaternion.RotateX( .5f * PI),
        quaternion.RotateX(-.5f * PI)
    };

    private void OnEnable()
    {
        // Each level gets its own array
        _parts = new NativeArray<FractalPart>[_depth];
        _matrices = new NativeArray<float3x4>[_depth];
        // _matrices = new NativeArray<float4x4>[_depth];
        _matricesBuffers = new ComputeBuffer[_depth];

        int stride = /*float3x4=*/12 * sizeof(float);
        // int stride = /*Mat4x4=*/16 * sizeof(float);

        // At the top level [0], there is only 1 fractal part, then each subsequent level gets 5 children per fractal part
        // Length is the number of fractals at the current level i
        for (int i = 0, length = 1; i < _parts.Length; i++, length *= 5)
        {
            _parts[i] = new NativeArray<FractalPart>(length, Allocator.Persistent);
            _matrices[i] = new NativeArray<float3x4>(length, Allocator.Persistent);
            // _matrices[i] = new NativeArray<float4x4>(length, Allocator.Persistent);
            _matricesBuffers[i] = new ComputeBuffer(length, stride);
        }

        _parts[0][0] = CreatePart(0);

        // Iterate thru each level of fractals
        for (int levelIndex = 1; levelIndex < _parts.Length; levelIndex++)
        {

            // Get the fractals at this level and iterate thru each
            NativeArray<FractalPart> levelParts = _parts[levelIndex];

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
        {
            _matricesBuffers[i].Release();
            // Free native array memory
            _parts[i].Dispose();
            _matrices[i].Dispose();
        }

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
        float deltaSpinAngle = (1.0f / 180.0f) * PI * _rotationSpeed * Time.deltaTime;
        
        // Initialize first fractal part
        FractalPart rootPart = _parts[0][0];
        rootPart.spinAngle += deltaSpinAngle;
        rootPart.worldRotation = mul(transform.rotation, mul(rootPart.rotation, quaternion.RotateY(rootPart.spinAngle)));
        rootPart.worldPosition = transform.position;
        _parts[0][0] = rootPart;

        // Transformation Matrix: Translation-Rotation-Scale
        float objectScale = transform.lossyScale.x;

        float3x3 r = float3x3(rootPart.worldRotation) * objectScale;
        _matrices[0][0] = float3x4(r.c0, r.c1, r.c2, rootPart.worldPosition);
        // _matrices[0][0] = float4x4.TRS(rootPart.worldPosition, rootPart.worldRotation, objectScale);

        float scale = objectScale;

        // Used to track jobs
        JobHandle jobHandle = default;
        for (int levelIndex = 1; levelIndex < _parts.Length; levelIndex++)
        {
            scale *= 0.5f;

            jobHandle = new UpdateFractalLevelJob
            {
                deltaSpinAngle = deltaSpinAngle,
                scale = scale,

                // Fractal array at previous level is parents to the fractals at this level
                parents = _parts[levelIndex - 1],
                parts = _parts[levelIndex],
                matrices = _matrices[levelIndex]

            }.ScheduleParallel(_parts[levelIndex].Length, BATCH_COUNT, jobHandle);
        
        }
        // After scheduling all jobs at this level, execute them
        jobHandle.Complete();

        // Due to the halving, fractal size converges and is guaranteed to fit in BB with size 3
        float edgeLength = 3.0f; 
        Bounds bounds = new Bounds(rootPart.worldPosition, float3(edgeLength * objectScale));

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