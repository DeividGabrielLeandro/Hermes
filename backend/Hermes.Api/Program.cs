using Hermes.Api;
using Hermes.Api.Services;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://127.0.0.1:5500")
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

app.Run();