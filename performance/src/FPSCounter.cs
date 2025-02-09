using UnityEngine;
using TMPro;

public class FPSCounter : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _text;

    // How many frames to include in the average
    [SerializeField, Range(0.1f, 2f)]
    private float _sampleDuration = 0.5f;

    public enum DisplayMode { FPS, MS };

    [SerializeField]
    private DisplayMode _displayMode = DisplayMode.FPS;

    private int _nFrames = 0;
    private float _totalTime = 0.0f;

    private float _bestTime = float.MaxValue;
    private float _worstTime = 0;

    private void Update()
    {
        // Want unscaled because delta time is subject to scaling (e.g., slow-mo, fast-forward, stop)
        float frameTime = Time.unscaledDeltaTime;
        _nFrames++;
        _totalTime += frameTime;

        if (frameTime < _bestTime)
            _bestTime = frameTime;
        if (frameTime > _worstTime)
            _worstTime = frameTime;

        if (_totalTime >= _sampleDuration)
        {
            string stats = $"";
            switch (_displayMode)
            {
                case DisplayMode.FPS:
                    stats += $"FPS\n" +
                    $"{(1.0f / _bestTime):0.0}\n" +                     // Best
                    $"{(_nFrames / _totalTime):0.0}\n" +                // Avg
                    $"{(1.0f / _worstTime):0.0}";                       // Worst
                break;

                case DisplayMode.MS:
                    stats += $"MS\n" +
                    $"{(1000f * _bestTime):0.0}\n" +                  // Best
                    $"{(1000f * _totalTime / _nFrames):0.0}\n" +      // Avg
                    $"{(1000f * _worstTime):0.0}";                    // Worst
                break;

                default:
                    break;
            }

            _text.SetText(stats);

            _nFrames = 0;
            _totalTime = 0.0f;
            _bestTime = float.MaxValue;
            _worstTime = 0;
        }
    }
}
