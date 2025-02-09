using UnityEngine;

public class Graph3D : MonoBehaviour
{
    [SerializeField]
    private Transform _point, _graph;

    [SerializeField, Range(10, 100)]
    private int _resolution = 50;
    
    private const float RANGE = 2.0f; // -1 to +1

    // Reference to an object that needs to be assigned
    private Transform[] points;

    [SerializeField]
    private FunctionLibrary.FunctionName _function = FunctionLibrary.FunctionName.Wave;

    // RANGE / 2.0f to set the starting point (in this case -1.f)
    private const float _startingPoint = - RANGE / 2.0f;
    private float _step = RANGE;
   
    private void Awake()
    {
        _step /= _resolution;

        Vector3 scale = Vector3.one * _step;

        // Create new object and assign reference
        // Converting to 3D: line of points becomes a plane of points, _resolution-length line * _resolution-length line
        points = new Transform[_resolution * _resolution];
        for (int i = 0; i < points.Length; i++)
        {
            Transform point = Instantiate(_point);
            points[i] = point;
            point.localScale = scale;
            point.SetParent(_graph, false);
        }

    }

    private void Update()
    {
        UpdateGraph();
    }

    private void UpdateGraph()
    {
        float t = Time.time;
        FunctionLibrary.Function func = FunctionLibrary.GetFunction(_function);

        float v = (.5f) * _step + _startingPoint;
        for (int i = 0, x = 0, z = 0; i < points.Length; i++, x++)
        {
            // Begin placing points on a new row once reaching the end
            if (x == _resolution)
            {
                x = 0;
                z += 1;
                v = (z + 0.5f) * _step - 1f;
            }

            // + .5f because position is at object's center
            float u = (x + .5f) * _step + _startingPoint;
            points[i].localPosition = func(u, v, t);
        }
    }
}
