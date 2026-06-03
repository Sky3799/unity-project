using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 손패 시스템 — 카드 정렬, 호버, 드래그, 사용 처리를 담당
/// </summary>
public class CardHand : MonoBehaviour
{
    [Header("손패 설정")]
    [SerializeField] private int maxCards = 10;             // 최대 보유 카드 수
    [SerializeField] private float cardSpacing = 160f;      // 카드 간 간격 (px)
    [SerializeField] private float fanAngle = 8f;           // 부채꼴 최대 회전 각도
    [SerializeField] private float arcHeightOffset = 20f;   // 가장자리 카드가 내려가는 정도
    [SerializeField] private float cardScale = 1.2f;        // 카드 기본 크기 배율
    [SerializeField] private int fanMinCount = 5;           // 부채꼴 회전 시작 카드 수
    [SerializeField] private GameObject cardPrefab;         // 카드 프리팹
    [SerializeField] private Canvas rootCanvas;             // 루트 캔버스
    [SerializeField] private RectTransform dropZone;        // 드롭존 RectTransform

    [Header("카드 등장 애니메이션")]
    [SerializeField] private float flyInStartX = 900f;      // 오른쪽 시작 X 위치
    [SerializeField] private float flyInStartY = -300f;     // 시작 Y 위치
    [SerializeField] private float flyInDuration = 0.35f;   // 날아오는 시간

    [Header("호버 애니메이션")]
    [SerializeField] private float hoverOffsetY = 40f;      // 호버 시 위로 올라오는 거리
    [SerializeField] private float hoverScale = 1.12f;      // 호버 시 확대 비율
    [SerializeField] private float hoverDuration = 0.12f;   // 호버 애니메이션 시간
    [SerializeField] private float pushOffset = 25f;        // 양옆 카드 밀려나는 거리

    private List<CardInteraction> cards = new List<CardInteraction>();
    private CardInteraction hoveredCard = null;
    private CardInteraction draggingCard = null;
    private QuizManager quizManager;
    private CanvasGroup dropZoneCanvasGroup;

