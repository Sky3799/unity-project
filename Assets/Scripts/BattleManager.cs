using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

[System.Serializable]
public class StageCardPool
{
    public List<CardData> cards = new List<CardData>();
}

public class BattleManager : MonoBehaviour
{
    [Header("HP 설정")]
    [SerializeField] private int playerMaxHp = 100;
    [SerializeField] private int enemyMaxHp  = 100;
    [SerializeField] private int correctDamage = 20;
    [SerializeField] private int wrongDamage   = 15;
    [SerializeField] private int healAmount    = 20;
    [SerializeField] private float extendedTimeLimit = 15f;

    [Header("HP UI")]
    [SerializeField] private Slider playerHpSlider;
    [SerializeField] private Slider enemyHpSlider;
    [SerializeField] private TextMeshProUGUI playerHpText;
    [SerializeField] private TextMeshProUGUI enemyHpText;

    [Header("참조")]
    [SerializeField] private CardHand cardHand;
    [SerializeField] private QuizManager quizManager;
    [SerializeField] private StageTimer stageTimer;
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private TextMeshProUGUI stageNameText;

    [Header("스테이지별 적 오브젝트 (1~5 순서)")]
    [SerializeField] private GameObject[] stageEnemyObjects;

    private Animator tigerAnimator; // 런타임에 현재 스테이지 적으로 설정됨

    [Header("스테이지 배경 (1~5스테이지 순서대로)")]
    [SerializeField] private Texture2D[] stageBackgrounds;

    [Header("데미지 팝업")]
    [SerializeField] private GameObject damagePopupPrefab;
    [SerializeField] private RectTransform popupCanvas;
    // 팝업 기준 위치 (앵커드 좌표)
    [SerializeField] private Vector2 enemyPopupPos  = new Vector2(300f, 50f);
    [SerializeField] private Vector2 playerPopupPos = new Vector2(-300f, 50f);

    [Header("스테이지별 카드 풀 (1~5스테이지 순서대로)")]
    [SerializeField] private StageCardPool[] stageCardPools = new StageCardPool[5];

    private List<CardData> quizCardPool;

    private int playerHp;
    private int enemyHp;
    private float nextTimeLimit = 0f;
    private int _currentCardDamage; // 현재 사용 중인 카드의 데미지
    private CardData _currentCardData; // 현재 퀴즈 카드

    // 이번 스테이지에서 정답 맞춘 카드 목록 (클리어 후 Gemini 전달용)
    private List<CardData> _correctAnsweredCards = new List<CardData>();

    // ─── 결과 추적 ───────────────────────────────────────────────────
    private int correctCount;
    private int wrongCount;

    // ─── 셔플 덱 ────────────────────────────────────────────────────
    [Header("셔플 덱 구성")]
    [SerializeField] private int healCardCount      = 2;
    [SerializeField] private int timeExtendCardCount = 2;

    private List<CardData> _shuffleDeck = new List<CardData>();
    private int _deckIndex = 0;

    private void Awake()
    {
        if (cardHand      == null) cardHand      = FindFirstObjectByType<CardHand>();
        if (quizManager   == null) quizManager   = FindFirstObjectByType<QuizManager>();
        if (playerAnimator == null)
        {
            var playerGO = GameObject.Find("player_stand");
            if (playerGO != null) playerAnimator = playerGO.GetComponent<Animator>();
        }

        QuizPopup.OnWrongAnswerRecorded += HandleWrongAnswer;
    }

    private void OnDestroy()
    {
        QuizPopup.OnWrongAnswerRecorded -= HandleWrongAnswer;
    }

    private void Start()
    {
        playerHp = playerMaxHp;

        // 난이도별 적 HP / 오답 피해 조정
        int difficulty = PlayerPrefs.GetInt("CurrentDifficulty", 1);
        switch (difficulty)
        {
            case 2: enemyHp = 150; wrongDamage = 30; break;
            case 3: enemyHp = 200; wrongDamage = 50; break;
            default: enemyHp = enemyMaxHp; break; // 1단계: Inspector 기본값
        }

        int stage = PlayerPrefs.GetInt("CurrentStage", 1);
        quizCardPool = BuildCardPoolForStage(stage);
        ApplyBackground(stage);
        ApplyEnemy(stage);
        if (stageNameText != null) stageNameText.text = $"스테이지 {stage}";

        UpdateHpUI();
        BuildShuffleDeck();
        StartCoroutine(DrawInitialHand());
    }

