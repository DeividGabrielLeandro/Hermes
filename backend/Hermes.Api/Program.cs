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

app.MapPost("api/auth/login", async (LoginRequest dados, UsuarioServices usuarios) =>
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

app.Run();