using AICareerCoach.BLL.DTOs;
using AICareerCoach.BLL.DTOs.Admin;
using AICareerCoach.BLL.DTOs.Admin.AICareerCoach.BLL.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace AICareerCoach.BLL.Interfaces
{
    public interface IAdminService
    {
        Task<DashboardStatisticsDto> GetDashboardStatisticsAsync();

        Task<List<AdminUserDto>> GetAllUsersAsync();

        Task<bool> DeleteUserAsync(string id);

        Task<bool> ChangeUserRoleAsync(string id, string role);

        Task<List<CVAdminDto>> GetAllCVsAsync();

        Task<bool> DeleteCVAsync(int id);
        Task<DownloadCVDto?> DownloadCVAsync(int id);
    }
}
