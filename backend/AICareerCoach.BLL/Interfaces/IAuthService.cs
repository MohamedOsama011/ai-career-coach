using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AICareerCoach.BLL.DTOs;
using AICareerCoach.BLL.DTOs.Auth;
using AICareerCoach.DAL.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;

namespace AICareerCoach.BLL.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
        Task<AuthResponseDto> LoginAsync(LoginDto dto);
		Task<Generalresponse> addrole(string role);
        Task<Generalresponse> Sign_IN_role(Role role);
        Task<Object> RefreshTocken(Refreshtokendto refreshtokendto);
        Task<Generalresponse> Logout(Refreshtokendto logout);
        Task<Generalresponse> Logoutall(int id);
        Task ForgotPassword(ForgotPassword forgotPassword);

        Task<Generalresponse> ResetPassword(ResetPassword resetPassword);

        Task<AuthResponseDto> GoogleLoginAsync(GoogleLoginDto dto);
        Task<Generalresponse> changepassword(User user,CangePassword cangePassword);

        Task<Generalresponse> Getsystemroles();
		Task<Generalresponse> Getuserroles( User user);
		Task<Generalresponse> Changeuserrole(User user, string role);





































	}
}
