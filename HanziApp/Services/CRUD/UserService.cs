using System.Data;
using Interfaces.DatabaseAccess;
using Interfaces.Services;
using Models;
using Models.Database_Entities;
using Microsoft.AspNetCore.Identity;

// *****************************************************
//
// - USER CREATION
// CheckIfUsernameIsTakenAsync()
// CreateNewUserAsync()
//
// - PASSWORD RECOVERY
// FindUserForPasswordRecoveryAsync()
// GetUsernameByRecoveryTokenAsync()
// ResetPasswordAsync()
// CheckIfPasswordRecoveryTokenIsValidAsync()
// RemoveInvalidRecoveryTokensAsync()
//
// - USER ACTIVITY
// GetPracticeHistoryForHeatmapByUserIdAsync()
// --> GetAllPracticeActivityLogsAsyn()
//
// - OTHER USER CRUD
// FindAndReturnUserAsync() x2
// EditUserInfoAsync()
//
// *****************************************************

namespace Services.CRUD
{
    public class UserService : IUserService
    {
        private readonly IExecuteSQLquery _database; 
        private readonly ISettingsService _settings;
        public UserService(IExecuteSQLquery database, ISettingsService settings)
        {
            _database = database;
            _settings = settings;
        }

        // ---------- USER CREATION ----------

        // CHECK IF USERNAME EXISTS
        public async Task<bool> CheckIfUsernameIsTakenAsync(string username)
        {
            string sqlQuery = @"SELECT * FROM UsersTable
                                WHERE Username = @Username;";

            var parameters = new
            {
                Username = username
            };

            // ExecuteQueryAsync_ReturnInt does not work and returns -1 due to no rows being affected (operated on), we are just looking up entries
            List<UserEntity> foundEntries = await _database.ExecuteQueryAsync_ReturnDTOlist<UserEntity, object>(parameters, sqlQuery);

            return foundEntries.Count > 0;
        }

        public async Task<bool> CreateNewUserAsync(UserEntity user)
        {
            // PASSWORD HASHING PROCESS
            IdentityUser identityUser = new IdentityUser { UserName = user.Username };
            PasswordHasher<IdentityUser> passwordHasher = new PasswordHasher<IdentityUser>();

            string password = passwordHasher.HashPassword(identityUser, user.Password);
            // ------------------------

            string sqlQuery = @"INSERT INTO UsersTable (Id, Username, Password, EmailAddress, UserRole, TimeAdded)
                                VALUES (@Id, @Username, @Password, @Email, @Role, @TimeAdded);";

            Guid userId = Guid.NewGuid();

            var parameters = new // Dapper spaja ove sa @ovim
            {
                Id = userId,
                Username = user.Username,
                Password = password,
                Email = user.EmailAddress,
                Role = "n/a",
                TimeAdded = DateTime.Now
            };

            int affectedRows = await _database.ExecuteQueryAsync_ReturnInt(parameters, sqlQuery);

            if (affectedRows > 0)
            {
                bool addedSettings = await _settings.CreateFlashcardPracticeSettingsEntryAsync(userId);
                return addedSettings;
            }
            else return false;
        }


        // ---------- PASSWORD RECOVERY ----------

        public async Task<UserEntity> FindUserForPasswordRecoveryAsync(string username, string emailAddress)
        {
            REDACTED FBI KGB CIA
        }

        public async Task<string> GetUsernameByRecoveryTokenAsync(Guid recoveryToken)
        {
            REDACTED FBI KGB CIA
        }
                
        public async Task<ResultModel> ResetPasswordAsync(string incomingPassword, Guid recoveryToken)
        {
            // incomingPassword is hashed in the controller

            ResultModel result = new ResultModel();

            bool tokenIsValid = await CheckIfPasswordRecoveryTokenIsValidAsync(recoveryToken);

            if (!tokenIsValid)
            {
                result.IsSuccess = false;
                result.Message = "Your password recovery token exired. Please submit a new request for password recovery.";
                return result;
            }

            // if token is valid, proceed

            string sqlQuery = @"UPDATE UsersTable 
                                SET Password = @Password
                                FROM UsersTable U
                                JOIN PasswordRecoveryTable P ON U.Id = P.UserId
                                WHERE P.RecoveryTokenId = @Token";

            var parameters = new
            {
                Token = recoveryToken,
                Password = incomingPassword
            };

            int affectedRows = await _database.ExecuteQueryAsync_ReturnInt(parameters, sqlQuery);

            if (affectedRows > 0)
            {
                result.IsSuccess = true;
                result.Message = "Your new password has been set!";

                await RemoveInvalidRecoveryTokensAsync(recoveryToken);
            }
            else
            {
                result.IsSuccess = false;
                result.Message = "Saving to database failed.";
            }
            return result;
        }

