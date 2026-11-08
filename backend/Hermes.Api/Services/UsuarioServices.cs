using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Hermes.Api;
using Npgsql;

namespace Hermes.Api.Services
{
    public class UsuarioServices
    {
        private readonly NpgsqlDataSource banco;

        public UsuarioServices(NpgsqlDataSource banco)
        {
            this.banco = banco;
        }
        public async Task<int > CriarCadastro(string senha, string nome , string email)
        {
            await using var conn = await banco.OpenConnectionAsync();

            var sql = @"INSERT INTO usuarios (nome, email, senha_hash) 
            VALUES (@nome, @email, @senha_hash) RETURNING id";      

            await using var comando = new NpgsqlCommand(sql, conn);

            comando.Parameters.AddWithValue("senha_hash", senha);
            comando.Parameters.AddWithValue("nome", nome);
            comando.Parameters.AddWithValue("email", email);

            int  id_gerado = (int )(await comando.ExecuteScalarAsync())!;

            return id_gerado;
        }
    }
}