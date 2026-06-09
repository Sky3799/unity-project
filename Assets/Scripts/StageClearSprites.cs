using UnityEngine;

// 스테이지별 클리어 일러스트를 Resources에서 로드
// 스프라이트는 Assets/Resources/StageClear/ 에 위치해야 함
public static class StageClearSprites
{
    private static readonly string[] ResourcePaths =
    {
        "StageClear/Stage1Clear",
        "StageClear/Stage2Clear",
        "StageClear/Stage3Clear",
        "StageClear/Stage4Clear",
        "StageClear/Stage5Clear",
    };

    public static Sprite Get(int stageIndex)
    {
        int i = Mathf.Clamp(stageIndex - 1, 0, ResourcePaths.Length - 1);
        return Resources.Load<Sprite>(ResourcePaths[i]);
    }
}

