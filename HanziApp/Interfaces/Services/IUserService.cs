using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models;
using Models.Database_Entities;

namespace Interfaces.Services
{
    public interface IUserService
    {
        REDACTED FBI KGB CIA
        Task<ResultModel> EditUserInfoAsync(UserEntity user);
        Task<ResultModel> ResetPasswordAsync(string incomingPassword, Guid recoveryToken);
        Task<(List<CalendarHeatmapModel>, int)> GetPracticeHistoryForHeatmapByUserIdAsync(Guid userId);
    }
}
