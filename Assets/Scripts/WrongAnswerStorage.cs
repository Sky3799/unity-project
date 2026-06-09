using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WrongAnswerEntry
{
    public string id;               // 단건 삭제용 고유 ID
    public int    stageIndex;       // 1~5
    public string originWord;       // 테스트된 단어
    public string exampleSentence;  // 예문
    public string correctAnswer;    // 정답
}

[Serializable]
class WrongAnswerList { public List<WrongAnswerEntry> items = new List<WrongAnswerEntry>(); }

public static class WrongAnswerStorage
{
    private static string Key(int stage) => $"WrongAnswers_Stage{stage}";

    public static void Save(WrongAnswerEntry entry)
    {
        if (string.IsNullOrEmpty(entry.id))
            entry.id = Guid.NewGuid().ToString();

        var list = LoadForStage(entry.stageIndex);
        list.Add(entry);
        PlayerPrefs.SetString(Key(entry.stageIndex), JsonUtility.ToJson(new WrongAnswerList { items = list }));
        PlayerPrefs.Save();
    }

    public static void DeleteEntry(string id, int stageIndex)
    {
        var list = LoadForStage(stageIndex);
        list.RemoveAll(e => e.id == id);

        if (list.Count == 0)
            PlayerPrefs.DeleteKey(Key(stageIndex));
        else
            PlayerPrefs.SetString(Key(stageIndex), JsonUtility.ToJson(new WrongAnswerList { items = list }));

        PlayerPrefs.Save();
    }

    public static List<WrongAnswerEntry> LoadForStage(int stage)
    {
        string json = PlayerPrefs.GetString(Key(stage), "");
        if (string.IsNullOrEmpty(json)) return new List<WrongAnswerEntry>();
        return JsonUtility.FromJson<WrongAnswerList>(json)?.items ?? new List<WrongAnswerEntry>();
    }

    public static List<WrongAnswerEntry> LoadAll()
    {
        var all = new List<WrongAnswerEntry>();
        for (int i = 1; i <= 5; i++) all.AddRange(LoadForStage(i));
        return all;
    }

    public static void ClearStage(int stage)
    {
        PlayerPrefs.DeleteKey(Key(stage));
        PlayerPrefs.Save();
    }

    public static void ClearAll()
    {
        for (int i = 1; i <= 5; i++) PlayerPrefs.DeleteKey(Key(i));
        PlayerPrefs.Save();
    }
}

