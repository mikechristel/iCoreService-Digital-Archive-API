using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;

using iCoreService.Models;

#if SCIENCEMAKERSONLY
using System.IO; // for StreamReader
using Newtonsoft.Json; // for serialization
#endif

namespace iCoreService.AzureServices
{
    /// <summary>
    /// A wrapper around the Azure Search API
    /// </summary>
    public class AzureSearch
    {
        #region ===== DECLARATIONS
        /// <summary>
        /// The maximum page size allowed to be requested from the search service.
        /// </summary>
        public const int MAX_PAGE_SIZE = 500;

        /// <summary>
        /// The name of the Azure Search index which will contain the biography (table of contents) data.
        /// </summary>
        private const string BIOGRAPHY_INDEX = "biographies";

        /// <summary>
        /// The name of the Azure Search index which will contain the interview session data.
        /// </summary>
        private const string SESSION_INDEX = "sessions";

        /// <summary>
        /// The name of the Azure Search index which will contain the story (segment) data.
        /// </summary>
        private const string STORY_INDEX = "stories";

        /// <summary>
        /// The name of the Azure Search index which will contain the the tape (movie) data.
        /// </summary>
        private const string TAPE_INDEX = "tapes";

        /// <summary>
        /// A cached static reference to the name of the search service currently in use.
        /// </summary>
        private static string searchServiceName;
        
        /// <summary>
        /// A cached static reference to the Azure Search service.
        /// </summary>
        private static readonly SearchServiceClient serviceClient;

        /// <summary>
        /// A cached static reference to the client used to search the biography index.
        /// </summary>
        private static readonly SearchIndexClient biographyIndex;
        
        /// <summary>
        /// A cached static reference to the client used to search the story index.
        /// </summary>
        private static readonly SearchIndexClient storyIndex;

        /// <summary>
        /// The streamlined set of fields to return when doing a biography search.
        /// </summary>
        private static readonly List<string> StreamlinedBiographyFieldsSet = 
            new List<string> { "biographyID",  "preferredName", "birthDate", "accession" };

        /// <summary>
        /// The streamline set of fields to return when doing a story search.
        /// </summary>
        private static readonly List<string> StreamlinedStoryFieldsSet =
            new List<string> { "storyID", "biographyID", "sessionOrder", "tapeOrder", "storyOrder", "duration", "interviewDate", "title" };

        /// <summary>
        /// The list of facets to be returned as part of any biography search result.
        /// </summary>
        private static readonly List<string> BiographySearchFacets =
            new List<String>() { "lastInitial,count:26", "gender", "birthYear,interval:10", "makerCategories,count:15", "occupationTypes" };

        /// <summary>
        /// The list of facets to be returned as part of any story search result.
        /// </summary>
        private static readonly List<string> StorySearchFacets =
            new List<String>() { "gender", "birthYear,interval:10", "makerCategories,count:15", "occupationTypes" };

        /// <summary>
        /// A reference to any error message which may be generated during static initialization.
        /// </summary>
        public static string errorMessage;

#endregion == DECLARATIONS

        #region ===== CONSTRUCTOR
        /// <summary>
        /// Static constructor initializes the Azure Search context.
        /// </summary>
        static AzureSearch()
        {
            string apiKey = ConfigurationManager.AppSettings["SearchServiceApiKey"];
            searchServiceName = ConfigurationManager.AppSettings["SearchServiceName"];

            var credentials = new SearchCredentials(apiKey);
            serviceClient = new SearchServiceClient(searchServiceName, credentials);
            biographyIndex = new SearchIndexClient(searchServiceName, BIOGRAPHY_INDEX, credentials);
            storyIndex = new SearchIndexClient(searchServiceName, STORY_INDEX, credentials);
        }
        #endregion == CONSTRUCTOR

