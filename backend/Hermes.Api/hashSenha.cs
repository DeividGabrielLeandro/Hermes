using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BCrypt.Net;

namespace Hermes.Api
{
    public class hashSenha
    {
        public static string CriaHash(string senha)
        {
            // Gera o hash da senha utilizando o algoritmo aprimorado do BCrypt com fator de custo (work factor) igual a 11.
            string senhaHash = BCrypt.Net.BCrypt.HashPassword(senha, 11);
            return senhaHash;
        }

        /// <summary>
        /// Verifica se uma senha em texto plano corresponde a um hash previamente salvo.
        /// </summary>
        /// <param name="senha">A senha em texto plano informada pelo usuário.</param>
        /// <param name="resultado">O objeto que contém o hash salvo no banco de dados.</param>
        /// <returns><c>true</c> se a senha for válida e corresponder ao hash; caso contrário, <c>false</c>.</returns>
        public static bool VerificaHash(string senha, object resultado)
        {
            // Converte o objeto recuperado (ex: retorno de consulta do banco) para string contendo o hash.
            string senhaHashSalva = resultado.ToString()!;

            // Compara a senha digitada com o hash armazenado para validar a autenticação.
            bool senhaValida = BCrypt.Net.BCrypt.EnhancedVerify(senha, senhaHashSalva);

            return senhaValida;
        }
    }
}