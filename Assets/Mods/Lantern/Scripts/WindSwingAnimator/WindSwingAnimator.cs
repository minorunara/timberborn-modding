using UnityEngine;
using System.Collections.Generic;
using Timberborn.WindSystem;
using Bindito.Core;
using Timberborn.BlockSystem;
using ToriiGatesLanternMod;

public class WindSwingAnimator : MonoBehaviour, IFinishedStateListener
{
    [SerializeField] private Transform SwingObjectParent; // 親オブジェクトを指定
    [SerializeField] private string childObjectPrefix = "#Empty"; // 子要素の接頭辞
    [SerializeField] private float baseSwingSpeed = 2.0f; // 揺れる速さ
    [SerializeField] private float baseSwingAmplitude = 10.0f; // 揺れ幅の基本値
    [SerializeField] private float maxSwingAmplitude = 25.0f; // 最大振幅を追加
    [SerializeField] private float maxTiltAngle = 15.0f; // 最大傾き度
    [SerializeField] private float threshold = 0.2f; // 感知する最小の風速

    private Vector2 windDirection;
    private float windStrength;
    private List<Transform> SwingObjects;
    private float[] swingOffsets;
    private float[] currentAmplitudes;
    private float[] swingSpeeds;
    private float[] tiltAngles; // 傾きの角度を保持する配列
    private bool _isBuildingComplete = false;
    private WindService _windService;
    private WindSwingAnimatorSettings _modSettings;
    private bool _wasWindStrengthBelowThreshold = false;

    [Inject]
    public void InjectDependencies(WindService windService, WindSwingAnimatorSettings modSettings)
    {
        _windService = windService;
        _modSettings = modSettings;
    }

    void Start()
    {
        // 風揺れが有効かどうかを設定から確認
        if (!_modSettings.WindSwingEnabledSetting.Value)
        {
            this.enabled = false; // 風揺れが無効の場合、処理をスキップ
        }
        else
        {
            SwingObjects = new List<Transform>();
            FindChildSwingObjects(SwingObjectParent, childObjectPrefix);

            int SwingObjectCount = SwingObjects.Count;
            swingOffsets = new float[SwingObjectCount];
            currentAmplitudes = new float[SwingObjectCount];
            swingSpeeds = new float[SwingObjectCount];
            tiltAngles = new float[SwingObjectCount]; // 傾き角度の初期化

            for (int i = 0; i < SwingObjectCount; i++)
            {
                swingOffsets[i] = UnityEngine.Random.Range(0f, Mathf.PI * 2);
                currentAmplitudes[i] = baseSwingAmplitude;
                swingSpeeds[i] = baseSwingSpeed * UnityEngine.Random.Range(0.8f, 1.2f);
                tiltAngles[i] = 0f; // 初期傾き角度を設定
            }
        }
    }
    public void OnEnterFinishedState()
    {
        _isBuildingComplete = true;
        //InvokeRepeating(nameof(UpdateWind), 0f ,10f);
    }

    public void OnExitFinishedState()
    {
        _isBuildingComplete = false;
        //CancelInvoke(nameof(UpdateWind));
    }

    void Update()
    {
        if (_isBuildingComplete && _modSettings.WindSwingEnabledSetting.Value)
        {
            UpdateWind();

            // 風速が閾値を超えている場合のみ調整する
            if (windStrength > threshold)
            {
                if (windStrength > 1)
                {
                    windStrength = 1; // MODかなにかで風力が大きすぎる時対策
                }
                windStrength = Mathf.Max(0, windStrength - threshold) * (1 / (1 - threshold) );

                // 風速が閾値を超えた場合はランタンの揺れを更新
                UpdateSwingObjectsSwing();
            }
            else if (!_wasWindStrengthBelowThreshold)
            {
                windStrength = 0;
                UpdateSwingObjectsSwing();
            }
        }
    }

    private void UpdateWind()
    {
        // 風の方向と強さを取得
        windDirection = GetWindDirection();
        windStrength = GetWindStrength();
    }

    private void UpdateSwingObjectsSwing()
    {
        // 風の方向をワールド空間で統一
        float windAngle = Vector2.SignedAngle(Vector2.down, windDirection);
        Quaternion windRotation = Quaternion.Euler(0, windAngle, 0);

        // 揺れ幅の目標振幅を計算
        float targetAmplitude = Mathf.Lerp(baseSwingAmplitude, maxSwingAmplitude, windStrength);

        for (int i = 0; i < SwingObjects.Count; i++)
        {
            if (SwingObjects[i] == null) continue;

            // 傾きを風の強さに応じて更新（傾きの角度を保持）
            tiltAngles[i] = Mathf.Lerp(tiltAngles[i], windStrength * maxTiltAngle, Time.deltaTime * 2);
            Quaternion tiltRotation = Quaternion.AngleAxis(tiltAngles[i], Vector3.left); // 反時計回りに90度回す

            // 風の強さに応じて振れ幅とスピードを調整
            float swingAngle = Mathf.Sin(Time.time * swingSpeeds[i] * (1.0f + windStrength) + swingOffsets[i]) * currentAmplitudes[i] * windStrength;

            // 最終的な回転を適用 (順序: 風向き -> 傾き -> 揺れ)
            SwingObjects[i].rotation = windRotation * tiltRotation * Quaternion.AngleAxis(swingAngle, Vector3.right);

            // 揺れ幅を風の強さに応じて変化させる（最大振幅を設定）
            currentAmplitudes[i] = Mathf.Lerp(currentAmplitudes[i], targetAmplitude, Time.deltaTime * 2);
        }
    }


    private Vector2 GetWindDirection()
    {
        // 風の方向をWindServiceから取得して返す
        if (_windService != null)
        {
            return _windService.WindDirection;
        }
        return Vector2.zero; // WindServiceがない場合の仮の値
    }

    private float GetWindStrength()
    {
        // 風の強さをWindServiceから取得して返す
        if (_windService != null)
        {
            return _windService.WindStrength;
        }
        return 0.0f; // WindServiceがない場合の仮の値
    }

    private void FindChildSwingObjects(Transform parent, string prefix)
    {
        foreach (Transform child in parent)
        {
            if (child.name.StartsWith(prefix))
            {
                SwingObjects.Add(child);
            }
            FindChildSwingObjects(child, prefix); // 再帰的に子要素を探す
        }
    }
}