        #region ===== BIOGRAPHY RELATED METHODS
        /// <summary>
        /// Perform full-text search of biograpy (collection) index and apply facets as given.
        /// </summary>
        /// <param name="query">Search terms.</param>
        /// <param name="pageSize">Number of items to return in the current result set.</param>
        /// <param name="currentPage">Which page of results to return based on page size.</param>
        /// <param name="searchFields">A comma-separated list of fields to be searched.</param>
        /// <param name="genderFacet">Filters results based on given gender.</param>
        /// <param name="birthYearFacet">Filters results based on given birth year.</param>
        /// <param name="makerFacet">Filters results based on the given comma-separated list of maker categories.</param>
        /// <param name="jobFacet">Filters results based on the given comma-separated list of job types.</param>
        /// <param name="lastInitialFacet">Filter results by first letter of last name, e.g., C is filtering to all last names starting with C.</param>
        /// <param name="sortField">Sort results by this field (with no sort qualifier added if null).</param>
        /// <param name="sortInDescendingOrder">If sortField is given, sort results in descending order if true, ascending order if false.</param>
        /// <returns>A DocumentSearchResult object containing the results of the specified search.</returns>
        internal async Task<DocumentSearchResult> BiographySearch(string query, int pageSize, int currentPage, string searchFields, string genderFacet, string birthYearFacet, string makerFacet, string jobFacet, string lastInitialFacet, string sortField, bool sortInDescendingOrder)
        {
            List<string> fieldsToSearch;
            if (searchFields == "all")
                fieldsToSearch = new List<string>() { "descriptionShort", "lastName", "preferredName", "accession" };
            else
                fieldsToSearch = new List<string>(searchFields.Split(','));

            SearchParameters sp = new SearchParameters()
            {
                SearchMode = SearchMode.All,
                SearchFields = fieldsToSearch,
                Top = pageSize,
                Skip = (currentPage - 1) * pageSize,
                IncludeTotalResultCount = true,
                HighlightFields = fieldsToSearch,
                HighlightPreTag = "<em>",
                HighlightPostTag = "</em>",
                Facets = BiographySearchFacets,
                Select = StreamlinedBiographyFieldsSet
            };

#if SCIENCEMAKERSONLY
            sp.Filter = GetFilterFromFacets(genderFacet, birthYearFacet, WebApiApplication.SCIENCEMAKER_FLAG, jobFacet, lastInitialFacet);
#else
            sp.Filter = GetFilterFromFacets(genderFacet, birthYearFacet, makerFacet, jobFacet, lastInitialFacet);
#endif
            if (!string.IsNullOrWhiteSpace(sortField))
            {
                string sortQualifier = sortInDescendingOrder ? sortField + " desc" : sortField + " asc";
                sp.OrderBy = new List<String> { sortQualifier };
            }

            return await biographyIndex.Documents.SearchAsync(query, sp);
        }

        /// <summary>
        /// Returns type-ahead search suggestions for the biography search box.
        /// </summary>
        /// <param name="query">The search fragment entered so far.</param>
        /// <param name="fuzzy">If true, use fuzzy term matching.</param>
        /// <returns></returns>
        internal async Task<DocumentSuggestResult> BiographySuggest(string query, bool fuzzy)
        {
            SuggestParameters sp = new SuggestParameters()
            {
                UseFuzzyMatching = fuzzy,
                Top = 8
            };

            return await biographyIndex.Documents.SuggestAsync(query, "biography_suggester", sp);
        }

        /// <summary>
        /// Get a list of biographies corresponding to people born in the current month.
        /// </summary>
        /// <param name="pageSize">Number of items to return in the current result set.</param>
        /// <param name="currentPage">Which page of results to return based on page size.</param>
        /// <param name="genderFacet">Filters results based on given gender.</param>
        /// <param name="birthYearFacet">Filters results based on given birth year.</param>
        /// <param name="makerFacet">Filters results based on the given comma-separated list of maker categories.</param>
        /// <param name="jobFacet">Filters results based on the given comma-separated list of job types.</param>
        /// <param name="lastInitialFacet">Filter results by first letter of last name, e.g., C is filtering to all last names starting with C.</param>
        /// <param name="dateToday">String equivalent of today's date.</param>
        /// <returns>A DocumentSearchResult object containing the biographies of people born in the current month.</returns>
        internal async Task<DocumentSearchResult> GetPeopleBornThisMonth(int pageSize, int currentPage, string genderFacet,
            string birthYearFacet, string makerFacet, string jobFacet, string lastInitialFacet, string dateToday)
        {
            SearchParameters sp = new SearchParameters()
            {
                SearchMode = SearchMode.Any,
                Top = pageSize,
                Skip = (currentPage - 1) * pageSize,
                IncludeTotalResultCount = true,
                Facets = BiographySearchFacets,
                Select = StreamlinedBiographyFieldsSet
            };

            // Compute the month as the full month bounding the current date.
            DateTime today;
            if (String.IsNullOrEmpty(dateToday))
                today = DateTime.Today;
            else
                today = DateTime.Parse(dateToday);
#if SCIENCEMAKERSONLY
            sp.Filter = GetFilterFromFacets(genderFacet, birthYearFacet, WebApiApplication.SCIENCEMAKER_FLAG, jobFacet, lastInitialFacet);
#else
            sp.Filter = GetFilterFromFacets(genderFacet, birthYearFacet, makerFacet, jobFacet, lastInitialFacet);
#endif
            if (sp.Filter != null) sp.Filter += " and ";

            sp.Filter += string.Format("(birthMonth eq {0} and birthDay ge {1} and birthDay le {2})",
                    today.Month, 1, 31); // don't worry about nonsensical dates like April 31 - they shouldn't be in data store

            return await biographyIndex.Documents.SearchAsync(string.Empty, sp);
        }

