#if UNITY_EDITOR
// ↑ 이 지시문 안의 코드는 에디터에서만 컴파일됨.
//   실제 게임 빌드(exe, apk 등)에는 이 파일의 내용이 아예 포함되지 않음.
//   CSV 임포트는 "개발 중에만" 쓰는 도구이지 게임 플레이 로직이 아니기 때문.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;   // AssetDatabase, EditorUtility 등 에디터 전용 API를 쓰기 위해 필요
using UnityEngine;

// 이 클래스는 인스턴스를 만들 필요가 없는 "도구성 클래스"라서 static으로 선언
public static class DevelopmentCardCSVImporter
{
    // 결과 asset들이 저장될 고정 경로.
    // 상수로 빼둔 이유: 나중에 폴더 구조가 바뀌어도 이 한 줄만 고치면 됨
    private const string OutputFolder = "Assets/Configs/DevelopmentCards";

    // [MenuItem]을 붙이면 유니티 상단 메뉴바에
    // Tools > Splendor > Import Development Cards CSV 항목이 생김.
    // 이 메뉴를 클릭하면 아래 ImportCSV() 메서드가 실행됨.
    [MenuItem("Tools/Splendor/Import Development Cards CSV")]
    public static void ImportCSV()
    {
        // 1. 파일 탐색기 창을 띄워서 사용자가 CSV 파일을 직접 선택하게 함
        //    확장자를 "csv"로 제한해서 엉뚱한 파일 선택 방지
        string path = EditorUtility.OpenFilePanel("Development Card CSV 선택", "", "csv");

        // 사용자가 창을 취소하면 path가 빈 문자열로 돌아옴 → 그냥 종료
        if (string.IsNullOrEmpty(path))
            return;

        // 2. CSV 파일을 한 줄씩 통째로 읽어서 배열로 저장
        //    lines[0]은 헤더(컬럼 이름) 줄이라서 실제 데이터는 lines[1]부터 시작
        var lines = File.ReadAllLines(path);

        // 3. 파싱된 카드들을 레벨별로 묶어서 담을 딕셔너리
        //    key = level(1,2,3), value = 그 레벨에 속한 카드 리스트
        //    나중에 레벨별로 asset을 따로 만들어야 하기 때문에 미리 분류해둠
        var cardsByLevel = new Dictionary<int, List<DevelopmentCardData>>();

        // 파싱 실패한 줄이 몇 개였는지 세어서, 끝나고 사용자에게 알려주기 위함
        int errorCount = 0;

        // 4. 헤더(0번 줄)를 제외하고 1번 줄부터 끝까지 순회
        for (int i = 1; i < lines.Length; i++)
        {
            // 빈 줄(엑셀에서 저장할 때 맨 아래 여백 등)은 그냥 건너뜀
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            // CSV는 쉼표로 값이 구분되어 있으므로 Split(',')로 컬럼 분리
            var cols = lines[i].Split(',');

            // 컬럼 개수가 예상(9개: id,level,bonus,vp,cost*5)보다 적으면
            // 이 줄은 형식이 깨진 것이므로 스킵하고 경고만 남김
            // → 한 줄이 잘못됐다고 임포트 전체가 멈추지 않게 하기 위한 방어 코드
            if (cols.Length < 9)
            {
                Debug.LogWarning($"{i + 1}번째 줄: 열 개수 부족, 건너뜀 → {lines[i]}");
                errorCount++;
                continue;
            }

            // 5. 실제 파싱 시도 (숫자 변환, enum 변환 등에서 예외가 날 수 있어 별도 메서드로 분리)
            if (!TryParseCard(cols, out var card))
            {
                Debug.LogWarning($"{i + 1}번째 줄: 파싱 실패, 건너뜀 → {lines[i]}");
                errorCount++;
                continue;
            }

            // 6. 파싱에 성공한 카드를 해당 레벨 리스트에 추가
            //    아직 그 레벨의 리스트가 없으면 새로 만들어줌
            if (!cardsByLevel.ContainsKey(card.level))
                cardsByLevel[card.level] = new List<DevelopmentCardData>();

            cardsByLevel[card.level].Add(card);
        }

        // 7. 저장할 폴더가 없으면 미리 생성
        //    (처음 임포트하는 경우 Assets/Configs/DevelopmentCards가 없을 수 있음)
        if (!Directory.Exists(OutputFolder))
            Directory.CreateDirectory(OutputFolder);

        // 8. 레벨별로 asset을 만들거나 갱신
        foreach (var kvp in cardsByLevel)
        {
            string assetPath = $"{OutputFolder}/DevelopmentCardDeckConfig_Level{kvp.Key}.asset";

            // 이미 같은 경로에 asset이 있으면 그걸 불러와서 "덮어쓰기"
            // (완전히 새로 만들지 않는 이유: 새로 만들면 GUID가 바뀌어서
            //  이 asset을 참조하던 다른 스크립트의 연결이 끊길 수 있음)
            var deckConfig = AssetDatabase.LoadAssetAtPath<DevelopmentCardDeckConfig>(assetPath);

            // 처음 임포트하는 거라 기존 asset이 없는 경우에만 새로 생성
            if (deckConfig == null)
            {
                deckConfig = ScriptableObject.CreateInstance<DevelopmentCardDeckConfig>();
                AssetDatabase.CreateAsset(deckConfig, assetPath);
            }

            // 9. 실제 데이터 갱신
            deckConfig.level = kvp.Key;
            deckConfig.cards = kvp.Value.ToArray();

            // ScriptableObject는 값을 바꿔도 자동 저장되지 않으므로
            // "이 asset이 변경됐다"고 에디터에 명시적으로 알려줘야 함
            EditorUtility.SetDirty(deckConfig);

            Debug.Log($"Level {kvp.Key}: {kvp.Value.Count}장 반영 완료 → {assetPath}");
        }

        // 10. SetDirty로 표시된 변경사항들을 실제로 디스크에 저장
        AssetDatabase.SaveAssets();

        // 11. 프로젝트 창(Project 뷰)을 새로고침해서
        //     새로 생긴/바뀐 asset 파일이 화면에 바로 보이게 함
        AssetDatabase.Refresh();

        // 12. 최종 결과를 콘솔에 요약해서 알려줌
        Debug.Log(errorCount == 0
            ? "전체 카드 임포트 완료 (에러 없음)"
            : $"임포트 완료, 단 {errorCount}줄에서 오류 발생 (콘솔 확인)");
    }

