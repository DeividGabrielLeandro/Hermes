using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hermes.Api.Provedores
{
    public record Provedor(string Nome, string Prefixo, string Endpoint, List<string> Modelos);

    public static class ListaProvedores
    {
        public static readonly List<Provedor> Todos = new()
    {
        new("NVIDIA", "nvapi-", "https://integrate.api.nvidia.com/v1", new()
        {
            "deepseek-ai/deepseek-v4.1-flash",
            "openai/gpt-oss-20b",
            "google/gemma-4-31b-it",
            "z-ai/glm-5.3-flash",
            "minimaxai/minimax-m3.1-flash",
            "stepfun-ai/step-5-preview",
            "meta/muse-glimmer-30b",
            "poolside/laguna-xs-2.1",
            "nvidia/nemotron-3.5-lightning-30b-a3b",
            "nvidia/nemotron-3.5-bolt-e4b",
            "nvidia/nemotron-3.5-super-vl-120b-a12b",
            "nvidia/nemotron-3-ultra-550b-a55b",
            "nvidia/nemotron-3-nano-omni-30b-a3b-reasoning"
        }),
                new("Groq", "gsk_", "https://api.groq.com/openai/v1", new()
        {
            "openai/gpt-oss-120b",
            "openai/gpt-oss-20b",
            "qwen/qwen3.8-27b"
        }),
        new("Gemini", "AIza", "https://generativelanguage.googleapis.com/v1beta/openai", new()
        {
            "gemini-2.5-flash",
            "gemini-2.5-flash-lite"
        }),

        new("OpenRouter", "sk-or-", "https://openrouter.ai/api/v1", new()
        {
            "deepseek/deepseek-chat"
        }),

        new("OpenAI", "sk-", "https://api.openai.com/v1", new()
        {
            "gpt-4o-mini"
        }),
        new("Gemini", "AQ.", "https://generativelanguage.googleapis.com/v1beta/openai", new()
        {
            "gemini-3.5-flash-lite"
        }),
    };

        public static Provedor? Encontrar(string chave)
        {
            return Todos.FirstOrDefault(p => chave.StartsWith(p.Prefixo));
        }
    }
}