using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SecurityAPI.Request;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;


namespace SecurityAPI.Controllers
{
    [ApiController]
    public class AuthController : Controller
    {

        private readonly IConfiguration _configuration;
        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        [HttpGet]
        [Route("api/GenerateToken")]
        [AllowAnonymous]
        public async Task<IActionResult> GenerarToken()
        {
            int tiempoExpiracion = 60;

            var userRoles = new List<string> { "UserApp" };

            var authClaims = new List<Claim>
                {
                    new Claim("ClaimTypes.Username", "user.Username"),
                    new Claim("ClaimTypes.CodigoUsuario","user.CodigoUsuario"),
                    new Claim("ClaimTypes.Email","user.Email"),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                };

            foreach (var userRole in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, userRole));
            }

            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JsonWebTokenKeys:IssuerSigningKey"]));

            var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
            var token = new JwtSecurityToken(
                expires: now.AddMinutes(tiempoExpiracion),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );

            return Ok(new
            {
                accessToken = new JwtSecurityTokenHandler().WriteToken(token),
                refreshToken = GenerateRefreshToken(),
                expiration = token.ValidTo.ToLocalTime(),
                user = "Auth Token",
                Role = userRoles,
                status = "User Login Successfully"
            });
            //}
            return Unauthorized();
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("api/RefreshToken")]
        public async Task<IActionResult> RefreshToken(TokenRequestDto tokenDto)
        {
            if (tokenDto is null)
            {
                return BadRequest("Invalid client request");
            }

            string? accessToken = tokenDto.AccessToken;
            string? refreshToken = tokenDto.RefreshToken;

            var principal = GetPrincipalFromExpiredToken(accessToken);
            if (principal == null)
            {
                return BadRequest("Invalid access token or refresh token");
            }

            var userRoles = new List<string> { "UserApp" };

            var authClaims = new List<Claim>
                {
                    new Claim("ClaimTypes.Username", "user.Username"),
                    new Claim("ClaimTypes.CodigoUsuario","user.CodigoUsuario"),
                    new Claim("ClaimTypes.Email","user.Email"),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                };

            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JsonWebTokenKeys:IssuerSigningKey"]));

            var token = new JwtSecurityToken(
                expires: DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc).AddMinutes(tokenDto.ExpiresIn),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );

            return Ok(new
            {
                accessToken = new JwtSecurityTokenHandler().WriteToken(token),
                refreshToken = GenerateRefreshToken(),
                expiration = token.ValidTo.ToLocalTime(),
                user = "Auth Token",
                Role = userRoles,
                status = "User Login Successfully"
            });
        }

        private JwtSecurityToken CreateToken(List<Claim> authClaims, int tokenValidityInMinutes)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));


            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: DateTime.Now.AddMinutes(tokenValidityInMinutes),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );

            return token;
        }

        private static string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string? token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JsonWebTokenKeys:IssuerSigningKey"])),
                ValidateLifetime = false
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
            if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");

            return principal;

        }

    }
}