    private List<CardData> BuildCardPoolForStage(int stage)
    {
        // 5스테이지는 1~4 전부 합산
        if (stage == 5)
        {
            var merged = new List<CardData>();
            for (int i = 0; i < 4 && i < stageCardPools.Length; i++)
                if (stageCardPools[i] != null) merged.AddRange(stageCardPools[i].cards);
            return merged;
        }

        int idx = Mathf.Clamp(stage - 1, 0, stageCardPools.Length - 1);
        return stageCardPools[idx] != null ? new List<CardData>(stageCardPools[idx].cards) : new List<CardData>();
    }

    // ─── 오답 이벤트 처리 ────────────────────────────────────────────

    private void HandleWrongAnswer(string word, string exampleSentence, string correct)
    {
        var entry = new WrongAnswerEntry
        {
            stageIndex       = PlayerPrefs.GetInt("CurrentStage", 1),
            originWord       = word,
            exampleSentence  = exampleSentence,
            correctAnswer    = correct
        };
        WrongAnswerStorage.Save(entry);
    }

    // ─── 카드 사용 진입점 ────────────────────────────────────────────

    public void UseCard(CardData cardData)
    {
        switch (cardData.cardType)
        {
            case CardType.Quiz:
                float timeLimit = nextTimeLimit > 0f ? nextTimeLimit : 0f;
                nextTimeLimit = 0f;
                _currentCardData   = cardData;
                _currentCardDamage = cardData.attackPower > 0 ? cardData.attackPower : correctDamage;
                quizManager.StartQuiz(cardData, OnQuizResult, timeLimit);
                break;

            case CardType.TimeExtend:
                nextTimeLimit = extendedTimeLimit;
                Debug.Log("[BattleManager] 시간연장 적용 → 다음 문제 15초");
                DrawCard();
                break;

            case CardType.Heal:
                playerHp = Mathf.Min(playerHp + healAmount, playerMaxHp);
                Debug.Log($"[BattleManager] 체력회복 +{healAmount} → 현재 HP: {playerHp}");
                UpdateHpUI();
                SpawnPopup($"+{healAmount}", Color.green, playerPopupPos);
                DrawCard();
                break;
        }
    }

    // ─── 퀴즈 결과 콜백 ─────────────────────────────────────────────

    private void OnQuizResult(bool correct)
    {
        StartCoroutine(OnQuizResultRoutine(correct));
    }

    private System.Collections.IEnumerator OnQuizResultRoutine(bool correct)
    {
        // 플레이어 애니메이션 (팝업 닫힌 직후)
        if (playerAnimator != null)
        {
            if (correct) playerAnimator.SetTrigger("Attack");
            else         playerAnimator.SetTrigger("Hit");
        }

        if (tigerAnimator != null)
        {
            tigerAnimator.ResetTrigger("Attack");
            tigerAnimator.ResetTrigger("Hit");

            if (correct)
            {
                tigerAnimator.SetTrigger("Hit");
            }
            else
            {
                tigerAnimator.SetTrigger("Attack");
            }
        }

        // 애니메이션 재생 시간 대기
        yield return new WaitForSeconds(0.6f);

        if (correct)
        {
            correctCount++;
            enemyHp = Mathf.Max(0, enemyHp - _currentCardDamage);
            SpawnPopup($"-{_currentCardDamage}", Color.red, enemyPopupPos);

            // 오답 횟수 감소 + 정답 카드 기록 (Gemini 심화 문제용)
            if (_currentCardData != null)
            {
                CardFrequencyStorage.DecrementWrong(_currentCardData.cardName);
                if (!_correctAnsweredCards.Contains(_currentCardData))
                    _correctAnsweredCards.Add(_currentCardData);
            }
        }
        else
        {
            wrongCount++;
            playerHp = Mathf.Max(0, playerHp - wrongDamage);
            SpawnPopup($"-{wrongDamage}", Color.red, playerPopupPos);

            // 오답 횟수 증가 → 다음 덱 빌드 시 해당 카드 복사본 추가
            if (_currentCardData != null)
                CardFrequencyStorage.IncrementWrong(_currentCardData.cardName);
        }

        UpdateHpUI();
        CheckBattleEnd();

        if (playerHp > 0 && enemyHp > 0)
            DrawCard();
    }

