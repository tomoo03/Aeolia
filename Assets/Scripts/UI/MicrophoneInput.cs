using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

// マイクから音声入力を取得
public class MicrophoneInput : MonoBehaviour
{
    public AudioSource audioSource;
    public int audioSampleRate = 44100;
    public string microphoneName;
    public bool isRecording = false;
    public string savePath = "Assets/SavedAudio/";
    public string serverURL = $"{ApiUrl.AEOLIA_URL}/voice-chat"; // 送信先のURL

    private AudioClip recordedClip;

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
        isRecording = true;
        audioSource = GetComponent<AudioSource>();
        recordedClip = Microphone.Start(microphoneName, false, 10, audioSampleRate);
        while (!(Microphone.GetPosition(null) > 0)) { }
        audioSource.Play();
    }

    // 録音を停止する
    public void StopRecording() {
        isRecording = false;
        Microphone.End(microphoneName);
        audioSource.Stop();
    }

    // 録音を停止してサーバーに送信する
    public void StopRecordingAndSendWav() {
        StopRecording();
        byte[] wavData = recordedClip.GetWavData();
        Debug.Log("WAV file uploaded successfully");
        Debug.Log(wavData);
        StartCoroutine(SendWavToServer());
    }

    public void SaveWavFile() {
        if (!Directory.Exists(savePath)) {
            Directory.CreateDirectory(savePath);
        }

        string filePath = Path.Combine(savePath, "RecordedAudio_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".wav");
        recordedClip.SaveWav(filePath);

        Debug.Log("Saved WAV file to: " + filePath);
    }

    // 録音したAudioClipをWAV形式のbyte配列に変換し、HTTPリクエストで指定されたURLにPOST
    public IEnumerator SendWavToServer() {
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
            }
        }
    }
}
