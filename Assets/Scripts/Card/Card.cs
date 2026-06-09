using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 카드의 시각적 상태
/// </summary>
public enum CardState
{
    Normal,     // 기본 상태
    Hover,      // 마우스 올린 상태
    Selected,   // 선택(드래그) 상태
    Disabled    // 마나 부족 등 비활성 상태
}

/// <summary>
/// 카드 UI 요소에 CardData를 반영하는 컴포넌트
/// </summary>
public class Card : MonoBehaviour
{
    [Header("카드 UI 참조")]
    [SerializeField] private Image cardBackgroundImage;         // 카드 배경 이미지
    [SerializeField] private Image categoryIconImage;           // 좌측 상단 카테고리 아이콘
    [SerializeField] private TextMeshProUGUI manaCostText;      // 우측 상단 마나 코스트
    [SerializeField] private TextMeshProUGUI categoryNameText;  // 카테고리 이름 텍스트
    [SerializeField] private TextMeshProUGUI cardNameText;      // 카드 이름 (큰 폰트)
    [SerializeField] private TextMeshProUGUI powerText;         // 공격/방어력 수치
    [SerializeField] private TextMeshProUGUI difficultyText;    // 난이도 별 표시
    [SerializeField] private GameObject disabledOverlay;        // 마나 부족 시 어둡게 처리하는 오버레이

    [Header("색상 설정")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    private CardData cardData;
    private CardState currentState = CardState.Normal;

    // 외부에서 읽기용 프로퍼티
    public CardData CardData => cardData;
    public CardState CurrentState => currentState;

    /// <summary>
    /// CardData를 받아 모든 UI 요소에 값을 반영한다
    /// </summary>
    public void SetCardData(CardData data)
    {
        cardData = data;
        if (data == null) return;

        // 카테고리 아이콘
        if (categoryIconImage != null)
            categoryIconImage.sprite = data.categoryIcon;

        // 마나 코스트
        if (manaCostText != null)
            manaCostText.text = data.manaCost.ToString();

        // 카드 이름
        if (cardNameText != null)
            cardNameText.text = data.cardName;

        bool isSpecial = data.cardType == CardType.Heal || data.cardType == CardType.TimeExtend;

        // 카테고리·난이도: 특수 카드는 숨김
        if (categoryNameText != null)
        {
            categoryNameText.gameObject.SetActive(!isSpecial);
            if (!isSpecial) categoryNameText.text = data.category.ToString();
        }
        if (difficultyText != null)
        {
            difficultyText.gameObject.SetActive(!isSpecial);
            if (!isSpecial) difficultyText.text = "Lv." + data.difficulty;
        }

        // powerText: 카드 타입별 표시
        if (powerText != null)
        {
            switch (data.cardType)
            {
                case CardType.TimeExtend:
                    powerText.text = $"다음 문제 +{data.attackPower}초";
                    break;
                case CardType.Heal:
                    powerText.text = $"+{data.attackPower}HP";
                    break;
                default:
                    if (data.attackPower > 0)       powerText.text = "피해 " + data.attackPower;
                    else if (data.defensePower > 0) powerText.text = "DEF " + data.defensePower;
                    else                            powerText.text = "";
                    break;
            }
        }

        // 카드 타입별 배경색
        switch (data.cardType)
        {
            case CardType.TimeExtend: normalColor = new Color(0.6f, 0.85f, 1f); break;
            case CardType.Heal:       normalColor = new Color(0.6f, 1f, 0.7f);  break;
            default:                  normalColor = Color.white;                 break;
        }

        // 상태 초기화
        SetState(CardState.Normal);
    }

    /// <summary>
    /// 카드 상태를 변경하고 비주얼을 업데이트한다
    /// </summary>
    public void SetState(CardState state)
    {
        currentState = state;
        UpdateVisuals();
    }

    /// <summary>
    /// 마나 부족 여부에 따라 Disabled/Normal 상태로 전환
    /// </summary>
    public void SetManaInsufficient(bool insufficient)
    {
        SetState(insufficient ? CardState.Disabled : CardState.Normal);
    }

    // 상태에 따른 비주얼 업데이트
    private void UpdateVisuals()
    {
        bool isDisabled = currentState == CardState.Disabled;

        if (cardBackgroundImage != null)
            cardBackgroundImage.color = isDisabled ? disabledColor : normalColor;

        if (disabledOverlay != null)
            disabledOverlay.SetActive(isDisabled);
    }

    // ─── 디버그용 ContextMenu ───────────────────────────────────────
    [ContextMenu("상태: Normal")]
    private void TestNormal() => SetState(CardState.Normal);

    [ContextMenu("상태: Hover")]
    private void TestHover() => SetState(CardState.Hover);

    [ContextMenu("상태: Disabled")]
    private void TestDisabled() => SetState(CardState.Disabled);
}
