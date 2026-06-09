using UnityEngine;

public static class CardFrequencyStorage
{
    private const string Prefix = "CF_";
    private const int MaxExtra = 3; // 최대 추가 복사본 수

    public static int GetWrongCount(string cardName)
    {
        return PlayerPrefs.GetInt(Prefix + cardName, 0);
    }

    public static void IncrementWrong(string cardName)
    {
        int cur = GetWrongCount(cardName);
        PlayerPrefs.SetInt(Prefix + cardName, Mathf.Min(cur + 1, MaxExtra));
        PlayerPrefs.Save();
    }

    public static void DecrementWrong(string cardName)
    {
        int cur = GetWrongCount(cardName);
        if (cur > 0)
        {
            PlayerPrefs.SetInt(Prefix + cardName, cur - 1);
            PlayerPrefs.Save();
        }
    }

    // 덱에 넣을 총 복사본 수: 기본 1장 + 오답 횟수만큼 추가
    public static int GetCopiesForDeck(string cardName)
    {
        return 1 + GetWrongCount(cardName);
    }
}
