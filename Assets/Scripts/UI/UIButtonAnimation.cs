using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class UIButtonAnimation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private Vector3 originalScale;
    public float hoverScale = 1.1f;
    public float animationDuration = 0.4f;

    [Header("Entrance Animation")]
    public bool animateOnStart = true;
    public float startDelay = 0f;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        if (animateOnStart)
        {
            // start from zero and grow
            transform.localScale = Vector3.zero;
            StartCoroutine(EntranceAnimation());
        }
        else
        {
            transform.localScale = originalScale;
        }
    }

    private IEnumerator EntranceAnimation()
    {
        yield return new WaitForSecondsRealtime(startDelay);
        float timer = 0f;

        while (timer < animationDuration)
        {
            timer += Time.unscaledDeltaTime; // unscaled so it works when paused
            float t = Mathf.Clamp01(timer / animationDuration);

            // math formula for "pop" effect EaseOutBack
            // found on internet
            float c1 = 1.70158f;
            float c3 = c1 + 1;
            float x = t - 1;
            float ease = 1 + c3 * Mathf.Pow(x, 3) + c1 * Mathf.Pow(x, 2);

            transform.localScale = Vector3.Lerp(Vector3.zero, originalScale, ease);
            yield return null;
        }
        transform.localScale = originalScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // play sound on hover
        if (MusicManager.Instance != null) MusicManager.Instance.PlayUIHoverSfx();
        StopAllCoroutines();
        // make it big
        StartCoroutine(ScaleTo(originalScale * hoverScale));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StopAllCoroutines();
        // back to normal
        StartCoroutine(ScaleTo(originalScale));
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // play sound click
        if (MusicManager.Instance != null) MusicManager.Instance.PlayUIClickSfx();
    }

    private IEnumerator ScaleTo(Vector3 target)
    {
        float timer = 0f;
        Vector3 start = transform.localScale;
        float duration = 0.1f; // short time
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            transform.localScale = Vector3.Lerp(start, target, timer / duration);
            yield return null;
        }
        transform.localScale = target;
    }
}