        /// <summary>
        /// Get a list of biographies corresponding to people born in the current week.
        /// </summary>
        /// <param name="pageSize">Number of items to return in the current result set.</param>
        /// <param name="currentPage">Which page of results to return based on page size.</param>
        /// <param name="genderFacet">Filters results based on given gender.</param>
        /// <param name="birthYearFacet">Filters results based on given birth year.</param>
        /// <param name="makerFacet">Filters results based on the given comma-separated list of maker categories.</param>
        /// <param name="jobFacet">Filters results based on the given comma-separated list of job types.</param>
        /// <param name="lastInitialFacet">Filter results by first letter of last name, e.g., C is filtering to all last names starting with C.</param>
        /// <param name="dateToday">String equivalent of today's date.</param>
        /// <returns>A DocumentSearchResult object containing the biographies of people born in the current week.</returns>
        internal async Task<DocumentSearchResult> GetPeopleBornThisWeek(int pageSize, int currentPage, string genderFacet, 
            string birthYearFacet, string makerFacet, string jobFacet, string lastInitialFacet, string dateToday)
        {
            SearchParameters sp = new SearchParameters()
            {
                SearchMode = SearchMode.Any,
                Top = pageSize,
                Skip = (currentPage - 1) * pageSize,
                IncludeTotalResultCount = true,
                Facets = BiographySearchFacets,
                Select = StreamlinedBiographyFieldsSet
            };

            // Compute the week as Sunday through Saturday bounding the current date.
            DateTime today;
            if (String.IsNullOrEmpty(dateToday))
                today = DateTime.Today;
            else
                today = DateTime.Parse(dateToday);
            DateTime startOfWeek = today.AddDays(-(int)today.DayOfWeek);
            DateTime endOfWeek = startOfWeek.AddDays(6);

#if SCIENCEMAKERSONLY
            sp.Filter = GetFilterFromFacets(genderFacet, birthYearFacet, WebApiApplication.SCIENCEMAKER_FLAG, jobFacet, lastInitialFacet);
#else
            sp.Filter = GetFilterFromFacets(genderFacet, birthYearFacet, makerFacet, jobFacet, lastInitialFacet);
#endif
            if (sp.Filter != null) sp.Filter += " and ";

            if (startOfWeek.Month == endOfWeek.Month)
            {
                sp.Filter += string.Format("(birthMonth eq {0} and birthDay ge {1} and birthDay le {2})",
                    startOfWeek.Month, startOfWeek.Day, endOfWeek.Day);
            }
            else
            {
                // Week spans across months so must adjust query accordingly
                sp.Filter += string.Format("((birthMonth eq {0} and birthDay ge {1}) or (birthMonth eq {2} and birthDay le {3}))",
                    startOfWeek.Month, startOfWeek.Day, endOfWeek.Month, endOfWeek.Day);
            }

            return await biographyIndex.Documents.SearchAsync(string.Empty, sp);
        }

