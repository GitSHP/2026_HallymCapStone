using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Promotion.Analysis
{
    [Serializable]
    public class ArchiveEntry   // 기록소에 저장되는 런 하나
    {
        public string id;           // 파일 이름 (저장 시각으로 만듦)
        public string savedAt;      // 저장 시각 "yyyy-MM-dd HH:mm:ss" -> 문자열 정렬이 곧 시간 순서
        public string version;      // 저장할 때의 게임 버전
        public bool pinned;         // 보관(★) -> 개수 제한으로 지우지 않음
        public string aiStatus;     // ok(AI 리포트 있음) / offline(오프라인 요약)
        public string report;       // 화면에 보였던 AI 리포트 글 (오프라인이면 빈 문자열 -> 볼 때 분석 결과로 다시 만듦)
        public RunAnalysis analysis;
    }

    public static class RunArchive
    // 복기 리포트를 이 PC에 저장하고 불러오는 클래스 (타이틀의 기록소에서 사용)
    // -> 런마다 JSON 파일 하나를 persistentDataPath/reports 폴더에 저장. 인터넷 없이도 다시 볼 수 있고 AI를 다시 부르지 않음
    {
        public const int MaxUnpinned = 30;  // 보관(★)하지 않은 기록은 최근 30개만 남김

        static string Folder
        {
            get { return Path.Combine(Application.persistentDataPath, "reports"); }
        }

        public static ArchiveEntry Create(RunAnalysis analysis) // 분석 결과로 새 기록을 만드는 함수 (아직 저장 전)
        {
            var now = DateTime.Now;
            return new ArchiveEntry
            {
                id = now.ToString("yyyyMMdd_HHmmss_fff"),
                savedAt = now.ToString("yyyy-MM-dd HH:mm:ss"),
                version = Application.version,
                aiStatus = "offline",
                report = "",
                analysis = analysis
            };
        }

        public static void Save(ArchiveEntry entry) // 기록을 파일로 저장하고 오래된 기록을 정리하는 함수
        {
            try
            {
                Directory.CreateDirectory(Folder);
                File.WriteAllText(PathOf(entry.id), JsonUtility.ToJson(entry));
                Trim();
            }
            catch (Exception e)     // 저장에 실패해도 게임은 계속 진행
            {
                Debug.LogWarning("기록 저장 실패: " + e.Message);
            }
        }

        public static List<ArchiveEntry> LoadAll()  // 저장된 기록을 최신순으로 모두 읽는 함수 (깨진 파일은 건너뜀)
        {
            var list = new List<ArchiveEntry>();
            if (!Directory.Exists(Folder))
                return list;

            foreach (var file in Directory.GetFiles(Folder, "*.json"))
            {
                try
                {
                    var entry = JsonUtility.FromJson<ArchiveEntry>(File.ReadAllText(file));
                    if (entry != null && entry.analysis != null)
                        list.Add(entry);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("기록 읽기 실패: " + Path.GetFileName(file) + " " + e.Message);
                }
            }
            list.Sort((a, b) => string.CompareOrdinal(b.savedAt, a.savedAt));
            return list;
        }

        public static void Delete(string id)
        {
            try
            {
                var path = PathOf(id);
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception e)
            {
                Debug.LogWarning("기록 삭제 실패: " + e.Message);
            }
        }

        public static void DeleteAll()  // 기록을 모두 지우는 함수 (타이틀 개발자 도구)
        {
            foreach (var entry in LoadAll())
                Delete(entry.id);
        }

        static void Trim()  // 보관하지 않은 기록이 30개를 넘으면 오래된 것부터 지우는 함수
        {
            int kept = 0;
            foreach (var entry in LoadAll())    // 최신순
            {
                if (entry.pinned)
                    continue;
                kept++;
                if (kept > MaxUnpinned)
                    Delete(entry.id);
            }
        }

        static string PathOf(string id)
        {
            return Path.Combine(Folder, id + ".json");
        }
    }
}
