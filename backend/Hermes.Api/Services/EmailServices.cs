using System.Net.Http.Json;

namespace Hermes.Api.Services;

public class EmailService
{
    private static readonly HttpClient client = new HttpClient();
    private readonly string remetente;
    private readonly string chaveApi;

    public EmailService(IConfiguration config)
    {
        remetente = config["Email:Remetente"]!;
        chaveApi = config["Brevo:ChaveApi"]!;
    }

    public async Task EnviarCodigo(string destinatario, string codigo)
    {
        var corpo = new
        {
            sender = new { name = "Hermes", email = remetente },
            to = new[] { new { email = destinatario } },
            subject = "Seu código do Hermes",
            textContent = $"Seu código de verificação é {codigo}. Ele vale por 15 minutos."
        };

        using var requisicao = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
        requisicao.Headers.Add("api-key", chaveApi);
        requisicao.Content = JsonContent.Create(corpo);

        var resposta = await client.SendAsync(requisicao);

        if (!resposta.IsSuccessStatusCode)
        {
            var erro = await resposta.Content.ReadAsStringAsync();
            throw new Exception($"A Brevo recusou o envio: {(int)resposta.StatusCode} {erro}");
        }
    }
}