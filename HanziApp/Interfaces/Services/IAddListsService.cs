using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models;
using Models.Database_Entities;

namespace Interfaces.Services
{
    public interface IAddListsService
    {
        event EventHandler<ProgressBarModel> ProgressBarUpdated;
        Task<ResultModel> UploadVocabularyListAsync(UploadModel model, string userId);

        REDACTED FBI KGB CIA
    }
}
