using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AICareerCoach.BLL.DTOs;
using AICareerCoach.BLL.DTOs.Auth;
using AICareerCoach.BLL.Interfaces;
using AICareerCoach.BLL.services;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using AICareerCoach.DAL.Models;
using AICareerCoach.DAL.repository;
using MailKit.Security;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MimeKit;


namespace AICareerCoach.BLL.Services
{
    public class AuthService : IAuthService ,IEmailservice
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _rolemanger;
        private readonly IConfiguration _config;
		protected readonly AICareerCoachDbContext context;
		protected readonly DbSet<User> dbset;



		public AuthService(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IConfiguration config,
            RoleManager<IdentityRole> identityRole,
            AICareerCoachDbContext _context
            )
        {
            _userManager = userManager;
            _config = config;
			_rolemanger = identityRole;
            context=_context;
            dbset=context.Set<User>();
        }
       
        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
        {
            var userExists = await _userManager.FindByEmailAsync(dto.Email);
            if (userExists !=null)
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

            var token = await GenerateJwtTokenAsync(user);

            return new AuthResponseDto
            {
                Token = token,
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

            var rolesList = await _userManager.GetRolesAsync(user);
            var roles = rolesList.ToList();

            var token = await GenerateJwtTokenAsync(user);
            var refreshtoken= Generaterefreshtoken();

            return new AuthResponseDto
            {
                Token = token,
                FullName = user.FullName,
                Email = user.Email,
                Roles = roles,
                refreshToken = refreshtoken

            };
        }


		public async Task<Generalresponse> addrole(string role)
        {
            var res=new Generalresponse();
            if(!await _rolemanger.RoleExistsAsync(role))
            {
                var result = await _rolemanger.CreateAsync(new IdentityRole(role));
                if(result.Succeeded)
                {
                   res.sucess=true;
                    res.Data = "role created";
                }
               res.sucess= false;
                res.Data = result.Errors;
            }
            res.sucess = false;
            res.Data = "role already exist";
            return res;


		}

        public async Task<Generalresponse> Sign_IN_role(Role role)
        {
           var response =new Generalresponse();
            var user= await _userManager.FindByEmailAsync(role.Email);
            if(user!=null)
            {
                 var result= await _userManager.AddToRoleAsync(user, role.role);
                if(result.Succeeded)
                {
					response.sucess = true;
                    response.Data = "added successfuly";
				}
				response.sucess=false;
                response.Data = result.Errors;
			}
            response.sucess = false;
            response.Data = "user not exist";
			return response;

		}

       public async Task<Object> RefreshTocken(Refreshtokendto refreshtokendto)
        {
            var token = await context.RefreshTokens.FirstOrDefaultAsync(r=>r.Token==refreshtokendto.token);
            if (token == null || token.IsRevoked || token.Expirydate < DateTime.UtcNow)
                return "error";
            else
            {
                var user =await _userManager.FindByIdAsync(token.Id.ToString());
                if(user==null)
                return "error";
                //var roles=await _userManager.GetRolesAsync(user);
                token.IsRevoked = true;
                var newrefreshtoken =Generaterefreshtoken();
                var reftoken = new RefreshToken();
                reftoken.Userid=token.Id;
                reftoken.Token = newrefreshtoken;
                reftoken.Expirydate = DateTime.UtcNow.AddDays(7);
                context.RefreshTokens.Add(reftoken);
                context.SaveChanges();
                return new
                {
                    acesstoken = GenerateJwtTokenAsync(user),
                    refreshtoken = reftoken
                };
            }

        }

        public async Task Logout(Refreshtokendto logout)
        {
            var token=await context.RefreshTokens.FirstOrDefaultAsync(x=>x.Token==logout.token);
            if (token != null)
            {
                token.IsRevoked = true;
                await context.SaveChangesAsync();
            }
               

        }

      public async Task Logoutall(int id)
        {
            var tokens = await context.RefreshTokens.Where(r => r.Id == id && r.IsRevoked == false).ToListAsync();
            foreach(var token in tokens)
            {
                token.IsRevoked = true;
                token.Expirydate = DateTime.UtcNow;
            }
            await context.SaveChangesAsync();
        }
        public async Task ForgotPassword(ForgotPassword forgotPassword)
        {
            
            var user=await _userManager.FindByEmailAsync(forgotPassword.Email);
            if (user != null)
            {
                var token=await _userManager.GeneratePasswordResetTokenAsync(user);
                var passwordresetlink = $"?Email={user.Email}&token={token}";
                await Sendemail(user.Email, "reset password", "< p > hi{ user.FullName}</ p > to reset paswoord please<a href = '{passwordresetlink}' > clicke here </ a >");

            }

          
        }

       public async Task<Generalresponse> ResetPassword(ResetPassword resetPassword)
        {
            var response= new Generalresponse();
            var user = await _userManager.FindByEmailAsync(resetPassword.Email);
            if(user != null) 
                {
                 var result=_userManager.ResetPasswordAsync(user, resetPassword.token, resetPassword.Password);
                response.sucess = true;
                response.Data = "password reset successfuly";
                }
            response.sucess=false;
            response.Data = "user not found";
            return response;
        }

		public async Task Sendemail(string receiver, string subject, string body)


		{
			var email = new MimeMessage();
			email.From.Add(MailboxAddress.Parse(_config["Emailsettings:Email"]));
			email.To.Add(MailboxAddress.Parse(receiver));
			email.Subject = subject;
			email.Body = new TextPart("html")
			{
				Text = body
			};

			var connection = new MailKit.Net.Smtp.SmtpClient();
			await connection.ConnectAsync(_config["Emailsetrtings:host"], int.Parse(_config["Emailsettings:port"]), SecureSocketOptions.StartTls);
			await connection.AuthenticateAsync(_config["Emailsettings:Email"], _config["Emailsettings:Password"]);

			await connection.SendAsync(email);
			await connection.DisconnectAsync(true);


		}
        private string Generaterefreshtoken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
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
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

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
       

        
    }
}
