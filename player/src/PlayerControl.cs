using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerControl : MonoBehaviour
{

    [SerializeField]
    private List<GameObject> _fractals;

    [SerializeField]
    private TextMeshProUGUI _text;

    [SerializeField]
    private GameObject _uipanel;

    private int _fractal;

    private void Awake()
    {
        foreach (GameObject fractal in _fractals)
            fractal.SetActive(false);
        _fractal = _fractals.Count - 1;
        _fractals[_fractal].SetActive(true);
        _text.text = _fractals[_fractal].name;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            _fractals[_fractal].SetActive(false);
            _fractal = (_fractal + 1) % _fractals.Count;
            _fractals[_fractal].SetActive(true);
            _text.text = _fractals[_fractal].name;
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            bool isToggled = _uipanel.activeSelf;
            _uipanel.SetActive(!isToggled);
        }
    }
}
