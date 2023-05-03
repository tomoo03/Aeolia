using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System;
using TMPro;
using UnityEngine;

public class CustomInputField : MonoBehaviour
{
    public TMP_InputField inputField;
    public Animator targetAnimator;
    AudioSource audioSource;
    private object _queueLock = new object();

    public async void OnEndEdit() {
        try {
            Debug.Log("OnEndEdit start");
            targetAnimator.SetBool(AnimatorParameterName.UNITY_CHAN_IS_TALK, false);
            targetAnimator.SetBool(AnimatorParameterName.UNITY_CHAN_IS_THINK, true);
            MessageManager.Instance.ResetClipPlayOrders();

            var text = inputField.text;
            inputField.text = "";
            var webSocketClient = WebSocketClient.getClient();
            audioSource = gameObject.GetComponent<AudioSource>();

            webSocketClient.Connect();
            Debug.Log("Connect OK");
            Debug.Log("SendMessageAsync Start");
            await webSocketClient.SendMessageAsync(text, SendMessageCallback);
            webSocketClient.Close();
            Debug.Log("All OK");
        } catch (Exception e) {
            Debug.Log(e);
        }
    }

    public async Task SendMessageCallback(
        WebSocketClient ws,
        AutoResetEvent messageReceivedEvent,
        params object[] args
    ) {
        // 無限ループ防止用の対応
            var LOOP_LIMIT = 100;
            var loopCount = 0;

            // すべての返却メッセージを処理したか否かを判定するフラグ
            bool isAllMessagesProcessed = false;
            var faceAnimUtility = new FaceAnimUtility();

            /*
             * 返却されたメッセージに対して以下の処理を行う
             * 1. 読み上げ用の音声を生成
             * 2. 感情分析
             * 3. 「1」「2」が完了後、VRMモデルに音声と表情を与えて、返却されたメッセージの順に再生する
             */
            while (!isAllMessagesProcessed && loopCount < LOOP_LIMIT) {
                UnityMainThreadDispatcher.Instance().Enqueue(() => {
                    Debug.Log(loopCount);
                    Debug.Log(isAllMessagesProcessed);
                });
                loopCount++;
                /// <summary>
                /// ManualResetEvent インスタンスがシグナル状態（Set() メソッドが呼ばれた状態）になるまで、
                /// 現在のスレッドをブロック（一時停止）する。
                /// Set() メソッドが呼ばれることで、WaitOne() が解除される。
                /// １つのループ中にSet()が複数呼ばれる可能性があるため、1秒ごとにブロックを解除している。
                ///</summary>
                messageReceivedEvent.WaitOne(1000);
                WebSocketClient.MessageForPlayback messageForPlayback = null;

                // キューからメッセージを取得
                lock (_queueLock) {
                    if (ws.GetQueueCount() > 0) {
                        messageForPlayback = ws.Dequeue();
                    }
                }

                if (messageForPlayback != null) {
                    // メッセージに対しての処理を実装する
                    UnityMainThreadDispatcher.Instance().Enqueue(() => {
                        Debug.Log("received Message: " + messageForPlayback.message);
                    });

                    var receivedMessage = messageForPlayback.message;
                    var targetOrder = messageForPlayback.order;

                    // 最後のメッセージである場合、音声の再生を待ちループを終了させる。
                    if (receivedMessage.Contains("finish")) {
                        var model = JsonConvert.DeserializeObject<WebSocketClient.ChatFinishMessageResponse>(receivedMessage);
                        if (MessageManager.Instance.GetMessageCount() == 0) {
                            MessageManager.Instance.AddRangeMessages(model.messages);
                        } else {
                            MessageManager.Instance.AddRangeMessages(model.messages.Skip(model.messages.Count - 2).ToList());
                        }
                        UnityMainThreadDispatcher.Instance().Enqueue(() => {
                            Debug.Log("Last Message: " + messageForPlayback);
                        });
                        while (true) {
                            var copyList = MessageManager.Instance.GetClipPlayOrdersCopy();
                            var previousClipPlayOrders = copyList.Where(value => value.order < targetOrder);
                            if (previousClipPlayOrders.Count() != targetOrder) {
                                continue;
                            }
                            bool continueWhileLoop = false;
                            foreach (ClipPlayOrder value in copyList) {
                                if (
                                    value.order < targetOrder &&
                                    !value.isPlayed
                                ) {
                                    // 再生終了待ち
                                    await Task.Delay(100);
                                    continueWhileLoop = true;
                                    break;
                                }
                            }
                            if (continueWhileLoop) {
                                continue;
                            }
                            break;
                        }
                        UnityMainThreadDispatcher.Instance().Enqueue(() => {
                            Debug.Log("フラグ更新！");
                        });
                        isAllMessagesProcessed = true;
                        break;
                    }

                    // 2つの非同期処理を定義
                    // VOICEVOXサーバーのAPIで音声合成を実行する。
                    Task<ClipPlayOrder> createAudioClipTask = CreateAudioClip(messageForPlayback);

                    // 感情分析用APIを叩いて、分析値を取得する。
                    Task<APIClient.AnalyzeResponse> analyzeTask = Analyze(messageForPlayback);

                    // 2つのタスクが完了するまで待つ
                    await Task.WhenAll(createAudioClipTask, analyzeTask);

                    // タスクの結果を取得
                    ClipPlayOrder clipPlayOrder = await createAudioClipTask;
                    APIClient.AnalyzeResponse analyzeResponse = await analyzeTask;

                    lock (_queueLock) {
                        MessageManager.Instance.AddClipPlayOrders(clipPlayOrder);
                    }

                    // 順番を待って、音声を再生する。
                    while (true) {
                        var copyList = MessageManager.Instance.GetClipPlayOrdersCopy();
                        var previousClipPlayOrders = copyList.Where(value => value.order < targetOrder);
                        if (previousClipPlayOrders.Count() != targetOrder) {
                            continue;
                        }
                        bool continueWhileLoop = false;
                        foreach (ClipPlayOrder value in copyList) {
                            if (
                                value.order < targetOrder &&
                                !value.isPlayed
                            ) {
                                // 再生終了待ち
                                await Task.Delay(100);
                                continueWhileLoop = true;
                                break;
                            }
                        }
                        if (continueWhileLoop) {
                            continue;
                        }
                        break;
                    }

                    // 再生開始
                    UniTaskCompletionSource<bool> playStarted = new UniTaskCompletionSource<bool>();
                    UnityMainThreadDispatcher.Instance().Enqueue(() => {
                        lock (_queueLock) {
                            var clip = MessageManager.Instance.clipPlayOrders.Find(value => value.order == targetOrder);
                            audioSource.clip = clip.clip;
                            inputField.text += receivedMessage;
                            targetAnimator.SetBool(AnimatorParameterName.UNITY_CHAN_IS_THINK, false);
                            targetAnimator.SetBool(AnimatorParameterName.UNITY_CHAN_IS_TALK, true);
                            // 表情変更
                            faceAnimUtility.ChangeFacialExpression(analyzeResponse);

                            audioSource.Play();
                            playStarted.TrySetResult(true);
                        }
                    });

                    // 再生終了待ち
                    await playStarted.Task;

                    await UniTask.SwitchToMainThread();
                    // audioSource.Play();
                    Debug.Log("再生待つ");
                    await UniTask.WaitWhile(() => audioSource.isPlaying);
                    Debug.Log("再生待った");

                    // ここでメインスレッド以外のスレッドに切り替える
                    await UniTask.SwitchToThreadPool();

                    foreach (ClipPlayOrder value in MessageManager.Instance.clipPlayOrders) {
                        if (
                            value.order == targetOrder
                        ) {
                            lock (_queueLock) {
                                UnityMainThreadDispatcher.Instance().Enqueue(() => {
                                    value.isPlayed = true;
                                    targetAnimator.SetBool(AnimatorParameterName.UNITY_CHAN_IS_TALK, false);
                                    faceAnimUtility.ResetToDefaultFace();
                                });
                            }
                        }
                    }

                    UnityMainThreadDispatcher.Instance().Enqueue(() => {
                        Debug.Log("再生終了！");
                    });
                }
            }
    }