    // CSV 한 줄(문자열 배열)을 DevelopmentCardData 구조체로 변환 시도하는 메서드
    // 성공하면 true + card에 값 채움, 실패하면 false + card는 기본값
    private static bool TryParseCard(string[] cols, out DevelopmentCardData card)
    {
        card = default; // out 파라미터는 실패 경로에서도 반드시 값을 할당해야 하므로 기본값으로 초기화

        try
        {
            card = new DevelopmentCardData
            {
                // 컬럼 순서: id, level, bonus, victoryPoints, costDiamond, costSapphire, costEmerald, costRuby, costOnyx
                id = int.Parse(cols[0]),
                level = int.Parse(cols[1]),

                // 문자열("Diamond", "Ruby" 등)을 GemType enum 값으로 변환
                // .Trim()으로 앞뒤 공백 제거 (엑셀 저장 시 공백이 섞여 들어가는 경우 방지)
                bonus = (GemType)System.Enum.Parse(typeof(GemType), cols[2].Trim()),

                victoryPoints = int.Parse(cols[3]),
                costDiamond = int.Parse(cols[4]),
                costSapphire = int.Parse(cols[5]),
                costEmerald = int.Parse(cols[6]),
                costRuby = int.Parse(cols[7]),
                costOnyx = int.Parse(cols[8]),
            };
            return true;
        }
        catch
        {
            // int.Parse 실패(숫자가 아닌 값), Enum.Parse 실패(오타난 색상명) 등
            // 어떤 이유로든 파싱이 깨지면 여기로 떨어짐 → false 반환해서 호출부가 skip 처리하게 함
            return false;
        }
    }
}
#endif