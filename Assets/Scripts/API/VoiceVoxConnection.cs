using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;

public class VoiceVoxConnection
{
    private static VoiceVoxConnection _instance;
    private const string _voiceVoxUrl = ApiUrl.VOICE_VOX_URL;
    private readonly int _speaker;

    private VoiceVoxConnection(int speaker) {
        _speaker = speaker;
    }

    public static VoiceVoxConnection GetClient(int speaker) {
        if (_instance == null) {
            _instance = new VoiceVoxConnection(speaker);
        }
        return _instance;
    }

    public async UniTask<AudioClip> TranslateTextToAudioClip(string text) {
        var queryJson = await SendAudioQuery(text);
        Debug.Log(queryJson);
        var clip = await GetAudioClip(queryJson);
        return clip;
    }

    private async UniTask<string> SendAudioQuery(string text) {
        var form = new WWWForm();
        // using var request = UnityWebRequest.Post($"{_voiceVoxUrl}/audio_query?text={text}&speaker={_speaker}", form);
        using var request = UnityWebRequest.Post($"{_voiceVoxUrl}/audio_query_from_preset?text={text}&preset_id=2", form);
        await request.SendWebRequest();

        if (
            request.result == UnityWebRequest.Result.ConnectionError ||
            request.result == UnityWebRequest.Result.ProtocolError
        ) {
            Debug.LogError(request.error);
        } else {
            var jsonString = request.downloadHandler.text;
            return jsonString;
        }

        return null;
    }

    private async UniTask<AudioClip> GetAudioClip(string queryJson) {
        var url = $"{_voiceVoxUrl}/synthesis?speaker={_speaker}";
        using var request = new UnityWebRequest(url, "POST");
        // Content-Typeを設定
        request.SetRequestHeader(HttpHeaderKey.CONTENT_TYPE, HttpHeader.APPLICATION_JSON);

        // リクエストボディを設定
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(queryJson);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        // レスポンスの取得に必要な設定を行う
        request.downloadHandler = new DownloadHandlerBuffer();

        await request.SendWebRequest();

        if (
            request.result == UnityWebRequest.Result.ConnectionError ||
            request.result == UnityWebRequest.Result.ProtocolError
        ) {
            Debug.LogError(request.error);
        } else {
            var audioClip = WavUtility.ToAudioClip(request.downloadHandler.data);
            return audioClip;
        }

        return null;
    }
}
