using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Hermes.Api.Services
{
    public class ApiServices
    {
        private static readonly HttpClient client = new HttpClient();


        public static async Task<string> Mensagens(string prompt, List<MensagemResumo> historico)
        {

            string dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ATHENA.db");


            string apiKey = "API-KEY";
            string Modelo = "gemini-3.5-flash-lite";

            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{Modelo}:generateContent?key={apiKey}";

            var requestBody = new
            {
                generationConfig = new
                {
                    maxOutputTokens = 800
                },

                systemInstruction = new
                {
                    parts = new[]
                    {
                        new { text = "responda normalmente" }
                    }
                },

                contents = historico
                .Select(m => new
                {
                    role = m.Papel,
                    parts = new[] { new { text = m.Conteudo } }
                })
                .Append(new
                {
                    role = "user",
                    parts = new[] { new { text = prompt } }
                }),
            };

            string Json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(Json, Encoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage response = await client.PostAsync(url, content);
                string resposta = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using JsonDocument doc = JsonDocument.Parse(resposta);

                    var textResponse = doc.RootElement
                           .GetProperty("candidates")[0]
                           .GetProperty("content")
                           .GetProperty("parts")[0]
                           .GetProperty("text")
                           .GetString();

                    if (string.IsNullOrEmpty(textResponse))
                        return "A IA não retornou uma resposta.";

                    return textResponse;
                }

                else
                {
                    int codigo = (int)response.StatusCode;

                    if (codigo == 503)
                        return "O modelo do Google está com alta demanda agora. Tente de novo em instantes ou escolha outro modelo no Perfil.";

                    if (codigo == 429)
                        return "O limite de uso gratuito foi atingido. Aguarde um pouco e tente de novo.";

                    return $"Erro na requisição: {response.StatusCode}\n{resposta}";
                }
            }
            catch (Exception ex)
            {
                return ($"Erro: {ex.Message}");
            }

        }
    }
}