        /// <summary>
        /// Get a list of biographies corresponding to people born on the current day.
        /// </summary>
        /// <param name="pageSize">Number of items to return in the current result set.</param>
        /// <param name="currentPage">Which page of results to return based on page size.</param>
        /// <param name="genderFacet">Filters results based on given gender.</param>
        /// <param name="birthYearFacet">Filters results based on given birth year.</param>
        /// <param name="makerFacet">Filters results based on the given comma-separated list of maker categories.</param>
        /// <param name="jobFacet">Filters results based on the given comma-separated list of job types.</param>
        /// <param name="lastInitialFacet">Filter results by first letter of last name, e.g., C is filtering to all last names starting with C.</param>
        /// <param name="dateToday">String equivalent of today's date.</param>
        /// <returns>A DocumentSearchResult object containing the biographies of people born on the current day, sorted oldest to youngest.</returns>
        internal async Task<DocumentSearchResult> GetPeopleBornThisDay(int pageSize, int currentPage, string genderFacet, 
            string birthYearFacet, string makerFacet, string jobFacet, string lastInitialFacet, string dateToday)
        {
            SearchParameters sp = new SearchParameters()
            {
                SearchMode = SearchMode.Any,
                Top = pageSize,
                Skip = (currentPage - 1) * pageSize,
                IncludeTotalResultCount = true,
                Facets = BiographySearchFacets,
                Select = StreamlinedBiographyFieldsSet
            };

            DateTime today;
            if (String.IsNullOrEmpty(dateToday))
                today = DateTime.Today;
            else
                today = DateTime.Parse(dateToday);

#if SCIENCEMAKERSONLY
            sp.Filter = GetFilterFromFacets(genderFacet, birthYearFacet, WebApiApplication.SCIENCEMAKER_FLAG, jobFacet, lastInitialFacet);
#else
            sp.Filter = GetFilterFromFacets(genderFacet, birthYearFacet, makerFacet, jobFacet, lastInitialFacet);
#endif
            if (sp.Filter != null) sp.Filter += " and ";

            // Add in the "born this day" filter:
            sp.Filter += string.Format("(birthMonth eq {0} and birthDay eq {1})",
                    today.Month, today.Day);

            // Order people oldest to youngest:
            sp.OrderBy = new List<String> { "birthYear asc" };

            return await biographyIndex.Documents.SearchAsync(string.Empty, sp);
        }

#endregion == BIOGRAPHY RELATED METHODS

#region ===== STORY RELATED METHODS
        /// <summary>
        /// Perform full-text search of the story index and apply facets as given.
        /// </summary>
        /// <param name="query">Search terms.</param>
        /// <param name="pageSize">Number of results to return per call.</param>
        /// <param name="currentPage">The 1-based page of results to return.</param>
        /// <param name="parentBiographyID">Filter stories by a biography ID.</param>
        /// <param name="searchFields">A comma-separated list of fields to be searched.</param>
        /// <param name="interviewYearFilterLowerBound">Lower bound on returned stories' interview year, ignored if 0.</param>
        /// <param name="interviewYearFilterUpperBound">Upper bound on returned stories' interview year, ignored if 0.</param>
        /// <param name="genderFacet">Filter stories by gender.</param>
        /// <param name="yearFacet">Filter stories by birth year.</param>
        /// <param name="makerFacet">Filter stories by comma-separated list of maker categories.</param>
        /// <param name="jobFacet">Filter stories by comma-separated list of job types.</param>
        /// <param name="sortField">Sort results by this field (with no sort qualifier added if null).</param>
        /// <param name="sortInDescendingOrder">If sortField is given, sort results in descending order if true, ascending order if false.</param>
        /// <returns>A DocumentSearchResult object containing the results of the specified search.</returns>
        /// <remarks>If searchFields is "all" it will be taken to be title,transcript.</remarks>
        internal async Task<DocumentSearchResult> StorySearch(string query, int pageSize, int currentPage, string parentBiographyID, 
            string searchFields, int interviewYearFilterLowerBound, int interviewYearFilterUpperBound, 
            string genderFacet, string yearFacet, string makerFacet, string jobFacet, string sortField, bool sortInDescendingOrder)
        {
            List<string> fieldsToSearch;

            if (searchFields == "all")
                fieldsToSearch = new List<string>(new string[] { "title", "transcript" });
            else
                fieldsToSearch = new List<string>(searchFields.Split(','));

            SearchParameters sp = new SearchParameters()
            {
                SearchMode = SearchMode.All,
                SearchFields = fieldsToSearch,
                Top = pageSize,
                Skip = (currentPage - 1) * pageSize,
                IncludeTotalResultCount = true,
                HighlightFields = fieldsToSearch,
                HighlightPreTag = "<em>",
                HighlightPostTag = "</em>",
                Facets = StorySearchFacets,
                Select = StreamlinedStoryFieldsSet
            };

#if SCIENCEMAKERSONLY
            sp.Filter = GetFilterFromFacets(genderFacet, yearFacet, WebApiApplication.SCIENCEMAKER_FLAG, jobFacet, null, parentBiographyID);
#else
            sp.Filter = GetFilterFromFacets(genderFacet, yearFacet, makerFacet, jobFacet, null, parentBiographyID);
#endif
            if (interviewYearFilterLowerBound != 0 || interviewYearFilterUpperBound != 0)
            {
                if (sp.Filter != null) sp.Filter += " and ";

                // Case 1: upper bound given, no lower bound:
                // interviewDate le UUUU-12-31
                // Case 2: lower bound given, no upper bound:
                // interviewDate ge LLLL-01-01
                // Case 3: both bounds make sense:
                // interviewDate ge LLLL-01-01 and interviewDate le UUUU-12-31
                // where for all cases UUUU is year of upper bound, LLLL is year of lower bound.
                string interviewDateFilter = "interviewDate ";

                if (interviewYearFilterLowerBound != 0)
                {
                    interviewDateFilter += "ge " + interviewYearFilterLowerBound + "-01-01";
                    if (interviewYearFilterUpperBound != 0)
                        interviewDateFilter += " and interviewDate ";
                }
                if (interviewYearFilterUpperBound != 0)
                    interviewDateFilter += "le " + interviewYearFilterUpperBound + "-12-31";
                sp.Filter += interviewDateFilter;
            }

            if (!string.IsNullOrWhiteSpace(sortField))
            {
                string sortQualifier = sortInDescendingOrder ? sortField + " desc" : sortField + " asc";
                sp.OrderBy = new List<String> { sortQualifier };
            }

            return await storyIndex.Documents.SearchAsync(query, sp);
        }

