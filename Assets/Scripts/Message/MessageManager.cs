using System.Collections.Generic;
using UnityEngine;

public class MessageManager : MonoBehaviour {
  public static MessageManager Instance;

  // コンストラクタをprivate化
  private MessageManager() {}

    /// <summary>
    /// グローバルに会話履歴を保持するリスト
    /// </summary>
    private readonly List<ChatGPTMessageModel> _messageList = new();

    public List<ChatGPTMessageModel> MessageList {
      get { return _messageList; }
    }

    public void AddRangeMessages(List<ChatGPTMessageModel> messages) {
      this._messageList.AddRange(messages);
    }

    public int GetMessageCount() {
      return this._messageList.Count;
    }

    public void ResetMessages() {
      this._messageList.Clear();
    }

    private void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else if (Instance != this) {
            Destroy(gameObject);
        }
    }
}

[System.Serializable]
  public class ChatGPTMessageModel {
      public string role;
      public string content;
  }