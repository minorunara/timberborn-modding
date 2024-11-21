using UnityEngine;
using System.Collections.Generic;
using Timberborn.WindSystem;
using Bindito.Core;
using Timberborn.BlockSystem;


public class WindSwingAnimator : MonoBehaviour, IFinishedStateListener
{
    [SerializeField] private Transform lanternParent; // 親オブジェクトを指定
    [SerializeField] private string childObjectPrefix = "#Empty"; // 子要素の接頭辞
    [SerializeField] private float baseSwingSpeed = 2.0f;
    [SerializeField] private float baseSwingAmplitude = 10.0f;
    [SerializeField] private float maxSwingAmplitude = 25.0f; // 最大振幅を追加
    [SerializeField] private float maxTiltAngle = 15.0f; // 最大傾き角度

    private Vector2 windDirection;
    private float windStrength;
    private List<Transform> lanterns;
    private float[] swingOffsets;
    private float[] currentAmplitudes;
    private float[] swingSpeeds;
    private float[] tiltAngles; // 傾きの角度を保持する配列
    private bool _isBuildingComplete = false;
    private WindService _windService;

    [Inject]
    public void InjectDependencies(WindService windService)
    {
        _windService = windService;
    }

    void Start()
    {
        lanterns = new List<Transform>();
        FindChildLanterns(lanternParent, childObjectPrefix);

        int lanternCount = lanterns.Count;
        swingOffsets = new float[lanternCount];
        currentAmplitudes = new float[lanternCount];
        swingSpeeds = new float[lanternCount];
        tiltAngles = new float[lanternCount]; // 傾き角度の初期化

        for (int i = 0; i < lanternCount; i++)
        {
            swingOffsets[i] = UnityEngine.Random.Range(0f, Mathf.PI * 2);
            currentAmplitudes[i] = baseSwingAmplitude;
            swingSpeeds[i] = baseSwingSpeed * UnityEngine.Random.Range(0.8f, 1.2f);
            tiltAngles[i] = 0f; // 初期傾き角度を設定
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
        if (_isBuildingComplete) {
            UpdateWind();
            UpdateLanternsSwing();
        }

    }

    private void UpdateWind()
    {
        // 風の方向と強さを取得
        windDirection = GetWindDirection();
        windStrength = GetWindStrength();
    }

    private void UpdateLanternsSwing()
    {
        // 風の方向をワールド空間で統一
        float windAngle = Vector2.SignedAngle(Vector2.down, windDirection);
        Quaternion windRotation = Quaternion.Euler(0, windAngle, 0);

        // 揺れ幅の目標振幅を計算
        float targetAmplitude = Mathf.Lerp(baseSwingAmplitude, maxSwingAmplitude, windStrength);

        for (int i = 0; i < lanterns.Count; i++)
        {
            if (lanterns[i] == null) continue;

            // 傾きを風の強さに応じて更新（傾きの角度を保持）
            tiltAngles[i] = Mathf.Lerp(tiltAngles[i], windStrength * maxTiltAngle, Time.deltaTime * 2);
            Quaternion tiltRotation = Quaternion.AngleAxis(tiltAngles[i], Vector3.left); // 反時計回りに90度回す

            // 風の強さに応じて振れ幅とスピードを調整
            float swingAngle = Mathf.Sin(Time.time * swingSpeeds[i] * (1.0f + windStrength) + swingOffsets[i]) * currentAmplitudes[i] * windStrength;

            // 最終的な回転を適用 (順序: 風向き -> 傾き -> 揺れ)
            lanterns[i].rotation = windRotation * tiltRotation * Quaternion.AngleAxis(swingAngle, Vector3.right);

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

    private void FindChildLanterns(Transform parent, string prefix)
    {
        foreach (Transform child in parent)
        {
            if (child.name.StartsWith(prefix))
            {
                lanterns.Add(child);
            }
            FindChildLanterns(child, prefix); // 再帰的に子要素を探す
        }
    }
}
