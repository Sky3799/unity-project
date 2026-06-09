using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// 퀴즈 출제 유형
/// </summary>
public enum QuizType
{
    의미매칭,   // 「단어」의 뜻은? + 뜻 보기 3개
    문장빈칸,   // 빈칸에 들어갈 단어는? + 단어 보기 3개
    예문해석    // 예문에서 「단어」의 의미는? + 뜻 보기 3개
}

/// <summary>
/// 카드 사용 시 퀴즈를 생성하고 QuizPopup에 전달하는 매니저
/// </summary>
public class QuizManager : MonoBehaviour
{
    [SerializeField] private QuizPopup quizPopup;   // 퀴즈 팝업 UI 참조

    // ─── 공개 API ───────────────────────────────────────────────────

    /// <summary>
    /// 카드 사용 시 호출 — 단어 풀에서 랜덤 단어를 뽑아 퀴즈를 시작한다
    /// </summary>
    /// <param name="cardData">사용된 카드 데이터</param>
    /// <param name="onComplete">정답 여부(bool)를 전달받는 콜백</param>
    public void StartQuiz(CardData cardData, Action<bool> onComplete, float timeLimitOverride = 0f)
    {
        if (quizPopup == null)
        {
            Debug.LogError("[QuizManager] QuizPopup 참조가 없습니다.");
            onComplete?.Invoke(false);
            return;
        }

        // 단어 풀이 비어 있으면 자동 성공
        if (cardData.wordPool == null || cardData.wordPool.Count == 0)
        {
            Debug.LogWarning($"[QuizManager] '{cardData.cardName}' 단어 풀이 비어있어 자동 성공 처리.");
            onComplete?.Invoke(true);
            return;
        }

        // 랜덤 단어 선택
        WordData word = cardData.wordPool[Random.Range(0, cardData.wordPool.Count)];

        // 출제 유형 결정 (예문 없으면 의미매칭만)
        QuizType type = PickQuizType(word);

        string question = BuildQuestion(type, word);
        string correct = GetCorrectAnswer(type, word);
        string[] answers = BuildShuffledAnswers(type, word, correct);

        quizPopup.ShowQuiz(cardData.cardName, question, answers, correct, word.word, word.exampleSentence, onComplete, timeLimitOverride);
    }

    // ─── 퀴즈 생성 내부 로직 ────────────────────────────────────────

    private QuizType PickQuizType(WordData word)
    {
        return QuizType.예문해석;
    }

    // 출제 유형별 문제 텍스트 생성
    private string BuildQuestion(QuizType type, WordData word)
    {
        switch (type)
        {
            case QuizType.의미매칭:
                return $"「{word.word}」의 뜻은?";

            case QuizType.문장빈칸:
                // 예문의 단어를 ___로 치환
                string blanked = word.exampleSentence.Replace(word.word, "___");
                return $"다음 빈칸에 알맞은 단어는?\n\n\"{blanked}\"";

            case QuizType.예문해석:
                return $"다음 예문에서 「{word.word}」의 의미는?\n\n\"{word.exampleSentence}\"";

            default:
                return $"「{word.word}」의 뜻은?";
        }
    }

    // 출제 유형에 따른 정답 문자열 반환
    private string GetCorrectAnswer(QuizType type, WordData word)
    {
        // 빈칸 문제는 단어 자체가 정답, 나머지는 뜻이 정답
        return type == QuizType.문장빈칸 ? word.word : word.correctMeaning;
    }

    // 정답 + 오답 2개를 합쳐 셔플한 보기 3개 반환
    private string[] BuildShuffledAnswers(QuizType type, WordData word, string correct)
    {
        var list = new List<string> { correct };

        // 빈칸 문제는 오답도 단어로 구성 (wrongMeanings를 단어 후보로 재사용)
        // 실제 프로젝트에서는 WordData에 wrongWords 필드 추가를 권장
        if (word.wrongMeanings != null)
        {
            foreach (var w in word.wrongMeanings)
            {
                if (!string.IsNullOrWhiteSpace(w))
                    list.Add(w);
            }
        }

        // 보기가 3개 미만이면 빈 자리 채우기
        while (list.Count < 3)
            list.Add("(보기 없음)");

        // Fisher-Yates 셔플
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }

        return list.ToArray();
    }

    // ─── 디버그 ─────────────────────────────────────────────────────

    [ContextMenu("테스트 퀴즈 실행 (금일)")]
    private void DebugTestQuiz()
    {
        var word = ScriptableObject.CreateInstance<WordData>();
        word.word = "금일";
        word.correctMeaning = "오늘";
        word.wrongMeanings = new[] { "내일", "어제" };
        word.exampleSentence = "금일 휴업합니다.";

        var card = ScriptableObject.CreateInstance<CardData>();
        card.cardName = "한자어 카드";
        card.wordPool = new List<WordData> { word };

        StartQuiz(card, (success) =>
            Debug.Log($"[QuizManager] 테스트 결과: {(success ? "정답 ✓" : "오답 ✗")}"));
    }
}
