using System.Collections;
using UnityEngine;

public class ShakeZ : MonoBehaviour
{
    [Header("Основные настройки")]
    public float amplitude = 10f;
    public float frequency = 1f;

    [Header("Режим с паузами")]
    public float shakeDuration = 0.5f;
    public float pauseDuration = 1f;

    [Header("Переключение режимов")]
    public bool useShakeWithPause = false;

    private RectTransform rectTransform;
    private float startAngle;
    private Vector3 startRotation;
    private Coroutine shakeRoutine;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        startRotation = rectTransform.localEulerAngles;
        startAngle = startRotation.z;
    }

    void OnEnable()
    {
        // При включении объекта запускаем тряску
        if (useShakeWithPause)
        {
            shakeRoutine = StartCoroutine(ShakeWithPause());
        }
    }

    void OnDisable()
    {
        // Останавливаем тряску при выключении объекта
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
        }
    }

    void Update()
    {
        if (!useShakeWithPause && gameObject.activeInHierarchy)
        {
            float angleZ = startAngle + amplitude * Mathf.Sin(Time.time * frequency * 2 * Mathf.PI);
            Vector3 rot = rectTransform.localEulerAngles;
            rot.z = angleZ;
            rectTransform.localEulerAngles = rot;
        }
    }

    public void SetShakeMode(bool withPause)
    {
        useShakeWithPause = withPause;

        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
        }

        if (useShakeWithPause && gameObject.activeInHierarchy)
        {
            shakeRoutine = StartCoroutine(ShakeWithPause());
        }
    }

    IEnumerator ShakeWithPause()
    {
        while (true)
        {
            float timer = 0f;

            while (timer < shakeDuration)
            {
                float angleZ = startAngle + amplitude * Mathf.Sin(Time.time * frequency * 2 * Mathf.PI);
                Vector3 rot = rectTransform.localEulerAngles;
                rot.z = angleZ;
                rectTransform.localEulerAngles = rot;

                timer += Time.deltaTime;
                yield return null;
            }

            // Плавный возврат
            float t = 0f;
            float currentZ = rectTransform.localEulerAngles.z;
            while (t < 1f)
            {
                float smoothZ = Mathf.LerpAngle(currentZ, startAngle, t);
                Vector3 rot = rectTransform.localEulerAngles;
                rot.z = smoothZ;
                rectTransform.localEulerAngles = rot;

                t += Time.deltaTime * 3.7f;
                yield return null;
            }

            Vector3 finalRot = rectTransform.localEulerAngles;
            finalRot.z = startAngle;
            rectTransform.localEulerAngles = finalRot;

            yield return new WaitForSeconds(pauseDuration);
        }
    }
}