        public async Task<bool> CheckIfPasswordRecoveryTokenIsValidAsync(Guid recoveryToken)
        {
            REDACTED FBI KGB CIA
        }

        public async Task RemoveInvalidRecoveryTokensAsync(Guid recoveryToken)
        {
            REDACTED FBI KGB CIA
        }


        // ---------- USER ACTIVITY ----------

        public async Task<(List<CalendarHeatmapModel>, int)> GetPracticeHistoryForHeatmapByUserIdAsync(Guid userId)
        {
            List<UserActivityEntity> allLogs = await GetAllPracticeActivityLogsAsync();

            List<UserActivityEntity> userPracticeHistory = allLogs.Where(log => log.UserId == userId).ToList();

            List<UserActivityEntity> others = allLogs.Except(userPracticeHistory).ToList();

            var distinctById = others
                .GroupBy(user => user.UserId)
                .Select(group => new
                {
                    Id = group.Key,
                    Count = group.Count()
                })
                .ToList();

            int userPracticeAmount = userPracticeHistory.Count();
            int betterThanHowMany = 0;
            foreach (var user in distinctById)
            {
                if (userPracticeAmount > user.Count)
                {
                    betterThanHowMany++;
                }
            }
            int allUsers = distinctById.Count() + 1;
            int topPercent = 100 - ((int)Math.Ceiling(((double)betterThanHowMany / allUsers) * 100));

            if (topPercent == 0)
            {
                topPercent = 1;
            }

            var condensedPracticeHistory = userPracticeHistory.GroupBy(entry => entry.ActivityTime.Date);

            List<CalendarHeatmapModel> list = condensedPracticeHistory
            .Select(group =>
                new CalendarHeatmapModel
                {
                    ThatDay = group.Key,
                    TotalPractice = group.Count()
                })
            .ToList();

            return (list, topPercent);
        }

        public async Task<List<UserActivityEntity>> GetAllPracticeActivityLogsAsync()
        {
            string sqlQuery = @"SELECT * FROM UserActivityTable
                                WHERE DidFlashcardPractice = 1;";

            List<UserActivityEntity> list = await _database.ExecuteQueryAsync_ReturnDTOlist<UserActivityEntity, object>(null, sqlQuery);
            return list;
        }


        // ---------- OTHER USER CRUD ----------

        // FIND USER FOR LOGIN 
        public async Task<UserEntity> FindAndReturnUserAsync(string username)
        {
            string sqlQuery = @"SELECT * FROM UsersTable
                                WHERE Username = @Username;";

            var parameters = new
            {
                Username = username
            };

            UserEntity user = await _database.ExecuteQueryAsync_ReturnDTO<UserEntity, object>(parameters, sqlQuery);
            return user;
        }
        
        // FIND USER BY ID 
        public async Task<UserEntity> FindAndReturnUserAsync(Guid id)
        {
            string sqlQuery = @"SELECT * FROM UsersTable
                                WHERE Id = @Id;";

            var parameters = new
            {
                Id = id
            };

            UserEntity user = await _database.ExecuteQueryAsync_ReturnDTO<UserEntity, object>(parameters, sqlQuery);
            return user;
        }
             
        // EDIT USER INFO
        public async Task<ResultModel> EditUserInfoAsync(UserEntity user)
        {
            string sqlQuery = @"UPDATE UsersTable SET 
                                Username = @Username, 
                                Password = @Password,
                                EmailAddress = @EmailAddress,
                                TimeUpdated = @TimeUpdated
                                WHERE Id = @Id";

            var parameters = new
            {
                Id = user.Id,
                Username = user.Username,
                Password = user.Password,
                EmailAddress = user.EmailAddress,
                TimeUpdated = DateTime.Now
            };

            int affectedRows = await _database.ExecuteQueryAsync_ReturnInt(parameters, sqlQuery);

            ResultModel result = new ResultModel();
            if (affectedRows > 0) 
            {
                result.IsSuccess = true;
                result.Message = "Updates have been saved!";                
            }
            else 
            {
                result.IsSuccess = false;
                result.Message = "Saving to database failed.";
            }
            return result;
        }

    }
}
