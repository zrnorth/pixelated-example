using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum PixelScreenMode { Resize, Scale }

[System.Serializable]
public struct ScreenSize
{
    public int width;
    public int height;
}


public class PixelatedCamera : MonoBehaviour
{
    [Header("Canvas and Display")]
    public Camera canvasCamera;
    public RawImage display;

    [Header("Settings")]
    [SerializeField]
    private bool _enablePixelation = true;

    [ShowIf(ActionOnConditionFail.DontDraw, ConditionOperator.And, nameof(_enablePixelation))]
    [SerializeField]
    private PixelScreenMode _mode;
    // Used for the below ShowIfs
    private bool _isScaleMode() { return _mode == PixelScreenMode.Scale; }
    private bool _isResizeMode() { return _mode == PixelScreenMode.Resize; }

    [ShowIf(ActionOnConditionFail.DontDraw, ConditionOperator.And, nameof(_enablePixelation), nameof(_isScaleMode))]
    [SerializeField]
    private int _screenScaleFactor = 1;

    [ShowIf(ActionOnConditionFail.DontDraw, ConditionOperator.And, nameof(_enablePixelation), nameof(_isResizeMode))]
    [SerializeField]
    private ScreenSize _targetScreenSize = new ScreenSize { width = 320, height = 180 };

    private Camera _renderCamera;
    private RenderTexture _renderTexture;
    private ScreenSize _currentScreenSize;

    private void OnValidate() {
        if (_screenScaleFactor < 1) {
            Debug.LogWarning("Screen Scale Factor cannot be < 1. Resetting to 1");
            _screenScaleFactor = 1;
        }
        if (_targetScreenSize.width < 1 || _targetScreenSize.height < 1) {
            Debug.LogWarning("Target Screen Size cannot have a height or width < 1. Resetting to defaults");
            _targetScreenSize.width = 320;
            _targetScreenSize.height = 180;
        }
        Init();
    }

    private void Awake() {
        _renderCamera = GetComponent<Camera>();
        canvasCamera.gameObject.SetActive(true);
    }

    private void Start() {
        Init();
    }
    void Init() {
        // If there is no render cam, we can't do anything so early out
        if (_renderCamera == null) return;
        // Update current screen size and calculate the render texture size.
        // If we have pixelation disabled, just use the current screen size as the target size.
        _currentScreenSize.width = Screen.width;
        _currentScreenSize.height = Screen.height;


        int width = (_mode == PixelScreenMode.Resize ? _targetScreenSize.width : _currentScreenSize.width / _screenScaleFactor);
        int height = (_mode == PixelScreenMode.Resize ? _targetScreenSize.height : _currentScreenSize.height / _screenScaleFactor);

        // Init a new render text
        _renderTexture = new RenderTexture(width, height, 24) {
            filterMode = FilterMode.Point,
            antiAliasing = 1
        };

        // Set the render text as camera's output and attach it to the displayed RawImage
        _renderCamera.targetTexture = _renderTexture;
        display.texture = _renderTexture;
    }

    private void Update() {
        if (ScreenWasResized()) Init();
    }

    private bool ScreenWasResized() {
        return (Screen.width != _currentScreenSize.width || Screen.height != _currentScreenSize.height);
    }

    public void SetPixelated(bool val) {
        _enablePixelation = val;
        Init();
    }
}
