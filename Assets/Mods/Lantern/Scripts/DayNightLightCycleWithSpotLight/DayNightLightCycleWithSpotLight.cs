using UnityEngine;
using System.Collections;
using Bindito.Core;
using Timberborn.Buildings;
using Timberborn.TimeSystem;
using Timberborn.SingletonSystem;
using Timberborn.Rendering;
using Timberborn.BlockSystem;

public class DayNightLightCycleWithSpotLight : MonoBehaviour, IFinishedStateListener
{
    public Light targetLight;  // 点滅させるライトを指定
    public float hueChangeSpeed = 0f;  // 色相の変化速度
    public float fadeDuration = 1.0f;  // フェードの持続時間（1速の秒）
    private BuildingLightToggle _buildingLightToggle;
    private BuildingLighting _buildingLighting;
    private MaterialColorer _materialColorer;
    private float _currentHue = 0.0f;
    private float _fadeTimer = 0.0f;
    private bool _isFadingIn = false;
    private bool _isLightOn = false;
    private bool _isBuildingComplete = false;
    private float _defaultLightIntensity = 0.0f;
    private EventBus _eventBus;
    private IDayNightCycle _dayNightCycle;
    private Vector3Int _coordinates;

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
            Debug.LogWarning("DayNightLightCycleWithSpotLight: BuildingLightingが見つかりませんでした。");
        }
        if (targetLight == null)
        {
            Debug.LogWarning("DayNightLightCycleWithSpotLight: targetLightが設定されていません。");
        }
        else
        {
            _defaultLightIntensity = targetLight.intensity;
        }
        // イベント登録
        _eventBus.Register(this);
        // フェードの更新
        if (_dayNightCycle.IsNighttime && _isBuildingComplete)
        {
            ImmediateLightOn();
        }
        else
        {
            ImmediateLightOff();
        }
        // このインスタンス自身のブロックの座標を取得
        BlockObject blockObject = GetComponent<BlockObject>();
        if (blockObject != null)
        {
            _coordinates = blockObject.Coordinates;
        }
        else
        {
            Debug.LogWarning("DayNightLightCycleWithSpotLight: BlockObjectが見つかりませんでした。");
        }
    }
    public void OnEnterFinishedState()
    {
        _isBuildingComplete = true;
        if (_dayNightCycle != null)
        {
            if (_dayNightCycle.IsNighttime && !_isLightOn)
            {
                ImmediateLightOn();
            }
            else if (!_dayNightCycle.IsNighttime && _isLightOn)
            {
                ImmediateLightOff();
            }
        }
    }
    public void OnExitFinishedState()
    {
        _isBuildingComplete = false;
        _eventBus.Unregister(this);
    }
    void OnDestroy()
    {
        // イベント登録解除
        _eventBus.Unregister(this);
    }
    void Update()
    {
        // 色相の更新
        //_currentHue += hueChangeSpeed * Time.deltaTime;
        //if (_currentHue > 1.0f)
        //{
        //    _currentHue -= 1.0f;
        //}
        //if (_materialColorer != null)
        //{
        //    _materialColorer.SetLightingHueOffset(gameObject, _currentHue);
        //}
        // UpdateLightColor();
        if (_fadeTimer < fadeDuration && _isBuildingComplete)
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
    private void ImmediateLightOn()
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
            Debug.LogWarning("DayNightLightCycleWithSpotLight: BuildingLightToggleが初期化されていません。");
        }
    }
    private void ImmediateLightOff()
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
            StartCoroutine(WaitAndTurnOnLight());
        }
    }

    private IEnumerator WaitAndTurnOnLight()
    {

        // 山の影に入る所ほどはやく点灯させる
        float adjustedX = Mathf.Max(0, 256f - _coordinates.x) * 0.5f;
        float adjustedY = Mathf.Max(0, 256f - _coordinates.y);
        float distance = Mathf.Sqrt(adjustedX * adjustedX + adjustedY * adjustedY);
        float maxDistance = 286f;
        float WaitTime = (distance / maxDistance) * 30.0f;
        yield return new WaitForSeconds(WaitTime);
        _fadeTimer = 0.0f;
        _isFadingIn = true;
        yield return new WaitForSeconds(0.05f);
        _buildingLightToggle.TurnOn();

    }
    private void StartFadeOut()
    {
        if (_isLightOn)
        {
            StartCoroutine(WaitAndTurnOffLight());
        }
    }

    private IEnumerator WaitAndTurnOffLight()
    {
        // 0秒から5秒の間でランダムに待機
        float WaitTime = UnityEngine.Random.Range(0f, 5f);
        yield return new WaitForSeconds(WaitTime);
        _fadeTimer = 0.0f;
        _isFadingIn = false;
        yield return new WaitForSeconds(0.05f);
        _buildingLightToggle.TurnOn();

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