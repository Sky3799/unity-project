using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 카드 카테고리 종류
/// </summary>
public enum CardCategory
{
    한자어,
    맞춤법,
    관용어,
    시사용어,
    고전
}

/// <summary>
/// 카드 한 장의 데이터를 담는 ScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "NewCard", menuName = "CardGame/Card Data")]
public class CardData : ScriptableObject
{
    [Header("기본 정보")]
    public string cardName;                     // 카드 이름
    public CardCategory category;               // 카테고리

    [Header("전투 수치")]
    public int attackPower;                     // 공격력 (0이면 공격 카드 아님)
    public int defensePower;                    // 방어력 (0이면 방어 카드 아님)
    [Range(1, 5)]
    public int manaCost = 1;                    // 마나 소비량 (1~5)

    [Header("난이도")]
    [Range(1, 5)]
    public int difficulty = 1;                  // 난이도 별 개수 (1~5)

    [Header("비주얼")]
    public Sprite categoryIcon;                 // 카테고리 아이콘 스프라이트

    [Header("단어 풀")]
    public List<WordData> wordPool;             // 이 카드로 출제될 단어 풀
}
