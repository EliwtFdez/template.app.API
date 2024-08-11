using element._118.app.API.Context;
using element._118.app.API.Helpers;
using element._118.app.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;

namespace element._118.app.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _authContext;

        public UserController(AppDbContext appDbContext)
        {
            _authContext = appDbContext;
        }

        [HttpPost("Authenticate")]
        public async Task<IActionResult> Authenticate([FromBody] User userObj)
        {
            if (userObj == null)
                return BadRequest(new { Message = "Request data is null." });

            // First, retrieve the user based on the username
            var user = await _authContext.Users.FirstOrDefaultAsync(x => x.Username == userObj.Username);

            if (user == null)
                return NotFound(new { Message = "User not found!" });

            // Verify the provided password with the stored hashed password
            if (!PasswordHasher.VerifyPassword(userObj.Password, user.Password))
                return BadRequest(new { Message = "Password is incorrect" });

            user.Token = CreateJwt(user);

            // If the username and password match, return a success message
            return Ok(new {Token = user.Token, Message = "Login Success!" });


        }

        [HttpPost("Register")]
        public async Task<IActionResult> RegisterUser([FromBody] User userObj)
        {
            if (userObj == null)
                return BadRequest();
            if (await CheckUserNameExistAsync(userObj.Username)) return BadRequest(new { Message = "Username Already Exist! " });
            if (await CheckEmailExistAsync(userObj.Email)) return BadRequest(new { Message = "Email Already Exist! " });

            // -- validation password
            var pass = CheckPasswordStrength(userObj.Password);
            if (!string.IsNullOrEmpty(pass))
                return BadRequest(new { Message = pass });

            userObj.Password = PasswordHasher.HashPassword(userObj.Password);
            userObj.Token = "";
            userObj.Role = "User";

            // -- Save the new user
            await _authContext.Users.AddAsync(userObj);
            await _authContext.SaveChangesAsync();

            return Ok(new { Message = "User registered successfully!" });
        }

        private Task<bool> CheckUserNameExistAsync(string username) => _authContext.Users.AnyAsync(x => x.Username == username);
        
        private Task<bool> CheckEmailExistAsync(string email) => _authContext.Users.AnyAsync(x => x.Email == email);
        
        private string CheckPasswordStrength(string password)
        {
            StringBuilder sb = new StringBuilder();

            // Check password length
            if (password.Length < 8)
                sb.Append("Minimum password length should be 8." + Environment.NewLine);

            // Check if password is alphanumeric
            if (!(Regex.IsMatch(password, "[a-z]") && Regex.IsMatch(password, "[A-Z]") && Regex.IsMatch(password, "[0-9]")))
                sb.Append("Password should contain at least one lowercase letter, one uppercase letter, and one digit." + Environment.NewLine);

            // Check if password contains a special character
            if (!Regex.IsMatch(password, @"[!@#$%^&*(),.?""{}|<>/\[\]\\`~;:'\-_=+]"))
                sb.Append("Password should contain at least one special character." + Environment.NewLine);

            return sb.ToString();
        }

        private string CreateJwt(User user)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes("aGVsbG9Ub0NoYXQxMjM0NTY3ODkwaGVsbG9Ub0NoYXQ=");
            var identity = new ClaimsIdentity(new Claim[]
            {
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            });

            var credential = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

            var tokenDescription = new SecurityTokenDescriptor
            {
                Subject = identity,
                Expires = DateTime.Now.AddDays(7),
                SigningCredentials = credential
            };

            var token = jwtTokenHandler.CreateToken(tokenDescription);
            return jwtTokenHandler.WriteToken(token);
        }


    }

}
