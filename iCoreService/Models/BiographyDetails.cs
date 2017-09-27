using System;
using System.Collections.Generic;

namespace iCoreService.Models
{
    /// <summary>
    /// Details regarding a single biographical collection including its interview
    /// sessions, video tapes, and processed story segments.
    /// </summary>
    public class BiographyDetails
    {
        /// <summary>
        /// Document key for Azure Search biography index
        /// </summary>
        public string BiographyID { get; set; }

        /// <summary>
        /// Accession number, the preferred unique identifier for an interview subject
        /// </summary>
        public string Accession { get; set; }

        /// <summary>
        /// A brief description of the interview subject
        /// </summary>
        public string DescriptionShort { get; set; }

        /// <summary>
        /// A paragraph biography of the interview subject, typically much longer than DescriptionShort
        /// </summary>
        public string BiographyShort { get; set; }

        /// <summary>
        /// Interview subject's first name
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// Interview subject's last name
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// Interview subject's full preferred name
        /// </summary>
        public string PreferredName { get; set; }

        /// <summary>
        /// Interview subject's gender; facetable
        /// </summary>
        public string Gender { get; set; }

        /// <summary>
        /// Interview subject's URL in the "master" website, e.g., The HistoryMakers
        /// </summary>
        public string WebsiteURL { get; set; }

        /// <summary>
        /// Geographic region (residence) for the interview subject at time of (latest?) interview
        /// </summary>
        public string Region { get; set; }

        /// <summary>
        /// City of birth
        /// </summary>
        public string BirthCity { get; set; }

        /// <summary>
        /// State (U.S.) of birth, left empty if birth country is not United States
        /// </summary>
        public string BirthState { get; set; }

        /// <summary>
        /// Country of birth
        /// </summary>
        public string BirthCountry { get; set; }

        /// <summary>
        /// Date of birth, may be null
        /// </summary>
        public DateTime? BirthDate { get; set; }

        /// <summary>
        /// 2 digit birth day
        /// </summary>
        public int? BirthDay { get; set; }

        /// <summary>
        /// 2 digit birth month
        /// </summary>
        public int? BirthMonth { get; set; }

        /// <summary>
        /// 4 digit birth year
        /// </summary>
        public int? BirthYear { get; set; }

        /// <summary>
        /// Date of death, may be null
        /// </summary>
        public DateTime? DeceasedDate { get; set; }

        /// <summary>
        /// Interview subject's "Maker" categorization from The HistoryMakers; facetable
        /// </summary>
        public string[] MakerCategories { get; set; }

        /// <summary>
        /// Interview subject's broad job families; facetable
        /// </summary>
        public string[] OccupationTypes { get; set; }

        /// <summary>
        /// Specific occupations of the interview subject
        /// </summary>
        public string[] Occupations { get; set; }

        /// <summary>
        /// Answers to "People Magazine"-type questions.
        /// </summary>
        public FavoritesSet Favorites { get; set; }

        /// <summary>
        /// List of Sessions related to this Biography.
        /// </summary>
        public List<BiographySession> Sessions { get; set; }
    }

    /// <summary>
    /// Details regarding a single interview session.
    /// </summary>
    public class BiographySession
    {
        /// <summary>
        /// 1-based ordering of the session within the biographical collection.
        /// </summary>
        public int? SessionOrder { get; set; }

        /// <summary>
        /// Date of interview session
        /// </summary>
        public DateTime? InterviewDate { get; set; }

        /// <summary>
        /// Name of person conducting the interview
        /// </summary>
        public string Interviewer { get; set; }

        /// <summary>
        /// Name of videographer
        /// </summary>
        public string Videographer { get; set; }

        /// <summary>
        /// Location of interview
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// Sponsor
        /// </summary>
        public string Sponsor { get; set; }

        /// <summary>
        /// Sponsor URL
        /// </summary>
        public string SponsorURL { get; set; }

        /// <summary>
        /// Sponsor image (ignore if it starts "TODO")
        /// </summary>
        public string SponsorImage { get; set; }

        /// <summary>
        /// List of Tapes in this session
        /// </summary>
        public List<BiographyTape> Tapes { get; set; }
    }

    /// <summary>
    /// Details regarding a specific video tape within a biographical collection.
    /// </summary>
    public class BiographyTape
    {
        /// <summary>
        /// A brief description about the contents of this tape
        /// </summary>
        public string Abstract { get; set; }

        /// <summary>
        /// List of Stories in this session
        /// </summary>
        public List<BiographyStory> Stories { get; set; }

        /// <summary>
        /// 1-based ordering of the tape within the interview session.
        /// </summary>
        public int? TapeOrder { get; set; }
    }

    /// <summary>
    /// Details regarding a specific story within the biographical collection.
    /// </summary>
    public class BiographyStory
    {
        /// <summary>
        /// Duration of segment in milliseconds
        /// </summary>
        public int? Duration { get; set; }

        /// <summary>
        /// Document key. Analogous to database SegmentID converted to a 
        /// string as required by Azure Search
        /// </summary>
        public string StoryID { get; set; }

        /// <summary>
        /// 1-based order of the story within the tape.
        /// </summary>
        public int? StoryOrder { get; set; }

        /// <summary>
        /// Story title
        /// </summary>
        public string Title { get; set; }
    }

}
