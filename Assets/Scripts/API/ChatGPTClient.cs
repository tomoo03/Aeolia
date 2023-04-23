using System.Collections.Generic;

public class ChatGPTClient
{
    /// <summary>
    /// openAI ChatGPTのrole
    /// </summary>
    private enum CHAT_GPT_ROLE {
        assistant,
        system,
        user
    };

    [System.Serializable]
    public class ChatGPTMessageModel {
        public string role;
        public string content;
    }

    [System.Serializable]
    public class ChatGPTRequestDTO {
        public string model;
        public List<ChatGPTMessageModel> messages;
    }

    [System.Serializable]
    public class ChatGPTResponse
    {
        public string id;
        public string @object;
        public int created;
        public Choice[] choices;
        public Usage usage;

        [System.Serializable]
        public class Choice
        {
            public int index;
            public ChatGPTMessageModel message;
            public string finish_reason;
        }

        [System.Serializable]
        public class Usage
        {
            public int prompt_tokens;
            public int completion_tokens;
            public int total_tokens;
        }
    }
}