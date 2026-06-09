using UnityEngine;

public static class CollectionStorage
{
    public static readonly string[] Monsters = {
        "호랑이", "도깨비", "구미호", "암행어사", "저승사자"
    };
    public static readonly string[] Titles = {
        "한자어의 달인", "순우리말의 달인", "고유어의 달인", "전문어의 달인", "언어의 신"
    };
    // Resources 폴더 기준 경로 (확장자 제외)
    public static readonly string[] MonsterSpritePaths = {
        "숲속의 전투용 호랑이",
        "도깨비수집",
        "",
        "",
        ""
    };

    public static Sprite GetMonsterSprite(int stageIndex)
    {
        int i = stageIndex - 1;
        if (i < 0 || i >= MonsterSpritePaths.Length) return null;
        if (string.IsNullOrEmpty(MonsterSpritePaths[i])) return null;
        return Resources.Load<Sprite>(MonsterSpritePaths[i]);
    }

    private static string ClearedKey(int stage) => $"Collection_Stage{stage}_Cleared";

    public static bool IsCleared(int stageIndex) =>
        PlayerPrefs.GetInt(ClearedKey(stageIndex), 0) == 1;

    public static void MarkCleared(int stageIndex)
    {
        PlayerPrefs.SetInt(ClearedKey(stageIndex), 1);
        PlayerPrefs.Save();
    }

    public static string GetMonster(int stageIndex) =>
        IsCleared(stageIndex) ? Monsters[stageIndex - 1] : "?";

    public static string GetTitle(int stageIndex) =>
        IsCleared(stageIndex) ? Titles[stageIndex - 1] : "?";
}

