using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AICareerCoach.BLL.DTOs;
using AICareerCoach.BLL.DTOs.Auth;
using AICareerCoach.BLL.Interfaces;
using AICareerCoach.BLL.Services;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using AICareerCoach.DAL.Models;
using MailKit.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MimeKit;

namespace AICareerCoach.BLL.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _config;
        private readonly AICareerCoachDbContext _context;

        public AuthService(
            UserManager<User> userManager,
            IConfiguration config,
            RoleManager<IdentityRole> roleManager,
            AICareerCoachDbContext context)
        {
            _userManager = userManager;
            _config = config;
            _roleManager = roleManager;
            _context = context;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
        {
            var userExists = await _userManager.FindByEmailAsync(dto.Email);
            if (userExists != null)
                throw new Exception("User with this email already exists.");

            var user = new User
            {
                UserName = dto.Email,
                Email = dto.Email,
                FullName = dto.FullName
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                var passwordErrors = string.Join(" | ", result.Errors.Select(e => e.Description));
                throw new Exception($"Error occurred while registering the user: {passwordErrors}");
            }

            await _userManager.AddToRoleAsync(user, "User");
            var roles = new List<string> { "User" };

            return new AuthResponseDto
            {
                Token = await GenerateJwtTokenAsync(user),
                Email = user.Email,
                FullName = user.FullName,
                Roles = roles
            };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                throw new Exception("Email or password is incorrect.");

            var result = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!result)
                throw new Exception("Email or password is incorrect.");

            var roles = (await _userManager.GetRolesAsync(user)).ToList();
            var refreshtoken = GenerateRefreshToken();

            var newrefreshtoken = new RefreshToken();
            newrefreshtoken.Userid = user.Id;
            newrefreshtoken.Token = refreshtoken;
            newrefreshtoken.IsRevoked = false;
            newrefreshtoken.Expirydate = DateTime.UtcNow.AddDays(7);
            _context.RefreshTokens.Add(newrefreshtoken);
           await _context.SaveChangesAsync();

            return new AuthResponseDto
            {
                Token = await GenerateJwtTokenAsync(user),
                FullName = user.FullName,
                Email = user.Email,
                Roles = roles,
                refreshToken =refreshtoken,
                id=user.Id
			};
        }

        public async Task<Generalresponse> addrole(string role)
        {
            if (await _roleManager.RoleExistsAsync(role))
                return new Generalresponse { Success = false, Data = "role already exist" };

            var result = await _roleManager.CreateAsync(new IdentityRole(role));
            return new Generalresponse
            {
                Success = result.Succeeded,
                Data = result.Succeeded ? "role created" : result.Errors
            };
        }

        public async Task<Generalresponse> Sign_IN_role(Role role)
        {
            var user = await _userManager.FindByEmailAsync(role.Email);
            if (user == null)
                return new Generalresponse { Success = false, Data = "user not exist" };

            var result = await _userManager.AddToRoleAsync(user, role.role);
            return new Generalresponse
            {
                Success = result.Succeeded,
                Data = result.Succeeded ? "added successfuly" : result.Errors
            };
        }

        public async Task<object> RefreshTocken(Refreshtokendto refreshtokendto)
        {
            
            var token = await _context.RefreshTokens.FirstOrDefaultAsync(r => r.Token == refreshtokendto.token);
			if (token == null)
				return "Token not found";

			if (token.IsRevoked)
				return "Token revoked";

			if (token.Expirydate < DateTime.UtcNow)
				return "Token expired";

			var user = await _userManager.FindByIdAsync(token.Userid);
            if (user == null)
                return "notfound";

            token.IsRevoked = true;

            var newRefreshToken = new RefreshToken
            {
                Userid = token.Userid,
                Token = GenerateRefreshToken(),
                Expirydate = DateTime.UtcNow.AddDays(7)
            };

            _context.RefreshTokens.Add(newRefreshToken);
            await _context.SaveChangesAsync();

            return new
            {
                acesstoken = await GenerateJwtTokenAsync(user),
                refreshtoken = newRefreshToken.Token
            };
        }

        public async Task<Generalresponse> Logout(Refreshtokendto logout)
        {
            var res = new Generalresponse();

            var token = await _context.RefreshTokens.FirstOrDefaultAsync(x => x.Token == logout.token);
            if (token != null)
            {
                token.IsRevoked = true;
                await _context.SaveChangesAsync();
                res.Success = true;
                res.Data = "logout successfuly";
            }
            else
            {
                res.Success = false;
                res.Data = "token is not valid";
            }
            return res;
        }

        public async Task<Generalresponse> Logoutall(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
                return new Generalresponse { Success = false, Data = "user is not exist" };

            var tokens = await _context.RefreshTokens
                .Where(r => r.Userid == id.ToString() && !r.IsRevoked)
                .ToListAsync();

            if (tokens.Count == 0)
                return new Generalresponse { Success = false, Data = "user already not login" };

            foreach (var token in tokens)
            {
                token.IsRevoked = true;
                token.Expirydate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return new Generalresponse { Success = true, Data = "logout to all machines successfuly" };
        }

        public async Task ForgotPassword(ForgotPassword forgotPassword)
        {
           
            var user = await _userManager.FindByEmailAsync(forgotPassword.Email);
            if (user != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passwordresetlink = $"?Email={user.Email}&token={Uri.EscapeDataString(token)}";
                var body = $"<p>Hi {user.FullName},</p><p>To reset your password please <a href='{passwordresetlink}'>click here</a>.</p>";
                await SendEmail(user.Email, "Reset Password", body);
            }
        }

        public async Task<Generalresponse> ResetPassword(ResetPassword resetPassword)
        {
            var user = await _userManager.FindByEmailAsync(resetPassword.Email);
            if (user == null)
                return new Generalresponse { Success = false, Data = "user not found" };

            var result = await _userManager.ResetPasswordAsync(user, resetPassword.token, resetPassword.Password);
            return new Generalresponse
            {
                Success = result.Succeeded,
                Data = result.Succeeded ? "password reset successfuly" : result.Errors
            };
        }

        public async Task<Generalresponse> changepassword(User user, CangePassword cangePassword)
        {
            if (cangePassword.NewPassword != cangePassword.ConfirmNewPassword)
                return new Generalresponse { Success = false, Data = "new password and confirm password do not match" };

            var oldPasswordCorrect = await _userManager.CheckPasswordAsync(user, cangePassword.OldPassword);
            if (!oldPasswordCorrect)
                return new Generalresponse { Success = false, Data = "old password is wrong" };

            var result = await _userManager.ChangePasswordAsync(user, cangePassword.OldPassword, cangePassword.NewPassword);
            return new Generalresponse
            {
                Success = result.Succeeded,
                Data = result.Succeeded ? "password updated successfuly" : "something went wrong"
            };
        }

        private async Task SendEmail(string receiver, string subject, string body)
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(_config["Emailsettings:Email"]));
            email.To.Add(MailboxAddress.Parse(receiver));
            email.Subject = subject;
            email.Body = new TextPart("html") { Text = body };

            using var connection = new MailKit.Net.Smtp.SmtpClient();
            await connection.ConnectAsync(_config["Emailsettings:host"], int.Parse(_config["Emailsettings:port"]!), SecureSocketOptions.StartTls);
            await connection.AuthenticateAsync(_config["Emailsettings:Email"], _config["Emailsettings:Password"]);
            await connection.SendAsync(email);
            await connection.DisconnectAsync(true);
        }



       public async Task<Generalresponse> Getsystemroles()
        {

			var roleNames = await _roleManager.Roles
								  .Select(r => r.Name)
								  .ToListAsync();

            return new Generalresponse
            {
                Success = true,
                Data = roleNames
            };

		}


        public async Task<Generalresponse> Getuserroles(User user1)
        {
            var roles = await _userManager.GetRolesAsync(user1);
            if (roles.Count > 0)
                return new Generalresponse { Success = true, Data = roles };

            return new Generalresponse { Success = true, Data = "user doesnt have any roles yet" };
        }

       public async Task<Generalresponse> Changeuserrole(User user,string role)
        {
            var response = new Generalresponse();

            var newrole =await _userManager.GetRolesAsync(user);
            if (newrole.Count == 0)
            {
                await _userManager.AddToRoleAsync(user, role);
                await _context.SaveChangesAsync();
            }
            else
            {
                foreach (var rol in newrole)
                {
                    await _userManager.RemoveFromRoleAsync(user, rol);
                }
				await _userManager.AddToRoleAsync(user, role);
				await _context.SaveChangesAsync();
			}
            response.Success = true;
            response.Data = "role changed successfuly";
            return response;
        }
        private async Task<string> GenerateJwtTokenAsync(User user)
        {
            var userRoles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Name, user.FullName)
            };

            foreach (var role in userRoles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
       
        private string GenerateRefreshToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        }

       
    }
}
