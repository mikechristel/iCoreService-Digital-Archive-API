using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Threading.Tasks;
using iCoreService.Models;


namespace iCoreService.Controllers
{
    /// <summary>
    /// Provides in-application feedback.
    /// </summary>
    public class FeedbackController : ApiController
    {
        private string feedbackMailTo = ConfigurationManager.AppSettings["FeedbackMailTo"];
        private string feedbackMailFrom = ConfigurationManager.AppSettings["FeedbackMailFrom"];
        private string feedbackSubject = ConfigurationManager.AppSettings["FeedbackSubject"];
        private string sendGridApiKey = ConfigurationManager.AppSettings["SendGridApiKey"];        

        /// <summary>
        /// Receives end-user feedback from the application and mails it to the development team.
        /// </summary>
        /// <param name="feedback">A JSON document containnig the end-user feedback and runtime environment information.</param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> PostFeedback([FromBody]Feedback feedback)
        {
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.InternalServerError);

            try
            {
                await SendFeedback(feedback);
                response.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content = new StringContent(ex.Message)
                };
            }            

            return response;
        }

        async Task SendFeedback(Feedback feedback)
        {
            var client = new SendGridClient(this.sendGridApiKey);

            var from = new EmailAddress(feedbackMailFrom, "Digital Archive Feedback");;
            var to = new EmailAddress(feedbackMailTo);
            var msg = MailHelper.CreateSingleEmail(from, to, this.feedbackSubject, feedback.ToString(), null);

            var response = await client.SendEmailAsync(msg);
        }
    }
}