    // ─── 덱 / 드로우 ────────────────────────────────────────────────

    private void BuildShuffleDeck()
    {
        _shuffleDeck.Clear();

        int difficulty = PlayerPrefs.GetInt("CurrentDifficulty", 1);
        int stage      = PlayerPrefs.GetInt("CurrentStage", 1);

        // 난이도 2·3: Gemini 생성 신규 단어 카드 로드
        List<CardData> geminiCards = new List<CardData>();
        if (difficulty >= 2)
            geminiCards = GeminiManager.LoadGeminiCards(stage, difficulty);

        // 문제카드 구성
        // 난이도 1: 원본만  난이도 2: 원본+심화 혼합  난이도 3: 심화 우선(없으면 원본)
        var pool = new List<CardData>();
        if (difficulty == 3 && geminiCards.Count > 0)
        {
            pool.AddRange(geminiCards);
            // 심화 없는 단어는 원본으로 보충
            foreach (var orig in quizCardPool)
                if (!geminiCards.Exists(g => g.cardName == orig.cardName + "_심화"))
                    pool.Add(orig);
        }
        else
        {
            if (quizCardPool != null) pool.AddRange(quizCardPool);
            if (difficulty >= 2)      pool.AddRange(geminiCards);
        }

        foreach (var card in pool)
        {
            int copies = CardFrequencyStorage.GetCopiesForDeck(card.cardName);
            for (int i = 0; i < copies; i++)
                _shuffleDeck.Add(card);
        }

        // ── 디버그: 덱 구성 로그 ──
        var deckLog = new System.Text.StringBuilder();
        deckLog.AppendLine($"[BattleManager] 덱 빌드 완료 | 스테이지={stage} 난이도={difficulty} 총={_shuffleDeck.Count}장");
        var cardCounts = new System.Collections.Generic.Dictionary<string, int>();
        foreach (var c in _shuffleDeck) {
            if (!cardCounts.ContainsKey(c.cardName)) cardCounts[c.cardName] = 0;
            cardCounts[c.cardName]++;
        }
        foreach (var kv in cardCounts)
            deckLog.AppendLine($"  {kv.Key} × {kv.Value}");
        Debug.Log(deckLog.ToString());

        // 회복 / 시간연장 카드 추가
        for (int i = 0; i < healCardCount;       i++) _shuffleDeck.Add(CreateRuntimeCard(CardType.Heal,       "체력회복"));
        for (int i = 0; i < timeExtendCardCount; i++) _shuffleDeck.Add(CreateRuntimeCard(CardType.TimeExtend, "시간연장"));

        ShuffleDeck();
        _deckIndex = 0;

        Debug.Log($"[BattleManager] 셔플 덱 구성: 총 {_shuffleDeck.Count}장 (문제 {quizCardPool?.Count ?? 0} / 회복 {healCardCount} / 시간연장 {timeExtendCardCount})");
    }

    private void ShuffleDeck()
    {
        for (int i = _shuffleDeck.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (_shuffleDeck[i], _shuffleDeck[j]) = (_shuffleDeck[j], _shuffleDeck[i]);
        }
    }

    private System.Collections.IEnumerator DrawInitialHand()
    {
        for (int i = 0; i < 5; i++)
        {
            DrawCard();
            yield return new WaitForSeconds(0.3f);
        }
    }

    public void DrawCard() => cardHand.AddCard(GetCardFromDeck());

    private CardData GetCardFromDeck()
    {
        if (_shuffleDeck.Count == 0)
        {
            Debug.LogWarning("[BattleManager] 덱이 비어있습니다.");
            return CreateRuntimeCard(CardType.Heal, "체력회복");
        }

        // 덱을 다 뽑으면 다시 셔플
        if (_deckIndex >= _shuffleDeck.Count)
        {
            ShuffleDeck();
            _deckIndex = 0;
            Debug.Log("[BattleManager] 덱 소진 → 다시 셔플");
        }

        return _shuffleDeck[_deckIndex++];
    }

