using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// ボタンを押した時に小さくなり、離すと元に戻るエフェクト
/// DOTweenを使用したスケールアニメーション
/// </summary>
public class ButtonScaleEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("スケール設定")]
    [SerializeField, Range(0.5f, 1f)]
    private float pressedScale = 0.9f;  // 押した時のスケール

    [Header("アニメーション設定")]
    [SerializeField, Range(0.01f, 0.5f)]
    private float duration = 0.1f;  // アニメーション時間

    [SerializeField]
    private Ease pressEase = Ease.OutQuad;  // 押す時のイージング
    
    [SerializeField]
    private Ease releaseEase = Ease.OutBack;  // 離す時のイージング（バウンス効果）

    [Header("オプション")]
    [SerializeField]
    private bool ignoreTimeScale = true;  // TimeScaleの影響を受けるか

    private Vector3 originalScale;
    private Tween currentTween;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    /// <summary>
    /// ボタンを押した時
    /// </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        currentTween?.Kill();
        currentTween = transform
            .DOScale(originalScale * pressedScale, duration)
            .SetEase(pressEase)
            .SetUpdate(ignoreTimeScale);
    }

    /// <summary>
    /// ボタンを離した時
    /// </summary>
    public void OnPointerUp(PointerEventData eventData)
    {
        currentTween?.Kill();
        currentTween = transform
            .DOScale(originalScale, duration)
            .SetEase(releaseEase)
            .SetUpdate(ignoreTimeScale);
    }

    private void OnDisable()
    {
        // 無効化時にアニメーションを止めてスケールをリセット
        currentTween?.Kill();
        transform.localScale = originalScale;
    }

    private void OnDestroy()
    {
        currentTween?.Kill();
    }
}
