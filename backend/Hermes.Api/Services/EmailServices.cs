using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Hermes.Api.Services;

public class EmailService
{
    private readonly string remetente;
    private readonly string senhaApp;

    public EmailService(IConfiguration config)
    {
        remetente = config["Email:Remetente"]!;
        senhaApp = config["Email:SenhaApp"]!;
    }

    public async Task EnviarCodigo(string destinatario, string codigo)
    {
        var mensagem = new MimeMessage();
        mensagem.From.Add(new MailboxAddress("Hermes", remetente));
        mensagem.To.Add(MailboxAddress.Parse(destinatario));
        mensagem.Subject = "Seu código do Hermes";
        mensagem.Body = new TextPart("plain")
        {
            Text = $"Seu código de verificação é {codigo}. Ele vale por 15 minutos."
        };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(remetente, senhaApp);
        await smtp.SendAsync(mensagem);
        await smtp.DisconnectAsync(true);
    }
}