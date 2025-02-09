// Sierpinski triangle

// Burst is a high performance C# compiler using LLVM to translate .NET bytecode to optimized native code
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;
using float3x4 = Unity.Mathematics.float3x4;
using quaternion = Unity.Mathematics.quaternion;
using Random = UnityEngine.Random;

public class VarietyFractal : MonoBehaviour
{
    [BurstCompile (FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    // A job interface used for functionality running inside for loops
    struct UpdateFractalLevelJob : IJobFor {

        public float deltaTime;
        public float scale;

        [ReadOnly]
        public NativeArray<FractalPart> parents;

        public NativeArray<FractalPart> parts;

        [WriteOnly]
        public NativeArray<float3x4> matrices;

        // i is iterator of for loop
        // this method replaces the code of the innermost loop in our update method
        public void Execute(int i) {
            // Index parent for this fractal
            FractalPart parent = parents[i / 5];
            FractalPart part = parts[i];

            // Animate
            part.spinAngle += part.spinVelocity * deltaTime;

            float3 upAxis = mul(mul(parent.worldRotation, part.rotation), up());
            float3 sagAxis = cross(up(), upAxis);
            float sagMag = length(sagAxis);
            quaternion baseRot;
            if (sagMag > 0)
            {
                sagAxis /= sagMag; // normalize
                quaternion sagRot = quaternion.AxisAngle(sagAxis, part.maxSagAngle * sagMag);
                baseRot = mul(sagRot, parent.worldRotation);
            }
            else
            {
                baseRot = parent.worldRotation;
            }

            // Upscale location by 1.5x because the pos is relative to the root rather than local
            part.worldRotation = mul(baseRot, mul(part.rotation, quaternion.RotateY(part.spinAngle)));
            part.worldPosition = parent.worldPosition + mul(part.worldRotation, float3(0f, 1.5f * scale, 0f));
            parts[i] = part;

            float3x3 r = float3x3(part.worldRotation) * scale;
            matrices[i] = float3x4(r.c0, r.c1, r.c2, part.worldPosition);
        }
    }

    [SerializeField, Range(3, 8)]
    private int _depth = 8;

    [SerializeField, Range(0.0f, 90f)]
    private float _rotationSpeedA = 30.0f, _rotationSpeedB=60.0f;

    [SerializeField, Range(0.0f, 1.0f)]
    private float _reverseChance = .25f;

    [SerializeField]
    private Mesh _mesh, _leafMesh;

    [SerializeField]
    private Material _material;

    private const int BATCH_COUNT = 5;

    private struct FractalPart
    {
        public float3 worldPosition;
        public quaternion rotation, worldRotation;
        public float spinAngle, maxSagAngle, spinVelocity;
    }

    private ComputeBuffer[] _matricesBuffers;
    private Vector4[] _sequenceNumbers;

    private static readonly int _baseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int _secondaryColorID = Shader.PropertyToID("_SecondaryColor");
    private static readonly int _matricesID = Shader.PropertyToID("_Matrices");
    private static readonly int _sequenceNumbersID = Shader.PropertyToID("_SequenceNumbers");

    private static MaterialPropertyBlock _propertyBlock;

    /* 
    private enum ColorMode { Greyscale, Interpolate, Gradient};

    [SerializeField]
    private ColorMode _colorMode = ColorMode.Greyscale;

    private delegate void Function(int i, ComputeBuffer[] matricesBuffers, List<Color> colors, Gradient gradient);
    
    private static readonly Function[] _functions =
    {
        Greyscale,
        Interpolate,
        Gradient
    };
    */

    [SerializeField]
    private List<Color> _leafColors;

    [SerializeField]
    private Gradient _gradientA, _gradientB;

    [SerializeField, Range(0f, 90f)]
    private float _maxSagAngleA = 15f, _maxSagAngleB = 25f;

    // Native code
    NativeArray<FractalPart>[] _parts;
    // Since the last row is always (0, 0, 0, 1), we optimize by instead creating a 3x4 then constructing the last row
    NativeArray<float3x4>[] _matrices;

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
        _matricesBuffers = new ComputeBuffer[_depth];

        int stride = /*float3x4=*/12 * sizeof(float);

        _sequenceNumbers = new Vector4[_depth];

        // At the top level [0], there is only 1 fractal part, then each subsequent level gets 5 children per fractal part
        // Length is the number of fractals at the current level i
        for (int i = 0, length = 1; i < _parts.Length; i++, length *= 5)
        {
            _parts[i] = new NativeArray<FractalPart>(length, Allocator.Persistent);
            _matrices[i] = new NativeArray<float3x4>(length, Allocator.Persistent);
            _matricesBuffers[i] = new ComputeBuffer(length, stride);
                                              // Color A    // Color B    // Smooth 1   // Smooth 2
            _sequenceNumbers[i] = new Vector4(Random.value, Random.value, Random.value, Random.value);
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
        _sequenceNumbers = null;
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
        maxSagAngle = radians(Random.Range(_maxSagAngleA, _maxSagAngleB)),
        rotation = rotations[childIndex],
        spinVelocity = (Random.value < _reverseChance ? -1f : 1f) * radians(Random.Range(_rotationSpeedA, _rotationSpeedB)),

    };

    private void Update()
    {
        // For animation
        float deltaTime = Time.deltaTime;
        
        // Initialize first fractal part
        FractalPart rootPart = _parts[0][0];
        rootPart.spinAngle += rootPart.spinVelocity * deltaTime;
        rootPart.worldRotation = mul(transform.rotation, mul(rootPart.rotation, quaternion.RotateY(rootPart.spinAngle)));
        rootPart.worldPosition = transform.position;
        _parts[0][0] = rootPart;

        // Transformation Matrix: Translation-Rotation-Scale
        float objectScale = transform.lossyScale.x;

        float3x3 r = float3x3(rootPart.worldRotation) * objectScale;
        _matrices[0][0] = float3x4(r.c0, r.c1, r.c2, rootPart.worldPosition);

        float scale = objectScale;

        // Used to track jobs
        JobHandle jobHandle = default;
        for (int levelIndex = 1; levelIndex < _parts.Length; levelIndex++)
        {
            scale *= 0.5f;

            jobHandle = new UpdateFractalLevelJob
            {
                deltaTime = deltaTime,
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

        int leafIndex = _matricesBuffers.Length - 1;

        // Send matrix data from CPU to compute buffers on GPU and draw it procedurally
        for (int i = 0; i < _matricesBuffers.Length; i++)
        {
            ComputeBuffer buffer = _matricesBuffers[i];
            buffer.SetData(_matrices[i]);

            float interpolator = i / (_matricesBuffers.Length - 1.0f);

            Color colA, colB;
            Mesh mesh;
            if (i == leafIndex)
            {
                colA = _leafColors[0];
                colB = _leafColors[1];
                mesh = _leafMesh;
            }
            else
            {
                colA = _gradientA.Evaluate(interpolator);
                colB = _gradientB.Evaluate(interpolator);
                mesh = _mesh;
            }
            _propertyBlock.SetColor(_baseColorID, colA);
            _propertyBlock.SetColor(_secondaryColorID, colB);

            _propertyBlock.SetBuffer(_matricesID, buffer);
            _propertyBlock.SetVector(_sequenceNumbersID, _sequenceNumbers[i]);
            
            Graphics.DrawMeshInstancedProcedural(mesh, 0, _material, bounds, buffer.count, _propertyBlock);
        }
    }
}