using Assets.Scripts;
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

    [Header("Setup")]
    public float _maxDistance = 1000f;
    [Range(1, 500)]
    public int _maxIterations = 250;
    [Range(0.1f, 0.001f)]
    public float _accuracy = 0.01f;
    public Vector3 _repeatInterval = new Vector3(40f, 40f, 40f);

    [Header("Directional Light")]
    public Transform _lightDir;
    public Color _lightColor = new Color(0.81f, 0.79f, 0.36f);
    [Range(0, 4)]
    public float _lightIntensity = 1.5f;

    [Header("Shadow")]
    [Range(1, 128)]
    public float _shadowPenumbra = 96;
    public Vector2 _shadowDistance = new Vector2(0.1f, 100f);
    [Range(0, 4)]
    public float _shadowIntensity = 1f;

    [Header("Ambient Occlusion")]
    [Range(0.01f, 10.0f)]
    public float _ambientOcclusionStepSize = 0.3f;
    [Range(0f, 1f)]
    public float _ambientOcclusionIntensity = 0.275f;
    [Range(1, 5)]
    public int _ambientOcclusionIterations = 3;

    [Header("Reflection")]
    [Range(0, 4)]
    public int _reflectionCount = 2;
    [Range(0f, 1f)]
    public float _reflectionIntensity = 0.15f;
    [Range(0f, 1f)]
    public float _envReflectionIntensity = 0.5f;
    public Cubemap _reflectionCube;

    [Header("Fog")]
    [Range(0f, 1f)]
    public float _fogIntensity = 0.5f;
    public Color _fogColor = new Color(0.6f, 0.11f, 0.4f);
    [Range(1f, 5000f)]
    public float _fogMinDistance = 50f;
    [Range(1f, 5000f)]
    public float _fogMaxDistance = 250f;

    [Header("Mandelbulb")]
    public Transform _mandelbulb;
    public float _mandelbulbW;
    [Range(1, 15)]
    public int _mandelbulbIterations;
    [Range(1, 100)]
    public int _mandelbulbExponent;
    public Color _mandelbulbColor = new Color(0.82f, 0.41f, 0.45f);
    public bool _mandelbulbRepeat = true;

    [Header("Mandelbox")]
    public Transform _mandelbox;
    [Range(0f, 10f)]
    public float _mandelboxW = 2f;
    [Range(1, 10)]
    public int _mandelboxIterations = 6;
    public Color _mandelboxColor = new Color(0.57f, 0.92f, 0.57f);
    public bool _mandelboxRepeat = true;

    [Header("Spheres")]
    public Transform _sphere;
    [Range(0f, 100f)]
    public float _sphereRadius = 3f;
    [Range(0f, 30f)]
    public float _sphereSmooth = 12f;
    [Range(0f, 360f)]
    public float _sphereRotate = 45f;
    public Gradient _sphereGradient;
    public bool _sphereRepeat = false;

    private Color[] _sphereColors = new Color[8];

    [Header("Color")]
    public Color _groundColor;

    [Range(0, 4)]
    public float _colorIntensity;

    RenderTexture myRenderTexture;
    void OnPreRender()
    {
        myRenderTexture = RenderTexture.GetTemporary(Screen.width, Screen.height, 16);
        _camera.targetTexture = myRenderTexture;
    }
    void OnPostRender()
    {
        RayMarch();

        _camera.targetTexture = null; //null means framebuffer
        Graphics.Blit(myRenderTexture, null as RenderTexture, _rayMarchMaterial, 0);
        RenderTexture.ReleaseTemporary(myRenderTexture);
    }

    private void RayMarch()
    {
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

        //RenderTexture.active = destination;

        AddMainTexture(myRenderTexture);

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

        _rayMarchMaterial.Add(nameof(_repeatInterval), _repeatInterval);
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
        _rayMarchMaterial.Add(nameof(_mandelbulbRepeat), _mandelbulbRepeat ? 1 : 0);
    }

    private void AddMandelbox()
    {
        _rayMarchMaterial.Add(nameof(_mandelbox), new Vector4(_mandelbox.position.x, -_mandelbox.position.y, _mandelbox.position.z, _mandelboxW));
        _rayMarchMaterial.Add(nameof(_mandelboxIterations), _mandelboxIterations);
        _rayMarchMaterial.Add(nameof(_mandelboxColor), _mandelboxColor);
        _rayMarchMaterial.Add(nameof(_mandelboxRepeat), _mandelboxRepeat ? 1 : 0);
    }

    private void AddSpheres()
    {
        _rayMarchMaterial.Add(nameof(_sphere), new Vector4(_sphere.position.x, -_sphere.position.y, _sphere.position.z, _sphereRadius));
        _rayMarchMaterial.Add(nameof(_sphereSmooth), _sphereSmooth);
        _rayMarchMaterial.Add(nameof(_sphereRotate), _sphereRotate);
        _rayMarchMaterial.Add(nameof(_sphereColors), _sphereColors);
        _rayMarchMaterial.Add(nameof(_sphereRepeat), _sphereRepeat ? 1 : 0);
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
