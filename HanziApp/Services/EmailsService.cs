using System.Net.Mail;
using System.Net;
using Interfaces.Services;
using Models;
using Interfaces.DatabaseAccess;
using Microsoft.Extensions.Configuration;

// *****************************************************
//
// SendEmail()
// GenerateStoreAndReturnPasswordRecoveryTokenAsync()
//
// *****************************************************

namespace Services
{
    public class EmailsService : IEmailsService
    {
        private readonly IExecuteSQLquery _database;
        private readonly IConfiguration _configuration;
        public EmailsService(IExecuteSQLquery database, IConfiguration configuration)
        {
            _database = database;
            _configuration = configuration;
        }

        public ResultModel SendEmail(string receivingAddress, string mailTitle, string mailBody)
        {
            MailAddress fromAddress = new MailAddress("ivan.kovac.app.emailservice@gmail.com", "Chinese practice app");
            MailAddress toAddress = new MailAddress(receivingAddress);

            string smtpPassword = _configuration.GetConnectionString("smtpPassword");

            SmtpClient smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential("ivan.kovac.app.emailservice@gmail.com", smtpPassword)
            };

            using (MailMessage message = new MailMessage(fromAddress, toAddress)
            {
                Subject = mailTitle,
                Body = mailBody
            })
            {
                smtp.Send(message);
            }

            ResultModel result = new ResultModel()
            {
                IsSuccess = true,
                Message = "Your password recovery email is on its way!"
            };

            return result;
        }

        public async Task<Guid> GenerateStoreAndReturnPasswordRecoveryTokenAsync(Guid userId)
        {
            Guid recoveryToken = Guid.NewGuid();

            string sqlQuery = @"INSERT INTO PasswordRecoveryTable (RecoveryTokenId, UserId, TimeTokenCreated)
                                VALUES (@RecoveryTokenId, @UserId, @TimeTokenCreated);";

            var parameters = new 
            {
                RecoveryTokenId = recoveryToken,
                UserId = userId,
                TimeTokenCreated = DateTime.Now
            };

            int affectedRows = await _database.ExecuteQueryAsync_ReturnInt(parameters, sqlQuery);

            if (affectedRows > 0) 
            { 
                return recoveryToken; 
            }
            else return Guid.Empty;
        }
    }
}
