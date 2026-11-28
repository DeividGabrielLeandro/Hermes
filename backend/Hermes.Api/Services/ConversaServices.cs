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
    public record ModeloConversa(string Provedor, string Modelo); 
    public class ConversaServices
    {
        private readonly NpgsqlDataSource banco;
        private readonly ChaveServices cripto;


        public ConversaServices(NpgsqlDataSource banco, ChaveServices cripto)
        {
            this.banco = banco;
            this.cripto = cripto;
        }
        public async Task<int> CriarConversa(string titulo, string email, string modelo, string provedor)
        {
            await using var conn = await banco.OpenConnectionAsync();
            var sqlBusca = @"INSERT INTO conversas (usuario_id, titulo, provedor, modelo)
                 SELECT id, @titulo, @provedor, @modelo
                 FROM usuarios
                 WHERE email = @email
                 RETURNING id";
            await using var comando = new NpgsqlCommand(sqlBusca, conn);
            comando.Parameters.AddWithValue("email", email);
            comando.Parameters.AddWithValue("modelo", modelo);
            comando.Parameters.AddWithValue("provedor", provedor);
            comando.Parameters.AddWithValue("titulo", cripto.CriptografarChave(titulo));

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
            comando.Parameters.AddWithValue("conteudo", cripto.CriptografarChave(conteudo));

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
                var titloDescriptografado = cripto.DescriptografarChave(titulo);
                lista.Add(new ConversaResumo(id, titloDescriptografado));
            }

            return lista;
        }
        public async Task<List<MensagemResumo>> ListarMensagens(int conversa_id)
        {
            await using var conn = await banco.OpenConnectionAsync();
            var sql = @"SELECT papel, conteudo
                        FROM (
                            SELECT papel, conteudo, criado_em
                            FROM mensagens
                            WHERE conversa_id = @id
                            ORDER BY criado_em DESC
                            LIMIT 20
                        ) AS recentes
                        ORDER BY criado_em";
            await using var comando = new NpgsqlCommand(sql, conn);

            comando.Parameters.AddWithValue("@id", conversa_id);

            var lista = new List<MensagemResumo>();
            await using var leitor = await comando.ExecuteReaderAsync();

            while (await leitor.ReadAsync())
            {
                var papel = leitor.GetString(0);
                var conteudo = leitor.GetString(1);
                var conteudoDescriptografado = cripto.DescriptografarChave(conteudo);
                lista.Add(new MensagemResumo(papel, conteudoDescriptografado));
            }

            return lista;
        }
        public async Task<List<MensagemResumo>> TodasMensagens(int id, string email)
        {
            await using var conn = await banco.OpenConnectionAsync();
            var sql = @"SELECT m.papel, m.conteudo
                        FROM mensagens m
                        JOIN conversas c ON c.id = m.conversa_id
                        JOIN usuarios u ON u.id = c.usuario_id
                        WHERE m.conversa_id = @id
                        AND u.email = @email
                        ORDER BY m.criado_em";
            await using var comando = new NpgsqlCommand(sql, conn);

            comando.Parameters.AddWithValue("@id", id);
            comando.Parameters.AddWithValue("@email", email);

            var lista = new List<MensagemResumo>();
            await using var leitor = await comando.ExecuteReaderAsync();

            while (await leitor.ReadAsync())
            {
                var papel = leitor.GetString(0);
                var conteudo = leitor.GetString(1);
                var conteudoDescriptografado = cripto.DescriptografarChave(conteudo);
                lista.Add(new MensagemResumo(papel, conteudoDescriptografado));
            }

            return lista;
        }

        public async Task<bool> ConversaPertence(int id, string email)
        {
            await using var conn = await banco.OpenConnectionAsync();
            var sql = @"SELECT 1
                        FROM conversas c
                        JOIN usuarios u ON u.id = c.usuario_id
                        WHERE c.id = @id AND u.email = @email;";
            await using var comando = new NpgsqlCommand(sql, conn);

            comando.Parameters.AddWithValue("@id", id);
            comando.Parameters.AddWithValue("@email", email);

            var resultado = await comando.ExecuteScalarAsync();

            return resultado != null;
        }

        public async Task<ModeloConversa> buscarModelo(int id)
        {
            await using var conn = await banco.OpenConnectionAsync();
            var sql = @"SELECT provedor, modelo FROM conversas WHERE id = @id;";
            await using var comando = new NpgsqlCommand(sql, conn);

            comando.Parameters.AddWithValue("@id", id);

            await using var leitor = await comando.ExecuteReaderAsync();

            if (await leitor.ReadAsync())
            {
                var provedor = leitor.GetString(0);
                var modelo = leitor.GetString(1);
                return new ModeloConversa(provedor, modelo);
            }
            if (leitor.IsDBNull(0) || leitor.IsDBNull(1))
                return null!;

            return null!;
        }
    }
}