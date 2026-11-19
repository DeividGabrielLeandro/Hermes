using Hermes.Api;
using Hermes.Api.Services;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://127.0.0.1:5500", "https://hermes-ia-chat.netlify.app")
              .AllowAnyHeader()
              .AllowAnyMethod());
});
// 1. Lê a connection string do User Secrets
var connectionString = builder.Configuration.GetConnectionString("Hermes")
    ?? throw new InvalidOperationException("Connection string 'Hermes' não encontrada.");

// 2. Cria o "caminho" pro banco uma vez só e registra na API
builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));
builder.Services.AddScoped<UsuarioServices>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<ConversaServices>();
builder.Services.AddScoped<ChaveServices>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseCors();
app.UseHttpsRedirection();

app.MapPost("/api/usuarios", async (CadastroRequest dados, UsuarioServices usuarios) =>
    {
        try
        {
            var senhaHash = hashSenha.CriaHash(dados.Senha);
            var id_usuario = await usuarios.CriarCadastro(senhaHash, dados.Nome, dados.Email);

            return Results.Ok();
        }
        catch (PostgresException ex)
        {
            if (ex.SqlState == "23505")
            {
                return Results.Conflict();
            }
            throw;
        }
    });

app.MapPost("/api/auth/login", async (LoginRequest dados, UsuarioServices usuarios) =>
    {
        try
        {
            bool resultado = await usuarios.RealizarLogin(dados.Senha, dados.Email);

            if (resultado)
            {
                return Results.Ok();
            }
            else
            {
                return Results.Unauthorized();
            }
        }
        catch
        {
            return Results.Problem();
        }
    });
app.MapPost("/api/auth/verificar-codigo", async (VerificarCodigo dados, UsuarioServices usuarios) =>
    {
        try
        {
            if (dados.tipo != "verificar_email" && dados.tipo != "redefinir_senha")
            {
                return Results.BadRequest();
            }
            bool resultado = await usuarios.VerificarCodigo(dados.Email, dados.Codigo, dados.tipo);

            if (resultado)
            {
                return Results.Ok();
            }
            else
            {
                return Results.Unauthorized();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return Results.Problem(ex.Message);
        }

    });
app.MapPost("/api/auth/enviar-codigo", async (VerificarCodigo dados, UsuarioServices usuarios, EmailService email) =>
    {
        try
        {
            var codigo = await usuarios.CriarCodigo(dados.Email, dados.tipo);

            if (codigo != null)
            {
                await email.EnviarCodigo(dados.Email, codigo);
            }

            return Results.Ok();

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return Results.Problem(ex.Message);
        }

    });
app.MapPost("/api/auth/redefinir-senha", async (RedefinirSenha dados, UsuarioServices usuarios) =>
    {
        try
        {
            if (dados.tipo != "verificar_email" && dados.tipo != "redefinir_senha")
            {
                return Results.BadRequest();
            }
            if (string.IsNullOrWhiteSpace(dados.Senha) || dados.Senha.Length < 8)
            {
                return Results.BadRequest();
            }

            bool resultado = await usuarios.VerificarCodigo(dados.Email, dados.Codigo, "redefinir_senha");

            if (resultado)
            {
                await usuarios.RedefinirSenha(dados.Senha, dados.Email);
                return Results.Ok();
            }
            else
            {
                return Results.Unauthorized();
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return Results.Problem(ex.Message);
        }

    });
app.MapPost("/api/auth/chat-bot", async (EnviarMensagem dados, UsuarioServices usuarios, ConversaServices conversa, ChaveServices chave) =>
    {
        try
        {
            var chaveCifrada = await chave.ObterChave(dados.Email);
            if (chaveCifrada == null)
                return Results.BadRequest("Configure sua chave de API antes de conversar.");

            string apiKey = chave.DescriptografarChave(chaveCifrada);

            int? conversaId = dados.idConversa;

            if (conversaId == null)
            {
                conversaId = await conversa.CriarConversa(dados.Mensagem, dados.Email);
            }

            var contexto = await conversa.ListarMensagens(conversaId.Value);
            await conversa.SalvarMensagem(conversaId.Value, "user", dados.Mensagem);
            var resposta = await ApiServices.Mensagens(dados.Mensagem, contexto, apiKey);
            await conversa.SalvarMensagem(conversaId.Value, "model", resposta);

            return Results.Ok(new { resposta, conversaId });

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return Results.Problem(ex.Message);
        }

    });
app.MapPost("/api/conversas", async (EnviarMensagem dados, UsuarioServices usuarios, ConversaServices conversa) =>
    {
        try
        {

            var ListarHistorico = await conversa.ListarHistorico(dados.Email);

            return Results.Ok(ListarHistorico);

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return Results.Problem(ex.Message);
        }

    });
app.MapPost("/api/conversas/abrir", async (EnviarMensagem dados, ConversaServices conversa) =>
{
    try
    {
        int? conversaId = dados.idConversa;
        if (conversaId == null)
        {
            return Results.Unauthorized();
        }
        var Mensagens = await conversa.TodasMensagens(conversaId.Value, dados.Email);

        return Results.Ok(Mensagens);

    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
        return Results.Problem(ex.Message);
    }

});
app.MapPost("/api/chaves", async (ApiKey dados, ChaveServices chave) =>
{
    try
    {
        if (string.IsNullOrWhiteSpace(dados.apiKey) || dados.apiKey.Length < 20)
            return Results.BadRequest("Chave inválida.");

        string finalChave = await chave.SalvarChave(dados.apiKey, dados.email);
        return Results.Ok(new { finalChave });
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
        return Results.Problem(ex.Message);
    }

});
app.MapPost("/api/usuario/nome", async (ApiKey dados, UsuarioServices usuario) =>
{
    try
    {
        var nome = await usuario.BuscarNome(dados.email);
        return Results.Ok(new { nome });

    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
        return Results.Problem(ex.Message);
    }

});

app.Run();