using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Servicio2.Utility
{
    public static class TokenFactory
    {
        public static Task<string> GenerateAccessToken(string userId, string correo, string role)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes("CLAVE_SE@#GURA_AUTH_BR&$AND$$ON_%");

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, correo),
            new Claim(ClaimTypes.Role,role)
            // Puedes agregar más claims si deseas
        }),
                Expires = DateTime.UtcNow.AddDays(15), // o más si lo deseas
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return Task.FromResult(tokenHandler.WriteToken(token));
        }
    }
}
