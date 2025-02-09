// Prepares the compute shader for displaying the graph
using UnityEngine;

public class GPUGraph : MonoBehaviour
{
    // Shader fields
    [SerializeField]
    private ComputeShader _computeShader;

    private static readonly int
        _positionsShaderID = Shader.PropertyToID("_PositionsBuffer"),
        _resolutionID = Shader.PropertyToID("_Resolution"),
        _stepID = Shader.PropertyToID("_Step"),
        _timeID = Shader.PropertyToID("_Time"),
        _transitionProgressID = Shader.PropertyToID("_TransitionProgress");

    private ComputeBuffer _positionBuffer;

    // Graph fields
    [SerializeField]
    private Transform _graph;
    private const int MAXRESOLUTION = 1000;
    [SerializeField, Range(10, MAXRESOLUTION)]
    private int _resolution = 100;

    // Fields for procedural drawing on GPU  
    [SerializeField]
    Material _material;
    [SerializeField]
    Mesh _mesh;

    // Transition fields
    [SerializeField, Min(0)]
    private float _duration = 5.0f, _transitionDuration = 1.0f;
    private float _currentDuration = 0.0f;
    private bool _isTransitioning = false;

    // Function library fields
    [SerializeField]
    private FunctionLibrary.FunctionName _function = FunctionLibrary.FunctionName.Wave;
    private FunctionLibrary.FunctionName _funcTransitioningFrom;

    // OnEnable() survives hot reloads, to contrast with Awake()
    private void OnEnable()
    {
        // Each position is a 3-float vector
        _positionBuffer = new ComputeBuffer(MAXRESOLUTION * MAXRESOLUTION, 3*sizeof(float));
    }

    // Free GPU memory when not in use
    private void OnDisable()
    {
        _positionBuffer.Release();

        // Let Unity reclaim this object
        _positionBuffer = null;
    }

    private void Update()
    {
        _currentDuration += Time.deltaTime;
        if (_isTransitioning)
        {
            if (_currentDuration >= _transitionDuration)
            {
                _currentDuration -= _transitionDuration;
                _isTransitioning = false;
            }
        }
        
        else if (_currentDuration >= _duration)
        {
            // Reset timer
            _currentDuration -= _duration;

            _isTransitioning = true;
            _funcTransitioningFrom = _function;
            _function = FunctionLibrary.GetNextFunctionName(_function);
        }

        UpdateGPUFunctions();
    }

    private void UpdateGPUFunctions()
    {
        // [-1, 1] = 2 length range / _resolution number of cubes
        float _step = 2f / _resolution;
        
        // Copy parameters to the compute shader
        _computeShader.SetInt(_resolutionID, _resolution);
        _computeShader.SetFloat(_stepID, _step);
        _computeShader.SetFloat(_timeID, Time.time);
        
        if (_isTransitioning)
        {
            float progress = _currentDuration / _transitionDuration;
            _computeShader.SetFloat(_transitionProgressID, Mathf.SmoothStep(0f, 1f, progress));
        }

        // Links position buffer to kernel (main func of compute shader)
        // Multiply by 2 because there are 2 functions named after each type of surface
        // One for just the surface, and the other from the surface transitioning to another surface

        int kernelIndex = (_isTransitioning ? (int)_funcTransitioningFrom + FunctionLibrary.FunctionCount : (int)_function);
        
        _computeShader.SetBuffer(kernelIndex, _positionsShaderID, _positionBuffer);

        // Determine number of x and y 8-length groups needed
        int groups = Mathf.CeilToInt(_resolution / 8f);
        _computeShader.Dispatch(kernelIndex, groups, groups, 1);

        // Set material properties
        _material.SetBuffer(_positionsShaderID, _positionBuffer);
        _material.SetFloat(_stepID, _step);

        // Creates a bounding box centered at 0 with width 2 to draw graph inside
        Bounds boundingBox = new Bounds(Vector3.zero, Vector3.one * (2f + _step));
        Graphics.DrawMeshInstancedProcedural(_mesh, 0, _material, boundingBox,_resolution*_resolution);
    }
}