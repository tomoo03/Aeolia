using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

// マイクから音声入力を取得
public class MicrophoneInput : MonoBehaviour
{
    public AudioSource audioSource;
    public int audioSampleRate = 44100;
    public string microphoneName;
    public bool isRecording = false;
    public string serverURL = $"{ApiUrl.AEOLIA_URL}/voice-chat"; // 送信先のURL
    private string voiceInputText;
    public TMP_InputField inputField;
    public TMP_InputField voiceInputField;

    private AudioClip recordedClip;

    public delegate void ServerResponseCallback(string response);

    private void Start() {
        if (Microphone.devices.Length > 0) {
            microphoneName = Microphone.devices[0];
        } else {
            Debug.LogError("No microphone devices found.");
            return;
        }
    }

    // 録音を開始する
    public void StartRecording() {
        ButtonColor.Instance.ActivateStartButton();
        Debug.Log("hoge");
        isRecording = true;
        audioSource = GetComponent<AudioSource>();
        recordedClip = Microphone.Start(microphoneName, false, 10, audioSampleRate);
        StartCoroutine(WaitForMicrophoneReady());
    }

    private IEnumerator WaitForMicrophoneReady() {
        while (!(Microphone.GetPosition(null) > 0)) {
            yield return null;
        }
        audioSource.Play();
    }

    // 録音を停止する
    private void StopRecording() {
        isRecording = false;
        Microphone.End(microphoneName);
        audioSource.Stop();
    }

    // 録音を停止してサーバーに送信する
    public void StopRecordingAndSendWav() {
        StopRecording();
        ButtonColor.Instance.DeactivateStartButton();
        ButtonColor.Instance.ActivateStopButton();
        byte[] wavData = recordedClip.GetWavData();
        Debug.Log("WAV file uploaded successfully");
        Debug.Log(wavData);
        StartCoroutine(SendWavToServer((responseText) => {
            if (responseText != null) {
                Debug.Log("Server response: " + responseText);
                voiceInputText = responseText;
                ButtonColor.Instance.DeactivateStopButton();
                SendMessage();
            } else {
                Debug.LogError("Error receiving response from server");
                ButtonColor.Instance.DeactivateStopButton();
            }
        }));
    }

    public void SendMessage() {
        // var inputFieldScript = inputFieldObject.GetComponent<CustomInputField>();
        if (inputField != null && voiceInputField != null) {
            voiceInputField.text = voiceInputText;
            inputField.text = voiceInputText;
            inputField.onEndEdit.Invoke(voiceInputText);
        }

    }

    // 録音したAudioClipをWAV形式のbyte配列に変換し、HTTPリクエストで指定されたURLにPOST
    public IEnumerator SendWavToServer(ServerResponseCallback callback) {
        // 録音データをWAVフォーマットに変換
        byte[] wavData = recordedClip.GetWavData();

        // フォームデータの作成
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", wavData, "audio.wav", "audio/wav");

        // 送信用リクエストの作成
        using (UnityWebRequest request = UnityWebRequest.Post(serverURL, form))
        {
            // リクエストを送信
            yield return request.SendWebRequest();

            // エラーチェック
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error sending WAV data: " + request.error);
            }
            else
            {
                Debug.Log("Successfully sent WAV data: " + request.downloadHandler.text);
                var responseObject = JsonUtility.FromJson<WhisperMessageResponse>(request.downloadHandler.text);
                callback?.Invoke(responseObject.text);
            }
        }
    }

    [System.Serializable]
    public class WhisperMessageResponse {
        public string text;
    }
}
