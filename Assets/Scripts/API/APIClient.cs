using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;

public class APIClient
{
    /// <summary>
    /// APIClientクラスのインスタンス
    /// </summary>
    public static APIClient Instance { get; private set; }
    private enum DEEPL_LANG {
        JA
    };

    /// <summary>
    /// Aeolia APIのURL
    /// </summary>
    private const string API_URL = ApiUrl.AEOLIA_URL;

    /// <summary>
    /// 会話履歴を保持するリスト
    /// </summary>
    private readonly List<ChatGPTMessageModel> _messageList = new();

    public static APIClient getClient() {
        if (Instance == null) {
            Instance = new APIClient();
        }
        return Instance;
    }

    public async UniTask<AnalyzeResponse> send(string userMessage) {
        var headers = new Dictionary<string, string> {
            {
                HttpHeaderKey.CONTENT_TYPE, HttpHeader.APPLICATION_JSON
            }
        };

        var dto = new AnalyzeRequestDTO() {
            source_lang = DEEPL_LANG.JA.ToString(),
            text = userMessage,
        };

        var json = JsonUtility.ToJson(dto);

        using var request = new UnityWebRequest($"{API_URL}/analyze", "POST") {
            uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
            downloadHandler = new DownloadHandlerBuffer()
        };

        foreach (var header in headers) {
            request.SetRequestHeader(header.Key, header.Value);
        }

        await request.SendWebRequest();

        if (
            request.result == UnityWebRequest.Result.ConnectionError ||
            request.result == UnityWebRequest.Result.ProtocolError
        ) {
            Debug.LogError(request.error);
            throw new Exception();
        } else {
            var responseString = request.downloadHandler.text;
            Debug.Log(responseString);
            var responseObject = JsonUtility.FromJson<AnalyzeResponse>(responseString);
            Debug.Log("APIResponse:" + responseObject);
            return responseObject;
        }
    }

    [System.Serializable]
    public class ChatGPTMessageModel {
        public string role;
        public string content;
    }

    [System.Serializable]
    public class AnalyzeRequestDTO {
        public string source_lang;
        public string text;
    }

    [System.Serializable]
    public class AnalyzeResponse {
        public float neg; // ネガティブ
        public float neu; // ニュートラル
        public float pos; // ポジティブ
        public float compound; // すべての単語のスコアの合計を [-1, +1] の間で正規化した値
    }
}
