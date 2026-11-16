using UnityEngine;

namespace Promotion.Analysis
{
    public class ReportServerConfig : ScriptableObject
    // 리포트 서버 주소와 앱 토큰 -> Resources/ReportServer 에셋에 저장 (저장소에는 올리지 않음)
    // -> 에셋이 없거나 비어 있으면 AI 호출 없이 오프라인 요약만 사용
    {
        public string url;          // 예: https://promotion-report.이름.workers.dev/report
        public string appToken;     // 서버의 APP_TOKEN과 같은 값

        static ReportServerConfig _instance;

        public static ReportServerConfig Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Resources.Load<ReportServerConfig>("ReportServer");
                return _instance;
            }
        }

        public static bool IsReady
        {
            get { return Instance != null && !string.IsNullOrEmpty(Instance.url) && !string.IsNullOrEmpty(Instance.appToken); }
        }
    }
}
