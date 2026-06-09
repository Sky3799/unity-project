using System.Collections.Generic;

// 씬 간 전투 결과 데이터를 전달하는 정적 컨테이너
public static class BattleResultData
{
    public static bool   IsCleared;
    public static bool   IsReviewMode;      // 오답 복습 모드 여부
    public static int    StageIndex;        // 1~5
    public static float  ElapsedTime;
    public static int    CorrectCount;
    public static int    WrongCount;
    public static string MonsterName;       // 해당 스테이지 몬스터 이름

    // 오답 복습 모드에서 BattleManager가 읽을 문제 목록
    public static List<WrongAnswerEntry> ReviewEntries = new List<WrongAnswerEntry>();

    public static float AccuracyPercent =>
        (CorrectCount + WrongCount) > 0
            ? (float)CorrectCount / (CorrectCount + WrongCount) * 100f
            : 0f;
}