    private CardData CreateRuntimeCard(CardType type, string name)
    {
        var card = ScriptableObject.CreateInstance<CardData>();
        card.cardName = name;
        card.cardType = type;
        // attackPower에 표시값 저장 (Card.cs에서 읽음)
        if (type == CardType.Heal)       card.attackPower = healAmount;
        if (type == CardType.TimeExtend) card.attackPower = (int)(extendedTimeLimit - 10f); // 기본 10초 대비 추가 시간
        return card;
    }

    // ─── 적 교체 ────────────────────────────────────────────────────

    private void ApplyEnemy(int stage)
    {
        int idx = Mathf.Clamp(stage - 1, 0, stageEnemyObjects.Length - 1);
        for (int i = 0; i < stageEnemyObjects.Length; i++)
        {
            if (stageEnemyObjects[i] != null)
                stageEnemyObjects[i].SetActive(i == idx);
        }
        if (stageEnemyObjects.Length > idx && stageEnemyObjects[idx] != null)
            tigerAnimator = stageEnemyObjects[idx].GetComponent<Animator>();
    }

    // ─── 배경 ───────────────────────────────────────────────────────

    private void ApplyBackground(int stage)
    {
        if (stageBackgrounds == null || stageBackgrounds.Length == 0) return;
        int idx = Mathf.Clamp(stage - 1, 0, stageBackgrounds.Length - 1);
        if (stageBackgrounds[idx] == null) return;

        var ground = GameObject.Find("ground");
        if (ground == null) return;
        var renderer = ground.GetComponent<Renderer>();
        if (renderer == null) return;
        renderer.material.mainTexture = stageBackgrounds[idx];
    }

    // ─── 팝업 ───────────────────────────────────────────────────────

    private void SpawnPopup(string text, Color color, Vector2 anchoredPos)
    {
        if (damagePopupPrefab == null || popupCanvas == null) return;
        DamagePopup.Create(damagePopupPrefab, popupCanvas, anchoredPos, text, color);
    }

    // ─── HP UI ──────────────────────────────────────────────────────

    private void UpdateHpUI()
    {
        if (playerHpSlider != null) playerHpSlider.value = (float)playerHp / playerMaxHp;
        if (enemyHpSlider  != null) enemyHpSlider.value  = (float)enemyHp  / enemyMaxHp;
        if (playerHpText   != null) playerHpText.text = $"{playerHp} / {playerMaxHp}";
        if (enemyHpText    != null) enemyHpText.text  = $"{enemyHp}  / {enemyMaxHp}";
    }

    // ─── 승패 판정 ───────────────────────────────────────────────────

    private void CheckBattleEnd()
    {
        if (enemyHp <= 0)
        {
            stageTimer?.Stop();
            int stage = PlayerPrefs.GetInt("CurrentStage", 1);

            int difficulty = PlayerPrefs.GetInt("CurrentDifficulty", 1);
            CollectionStorage.MarkCleared(stage);
            StageSelectManager.OnDifficultyCleared(stage - 1, difficulty);

            // 난이도1·2 클리어 시 Gemini로 새 단어 5개 생성 (다음 난이도용)
            if (difficulty <= 2)
            {
                var gemini = GeminiManager.Instance;
                if (gemini != null && quizCardPool != null)
                    gemini.GenerateNewWords(stage, difficulty, quizCardPool);
            }

            PrepareResultData(cleared: true);

            var overlay = FindFirstObjectByType<StageClearOverlay>();
            if (overlay != null)
                overlay.ShowClear(
                    StageClearSprites.Get(stage),
                    $"{stage}스테이지 클리어!",
                    () => SceneFader.LoadScene("ResultScene"));
            else
                SceneFader.LoadScene("ResultScene");
        }
        else if (playerHp <= 0)
        {
            stageTimer?.Stop();
            PrepareResultData(cleared: false);
            SceneFader.LoadScene("ResultScene");
        }
    }

    private void PrepareResultData(bool cleared)
    {
        int stage = PlayerPrefs.GetInt("CurrentStage", 1);
        BattleResultData.IsCleared    = cleared;
        BattleResultData.StageIndex   = stage;
        BattleResultData.ElapsedTime  = stageTimer != null ? stageTimer.GetElapsed() : 0f;
        BattleResultData.CorrectCount = correctCount;
        BattleResultData.WrongCount   = wrongCount;
        BattleResultData.MonsterName  = CollectionStorage.Monsters[stage - 1];
    }
}

