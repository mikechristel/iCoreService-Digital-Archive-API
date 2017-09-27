using System;

namespace iCoreService.Models
{
    /// <summary>
    /// Data structure used by client to send end-user feedback.
    /// </summary>
    public class Feedback
    {
        /// <summary>
        /// End-user submitted comments.
        /// </summary>
        public string Comments { get; set; }

        /// <summary>
        /// End-user's browser navigator.platform value.
        /// </summary>
        public string Platform { get; set; }

        /// <summary>
        /// End-user's browser navigator.userAgent value.
        /// </summary>
        public string UserAgent { get; set; }

        /// <summary>
        /// End-user's browser navigator.language value.
        /// </summary>
        public string Language { get; set; }

        /// <summary>
        /// End-user's browser window.width x window.height value.
        /// </summary>
        public string Resolution { get; set; }

        /// <summary>
        /// End-user's system date at time of feedback submission.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// End-user's browser windows.location value.
        /// </summary>
        public string URL { get; set; }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return string.Format(
                "Comments: {0}\r\n\r\nPlatform: {1}\r\n\r\nUserAgent: {2}\r\n\r\nLanguage: {3}\r\n\r\nResolution: {4}\r\n\r\nDate: {5}\r\n\r\nURL: {6}", 
                this.Comments,
                this.Platform,
                this.UserAgent,
                this.Language,
                this.Resolution,
                this.Date,
                this.URL
            );
        }
    }

}