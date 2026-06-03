using UnityEngine;

/// <summary>
/// 퀴즈에 출제될 단어 데이터 ScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "NewWord", menuName = "CardGame/Word Data")]
public class WordData : ScriptableObject
{
    [Header("단어 정보")]
    public string word;                         // 단어 (예: "금일")
    public string correctMeaning;               // 정답 뜻 (예: "오늘")
    public string[] wrongMeanings = new string[2]; // 오답 보기 2개 (예: "내일", "어제")

    [Header("예문 & 힌트")]
    public string exampleSentence;              // 예문 (예: "금일 휴업합니다")
    public string hint;                         // 힌트 (선택 사항)
}
