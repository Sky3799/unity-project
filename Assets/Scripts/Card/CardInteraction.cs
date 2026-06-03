using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 카드 한 장의 드래그/드롭 및 호버 인터랙션을 처리
/// IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler 구현
/// </summary>
[RequireComponent(typeof(Card))]
[RequireComponent(typeof(CanvasGroup))]
public class CardInteraction : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("드래그 설정")]
    [SerializeField] private Canvas rootCanvas;         // 루트 캔버스 (드래그 시 부모로 올라감)
    [SerializeField] private RectTransform dropZone;    // 카드를 드롭할 수 있는 영역

    [Header("흔들림 효과 (마나 부족 시)")]
    [SerializeField] private float shakeAmount = 8f;
    [SerializeField] private float shakeDuration = 0.35f;

    private Card card;
    private CardHand cardHand;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private Vector2 originalPosition;      // 손패에서의 원래 anchoredPosition
    private Quaternion originalRotation;   // 손패에서의 원래 회전값
    private int originalSiblingIndex;      // 형제 순서 (렌더링 순서)
    private Transform originalParent;      // 드래그 전 부모

    private bool isDragging = false;
    private Vector2 dragOffset;          // 클릭 지점과 카드 피벗 간 오프셋
    private Coroutine shakeCoroutine;
    private Coroutine returnCoroutine;

    // 손패가 정렬 시 읽고 쓰는 원래 위치
    public Vector2 OriginalPosition
    {
        get => originalPosition;
        set => originalPosition = value;
    }

    public RectTransform RectTransform => rectTransform;

    private void Awake()
    {
        card = GetComponent<Card>();
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    /// <summary>
    /// CardHand에서 초기화 시 호출 — 참조 주입
    /// </summary>
    public void Init(CardHand hand, Canvas canvas, RectTransform zone)
    {
        cardHand = hand;
        rootCanvas = canvas;
        dropZone = zone;
    }

    // ─── IPointerEnterHandler ──────────────────────────────────────
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isDragging || card.CurrentState == CardState.Disabled) return;
        card.SetState(CardState.Hover);
        cardHand?.OnCardHoverEnter(this);
    }

    // ─── IPointerExitHandler ───────────────────────────────────────
    public void OnPointerExit(PointerEventData eventData)
    {
        if (isDragging || card.CurrentState == CardState.Disabled) return;
        card.SetState(CardState.Normal);
        cardHand?.OnCardHoverExit(this);
    }

    // ─── IBeginDragHandler ─────────────────────────────────────────
    public void OnBeginDrag(PointerEventData eventData)
    {
        // 마나 부족 시 드래그 불가 → 흔들림 효과
        if (card.CurrentState == CardState.Disabled)
        {
            if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
            shakeCoroutine = StartCoroutine(ShakeEffect());
            return;
        }

        isDragging = true;
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();
        originalRotation = rectTransform.localRotation;

        // 캔버스 최상위로 올려 다른 UI 위에 렌더링
        transform.SetParent(rootCanvas.transform, true);
        transform.SetAsLastSibling();

        // 클릭 지점과 카드 피벗 간 오프셋 기록 (카드가 마우스에서 벗어나지 않도록)
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.GetComponent<RectTransform>(),
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 mouseLocalPos);
        dragOffset = rectTransform.anchoredPosition - mouseLocalPos;

        // 드래그 중 이 카드로의 레이캐스트 차단 해제 (다른 오브젝트 감지용)
        canvasGroup.blocksRaycasts = false;
        rectTransform.localRotation = Quaternion.identity;

        card.SetState(CardState.Selected);
        cardHand?.OnCardDragStart(this);
        cardHand?.ShowDropZone(true);
    }

    // ─── IDragHandler ──────────────────────────────────────────────
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.GetComponent<RectTransform>(),
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 mouseLocalPos);
        rectTransform.anchoredPosition = mouseLocalPos + dragOffset;
    }

    // ─── IEndDragHandler ───────────────────────────────────────────
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        isDragging = false;
        canvasGroup.blocksRaycasts = true;
        cardHand?.OnCardDragEnd(this);
        cardHand?.ShowDropZone(false);

        // 드롭존 위에 놓였으면 카드 사용, 아니면 손패 복귀
        if (dropZone != null && IsOverDropZone(eventData.position))
            cardHand?.OnCardUsed(this);
        else
            ReturnToHand();
    }

    /// <summary>
    /// 손패로 복귀하는 애니메이션 (외부에서도 호출 가능)
    /// </summary>
    public void ReturnToHand()
    {
        transform.SetParent(originalParent, true);
        transform.SetSiblingIndex(originalSiblingIndex);
        card.SetState(CardState.Normal);

        if (returnCoroutine != null) StopCoroutine(returnCoroutine);
        returnCoroutine = StartCoroutine(ReturnAnimation());
    }

    // 드롭존 영역 안에 있는지 체크
    private bool IsOverDropZone(Vector2 screenPos)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(
            dropZone, screenPos, rootCanvas.worldCamera);
    }

    // 손패 원래 위치/회전으로 부드럽게 복귀
    private IEnumerator ReturnAnimation()
    {
        float duration = 0.2f;
        float elapsed = 0f;
        Vector2 startPos = rectTransform.anchoredPosition;
        Quaternion startRot = rectTransform.localRotation;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, originalPosition, t);
            rectTransform.localRotation = Quaternion.Lerp(startRot, originalRotation, t);
            yield return null;
        }
        rectTransform.anchoredPosition = originalPosition;
        rectTransform.localRotation = originalRotation;
    }

    // 마나 부족 시 좌우 흔들림 효과
    private IEnumerator ShakeEffect()
    {
        float elapsed = 0f;
        Vector2 basePos = rectTransform.anchoredPosition;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float decay = 1f - (elapsed / shakeDuration);
            float offsetX = Mathf.Sin(elapsed * 60f) * shakeAmount * decay;
            rectTransform.anchoredPosition = basePos + new Vector2(offsetX, 0f);
            yield return null;
        }
        rectTransform.anchoredPosition = basePos;
    }
}
