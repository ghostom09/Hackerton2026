using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>중앙 UI Image에 4개의 스프라이트를 순서대로 표시합니다.</summary>
public class EmergencyImageSequence : MonoBehaviour
{
    [SerializeField] private Image patientImage;
    [SerializeField] private Sprite[] displaySequence = new Sprite[4];
    [SerializeField, Min(0.05f)] private float imageDisplayDuration = 0.8f;
    [SerializeField, Min(0f)] private float intervalBetweenImages = 0.2f;
    [SerializeField] private bool hideWhenFinished = true;
    [SerializeField] private bool randomizeOrder = true;

    public event Action Shown;
    public Sprite[] DisplaySequence => runtimeSequence;

    private Coroutine playRoutine;
    private Sprite[] runtimeSequence;

    public void Play()
    {
        if (!HasValidSetup())
            return;

        if (playRoutine != null)
            StopCoroutine(playRoutine);

        runtimeSequence = (Sprite[])displaySequence.Clone();
        if (randomizeOrder)
            Shuffle(runtimeSequence);

        playRoutine = StartCoroutine(PlayRoutine());
    }

    private void OnDisable()
    {
        if (playRoutine != null)
            StopCoroutine(playRoutine);

        playRoutine = null;
    }

    private IEnumerator PlayRoutine()
    {
        patientImage.gameObject.SetActive(true);

        for (int i = 0; i < runtimeSequence.Length; i++)
        {
            patientImage.sprite = runtimeSequence[i];
            yield return new WaitForSeconds(imageDisplayDuration);

            if (i < runtimeSequence.Length - 1 && intervalBetweenImages > 0f)
                yield return new WaitForSeconds(intervalBetweenImages);
        }

        if (hideWhenFinished)
            patientImage.gameObject.SetActive(false);

        playRoutine = null;
        Shown?.Invoke();
    }

    private bool HasValidSetup()
    {
        if (patientImage == null || displaySequence == null || displaySequence.Length != 4)
        {
            Debug.LogError("EmergencyImageSequence: 중앙 Image와 표시 이미지 4개를 연결하세요.", this);
            return false;
        }

        for (int i = 0; i < displaySequence.Length; i++)
        {
            if (displaySequence[i] == null)
            {
                Debug.LogError($"EmergencyImageSequence: {i + 1}번 이미지가 비어 있습니다.", this);
                return false;
            }
        }

        return true;
    }

    private static void Shuffle(Sprite[] images)
    {
        for (int i = images.Length - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            (images[i], images[randomIndex]) = (images[randomIndex], images[i]);
        }
    }
}
