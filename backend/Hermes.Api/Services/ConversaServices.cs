using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using System.Security.Cryptography;

namespace Hermes.Api.Services
{
        public record ConversaResumo(int Id, string Titulo);
        public record MensagemResumo(string Papel, string Conteudo);
    public class ConversaServices
    {
        private readonly NpgsqlDataSource banco;

        public ConversaServices(NpgsqlDataSource banco)
        {
            this.banco = banco;
        }
        public async Task<int> CriarConversa(string titulo, string email)
        {
            await using var conn = await banco.OpenConnectionAsync();
            var sqlBusca = @"INSERT INTO conversas (usuario_id, titulo)
                            SELECT id, @titulo
                            FROM usuarios
                            WHERE email = @email
                            RETURNING id";
            await using var comando = new NpgsqlCommand(sqlBusca, conn);
            comando.Parameters.AddWithValue("email", email);
            comando.Parameters.AddWithValue("titulo", titulo);

            int id_gerado = (int)(await comando.ExecuteScalarAsync())!;

            return id_gerado;
        }
        public async Task SalvarMensagem(int conversa_id, string papel, string conteudo)
        {
            await using var conn = await banco.OpenConnectionAsync();
            var sql = @"INSERT INTO mensagens (conversa_id, papel, conteudo)
                        VALUES (@conversa_id, @papel, @conteudo)";
            await using var comando = new NpgsqlCommand(sql, conn);

            comando.Parameters.AddWithValue("conversa_id", conversa_id);
            comando.Parameters.AddWithValue("papel", papel);
            comando.Parameters.AddWithValue("conteudo", conteudo);

            await comando.ExecuteNonQueryAsync();
        }
        public async Task<List<ConversaResumo>> ListarHistorico(string email)
        {
            await using var conn = await banco.OpenConnectionAsync();
            var sql = @"SELECT c.id, c.titulo
                        FROM conversas c
                        JOIN usuarios u ON u.id = c.usuario_id
                        WHERE u.email = @email
                        ORDER BY c.atualizado_em DESC";
            await using var comando = new NpgsqlCommand(sql, conn);

            comando.Parameters.AddWithValue("email", email);

            var lista = new List<ConversaResumo>();
            await using var leitor = await comando.ExecuteReaderAsync();

            while (await leitor.ReadAsync())
            {
                var id = leitor.GetInt32(0);
                var titulo = leitor.GetString(1);
                lista.Add(new ConversaResumo(id, titulo));
            }

            return lista;
        }
        public async Task<List<MensagemResumo>> ListarMensagens(int conversa_id)
        {
            await using var conn = await banco.OpenConnectionAsync();
            var sql = @"SELECT papel, conteudo
                        FROM mensagens
                        WHERE conversa_id = @id
                        ORDER BY criado_em";
            await using var comando = new NpgsqlCommand(sql, conn);

            comando.Parameters.AddWithValue("@id", conversa_id);

            var lista = new List<MensagemResumo>();
            await using var leitor = await comando.ExecuteReaderAsync();

            while (await leitor.ReadAsync())
            {
                var papel = leitor.GetString(0);
                var conteudo = leitor.GetString(1);
                lista.Add(new MensagemResumo(papel, conteudo));
            }

            return lista;
        }
    }
}