using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Threading.Tasks;

namespace Hermes.Api.Services
{
    public class TokenServices
    {
        private readonly IConfiguration config;

        public TokenServices(IConfiguration config)
        {
            this.config = config;
        }
         public string GerarToken(string email)
        {
            var claim = new Claim("email", email);
            var chave = config["Jwt:Chave"];

            var chaveSeguranca = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave!));
            var credenciais = new SigningCredentials(chaveSeguranca, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: new[] { claim },
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: credenciais);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}