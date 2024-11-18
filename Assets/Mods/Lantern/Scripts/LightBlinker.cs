using UnityEngine;
using Bindito.Core;
using Timberborn.Buildings;
using Timberborn.TimeSystem;
using Timberborn.SingletonSystem;
using Timberborn.Rendering;
using Timberborn.BlockSystem;
using Timberborn.BaseComponentSystem;

public class LightBlinker : MonoBehaviour, IFinishedStateListener
{
    public Light targetLight;  // 点滅させるライトを指定
    public float hueChangeSpeed = 0f;  // 色相の変化速度
    public float fadeDuration = 1.0f;  // フェードの持続時間（秒）

    private BuildingLightToggle _buildingLightToggle;
    private BuildingLighting _buildingLighting;
    private MaterialColorer _materialColorer;
    private float _currentHue = 0.0f;
    private float _fadeTimer = 0.0f;
    private bool _isFadingIn = false;
    private bool _isLightOn = false;
    private float _defaultLightIntensity = 0.0f;

    private EventBus _eventBus;
    private IDayNightCycle _dayNightCycle;

    [Inject]
    public void InjectDependencies(EventBus eventBus, IDayNightCycle dayNightCycle, MaterialColorer materialColorer)
    {
        _eventBus = eventBus;
        _dayNightCycle = dayNightCycle;
        _materialColorer = materialColorer;
    }

    void Start()
    {
        _buildingLighting = GetComponent<BuildingLighting>();
        if (_buildingLighting != null)
        {
            _buildingLightToggle = _buildingLighting.GetBuildingLightToggle();
        }
        else
        {
            Debug.LogWarning("LightBlinker: BuildingLightingが見つかりませんでした。");
        }

        if (targetLight == null)
        {
            Debug.LogWarning("LightBlinker: targetLightが設定されていません。");
        }
        else
        {
            _defaultLightIntensity = targetLight.intensity;
        }

        // イベント登録
        _eventBus.Register(this);
        // フェードの更新
        if (_dayNightCycle.IsNighttime)
        {
            StartFadeInInstant();
        }
        else
        {
            StartFadeOutInstant();
        }
    }

    public void OnEnterFinishedState()
    {
        if (_dayNightCycle != null)
        {
            if (_dayNightCycle.IsNighttime && !_isLightOn)
            {
                StartFadeIn();
            }
            else if (!_dayNightCycle.IsNighttime && _isLightOn)
            {
                StartFadeOutInstant();
            }
        }
    }

    public void OnExitFinishedState()
    {
        _eventBus.Unregister(this);
    }

    void OnDestroy()
    {
        // イベント登録解除
        _eventBus.Unregister(this);
    }

    void Update()
    {
        if (_fadeTimer < fadeDuration)
        {
            _fadeTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_fadeTimer / fadeDuration);
            float intensity = _isFadingIn ? t : 1.0f - t;

            if (_buildingLighting != null)
            {
                _buildingLighting.SetLightingStrength(intensity);
            }

            if (targetLight != null)
            {
                targetLight.intensity = intensity * _defaultLightIntensity;
            }

            if (_fadeTimer >= fadeDuration)
            {
                _fadeTimer = fadeDuration;  // 確実に終了状態にする
                if (_isFadingIn)
                {
                    _isLightOn = true;
                    _isFadingIn = false;
                }
                else
                {
                    _buildingLightToggle.TurnOff();
                    _isLightOn = false;
                }
            }
        }
    }

    [OnEvent]
    public void OnNighttimeStartEvent(NighttimeStartEvent nighttimeStartEvent)
    {
        StartFadeIn();
    }

    [OnEvent]
    public void OnDayTimeStartEvent(DaytimeStartEvent daytimeStartEvent)
    {
        StartFadeOut();
    }

    private void StartFadeInInstant()
    {
        if (_buildingLightToggle != null)
        {
            _buildingLightToggle.TurnOn();
            _isLightOn = true;
            _isFadingIn = false;
            _fadeTimer = fadeDuration;
            if (targetLight != null)
            {
                targetLight.intensity = _defaultLightIntensity;
            }
        }
        else
        {
            Debug.LogWarning("LightBlinker: BuildingLightToggleが初期化されていません。");
        }
    }

    private void StartFadeOutInstant()
    {
        if (_buildingLightToggle != null)
        {
            _buildingLightToggle.TurnOff();
            _isLightOn = false;
            _isFadingIn = false;
            _fadeTimer = fadeDuration;
            if (targetLight != null)
            {
                targetLight.intensity = 0.0f;
            }
        }
    }

    private void StartFadeIn()
    {
        if (!_isLightOn && _buildingLightToggle != null)
        {
            _buildingLightToggle.TurnOn();
            _isFadingIn = true;
            _fadeTimer = 0.0f;
        }
    }

    private void StartFadeOut()
    {
        if (_isLightOn)
        {
            _isFadingIn = false;
            _fadeTimer = 0.0f;
        }
    }

    void UpdateLightColor()
    {
        if (targetLight != null)
        {
            Color color = Color.HSVToRGB(_currentHue, 1.0f, 1.0f);
            targetLight.color = color;
        }
    }
}
