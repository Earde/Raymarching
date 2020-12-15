using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
[ExecuteInEditMode]
public class RayMarchCamera : SceneViewFilter
{
    [SerializeField]
    private Shader _shader;

    private Material _rayMarchMat;
    public Material _rayMarchMaterial {
        get {
            if (!_rayMarchMat && _shader)
            {
                _rayMarchMat = new Material(_shader);
                _rayMarchMat.hideFlags = HideFlags.HideAndDontSave;
            }
            return _rayMarchMat;
        }
    }

    private Camera _cam;
    public Camera _camera {
        get {
            if (!_cam)
            {
                _cam = GetComponent<Camera>();
            }
            return _cam;
        }
    }

    private Transform _mb;
    public Transform _mandelbulb {
        get {
            if (!_mb)
            {
                _mb = GameObject.FindGameObjectWithTag("Mandelbulb").GetComponent<Transform>();
            }
            return _mb;
        }
    }

    private Transform _mbox;
    public Transform _mandelbox {
        get {
            if (!_mbox)
            {
                _mbox = GameObject.FindGameObjectWithTag("Mandelbox").GetComponent<Transform>();
            }
            return _mbox;
        }
    }

    private Transform _sp;
    public Transform _sphere {
        get {
            if (!_sp)
            {
                _sp = GameObject.FindGameObjectWithTag("Sphere").GetComponent<Transform>();
            }
            return _sp;
        }
    }

    [Header("Setup")]
    public float _maxDistance;
    [Range(1, 500)]
    public int _maxIterations;
    [Range(0.1f, 0.001f)]
    public float _accuracy;

    [Header("Directional Light")]
    public Transform _lightDir;
    public Color _lightColor;
    public float _lightIntensity;

    [Header("Shadow")]
    [Range(1, 128)]
    public float _shadowPenumbra;
    public Vector2 _shadowDistance;
    [Range(0, 4)]
    public float _shadowIntensity;

    [Header("Ambient Occlusion")]
    [Range(0.01f, 10.0f)]
    public float _ambientOcclusionStepSize;
    [Range(0f, 1f)]
    public float _ambientOcclusionIntensity;
    [Range(1, 5)]
    public int _ambientOcclusionIterations;

    [Header("Reflection")]
    [Range(0, 4)]
    public int _reflectionCount;
    [Range(0f, 1f)]
    public float _reflectionIntensity;
    [Range(0f, 1f)]
    public float _envReflectionIntensity;
    public Cubemap _reflectionCube;

    [Header("Fog")]
    [Range(0f, 1f)]
    public float _fogIntensity;
    public Color _fogColor;
    public float _fogMinDistance;
    public float _fogMaxDistance;

    [Header("Mandelbulb")]
    public float _mandelbulbW;
    [Range(1, 15)]
    public int _mandelbulbIterations;
    public int _mandelbulbExponent;
    public Color _mandelbulbColor;

    [Header("Mandelbox")]
    public float _mandelboxW;
    [Range(1, 10)]
    public int _mandelboxIterations;
    public Color _mandelboxColor;

    [Header("Spheres")]
    public float _sphereW;
    [Range(0f, 30f)]
    public float _sphereSmooth;
    public float _degreeRotate;
    public Vector3 _sphereModInterval;
    public Gradient _sphereGradient;
    private Color[] _sphereColors = new Color[8];

    [Header("Color")]
    public Color _groundColor;

