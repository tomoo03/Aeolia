using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MessageManager : MonoBehaviour {
  public static MessageManager Instance;

  // コンストラクタをprivate化
  private MessageManager() {}

    /// <summary>
    /// グローバルに会話履歴を保持するリスト
    /// </summary>
    private readonly List<ChatGPTMessageModel> _messageList = new();

    private readonly List<ClipPlayOrder> _clipPlayOrders = new();


    public List<ChatGPTMessageModel> MessageList {
      get { return _messageList; }
    }

    public List<ClipPlayOrder> clipPlayOrders {
      get { return _clipPlayOrders; }
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

    public void ResetClipPlayOrders() {
      this._clipPlayOrders.Clear();
    }

    public List<ClipPlayOrder> GetClipPlayOrdersCopy() {
      return _clipPlayOrders.ToList();
    }

    public void AddClipPlayOrders(ClipPlayOrder clipPlayOrder) {
      _clipPlayOrders.Add(clipPlayOrder);
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

[System.Serializable]
public class ClipPlayOrder {
    public int order;
    public bool isPlayed;
    public AudioClip clip;
}