        /// <summary>
        /// Perform a tag-based search of the story index and apply facets as given.
        /// </summary>
        /// <param name="tags">A comma-separated list of tags.</param>
        /// <param name="pageSize">Number of results to return per call.</param>
        /// <param name="currentPage">The 1-based page of results to return.</param>
        /// <param name="genderFacet">Filter stories by gender.</param>
        /// <param name="yearFacet">Filter stories by birth year.</param>
        /// <param name="makerFacet">Filter stories by comma-separated list of maker categories.</param>
        /// <param name="jobFacet">Filter stories by comma-separated list of job types.</param>
        /// <returns>A DocumentSearchResult object containing the results of the specified tag search.</returns>
        internal async Task<DocumentSearchResult> StorySearchByTags(string tags, int pageSize, int currentPage, string genderFacet, string yearFacet, string makerFacet, string jobFacet)
        {
            SearchParameters sp = new SearchParameters()
            {
                Top = pageSize,
                Skip = (currentPage - 1) * pageSize,
                IncludeTotalResultCount = true,
                Facets = StorySearchFacets,
                Select = StreamlinedStoryFieldsSet
            };

#if SCIENCEMAKERSONLY
            sp.Filter = GetFilterFromFacets(genderFacet, yearFacet, WebApiApplication.SCIENCEMAKER_FLAG, jobFacet, null, null, tags);
#else
            sp.Filter = GetFilterFromFacets(genderFacet, yearFacet, makerFacet, jobFacet, null, null, tags);
#endif
            return await storyIndex.Documents.SearchAsync("*", sp);
        }

