using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// 1. Lê a connection string do User Secrets
var connectionString = builder.Configuration.GetConnectionString("Hermes")
    ?? throw new InvalidOperationException("Connection string 'Hermes' não encontrada.");

// 2. Cria o "caminho" pro banco uma vez só e registra na API
builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// 3. Endereço de teste: conta quantos usuários existem
app.MapGet("/api/teste-banco", async (NpgsqlDataSource banco) =>
{
    await using var conexao = await banco.OpenConnectionAsync();
    await using var comando = new NpgsqlCommand("SELECT COUNT(*) FROM usuarios", conexao);
    var total = (long)(await comando.ExecuteScalarAsync())!;
    return Results.Ok(new { usuarios = total });
});

app.Run();