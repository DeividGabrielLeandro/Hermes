using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Hermes.Api.Services;

namespace Hermes.Api.Provedores
{

    public class OpenAiServices
    {
        private static readonly HttpClient http = new();

        public async Task<string> Enviar(string endpoint, string chave, string modelo, List<MensagemResumo> mensagens)
        {
            var requisicao = new HttpRequestMessage(HttpMethod.Post, endpoint.TrimEnd('/') + "/chat/completions");
            requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", chave);
            requisicao.Content = JsonContent.Create(new
            {
                model = modelo,
                messages = mensagens.Select(m => new { role = m.Papel == "model" ? "assistant" : m.Papel, content = m.Conteudo })
            });

            var resposta = await http.SendAsync(requisicao);
            string json = await resposta.Content.ReadAsStringAsync();

            if (!resposta.IsSuccessStatusCode)
                throw new Exception($"O provedor recusou ({(int)resposta.StatusCode}): {json}");

            using var doc = JsonDocument.Parse(json);
            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString()!;
        }
    }
}