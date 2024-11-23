using UnityEngine;
using System.Collections.Generic;
using Timberborn.WindSystem;
using Bindito.Core;
using Timberborn.BlockSystem;


public class WindSwingAnimator : MonoBehaviour, IFinishedStateListener
{
    [SerializeField] private Transform SwingObjectParent; // �e�I�u�W�F�N�g���w��
    [SerializeField] private string childObjectPrefix = "#Empty"; // �q�v�f�̐ړ���
    [SerializeField] private float baseSwingSpeed = 2.0f; // �h��鑬��
    [SerializeField] private float baseSwingAmplitude = 10.0f; // �h�ꕝ�̊�{�l
    [SerializeField] private float maxSwingAmplitude = 25.0f; // �ő�U����ǉ�
    [SerializeField] private float maxTiltAngle = 15.0f; // �ő�X���x
    [SerializeField] private float threshold = 0.2f; // ���m����ŏ��̕���

    private Vector2 windDirection;
    private float windStrength;
    private List<Transform> SwingObjects;
    private float[] swingOffsets;
    private float[] currentAmplitudes;
    private float[] swingSpeeds;
    private float[] tiltAngles; // �X���̊p�x��ێ�����z��
    private bool _isBuildingComplete = false;
    private WindService _windService;
    private bool _wasWindStrengthBelowThreshold = false;

    [Inject]
    public void InjectDependencies(WindService windService)
    {
        _windService = windService;
    }

    void Start()
    {
        SwingObjects = new List<Transform>();
        FindChildSwingObjects(SwingObjectParent, childObjectPrefix);

        int SwingObjectCount = SwingObjects.Count;
        swingOffsets = new float[SwingObjectCount];
        currentAmplitudes = new float[SwingObjectCount];
        swingSpeeds = new float[SwingObjectCount];
        tiltAngles = new float[SwingObjectCount]; // �X���p�x�̏�����

        for (int i = 0; i < SwingObjectCount; i++)
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
        if (_isBuildingComplete)
        {
            UpdateWind();

            // ������臒l�𒴂��Ă���ꍇ�̂ݒ�������
            if (windStrength > threshold)
            {
                windStrength = Mathf.Max(0, windStrength - threshold) * (1 / (1 - threshold) );

                // ������臒l�𒴂����ꍇ�̓����^���̗h����X�V
                UpdateSwingObjectsSwing();
                _wasWindStrengthBelowThreshold = false; // �t���O�����Z�b�g
            }
            else if (!_wasWindStrengthBelowThreshold)
            {
                // ����̂݃����^���̗h����X�V
                UpdateSwingObjectsSwing();
                _wasWindStrengthBelowThreshold = true; // �t���O��ݒ�
            }
        }
    }

    private void UpdateWind()
    {
        // ���̕����Ƌ������擾
        windDirection = GetWindDirection();
        windStrength = GetWindStrength();
    }

    private void UpdateSwingObjectsSwing()
    {
        // ���̕��������[���h��Ԃœ���
        float windAngle = Vector2.SignedAngle(Vector2.down, windDirection);
        Quaternion windRotation = Quaternion.Euler(0, windAngle, 0);

        // �h�ꕝ�̖ڕW�U�����v�Z
        float targetAmplitude = Mathf.Lerp(baseSwingAmplitude, maxSwingAmplitude, windStrength);

        for (int i = 0; i < SwingObjects.Count; i++)
        {
            if (SwingObjects[i] == null) continue;

            // �X���𕗂̋����ɉ����čX�V�i�X���̊p�x��ێ��j
            tiltAngles[i] = Mathf.Lerp(tiltAngles[i], windStrength * maxTiltAngle, Time.deltaTime * 2);
            Quaternion tiltRotation = Quaternion.AngleAxis(tiltAngles[i], Vector3.left); // �����v����90�x��

            // ���̋����ɉ����ĐU�ꕝ�ƃX�s�[�h�𒲐�
            float swingAngle = Mathf.Sin(Time.time * swingSpeeds[i] * (1.0f + windStrength) + swingOffsets[i]) * currentAmplitudes[i] * windStrength;

            // �ŏI�I�ȉ�]��K�p (����: ������ -> �X�� -> �h��)
            SwingObjects[i].rotation = windRotation * tiltRotation * Quaternion.AngleAxis(swingAngle, Vector3.right);

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

    private void FindChildSwingObjects(Transform parent, string prefix)
    {
        foreach (Transform child in parent)
        {
            if (child.name.StartsWith(prefix))
            {
                SwingObjects.Add(child);
            }
            FindChildSwingObjects(child, prefix); // �ċA�I�Ɏq�v�f��T��
        }
    }
}