    [Range(0, 4)]
    public float _colorIntensity;

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!_rayMarchMaterial)
        {
            Graphics.Blit(source, destination);
            return;
        }

        for (int i = 0; i < _sphereColors.Length; i++)
        {
            _sphereColors[i] = _sphereGradient.Evaluate((1f / _sphereColors.Length) * i);
        }

        AddCamera();
        AddSetup();
        AddLight();
        AddShadow();
        AddAmbientOcclusion();
        AddReflections();
        AddFog();
        AddColors();
        AddSpheres();
        AddMandelbox();
        AddMandelbulb();

        RenderTexture.active = destination;

        AddMainTexture(source);

        GL.PushMatrix();
        GL.LoadOrtho();
        _rayMarchMaterial.SetPass(0);
        GL.Begin(GL.QUADS);

        //Onder links
        GL.MultiTexCoord2(0, 0.0f, 0.0f);
        GL.Vertex3(0.0f, 0.0f, 3.0f);
        //Onder rechts
        GL.MultiTexCoord2(0, 1.0f, 0.0f);
        GL.Vertex3(1.0f, 0.0f, 2.0f);
        //Boven rechts
        GL.MultiTexCoord2(0, 1.0f, 1.0f);
        GL.Vertex3(1.0f, 1.0f, 1.0f);
        //Boven links
        GL.MultiTexCoord2(0, 0.0f, 1.0f);
        GL.Vertex3(0.0f, 1.0f, 0.0f);

        GL.End();
        GL.PopMatrix();
    }

    private void AddMainTexture(Texture source)
    {
        _rayMarchMaterial.Add("_MainTex", source);
    }

    private void AddFog()
    {
        _rayMarchMaterial.Add(nameof(_fogIntensity), _fogIntensity);
        _rayMarchMaterial.Add(nameof(_fogColor), _fogColor);
        _rayMarchMaterial.Add(nameof(_fogMinDistance), _fogMinDistance);
        _rayMarchMaterial.Add(nameof(_fogMaxDistance), _fogMaxDistance);
    }

    private void AddLight()
    {
        _rayMarchMaterial.Add(nameof(_lightColor), _lightColor);
        _rayMarchMaterial.Add(nameof(_lightDir), _lightDir ? _lightDir.forward : Vector3.down);
        _rayMarchMaterial.Add(nameof(_lightIntensity), _lightIntensity);
    }

    private void AddCamera()
    {
        _rayMarchMaterial.Add("_CamFrustum", CamFrustum(_camera));
        _rayMarchMaterial.Add("_CamToWorld", _camera.cameraToWorldMatrix);
    }

    private void AddSetup()
    {
        _rayMarchMaterial.Add(nameof(_maxDistance), _maxDistance);
        _rayMarchMaterial.Add(nameof(_maxIterations), _maxIterations);
        _rayMarchMaterial.Add(nameof(_accuracy), _accuracy);
    }

    private void AddShadow()
    {
        _rayMarchMaterial.Add(nameof(_shadowPenumbra), _shadowPenumbra);
        _rayMarchMaterial.Add(nameof(_shadowDistance), _shadowDistance);
        _rayMarchMaterial.Add(nameof(_shadowIntensity), _shadowIntensity);
    }

    private void AddAmbientOcclusion()
    {
        _rayMarchMaterial.Add(nameof(_ambientOcclusionStepSize), _ambientOcclusionStepSize);
        _rayMarchMaterial.Add(nameof(_ambientOcclusionIntensity), _ambientOcclusionIntensity);
        _rayMarchMaterial.Add(nameof(_ambientOcclusionIterations), _ambientOcclusionIterations);
    }

    private void AddReflections()
    {
        _rayMarchMaterial.Add(nameof(_reflectionCount), _reflectionCount);
        _rayMarchMaterial.Add(nameof(_reflectionIntensity), _reflectionIntensity);
        _rayMarchMaterial.Add(nameof(_envReflectionIntensity), _envReflectionIntensity);
        _rayMarchMaterial.Add(nameof(_reflectionCube), _reflectionCube);
}

    private void AddColors()
    {
        _rayMarchMaterial.Add(nameof(_groundColor), _groundColor);
        _rayMarchMaterial.Add(nameof(_colorIntensity), _colorIntensity);
    }

    private void AddMandelbulb()
    {
        _rayMarchMaterial.Add(nameof(_mandelbulb), new Vector4(_mandelbulb.position.x, -_mandelbulb.position.y, _mandelbulb.position.z, _mandelbulbW));
        _rayMarchMaterial.Add(nameof(_mandelbulbIterations), _mandelbulbIterations);
        _rayMarchMaterial.Add(nameof(_mandelbulbExponent), _mandelbulbExponent);
        _rayMarchMaterial.Add(nameof(_mandelbulbColor), _mandelbulbColor);
    }

    private void AddMandelbox()
    {
        _rayMarchMaterial.Add(nameof(_mandelbox), new Vector4(_mandelbox.position.x, -_mandelbox.position.y, _mandelbox.position.z, _mandelboxW));
        _rayMarchMaterial.Add(nameof(_mandelboxIterations), _mandelboxIterations);
        _rayMarchMaterial.Add(nameof(_mandelboxColor), _mandelboxColor);
    }

    private void AddSpheres()
    {
        _rayMarchMaterial.Add(nameof(_sphere), new Vector4(_sphere.position.x, -_sphere.position.y, _sphere.position.z, _sphereW));
        _rayMarchMaterial.Add(nameof(_sphereSmooth), _sphereSmooth);
        _rayMarchMaterial.Add(nameof(_degreeRotate), _degreeRotate);
        _rayMarchMaterial.Add(nameof(_sphereColors), _sphereColors);

        _rayMarchMaterial.Add(nameof(_sphereModInterval), _sphereModInterval);
}

    private Matrix4x4 CamFrustum(Camera cam)
    {
        Matrix4x4 frustum = Matrix4x4.identity;
        float fov = Mathf.Tan((cam.fieldOfView * 0.5f) * Mathf.Deg2Rad);

        Vector3 goUp = Vector3.up * fov;
        Vector3 goRight = Vector3.right * fov * cam.aspect;

        Vector3 TopLeft = (-Vector3.forward - goRight + goUp);
        Vector3 TopRight = (-Vector3.forward + goRight + goUp);
        Vector3 BotRight = (-Vector3.forward + goRight - goUp);
        Vector3 BotLeft = (-Vector3.forward - goRight - goUp);

        frustum.SetRow(0, TopLeft);
        frustum.SetRow(1, TopRight);
        frustum.SetRow(2, BotRight);
        frustum.SetRow(3, BotLeft);

        return frustum;
    }
}
