using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIObjectPulse : MonoBehaviour
{
    public GameObject targetButton;
    public float scaleAmount = 1.1f;
    public float pulseDuration = 0.5f;

    void Start()
    {
        // Ќачинаем бесконечную анимацию пульсации
        targetButton.transform
            .DOScale(Vector3.one * scaleAmount, pulseDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

}