    private async Task<ClipPlayOrder> CreateAudioClip(WebSocketClient.MessageForPlayback messageForPlayback) {
        var queryJson = "";
        var voiceVoxUrl = ApiUrl.VOICE_VOX_URL;

        using (HttpClient client = new HttpClient()) {

            var url = $"{voiceVoxUrl}/audio_query_from_preset?text={messageForPlayback.message}&preset_id=2";
            HttpResponseMessage response = await client.PostAsync(url, null);
            if (response.IsSuccessStatusCode) {
                // レスポンスを文字列として読み取る
                queryJson = await response.Content.ReadAsStringAsync();
            }
        }

        ClipPlayOrder clipPlayOrder = new();

        using (HttpClient client = new HttpClient()) {
            var url = $"{voiceVoxUrl}/synthesis?speaker={VoiceVoxSpeakerId.KASUKABE_TSUMUGI_NORMAL}";

            // StringContent を作成し、Content-Type ヘッダーを設定
            var content = new StringContent(queryJson, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync(url, content);
            if (response.IsSuccessStatusCode) {
                // レスポンスボディを byte[] として読み取る
                byte[] responseBody = await response.Content.ReadAsByteArrayAsync();
                TaskCompletionSource<ClipPlayOrder> taskCompletionSource = new TaskCompletionSource<ClipPlayOrder>();

                UnityMainThreadDispatcher.Instance().Enqueue(() => {
                    Debug.Log("IsSuccess, ToAudioClip Start");
                    var audioClip = WavUtility.ToAudioClip(responseBody);
                    Debug.Log(audioClip);
                    clipPlayOrder = new ClipPlayOrder() {
                        order = messageForPlayback.order,
                        isPlayed = false,
                        clip = audioClip,
                    };
                    taskCompletionSource.SetResult(clipPlayOrder);
                });

                clipPlayOrder = await taskCompletionSource.Task;
            }
        }
        return clipPlayOrder;
    }

    private async Task<APIClient.AnalyzeResponse> Analyze(WebSocketClient.MessageForPlayback messageForPlayback) {
        // 感情分析用APIを叩いて、分析値を取得する。
        APIClient apiClient = APIClient.getClient();
        APIClient.AnalyzeResponse response = new();
        TaskCompletionSource<APIClient.AnalyzeResponse> taskCompletionSource = new TaskCompletionSource<APIClient.AnalyzeResponse>();
        UnityMainThreadDispatcher.Instance().Enqueue(async () => {
            response = await apiClient.send(messageForPlayback.message);
            Debug.Log("感情分析値: " + response.ToString());
            taskCompletionSource.SetResult(response);
        });
        return await taskCompletionSource.Task;
    }
}
