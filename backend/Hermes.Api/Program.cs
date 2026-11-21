using Hermes.Api;
using Hermes.Api.Services;
using Npgsql;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

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
app.MapPost("/api/auth/chat-bot", async (ClaimsPrincipal usuario, EnviarMensagem dados, UsuarioServices usuarios, ConversaServices conversa, ChaveServices chave) =>
    {
        try
        {
            string email = usuario.FindFirst("email")!.Value;

            var chaveCifrada = await chave.ObterChave(email);
            if (chaveCifrada == null)
                return Results.BadRequest("Configure sua chave de API antes de conversar.");

            string apiKey = chave.DescriptografarChave(chaveCifrada);

            int? conversaId = dados.idConversa;

            if (conversaId == null)
            {
                conversaId = await conversa.CriarConversa(dados.Mensagem, email);
            }
            else if (!await conversa.ConversaPertence(conversaId.Value, email))
            {
                return Results.NotFound("Conversa não encontrada.");
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
        if (string.IsNullOrWhiteSpace(dados.apiKey) || dados.apiKey.Length < 20)
            return Results.BadRequest("Chave inválida.");

        string finalChave = await chave.SalvarChave(dados.apiKey, email);
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

app.Run();