        /// <summary>
        /// Return the count of stories matching each tag filtered by the given list of tags.
        /// </summary>
        /// <param name="tags">A comma separate list of tags.</param>
        /// <returns>A DocumentSearchResult object containing the results of the search operation.</returns>
        internal async Task<DocumentSearchResult> StorySearchTagCounts(string tags)
        {
            // Per documentation: set the requested number of values higher
            // that the number of existing tag values to force Azure Search
            // to do a deep search resulting in more accurate counts.
            // See: Make sure you get accurate facet counts
            // https://docs.microsoft.com/en-us/azure/search/search-faceted-navigation
            var tagFacet = new List<string> { "tags, count:200" };

            SearchParameters sp = new SearchParameters()
            {
                IncludeTotalResultCount = true,
                Top = 0,                            // We don't need documents, just the tag (facet) counts.
                Facets = tagFacet,                  // We only need the Tags facet for counts
                Select = new List<string> { }       // We don't need documents returned.                    
            };

            sp.Filter = GetFilterFromTagList(tags);

            return await storyIndex.Documents.SearchAsync("*", sp);
        }

        /// <summary>
        /// Get the story documents corresponding to the given list.
        /// </summary>
        /// <param name="storyIDs">A list of StoryIDs to be retrieved.</param>
        /// <param name="genderFacet">Filters results based on given gender.</param>
        /// <param name="yearFacet">Filters results based on given birth year.</param>
        /// <param name="makerFacet">Filters results based on the given comma-separated list of maker categories.</param>
        /// <param name="jobFacet">Filters results based on the given comma-separated list of job types.</param>
        /// <returns>A DocumentSearchResult object containing an ordered list of stories.</returns>
        internal async Task<DocumentSearchResult> StorySet(List<string> storyIDs, string genderFacet, string yearFacet, string makerFacet, string jobFacet)
        {

            SearchParameters sp = new SearchParameters()
            {
                SearchMode = SearchMode.Any,
                SearchFields = new List<string> { "storyID" },
                Top = MAX_PAGE_SIZE,
                IncludeTotalResultCount = true,
                Facets = StorySearchFacets,
                Select = StreamlinedStoryFieldsSet                    
            };

#if SCIENCEMAKERSONLY
            sp.Filter = GetFilterFromFacets(genderFacet, yearFacet, WebApiApplication.SCIENCEMAKER_FLAG, jobFacet);
#else
            sp.Filter = GetFilterFromFacets(genderFacet, yearFacet, makerFacet, jobFacet);
#endif
            var query = String.Join(" ", storyIDs);
            var searchResult = await storyIndex.Documents.SearchAsync(query, sp);

            // Reordering the results per the given list ordering using LINQ:
            // http://stackoverflow.com/questions/3945935/sort-one-list-by-another
            var orderedByIDList = from storyID in storyIDs
                                  join result in searchResult.Results
                                  on storyID equals result.Document["storyID"]
                                  select result;

            searchResult.Results = orderedByIDList.ToList();
            return searchResult;
        }
#endregion == STORY RELATED METHODS

#region ===== ADDITIONAL SEARCH METHODS
        /// <summary>
        /// Perform language analysis (tokenization, lemmatization, etc...) on the given text.
        /// </summary>
        /// <param name="text">The text to be analyzed.</param>
        /// <returns>The AnalyzeResult instance returned by the API on success; null on failure.</returns>
        internal async Task<AnalyzeResult> AnalyzeText(string text)
        {
            return await serviceClient.Indexes.AnalyzeAsync(STORY_INDEX, new AnalyzeRequest(text, AnalyzerName.EnMicrosoft));
        }

