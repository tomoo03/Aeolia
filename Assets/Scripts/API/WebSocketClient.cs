using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using UnityEngine;
using WebSocketSharp;

public class WebSocketClient
{
    private static WebSocketClient _instance;
    private WebSocket _ws;
    private Queue<MessageForPlayback> _messageQueue = new Queue<MessageForPlayback>();
    private object _queueLock = new object();
    private AutoResetEvent _messageReceivedEvent = new AutoResetEvent(false);
    public delegate Task SendMessageCallback(
        WebSocketClient ws,
        AutoResetEvent messageReceivedEvent,
        params object[] args
    );

    private WebSocketClient() {
        // WebSocketサーバーのURLを指定
        _ws = new WebSocket(ApiUrl.AEOLIA_WEB_SOCKET_URL);
        // イベントハンドラの設定
        _ws.OnOpen += OnOpenHandler;
        _ws.OnMessage += OnMessageHandler;
        _ws.OnClose += OnCloseHandler;
    }

    public static WebSocketClient getClient() {
        if (_instance == null) {
            _instance = new WebSocketClient();
        }
        return _instance;
    }

    public int GetQueueCount() {
        return _messageQueue.Count();
    }

    public MessageForPlayback Dequeue() {
        return _messageQueue.Dequeue();
    }

    public void Connect()
    {
        // 接続開始
        _ws.Connect();
    }

    public void Close() {
        _ws.Close();
    }

    public async Task SendMessageAsync(
        string message,
        SendMessageCallback callback,
        params object[] args

    ) {
        await Task.Run(() => SendMessage(message, callback, _messageReceivedEvent, args));
    }

    private async Task SendMessage(
        string message,
        SendMessageCallback callback,
        AutoResetEvent messageReceivedEvent,
        params object[] args
    ) {
        try {
            var dto = new ChatRequestDTO() {
                messages = MessageManager.Instance.MessageList,
                text = message,
            };
            // APIサーバーにメッセージを送信
            _ws.Send(JsonConvert.SerializeObject(dto));

            await callback(_instance, messageReceivedEvent, args);

        } catch (Exception e) {
            Debug.Log(e);
        }
    }

    private void OnOpenHandler(object sender, System.EventArgs e)
    {
        Debug.Log("WebSocket connected!");
    }

    private void OnMessageHandler(object sender, MessageEventArgs e)
    {
        try {
            var response = e.Data;
            Debug.Log("Message from server: " + response);
            if (response.Contains("split")) {
                var responseObject = JsonConvert.DeserializeObject<ChatMessageResponse>(response);

                var message = responseObject.message;
                Debug.Log("ChatGPT:" + message);

                lock (_queueLock) {
                    MessageForPlayback messageForPlayback = CreateMessageForPlayback(message, responseObject.message_index);
                    _messageQueue.Enqueue(messageForPlayback);
                }
            } else {
                var responseObject = JsonConvert.DeserializeObject<ChatFinishMessageResponse>(response);
                Debug.Log("Response: " + responseObject);
                Debug.Log("ChatGPT:" + responseObject.messages);

                MessageForPlayback messageForPlayback = CreateMessageForPlayback(response, responseObject.message_index);
                Debug.Log("queueMessage:" + messageForPlayback.message);

                lock (_queueLock) {
                    _messageQueue.Enqueue(messageForPlayback);
                }
            }
            _messageReceivedEvent.Set();
        } catch (Exception ex) {
            Debug.LogError("Exception in WebSocket OnMessage event: " + ex);
        }
    }

    private void OnCloseHandler(object sender, CloseEventArgs e)
    {
        Debug.Log("WebSocket closed with reason: " + e.Reason);
    }

    private void OnDestroy()
    {
        // オブジェクトが破棄される際に、WebSocket接続を閉じる
        if (_ws != null)
        {
            _ws.Close();
            _ws = null;
        }
    }

    private MessageForPlayback CreateMessageForPlayback(string message, int order) {
        return new MessageForPlayback() {
            message = message,
            order = order
        };
    }

    [System.Serializable]
    public class ChatRequestDTO {
        public List<ChatGPTMessageModel> messages;
        public string text;
    }

    [System.Serializable]
    public class ChatMessageResponse {
        public string status;
        public string message_category;
        public string message;
        public int message_index;
    }

    [System.Serializable]
    public class ChatFinishMessageResponse {
        public string status;
        public string message_category;
        public List<ChatGPTMessageModel> messages;
        public int message_index;
    }

    [System.Serializable]
    public class MessageForPlayback {
        public string message;
        public int order;
    }

    [System.Serializable]
    class ClipPlayOrder {
        public int order;
        public bool isPlayed;
        public AudioClip clip;
    }
}