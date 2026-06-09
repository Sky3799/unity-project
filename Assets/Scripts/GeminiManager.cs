using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GeminiManager : MonoBehaviour
{
    // ← Google AI Studio (aistudio.google.com) 에서 발급 후 여기에 입력
    private const string ApiKey = ""; // Google AI Studio에서 발급받은 키 입력
    private const string ApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key=";

    // 주제 + 설명 + 예시 (Gemini 프롬프트용)
    private static readonly string[] StageThemeDescriptions =
    {
        "카테고리: 공문서·행정 한자어\n범위: 공공기관 문서·공문·행정에서 쓰는 한자어 (금일, 명일, 익일, 상기, 미결, 하기, 공란, 당일 계열)\n경계: 법률·경제어(압류, 담보)나 순우리말 날수 표현(사흘, 나흘)은 범위 밖",
        "카테고리: 순우리말 날수·시간 표현\n범위: 날수를 나타내는 순우리말 (사흘, 나흘, 닷새, 엿새, 이레, 여드레, 아흐레, 열흘, 그저께, 글피 계열)\n경계: 한자어 시간 표현(금일, 명일)이나 일반 생활어는 범위 밖",
        "카테고리: 고유어·옛말·사라져가는 생활 어휘\n범위: 일상·생활에서 쓰이는 순우리말 (이부자리, 부뚜막, 나락, 여닫이, 미닫이, 볼모, 나들이, 가뭄 계열)\n경계: 공문서 한자어(금일, 상기)나 법률·경제 전문어는 범위 밖",
        "카테고리: 사회·경제·법률 한자어 전문어\n범위: 법적·경제적 상황에서 쓰는 한자어 (압류, 담보, 배당, 변제, 소급, 공탁, 가압류, 명도, 채굴 계열)\n경계: 행정·공문서 한자어(금일, 상기)나 순우리말 날수 표현은 범위 밖",
        "카테고리: 혼합\n범위: 공문서 한자어·순우리말 날수·생활 고유어·법률경제 한자어를 1~2개씩 균형 있게 선택"
    };

    public static GeminiManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    // 저장된 신규 단어 카드 로드 ───────────────────────────────────────────────
    // difficulty 2 → 1난이도 생성분 로드
    // difficulty 3 → 1난이도 + 2난이도 생성분 모두 로드
    public static List<CardData> LoadGeminiCards(int stage, int difficulty)
    {
        var result = new List<CardData>();
        int maxDiff = difficulty - 1;
        for (int d = 1; d <= maxDiff; d++)
            LoadCardsFromKey($"GeminiNew_{stage}_{d}", result);
        Debug.Log($"[Gemini] {stage}스테이지 신규 문제 {result.Count}장 로드 (난이도{difficulty}용)");
        return result;
    }

    private static void LoadCardsFromKey(string key, List<CardData> result)
    {
        string json = PlayerPrefs.GetString(key, "");
        if (string.IsNullOrEmpty(json)) return;
        try
        {
            var wrapper = JsonUtility.FromJson<GeminiResultList>("{\"items\":" + json + "}");
            foreach (var item in wrapper.items)
            {
                if (string.IsNullOrEmpty(item.word)) continue;

                var word = ScriptableObject.CreateInstance<WordData>();
                word.word            = item.word;
                word.exampleSentence = item.exampleSentence;
                word.correctMeaning  = item.correctMeaning;
                word.wrongMeanings   = item.wrongMeanings ?? new string[2];

                var card = ScriptableObject.CreateInstance<CardData>();
                card.cardName    = item.word;
                card.cardType    = CardType.Quiz;
                card.attackPower = 20;
                card.difficulty  = 2;
                card.wordPool    = new List<WordData> { word };

                result.Add(card);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Gemini] 로드 실패 ({key}): {e.Message}");
        }
    }

    // 난이도 클리어 후 호이출 → 새 단어 5개 생성 ─────────────────
    public void GenerateNewWords(int stage, int difficulty, List<CardData> existingCards)
    {
        string saveKey = $"GeminiNew_{stage}_{difficulty}";
        if (!string.IsNullOrEmpty(PlayerPrefs.GetString(saveKey, "")))
        {
            Debug.Log($"[Gemini] {stage}스테이지 난이도{difficulty} 이미 생성됨. 재요청 생략.");
            return;
        }
        if (ApiKey == "YOUR_GEMINI_API_KEY")
        {
            Debug.LogWarning("[Gemini] API 키 미설정. GeminiManager.cs 9번째 줄에 입력하세요.");
            return;
        }
        StartCoroutine(RequestNewWords(stage, difficulty, existingCards));
    }

    private IEnumerator RequestNewWords(int stage, int difficulty, List<CardData> existingCards)
    {
        string themeDesc = stage >= 1 && stage <= 5 ? StageThemeDescriptions[stage - 1] : "한국어 어휘";

        var existing = new System.Text.StringBuilder();
        foreach (var c in existingCards)
            if (c.wordPool != null && c.wordPool.Count > 0)
                existing.Append(c.wordPool[0].word + ", ");

        string prompt =
            $"한국어 어휘 퀴즈 카드 5개를 JSON으로 만들어 주세요.\n\n" +
            $"[어휘 카테고리]\n{themeDesc}\n\n" +
            $"[조건]\n" +
            $"- 이미 사용된 단어 제외: {existing}\n" +
            $"- 오답 2개는 정답과 의미나 형태가 비슷해서 헷갈릴 것\n" +
            $"- 예문에 빈칸(___)을 포함하고, 문맥상 정답이 자연스럽게 들어갈 것\n" +
            $"- word, correctMeaning, wrongMeanings는 6글자 이내로 간결하게\n\n" +
            $"[출력] 아래 형식의 JSON 배열만 출력 (설명 없이):\n" +
            $"[{{\"word\":\"단어\",\"exampleSentence\":\"___ 포함 예문\",\"correctMeaning\":\"뜻풀이\",\"wrongMeanings\":[\"오답1\",\"오답2\"]}}]";

        string body = "{\"contents\":[{\"parts\":[{\"text\":\"" + EscapeJson(prompt) + "\"}]}]}";
        byte[] bodyBytes = System.Text.Encoding.UTF8.GetBytes(body);

        int[] retryDelays = { 15, 30, 60 }; // 429 발생 시 대기 시간(초)
        for (int attempt = 0; attempt <= retryDelays.Length; attempt++)
        {
            var req = new UnityWebRequest(ApiUrl + ApiKey, "POST");
            req.uploadHandler   = new UploadHandlerRaw(bodyBytes);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                ParseAndSave(req.downloadHandler.text, stage, difficulty);
                yield break;
            }

            // 429 Too Many Requests → 대기 후 재시도
            bool is429 = req.responseCode == 429;
            if (is429 && attempt < retryDelays.Length)
            {
                int wait = retryDelays[attempt];
                Debug.LogWarning($"[Gemini] 요청 제한(429). {wait}초 후 재시도 ({attempt + 1}/{retryDelays.Length})...");
                yield return new WaitForSeconds(wait);
            }
            else
            {
                Debug.LogError($"[Gemini] API 오류 (시도 {attempt + 1}): {req.error} (HTTP {req.responseCode})");
                yield break;
            }
        }
    }

    private void ParseAndSave(string rawJson, int stage, int difficulty)
    {
        int start = rawJson.IndexOf("[{");
        int end   = rawJson.LastIndexOf("}]") + 2;
        if (start < 0 || end < 2)
        {
            Debug.LogWarning("[Gemini] JSON 배열 파싱 실패:\n" + rawJson);
            return;
        }

        string extracted = rawJson.Substring(start, end - start);
        try
        {
            var parsed = JsonUtility.FromJson<GeminiResultList>("{\"items\":" + extracted + "}");
            var filtered = new System.Collections.Generic.List<GeminiQuizItem>();
            foreach (var item in parsed.items)
            {
                if (string.IsNullOrEmpty(item.word)) continue;
                if (IsThemeValid(stage, item.word))
                    filtered.Add(item);
                else
                    Debug.LogWarning($"[Gemini] 주제 불일치 단어 제외됨: '{item.word}' (stage={stage})");
            }

            if (filtered.Count == 0)
            {
                Debug.LogWarning($"[Gemini] 필터링 후 유효한 단어가 없음. 저장 생략.");
                return;
            }

            // 재직렬화
            var sb = new System.Text.StringBuilder("[");
            for (int i = 0; i < filtered.Count; i++)
            {
                var it = filtered[i];
                string wrongs = it.wrongMeanings != null && it.wrongMeanings.Length >= 2
                    ? $"\"{EscapeJson(it.wrongMeanings[0])}\",\"{EscapeJson(it.wrongMeanings[1])}\""
                    : "\"?\",\"?\"";
                sb.Append($"{{\"word\":\"{EscapeJson(it.word)}\",\"exampleSentence\":\"{EscapeJson(it.exampleSentence)}\",\"correctMeaning\":\"{EscapeJson(it.correctMeaning)}\",\"wrongMeanings\":[{wrongs}]}}");
                if (i < filtered.Count - 1) sb.Append(",");
            }
            sb.Append("]");

            string saveKey = $"GeminiNew_{stage}_{difficulty}";
            PlayerPrefs.SetString(saveKey, sb.ToString());
            PlayerPrefs.Save();
            Debug.Log($"[Gemini] {stage}스테이지 난이도{difficulty} 신규 문제 {filtered.Count}개 저장 완료:\n" + sb.ToString());
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Gemini] 저장 실패: " + e.Message);
        }
    }

    // Stage별 주제 부합 여부 간단 검증
    // Gemini가 stage 4 단어(법률·경제)를 stage 1에 넣는 것을 막기 위한 블랙리스트 방어
    private static readonly string[][] StageBlacklist =
    {
        // Stage 1: 공문서 한자어 → 사회·경제·법률 단어 차단
        new[] { "압류", "채굴", "소급", "도축", "명도", "담보", "배당", "공탁", "변제", "가압류",
                "편취", "횡령", "사기", "배임", "탈세", "도용", "착복", "갈취", "약탈", "수뢰",
                "사흘", "나흘", "글피", "이레", "닷새", "엿새", "여드레", "아흐레", "열흘" },
        // Stage 2: 순우리말 시간어 → 한자어·사회어 차단
        new[] { "압류", "채굴", "소급", "금일", "명일", "익일", "상기", "미결", "하기", "당일" },
        // Stage 3: 생활 어휘 → 공문서 한자어·경제 전문어 차단
        new[] { "압류", "채굴", "소급", "금일", "명일", "익일", "상기", "미결" },
        // Stage 4: 사회·경제 → 순우리말 시간어 차단
        new[] { "사흘", "나흘", "글피", "이레", "닷새", "엿새", "여드레", "아흐레", "열흘" },
        // Stage 5: 혼합 → 제한 없음
        System.Array.Empty<string>()
    };

    private static bool IsThemeValid(int stage, string word)
    {
        int idx = stage - 1;
        if (idx < 0 || idx >= StageBlacklist.Length) return true;
        foreach (var banned in StageBlacklist[idx])
            if (word == banned) return false;
        return true;
    }

    private string EscapeJson(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");

    [System.Serializable]
    private class GeminiQuizItem
    {
        public string   word;
        public string   exampleSentence;
        public string   correctMeaning;
        public string[] wrongMeanings;
    }

    [System.Serializable]
    private class GeminiResultList
    {
        public List<GeminiQuizItem> items;
    }
}