        /// <summary>
        /// Returns summary information used on the homepage.
        /// </summary>
        /// <returns>A HomePageInfo object with the current index counts.</returns>
        internal async Task<HomePageInfo> HomePageInfo()
        {
            HomePageInfo hpi = new HomePageInfo();
            hpi.SearchServiceName = searchServiceName;
            hpi.BiographyCount = await biographyIndex.Documents.CountAsync();
            hpi.StoryCount = await storyIndex.Documents.CountAsync();

#if SCIENCEMAKERSONLY
            // NOTE:  This is NOT optimized for performance.  
            // It uses GetBiographySearch with filter to just ScienceMakers to fill BiographyCount.
            // It uses GetStorySearch with filter to just ScienceMakers to fill StoryCount.
            // It would be nicer to have more direct access, but this "filter down to just ScienceMakers" 
            // is making use of a larger corpus for convenience and simplicity of publishing 
            // one large corpus with an API-defined subsetting rather than two ("all" and "ScienceMakers").

            var result = await BiographySearch("*", 1, 1, "", "", "", WebApiApplication.SCIENCEMAKER_FLAG, "", "", "", false);
            var countOfScienceMakers = 0;
            var countOfScienceMakerStories = 0;

            if (result != null)
            {
                if (result.Count != null)
                {
                    countOfScienceMakers = Convert.ToInt32(result.Count);
                    var storyResult = await StorySearch("*", 1, 1, "", "all", 0, 0, "", "", WebApiApplication.SCIENCEMAKER_FLAG, "", "", false);
                    if (storyResult.Count != null)
                    {
                        countOfScienceMakerStories = Convert.ToInt32(storyResult.Count);
                    }
                    else
                        countOfScienceMakers = 0; // give up on errorful data (no stories, then no ScienceMakers)
                }
            }
            hpi.ScienceMakersBiographyCount = countOfScienceMakers;
            hpi.ScienceMakersStoryCount = countOfScienceMakerStories;
#endif
            return hpi;
        }
#endregion == ADDITIONAL SEARCH METHODS

#region ===== PRIVATE HELPER METHODS
        /// <summary>
        /// Formulates the Azure Search filter specification for the given set of facets.
        /// </summary>
        /// <param name="genderFacet">A singular gender specification.</param>
        /// <param name="birthYearFacet">A singular date.</param>
        /// <param name="makerFacet">A comma-separated list of maker categories.</param>
        /// <param name="jobFacet">A comma-separated list of job types.</param>
        /// <param name="lastInitialFacet">A singular letter, first letter of last name (null if not used)</param>
        /// <param name="parentBiographyID">A singular parent biography (null if not used)</param>
        /// <param name="tags">A comma-separated list of tags.</param>
        /// <returns>An ODATA compliant search filter expression as a string.</returns>
        private string GetFilterFromFacets(string genderFacet, string birthYearFacet, string makerFacet, string jobFacet, string lastInitialFacet = null, string parentBiographyID = null, string tags = null)
        {
            string filter = null;

            if (!String.IsNullOrEmpty(genderFacet))
            {
                if (filter != null) filter += " and ";
                filter += GetFilterFromGenderFacet(genderFacet);
            }

            if (!String.IsNullOrEmpty(birthYearFacet))
            {
                if (filter != null) filter += " and ";
                filter += GetFilterFromBirthYearFacet(birthYearFacet);
            }

            if (!String.IsNullOrEmpty(makerFacet))
            {
                if (filter != null) filter += " and ";
                filter += GetFilterFromMakerFacet(makerFacet);
            }

            if (!String.IsNullOrEmpty(jobFacet))
            {
                if (filter != null) filter += " and ";
                filter += GetFilterFromJobFacet(jobFacet);
            }

            if (!String.IsNullOrEmpty(lastInitialFacet))
            {
                if (filter != null) filter += " and ";
                filter += GetFilterFromLastInitialFacet(lastInitialFacet);
            }

            if (!String.IsNullOrEmpty(parentBiographyID))
            {
                if (filter != null) filter += " and ";
                filter += GetFilterFromParentBiography(parentBiographyID);
            }

            if (!String.IsNullOrEmpty(tags))
            {
                if (filter != null) filter += " and ";
                filter += GetFilterFromTagList(tags);
            }

            return filter;
        }

        /// <summary>
        /// Formulates an ODATA search filter expression for the given last name opening character.
        /// </summary>
        /// <param name="lastInitialFacet">A singular opening character for last name specifier.</param>
        /// <returns>An ODATA compliant search filter expression as a string.</returns>
        private string GetFilterFromLastInitialFacet(string lastInitialFacet)
        {
            if (string.IsNullOrEmpty(lastInitialFacet))
            {
                return null;
            }
            else
            {
                return string.Format("lastInitial eq '{0}'", lastInitialFacet);
            }
        }

        /// <summary>
        /// Formulates an ODATA search filter expression for the given gender.
        /// </summary>
        /// <param name="genderFacet">A singular gender specifier.</param>
        /// <returns>An ODATA compliant search filter expression as a string.</returns>
        private string GetFilterFromGenderFacet(string genderFacet)
        {
            if (string.IsNullOrEmpty(genderFacet))
            {
                return null;
            }
            else
            {
                return string.Format("gender eq '{0}'", genderFacet);
            }            
        }

