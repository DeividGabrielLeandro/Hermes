using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Hermes.Api;
using Npgsql;
using System.Security.Cryptography;

namespace Hermes.Api.Services
{
    public class UsuarioServices
    {
        private readonly NpgsqlDataSource banco;

        public UsuarioServices(NpgsqlDataSource banco)
        {
            this.banco = banco;
        }
        public async Task<int> CriarCadastro(string senha, string nome, string email)
        {
            await using var conn = await banco.OpenConnectionAsync();
            var sqlLimpeza = @"DELETE FROM usuarios
                   WHERE email_verificado = false
                     AND (criado_em < now() - INTERVAL '1 day' OR email = @email)";
            await using var command = new NpgsqlCommand(sqlLimpeza, conn);
            command.Parameters.AddWithValue("email", email);

            await command.ExecuteNonQueryAsync();

            var sql = @"INSERT INTO usuarios (nome, email, senha_hash) 
            VALUES (@nome, @email, @senha_hash) RETURNING id";

            await using var comando = new NpgsqlCommand(sql, conn);

            comando.Parameters.AddWithValue("senha_hash", senha);
            comando.Parameters.AddWithValue("nome", nome);
            comando.Parameters.AddWithValue("email", email);

            int id_gerado = (int)(await comando.ExecuteScalarAsync())!;

            return id_gerado;
        }
        public async Task<bool> RealizarLogin(string senha, string email)
        {
            await using var conn = await banco.OpenConnectionAsync();

            var sql = "SELECT senha_hash FROM usuarios WHERE email = @email AND email_verificado = true";

            await using var comando = new NpgsqlCommand(sql, conn);

            comando.Parameters.AddWithValue("@email", email);

            var resultado = await comando.ExecuteScalarAsync();
            if (resultado == null)
            {
                return false;
            }

            return hashSenha.VerificaHash(senha, (string)resultado);

        }
        public async Task<string> CriarCodigo(string email, string tipo)
        {
            var codigo = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            string codigoHash = hashSenha.CriaHash(codigo);

            await using var conn = await banco.OpenConnectionAsync();

            var sql = @"INSERT INTO codigos_verificacao (usuario_id, tipo, codigo_hash, expira_em)
            SELECT id, @tipo, @codigo_hash, now() + INTERVAL '15 minutes'
            FROM usuarios
            WHERE email = @email";

            await using var comando = new NpgsqlCommand(sql, conn);

            comando.Parameters.AddWithValue("@email", email);
            comando.Parameters.AddWithValue("@codigo_hash", codigoHash);
            comando.Parameters.AddWithValue("@tipo", tipo);

            var linhas = await comando.ExecuteNonQueryAsync();
            if (linhas == 0)
            {
                return null!;
            }

            return codigo;
        }
        public async Task<bool> VerificarCodigo(string email, string codigo, string tipo)
        {
            await using var conn = await banco.OpenConnectionAsync();

           var sql = @"SELECT c.id, c.codigo_hash
            FROM codigos_verificacao c
            JOIN usuarios u ON u.id = c.usuario_id
            WHERE u.email = @email
              AND c.tipo = @tipo
              AND c.usado_em IS NULL
              AND c.expira_em > now()
              AND c.tentativas < 5
            ORDER BY c.criado_em DESC
            LIMIT 1";

            await using var comando = new NpgsqlCommand(sql, conn);

            comando.Parameters.AddWithValue("@email", email);
            comando.Parameters.AddWithValue("@tipo", tipo);

            await using var leitor = await comando.ExecuteReaderAsync();

            if (!await leitor.ReadAsync())
            {
                return false;
            }

            var codigoId = leitor.GetInt32(0);
            var hash = leitor.GetString(1);

            await leitor.CloseAsync();

            bool confirmado = hashSenha.VerificaHash(codigo, hash);
            if (!confirmado)
            {
                var sqlErro = "UPDATE codigos_verificacao SET tentativas = tentativas + 1 WHERE id = @id";
                await using var comando_erro = new NpgsqlCommand(sqlErro, conn);
                comando_erro.Parameters.AddWithValue("id", codigoId);
                await comando_erro.ExecuteNonQueryAsync();
                return false;
            }
            else
            {
                if (tipo == "verificar_email")
                {
                    var sqlAcerto = @"UPDATE codigos_verificacao SET usado_em = now() WHERE id = @id;
                  UPDATE usuarios SET email_verificado = true WHERE email = @email;";
                    await using var comando_acerto = new NpgsqlCommand(sqlAcerto, conn);
                    comando_acerto.Parameters.AddWithValue("id", codigoId);
                    comando_acerto.Parameters.AddWithValue("email", email);
                    await comando_acerto.ExecuteNonQueryAsync();
                }
               
                return true;
            }

        }
        public async Task<bool> RedefinirSenha(string senha, string email)
        {
            var novaSenhaHash = hashSenha.CriaHash(senha);
            await using var conn = await banco.OpenConnectionAsync();

            var sql = @"UPDATE usuarios SET senha_hash = @senha_hash WHERE email = @email;
            UPDATE codigos_verificacao SET usado_em = now()
            WHERE tipo = 'redefinir_senha'
              AND usado_em IS NULL
              AND usuario_id = (SELECT id FROM usuarios WHERE email = @email);";

            await using var comando = new NpgsqlCommand(sql, conn);

            comando.Parameters.AddWithValue("senha_hash", novaSenhaHash);
            comando.Parameters.AddWithValue("email", email);

            await comando.ExecuteNonQueryAsync();
            return true;
        }
    }
}