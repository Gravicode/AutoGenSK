using AgentChat.OpenAI;
using Azure.AI.OpenAI;
using System;

namespace AgentChat.Example.Share
{
    public static class Constant
    {
        //public static string MLNET101SEARCHTOEKN { get; set; } 
        public static string BingKey{ get; set; } = "aab3c839bcaf42b38eb8315eee01fbb9";
        public static string GPT_35_MODEL_ID { get; set; } = "gpt-3.5-turbo";

        public static string GPT_4_MODEL_ID { get; set; } = "gpt-4-1106-preview";

        public static string? OPENAI_API_KEY { get; set; }

        public static GPT? OpenAIGPT4 { get; set; } = OPENAI_API_KEY != null ? GPT.CreateFromOpenAI(OPENAI_API_KEY, GPT_4_MODEL_ID) : null;

        public static GPT? OpenAIGPT35 { get; set; } = OPENAI_API_KEY != null ? GPT.CreateFromOpenAI(OPENAI_API_KEY, GPT_35_MODEL_ID) : null;


    }
}