        /// <summary>
        /// Formulates an ODATA search filter expression for the given year.
        /// </summary>
        /// <param name="birthYearFacet">A singular year as a four-digit string.</param>
        /// <returns>An ODATA compliant search filter expression as a string.</returns>
        private string GetFilterFromBirthYearFacet(string birthYearFacet)
        {
            if (string.IsNullOrEmpty(birthYearFacet))
            {
                return null;
            }
            else
            {
                // NOTE: this ASSUMES that we have interval:10 for birthYear in BiographySearchFacets, StorySearchFacets, or whatever
                // facet set is used to cluster birth years into decades.
                // Hence, if a decade is picked, any year in that range of [X, X+10) is valid.  Don't match X+10: use less than!
                return string.Format("birthYear ge {0} and birthYear lt {1}", birthYearFacet, Convert.ToInt32(birthYearFacet) + 10);
            }
        }

        /// <summary>
        /// Formulates an ODATA search filter expression for the given list of maker categories.
        /// </summary>
        /// <param name="makerFacet">A comma-separated list of maker categories.</param>
        /// <returns>An ODATA compliant search filter expression as a string.</returns>
        private string GetFilterFromMakerFacet(string makerFacet)
        {
            if (string.IsNullOrEmpty(makerFacet))
            {
                return null;
            }
            else
            {
                var expressions = new List<string>();

                var values = makerFacet.Split(',');
                foreach (var value in values)
                {
                    if (!String.IsNullOrEmpty(value))
                        expressions.Add(string.Format("makerCategories/any(c: c eq '{0}')", value));
                }

                return string.Join(" and ", expressions);
            }
        }

        /// <summary>
        /// Formulates an ODATA search filter expression for the given list of job types.
        /// </summary>
        /// <param name="jobFacet">A comma-separated list of job types.</param>
        /// <returns>An ODATA compliant search filter expression as a string.</returns>
        private string GetFilterFromJobFacet(string jobFacet)
        {
            if (string.IsNullOrEmpty(jobFacet))
            {
                return null;
            }
            else
            {
                var expressions = new List<string>();

                var values = jobFacet.Split(',');
                foreach (var value in values)
                {
                    if (!String.IsNullOrEmpty(value))
                        expressions.Add(string.Format("occupationTypes/any(o: o eq '{0}')", value));
                }

                return string.Join(" and ", expressions);
            }
        }

        /// <summary>
        /// Formulates an ODATA search filter expression for the given biography ID.
        /// </summary>
        /// <param name="parentBiographyID">A single biography ID.</param>
        /// <returns>An ODATA compliant search filter expression as a string.</returns>
        private string GetFilterFromParentBiography(string parentBiographyID)
        {
            if (string.IsNullOrEmpty(parentBiographyID))
            {
                return null;
            }
            else
            {
                return string.Format("biographyID eq '{0}'", parentBiographyID);
            }
        }

        /// <summary>
        /// Formulates an ODATA search filter expression for the given list of tags.
        /// </summary>
        /// <param name="tags">A comma-separated list of tags.</param>
        /// <returns>An ODATA compliant search filter expression as a string.</returns>
        private string GetFilterFromTagList(string tags)
        {
            if (string.IsNullOrEmpty(tags))
            {
                return null;
            }
            else
            {
                var expressions = new List<string>();

                var values = tags.Split(',');
                foreach (var value in values)
                {
                    if (!String.IsNullOrEmpty(value))
                        expressions.Add(string.Format("tags/any(t: t eq '{0}')", value));
                }

                return string.Join(" and ", expressions);
            }
        }

        /// <summary>
        /// Logs the given exception.
        /// </summary>
        /// <param name="ex">An Exception object.</param>
        private void LogException(Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("--------------------------------------------------------------------------------");
            System.Diagnostics.Debug.WriteLine("An error occurred in {0}:\n", ex.TargetSite);
            System.Diagnostics.Debug.WriteLine("Message:\n{0}\n", ex.Message);
            System.Diagnostics.Debug.WriteLine("Inner Exception:\n{0}\n", ex.InnerException.Message);
            System.Diagnostics.Debug.WriteLine("Stack Trace:\n{0}", ex.StackTrace);
            System.Diagnostics.Debug.WriteLine("--------------------------------------------------------------------------------");
        }
#endregion == PRIVATE HELPER METHODS
    }
}