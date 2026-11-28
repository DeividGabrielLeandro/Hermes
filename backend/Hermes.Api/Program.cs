using Hermes.Api;
using Hermes.Api.Services;
using Hermes.Api.Provedores;
using Npgsql;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Diagnostics;
using Microsoft.AspNetCore.Http.HttpResults;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://127.0.0.1:5500", "https://hermes-ia-chat.netlify.app", "https://hermes-b1r.pages.dev")
              .AllowAnyHeader()
              .AllowAnyMethod());
});
var connectionString = builder.Configuration.GetConnectionString("Hermes")
    ?? throw new InvalidOperationException("Connection string 'Hermes' não encontrada.");

builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));
builder.Services.AddScoped<UsuarioServices>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<ConversaServices>();
builder.Services.AddScoped<ChaveServices>();
builder.Services.AddScoped<TokenServices>();
builder.Services.AddScoped<OpenAiServices>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opcoes =>
    {
        opcoes.MapInboundClaims = false;
        opcoes.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Chave"]!))
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();

app.MapPost("/api/usuarios", async (CadastroRequest dados, UsuarioServices usuarios) =>
    {
        try
        {
            string email = dados.Email;

            var senhaHash = hashSenha.CriaHash(dados.Senha);
            var id_usuario = await usuarios.CriarCadastro(senhaHash, dados.Nome, email);

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

app.MapPost("/api/auth/login", async (LoginRequest dados, UsuarioServices usuarios, TokenServices tokens) =>
    {
        try
        {
            string email = dados.Email;

            bool resultado = await usuarios.RealizarLogin(dados.Senha, email);

            if (resultado)
            {
                string token = tokens.GerarToken(email);
                return Results.Ok(new { token });
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
            string email = dados.Email;

            if (dados.tipo != "verificar_email" && dados.tipo != "redefinir_senha")
            {
                return Results.BadRequest();
            }
            bool resultado = await usuarios.VerificarCodigo(email, dados.Codigo, dados.tipo);

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
            string Email = dados.Email;

            var codigo = await usuarios.CriarCodigo(Email, dados.tipo);

            if (codigo != null)
            {
                await email.EnviarCodigo(Email, codigo);
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
            string email = dados.Email;

            if (dados.tipo != "verificar_email" && dados.tipo != "redefinir_senha")
            {
                return Results.BadRequest();
            }
            if (string.IsNullOrWhiteSpace(dados.Senha) || dados.Senha.Length < 8)
            {
                return Results.BadRequest();
            }

            bool resultado = await usuarios.VerificarCodigo(email, dados.Codigo, "redefinir_senha");

            if (resultado)
            {
                await usuarios.RedefinirSenha(dados.Senha, email);
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
app.MapPost("/api/auth/chat-bot", async (ClaimsPrincipal usuario, EnviarMensagem dados, UsuarioServices usuarios, ConversaServices conversa, ChaveServices chave, OpenAiServices openIA) =>
    {
        try
        {

            string email = usuario.FindFirst("email")!.Value;

            // 1. Conversa: cria uma nova ou confere se a antiga é do usuário
            int? conversaId = dados.idConversa;
            if (conversaId == null)
            {
                conversaId = await conversa.CriarConversa(dados.Mensagem, email, dados.Modelo, dados.Provedor);
            }
            else if (!await conversa.ConversaPertence(conversaId.Value, email))
            {
                return Results.NotFound("Conversa não encontrada.");
            }

            // 2. Qual modelo e provedor essa conversa usa
            var infoModelo = await conversa.buscarModelo(conversaId.Value);
            if (infoModelo == null)
                return Results.NotFound("Conversa não encontrada.");

            // 3. A chave desse provedor
            var chaveUsuario = await chave.ObterChave(email);
            var chaveCerta = chaveUsuario.FirstOrDefault(c => c.Provedor == infoModelo.Provedor);
            if (chaveCerta == null)
                return Results.BadRequest("Você não tem uma chave salva para esse provedor.");
            string apiKey = chaveCerta.Chave;

            // 4. O endereço do provedor
            var provedor = ListaProvedores.Encontrar(apiKey);
            if (provedor == null)
                return Results.BadRequest("Chave não reconhecida.");

            // 5. Instrução + histórico
            string instrucao = $@"Você é o assistente do Hermes, um site criado por Deivid Gabriel 
            (Hyerofania, marca por trás dos projetos, onde cada um recebe o nome de um deus do olimpo)
            que reúne várias IAs num só lugar. Nesta conversa você está rodando no modelo {infoModelo.Modelo},
            do provedor {infoModelo.Provedor}. Se perguntarem qual IA você é, diga isso.
            Responda SEMPRE em português do Brasil, mesmo que a mensagem tenha palavras em inglês. 
            Nunca troque de idioma no meio da resposta.Adapte seu jeito de falar ao do usuário: 
            se ele escrever de forma informal, com gírias, responda no mesmo tom, de um jeito natural; 
            se ele for mais formal, seja formal também.";

            var contexto = await conversa.ListarMensagens(conversaId.Value);
            contexto.Add(new MensagemResumo("user", dados.Mensagem));
            contexto.Insert(0, new MensagemResumo("system", instrucao));
            await conversa.SalvarMensagem(conversaId.Value, "user", dados.Mensagem);

            // 6. Manda pra IA e salva a resposta
            var resposta = await openIA.Enviar(provedor.Endpoint, apiKey, infoModelo.Modelo, contexto);
            await conversa.SalvarMensagem(conversaId.Value, "model", resposta);

            return Results.Ok(new { resposta, conversaId });

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return Results.Problem(ex.Message);
        }

    }).RequireAuthorization();
app.MapPost("/api/conversas", async (ClaimsPrincipal usuario, EnviarMensagem dados, UsuarioServices usuarios, ConversaServices conversa) =>
    {
        try
        {
            string email = usuario.FindFirst("email")!.Value;

            var ListarHistorico = await conversa.ListarHistorico(email);

            return Results.Ok(ListarHistorico);

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return Results.Problem(ex.Message);
        }

    }).RequireAuthorization();
app.MapPost("/api/conversas/abrir", async (ClaimsPrincipal usuario, EnviarMensagem dados, ConversaServices conversa) =>
{
    try
    {
        string email = usuario.FindFirst("email")!.Value;

        int? conversaId = dados.idConversa;
        if (conversaId == null)
        {
            return Results.Unauthorized();
        }
        var Mensagens = await conversa.TodasMensagens(conversaId.Value, email);

        return Results.Ok(Mensagens);

    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
        return Results.Problem(ex.Message);
    }

}).RequireAuthorization();

app.MapPost("/api/chaves", async (ClaimsPrincipal usuario, ApiKey dados, ChaveServices chave) =>
{
    try
    {
        string email = usuario.FindFirst("email")!.Value;
        var provedor = ListaProvedores.Encontrar(dados.apiKey);
        if (string.IsNullOrWhiteSpace(dados.apiKey) || dados.apiKey.Length < 20 || provedor == null)
            return Results.BadRequest("Chave inválida.");

        string finalChave = await chave.SalvarChave(dados.apiKey, email, provedor.Nome);
        return Results.Ok(new { finalChave });
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
        return Results.Problem(ex.Message);
    }

}).RequireAuthorization();
app.MapPost("/api/usuario/nome", async (ClaimsPrincipal usuario, UsuarioServices usuarios) =>
{
    try
    {
        string email = usuario.FindFirst("email")!.Value;
        var nome = await usuarios.BuscarNome(email);
        return Results.Ok(new { nome });

    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
        return Results.Problem(ex.Message);
    }

}).RequireAuthorization();
app.MapGet("/api/modelos", async (ClaimsPrincipal usuario, UsuarioServices usuarios, ChaveServices chave) =>
{
    try
    {
        List<Provedor> provedores = new List<Provedor>();
        string email = usuario.FindFirst("email")!.Value;
        var chaves = await chave.ObterChave(email);

        foreach (var item in chaves)
        {
            var provedor = ListaProvedores.Encontrar(item.Chave);
            if (provedor == null)
                continue;

            provedores.Add(provedor);
        }


        return Results.Ok(new { provedores });
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
        return Results.Problem(ex.Message);
    }

}).RequireAuthorization();

app.Run();