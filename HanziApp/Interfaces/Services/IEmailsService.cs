using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models;

namespace Interfaces.Services
{
    public interface IEmailsService
    {
        ResultModel SendEmail(string receivingAddress, string mailTitle, string mailBody);
        Task<Guid> GenerateStoreAndReturnPasswordRecoveryTokenAsync(Guid userId);
    }
}
