using element._118.app.API.Context;
using element._118.app.API.Helpers;
using element._118.app.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

            // If the username and password match, return a success message
            return Ok(new { Message = "Login Success!" });
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

        // Change the return type from object to string
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

        private Task<bool> CheckUserNameExistAsync(string username) => _authContext.Users.AnyAsync(x => x.Username == username);
        private Task<bool> CheckEmailExistAsync(string email) => _authContext.Users.AnyAsync(x => x.Email == email);
    }
}
