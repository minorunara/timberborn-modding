using UnityEngine;
using System.Collections.Generic;
using Timberborn.WindSystem;
using Bindito.Core;
using Timberborn.BlockSystem;


public class WindSwingAnimator : MonoBehaviour, IFinishedStateListener
{
    [SerializeField] private Transform lanternParent; // �e�I�u�W�F�N�g���w��
    [SerializeField] private string childObjectPrefix = "#Empty"; // �q�v�f�̐ړ���
    [SerializeField] private float baseSwingSpeed = 2.0f;
    [SerializeField] private float baseSwingAmplitude = 10.0f;
    [SerializeField] private float maxSwingAmplitude = 25.0f; // �ő�U����ǉ�
    [SerializeField] private float maxTiltAngle = 15.0f; // �ő�X���p�x

    private Vector2 windDirection;
    private float windStrength;
    private List<Transform> lanterns;
    private float[] swingOffsets;
    private float[] currentAmplitudes;
    private float[] swingSpeeds;
    private float[] tiltAngles; // �X���̊p�x��ێ�����z��
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
        tiltAngles = new float[lanternCount]; // �X���p�x�̏�����

        for (int i = 0; i < lanternCount; i++)
        {
            swingOffsets[i] = UnityEngine.Random.Range(0f, Mathf.PI * 2);
            currentAmplitudes[i] = baseSwingAmplitude;
            swingSpeeds[i] = baseSwingSpeed * UnityEngine.Random.Range(0.8f, 1.2f);
            tiltAngles[i] = 0f; // �����X���p�x��ݒ�
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
        // ���̕����Ƌ������擾
        windDirection = GetWindDirection();
        windStrength = GetWindStrength();
    }

    private void UpdateLanternsSwing()
    {
        // ���̕��������[���h��Ԃœ���
        float windAngle = Vector2.SignedAngle(Vector2.down, windDirection);
        Quaternion windRotation = Quaternion.Euler(0, windAngle, 0);

        // �h�ꕝ�̖ڕW�U�����v�Z
        float targetAmplitude = Mathf.Lerp(baseSwingAmplitude, maxSwingAmplitude, windStrength);

        for (int i = 0; i < lanterns.Count; i++)
        {
            if (lanterns[i] == null) continue;

            // �X���𕗂̋����ɉ����čX�V�i�X���̊p�x��ێ��j
            tiltAngles[i] = Mathf.Lerp(tiltAngles[i], windStrength * maxTiltAngle, Time.deltaTime * 2);
            Quaternion tiltRotation = Quaternion.AngleAxis(tiltAngles[i], Vector3.left); // �����v����90�x��

            // ���̋����ɉ����ĐU�ꕝ�ƃX�s�[�h�𒲐�
            float swingAngle = Mathf.Sin(Time.time * swingSpeeds[i] * (1.0f + windStrength) + swingOffsets[i]) * currentAmplitudes[i] * windStrength;

            // �ŏI�I�ȉ�]��K�p (����: ������ -> �X�� -> �h��)
            lanterns[i].rotation = windRotation * tiltRotation * Quaternion.AngleAxis(swingAngle, Vector3.right);

            // �h�ꕝ�𕗂̋����ɉ����ĕω�������i�ő�U����ݒ�j
            currentAmplitudes[i] = Mathf.Lerp(currentAmplitudes[i], targetAmplitude, Time.deltaTime * 2);
        }
    }


    private Vector2 GetWindDirection()
    {
        // ���̕�����WindService����擾���ĕԂ�
        if (_windService != null)
        {
            return _windService.WindDirection;
        }
        return Vector2.zero; // WindService���Ȃ��ꍇ�̉��̒l
    }

    private float GetWindStrength()
    {
        // ���̋�����WindService����擾���ĕԂ�
        if (_windService != null)
        {
            return _windService.WindStrength;
        }
        return 0.0f; // WindService���Ȃ��ꍇ�̉��̒l
    }

    private void FindChildLanterns(Transform parent, string prefix)
    {
        foreach (Transform child in parent)
        {
            if (child.name.StartsWith(prefix))
            {
                lanterns.Add(child);
            }
            FindChildLanterns(child, prefix); // �ċA�I�Ɏq�v�f��T��
        }
    }
}