    private void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current[UnityEngine.InputSystem.Key.Digit1].wasPressedThisFrame)
            DebugAddCard();
    }

    private void Awake()
    {
        quizManager = FindFirstObjectByType<QuizManager>();

        if (dropZone != null)
        {
            dropZoneCanvasGroup = dropZone.GetComponent<CanvasGroup>();
            if (dropZoneCanvasGroup == null)
                dropZoneCanvasGroup = dropZone.gameObject.AddComponent<CanvasGroup>();
            dropZoneCanvasGroup.alpha = 0f;
            dropZoneCanvasGroup.blocksRaycasts = false;
        }
    }

    private Vector3 NormalScale => Vector3.one * cardScale;

    public void ShowDropZone(bool show)
    {
        if (dropZoneCanvasGroup == null) return;
        dropZoneCanvasGroup.alpha = show ? 1f : 0f;
        dropZoneCanvasGroup.blocksRaycasts = show;
    }

    // ─── 공개 API ───────────────────────────────────────────────────

    /// <summary>
    /// 손패에 카드를 한 장 추가한다
    /// </summary>
    public void AddCard(CardData cardData)
    {
        if (cards.Count >= maxCards)
        {
            Debug.LogWarning("[CardHand] 손패가 가득 찼습니다 (최대 " + maxCards + "장).");
            return;
        }

        var cardObj = Instantiate(cardPrefab, transform);
        var card = cardObj.GetComponent<Card>();
        var interaction = cardObj.GetComponent<CardInteraction>();

        cardObj.transform.localScale = NormalScale;
        interaction.RectTransform.anchoredPosition = new Vector2(flyInStartX, flyInStartY);
        interaction.RectTransform.localRotation = Quaternion.Euler(0f, 0f, -20f);
        card.SetCardData(cardData);
        interaction.Init(this, rootCanvas, dropZone);
        cards.Add(interaction);

        ArrangeCards(cards.Count - 1);
    }

    /// <summary>
    /// 손패에서 카드를 제거하고 파괴한다
    /// </summary>
    public void RemoveCard(CardInteraction interaction)
    {
        if (cards.Remove(interaction))
        {
            Destroy(interaction.gameObject);
            ArrangeCards();
        }
    }

    // ─── CardInteraction 콜백 ───────────────────────────────────────

    /// <summary>호버 시작 — 카드를 위로 올리고 양옆을 밀어낸다</summary>
    public void OnCardHoverEnter(CardInteraction target)
    {
        if (draggingCard != null) return;
        hoveredCard = target;

        // 호버 카드: 위로 + 확대
        Vector2 hoverPos = target.OriginalPosition + new Vector2(0f, hoverOffsetY);
        StartCoroutine(MoveCard(target.RectTransform, hoverPos, Quaternion.identity,
            NormalScale * hoverScale, hoverDuration));

        // 양옆 카드 밀어내기
        PushSiblingCards(target);
    }

    /// <summary>호버 종료 — 모든 카드를 원래 위치로 되돌린다</summary>
    public void OnCardHoverExit(CardInteraction target)
    {
        hoveredCard = null;
        ArrangeCardsImmediate();
    }

    /// <summary>드래그 시작 — 호버 상태 초기화</summary>
    public void OnCardDragStart(CardInteraction target)
    {
        hoveredCard = null;
        draggingCard = target;
        ArrangeCardsImmediate();
    }

    /// <summary>드래그 종료 — 드래그 상태 해제</summary>
    public void OnCardDragEnd(CardInteraction target)
    {
        draggingCard = null;
    }

    /// <summary>카드가 드롭존에 드롭됨 — 카드 즉시 제거 후 퀴즈 시작</summary>
    public void OnCardUsed(CardInteraction interaction)
    {
        var card = interaction.GetComponent<Card>();
        if (card?.CardData == null) return;

        var cardData = card.CardData;
        RemoveCard(interaction); // 문제 가림 방지: 카드 즉시 제거

        if (quizManager != null)
            quizManager.StartQuiz(cardData, (success) => { });
    }

    // ─── 정렬 ───────────────────────────────────────────────────────

    /// <summary>카드들을 부채꼴로 정렬 (애니메이션 포함)</summary>
    /// <param name="newCardIndex">방금 추가된 카드 인덱스 (-1이면 없음)</param>
    public void ArrangeCards(int newCardIndex = -1)
    {
        int count = cards.Count;
        if (count == 0) return;

        for (int i = 0; i < count; i++)
        {
            if (cards[i] == null || cards[i] == hoveredCard) continue;
            var (pos, rot) = GetCardTransform(i, count);
            cards[i].OriginalPosition = pos;
            float duration = (i == newCardIndex) ? flyInDuration : 0.2f;
            StartCoroutine(MoveCard(cards[i].RectTransform, pos, rot, NormalScale, duration));
        }
    }

    // 즉시 정렬 (애니메이션 없이)
    private void ArrangeCardsImmediate()
    {
        int count = cards.Count;
        for (int i = 0; i < count; i++)
        {
            if (cards[i] == null) continue;
            var (pos, rot) = GetCardTransform(i, count);
            cards[i].OriginalPosition = pos;
            cards[i].RectTransform.anchoredPosition = pos;
            cards[i].RectTransform.localRotation = rot;
            cards[i].RectTransform.localScale = NormalScale;
        }
    }

    // i번째 카드의 위치/회전 계산 (부채꼴)
    private (Vector2 pos, Quaternion rot) GetCardTransform(int i, int count)
    {
        float t = count == 1 ? 0.5f : (float)i / (count - 1);
        float x = (i - (count - 1) / 2f) * cardSpacing;
        bool useFan = count >= fanMinCount;
        float angle = useFan ? Mathf.Lerp(fanAngle, -fanAngle, t) : 0f;
        float y = useFan ? -Mathf.Abs(t - 0.5f) * 2f * arcHeightOffset : 0f;

        return (new Vector2(x, y), Quaternion.Euler(0f, 0f, angle));
    }

    // 호버 카드 기준으로 양옆 카드 밀어내기
    private void PushSiblingCards(CardInteraction hoveredCard)
    {
        int hoveredIndex = cards.IndexOf(hoveredCard);
        int count = cards.Count;

        for (int i = 0; i < count; i++)
        {
            if (cards[i] == null || cards[i] == hoveredCard) continue;
            var (basePos, rot) = GetCardTransform(i, count);
            float push = (i < hoveredIndex) ? -pushOffset : pushOffset;
            Vector2 pushedPos = basePos + new Vector2(push, 0f);
            StartCoroutine(MoveCard(cards[i].RectTransform, pushedPos, rot, NormalScale, hoverDuration));
        }
    }

    // RectTransform을 목표 위치/회전/스케일로 부드럽게 이동
    private IEnumerator MoveCard(RectTransform rt, Vector2 targetPos, Quaternion targetRot,
        Vector3 targetScale, float duration)
    {
        float elapsed = 0f;
        Vector2 startPos = rt.anchoredPosition;
        Quaternion startRot = rt.localRotation;
        Vector3 startScale = rt.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            rt.localRotation = Quaternion.Lerp(startRot, targetRot, t);
            rt.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }
        rt.anchoredPosition = targetPos;
        rt.localRotation = targetRot;
        rt.localScale = targetScale;
    }

    // ─── 디버그 ─────────────────────────────────────────────────────

#if UNITY_EDITOR
    [ContextMenu("테스트 카드 추가")]
    private void DebugAddCard()
    {
        var guids = UnityEditor.AssetDatabase.FindAssets("t:CardData", new[] { "Assets/CardData" });
        if (guids.Length == 0) return;
        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[Random.Range(0, guids.Length)]);
        var data = UnityEditor.AssetDatabase.LoadAssetAtPath<CardData>(path);
        AddCard(data);
    }
#else
    private void DebugAddCard() { }
#endif

    [ContextMenu("모든 카드 제거")]
    private void DebugClearCards()
    {
        for (int i = cards.Count - 1; i >= 0; i--)
        {
            if (cards[i] != null) Destroy(cards[i].gameObject);
        }
        cards.Clear();
    }
}
