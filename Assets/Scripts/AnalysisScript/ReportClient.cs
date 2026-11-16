using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Promotion.Analysis
{
    public static class ReportClient
    // 분석 결과를 서버로 보내고, 작성되는 글을 조각 단위로 받는 클래스
    {
        const int TimeoutSeconds = 60;
        const string InstallIdKey = "Report.InstallId";

        [Serializable]
        class ReportRequest
        {
            public string installId;
            public string version;
            public bool test;
            public RunAnalysis analysis;
        }

        class StreamHandler : DownloadHandlerScript
        // 받은 바이트를 바로 글자로 바꿔 넘기는 처리기 -> 한글이 조각 경계에서 잘려도 Decoder가 이어 붙임
        {
            readonly Decoder _decoder = Encoding.UTF8.GetDecoder();
            readonly Action<string> _onText;

            public StreamHandler(Action<string> onText) : base(new byte[4096])
            {
                _onText = onText;
            }

            protected override bool ReceiveData(byte[] data, int dataLength)
            {
                var chars = new char[_decoder.GetCharCount(data, 0, dataLength)];
                int count = _decoder.GetChars(data, 0, dataLength, chars, 0);
                if (count > 0)
                    _onText(new string(chars, 0, count));
                return true;
            }
        }

        static string InstallId     // 이 PC를 구분하는 무작위 번호 -> 개인 정보와 연결되지 않음
        {
            get
            {
                string id = PlayerPrefs.GetString(InstallIdKey, "");
                if (id.Length == 0)
                {
                    id = Guid.NewGuid().ToString("N");
                    PlayerPrefs.SetString(InstallIdKey, id);
                    PlayerPrefs.Save();
                }
                return id;
            }
        }

        public static IEnumerator Send(RunAnalysis analysis, bool test, Action<string> onText, Action<bool> done)
        // 서버에 요청을 보내는 코루틴 -> 글이 올 때마다 onText, 끝나면 done(HTTP 요청 성공 여부)
        {
            var config = ReportServerConfig.Instance;
            var payload = new ReportRequest { installId = InstallId, version = Application.version, test = test, analysis = analysis };
            byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));

            using var request = new UnityWebRequest(config.url, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(body),
                downloadHandler = new StreamHandler(onText),
                timeout = TimeoutSeconds
            };
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("X-App-Token", config.appToken);

            yield return request.SendWebRequest();

            bool ok = request.result == UnityWebRequest.Result.Success;
            if (!ok)
                Debug.LogWarning("리포트 서버 요청 실패: " + request.responseCode + " " + request.error);
            done?.Invoke(ok);
        }
    }
}
