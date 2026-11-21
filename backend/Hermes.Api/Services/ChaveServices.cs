using System.Security.Cryptography;
using Npgsql;
using System.Text;
using System.Text.Unicode;

namespace Hermes.Api.Services
{
    public class ChaveServices
    {
        private readonly NpgsqlDataSource banco;

        private readonly byte[] chaveSecreta;

        public ChaveServices(NpgsqlDataSource banco, IConfiguration config)
        {
            this.banco = banco;

            var texto = config["Criptografia:Chave"];

            if (string.IsNullOrEmpty(texto))
                throw new Exception("Falta Criptografia:Chave nos User Secrets.");

            chaveSecreta = Convert.FromBase64String(texto);

            if (chaveSecreta.Length != 32)
                throw new Exception("A chave de criptografia precisa ter 32 bytes.");
        }
        public string CriptografarChave(string apiKey)
        {
            byte[] texto = Encoding.UTF8.GetBytes(apiKey);      
            byte[] nonce = RandomNumberGenerator.GetBytes(12);   
            byte[] tag = new byte[16];                          
            byte[] cifrado = new byte[texto.Length];            

            using var aes = new AesGcm(chaveSecreta, 16);       
            aes.Encrypt(nonce, texto, cifrado, tag);           

            byte[] tudo = nonce.Concat(tag).Concat(cifrado).ToArray();   

            return Convert.ToBase64String(tudo);                 
        }
        public string DescriptografarChave(string textoCriptografado)
        {
            byte[] tudo = Convert.FromBase64String(textoCriptografado);

            byte[] nonce = tudo[..12];       
            byte[] tag = tudo[12..28];    
            byte[] cifrado = tudo[28..];    
            byte[] texto = new byte[cifrado.Length];

            using var aes = new AesGcm(chaveSecreta, 16);
            aes.Decrypt(nonce, cifrado, tag, texto);

            return Encoding.UTF8.GetString(texto);
        }
        public async Task<string> SalvarChave(string apiKey, string email)
        {
            var chaveCriptografada = CriptografarChave(apiKey);
            string finalChave = apiKey[^4..];
            await using var conn = await banco.OpenConnectionAsync();

            var sql = @"DELETE FROM chaves_api
            WHERE provedor = 'gemini'
              AND usuario_id = (SELECT id FROM usuarios WHERE email = @email);

            INSERT INTO chaves_api (usuario_id, provedor, apelido, chave_criptografada, final_chave)
            SELECT id, 'gemini', 'Gemini', @chave, @final
            FROM usuarios
            WHERE email = @email;";

            await using var comando = new NpgsqlCommand(sql, conn);

            comando.Parameters.AddWithValue("email", email);
            comando.Parameters.AddWithValue("chave", chaveCriptografada);
            comando.Parameters.AddWithValue("final", finalChave);

            await comando.ExecuteNonQueryAsync();


            return finalChave;

        }
        public async Task<string?> ObterChave(string email)
        {
            await using var conn = await banco.OpenConnectionAsync();

            var sql = @"SELECT k.chave_criptografada
                        FROM chaves_api k
                        JOIN usuarios u ON u.id = k.usuario_id
                        WHERE u.email = @email
                        AND k.provedor = 'gemini'";

            await using var comando = new NpgsqlCommand(sql, conn);

            comando.Parameters.AddWithValue("email", email);

            var chave = await comando.ExecuteScalarAsync();

            if (chave == null)
                return null;
                
            return chave.ToString();

        }
    }
}