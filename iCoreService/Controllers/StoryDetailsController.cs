using System;
using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using System.Web.Http.Description;
using System.Threading.Tasks;

using Microsoft.Azure.Search.Models;

using iCoreService.AzureServices;
using iCoreService.Models;

using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace iCoreService.Controllers
{
    /// <summary>
    /// Provides data to the story details (aka video) view.
    /// </summary>
    public class StoryDetailsController : ApiController
    {
        private AzureSearch azureSearch = new AzureSearch();
        private AzureStorage azureStorage = new AzureStorage();

        /// <summary>
        /// Get full details for the specified story.
        /// </summary>
        /// <param name="storyID">A story identifier.</param>
        /// <param name="queryTerms">A list of terms to match against the transcript.</param>
        /// <returns>
        /// The StoryDetails document for the requested story.
        /// </returns>
        [ResponseType(typeof(StoryDetails))]
        public async Task<HttpResponseMessage> GetStoryDetails(string storyID = "", string queryTerms = "")
        {
            if (string.IsNullOrEmpty(storyID))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }

            try
            {
                var result = await azureStorage.GetStoryDetails(storyID);

#if SCIENCEMAKERSONLY
                if (result != null)
                {
                    result.BlobStream.Position = 0;
                    var reader = new StreamReader(result.BlobStream);

                    JsonSerializer serializer = new JsonSerializer();
                    StoryDetails singleStoryDetails = (StoryDetails)serializer.Deserialize(reader, typeof(StoryDetails));
                    // "Eat" the result unless its makers include WebApiApplication.SCIENCEMAKER_FLAG which is the code for ScienceMakers
                    if (singleStoryDetails != null)
                    {
                        var isScienceMaker = false;
                        var makerList = singleStoryDetails.MakerCategories;
                        for (int makerIndex = 0; makerIndex < makerList.Length && !isScienceMaker; makerIndex++)
                        {
                            if (makerList[makerIndex] == WebApiApplication.SCIENCEMAKER_FLAG)
                                isScienceMaker = true; // story is from a confirmed ScienceMaker and can be returned by the call
                        }
                        if (!isScienceMaker)
                        {
                            result = null; // forget this non-ScienceMaker story
                        }
                    }
                }
#endif

                if (result == null)
                {
                    return new HttpResponseMessage(HttpStatusCode.NotFound);
                }

                result.BlobStream.Position = 0;

                if (String.IsNullOrEmpty(queryTerms))
                {
                    // No query terms present so return the raw json...
                    var response = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StreamContent(result.BlobStream)
                    };

                    response.Content.Headers.ContentLength = result.BlobLength;
                    response.Content.Headers.ContentType = new MediaTypeHeaderValue(result.BlobContentType);
                    response.Headers.CacheControl = new CacheControlHeaderValue() { MaxAge = TimeSpan.FromDays(1), Public = true };

                    return response;
                }
                else
                {
                    // Query terms given, deserialize the story details and append the match terms.
                    var reader = new StreamReader(result.BlobStream);

                    JsonSerializer serializer = new JsonSerializer();
                    StoryDetails storyDetails = (StoryDetails)serializer.Deserialize(reader, typeof(StoryDetails));

                    storyDetails.MatchTerms = await FindMatches(queryTerms, storyDetails.Transcript);

                    var response = Request.CreateResponse(HttpStatusCode.OK, storyDetails, Configuration.Formatters.JsonFormatter);
                    response.Headers.CacheControl = new CacheControlHeaderValue() { MaxAge = TimeSpan.FromDays(1), Public = true };

                    return response;
                }
            }
            catch (Exception ex)
            {
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content = new StringContent(ex.Message)
                };
            }
        }

        /// <summary>
        /// Utility function to remove quoted phrases from a given string and put them in the list parameter,
        /// returning what's left of the source with phrases all removed.
        /// </summary>
        /// <param name="origSource">source string, such as: football "Buffalo Bills"</param>
        /// <param name="phrases">list of phrases found excluding negated phrases, e.g., for given source (football "Buffalo Bills") would return 1 phrase "Buffalo Bills"</param>
        /// <returns>phrase with all quoted strings removed, e.g., football returned for input of: football "Buffalo Bills"</returns>
        /// <remarks>Negated phrases such as ...football -"Buffalo Bills" ...are removed with no notice back to caller; the preceding - is taken out of the returned processed string as well.</remarks>
        private string HandlePhraseExtractionWithNegation(string origSource, List<string> phrases)
        {
            var retVal = "";
            phrases.Clear();
            var workVal = 0;
            int openingPhraseMarker, endingPhraseMarker;
            string foundPhrase;
            bool isNegatedPhrase;
            string testChar;

            while (workVal < origSource.Length) {
                openingPhraseMarker = origSource.IndexOf("\"", workVal);
                if (openingPhraseMarker >= 0)
                { // found first "
                    endingPhraseMarker = origSource.IndexOf("\"", openingPhraseMarker + 1);
                    if (endingPhraseMarker >= 0)
                    {
                        // NOTE: the query -"Buffalo Bills" will return all documents that do NOT have the phrase Buffalo Bills,
                        // i.e., it is a negated term query.  So, as a special case, see if the character before the opening " was a -,
                        // and the character before that a term-break character of start of document or ' ' or '\t' or similar.
                        // If so, do not include negated phrases in the returned phrases list.
                        isNegatedPhrase = ((openingPhraseMarker > 0) && origSource.Substring(openingPhraseMarker - 1, 1) == "-");
                        if (isNegatedPhrase)
                        {
                            // Confirm by finding a term break character right before.  
                            // What is a term break character?  This question is also in FindMatches().
                            // TODO: Not sure what to do about the operator characters +|() ...for now, treating them as query term separators.
                            if (openingPhraseMarker > 1)
                            {
                                testChar = origSource.Substring(openingPhraseMarker - 2, 1);
                                isNegatedPhrase = (testChar == " " || testChar == "\t" || testChar == "\n" || testChar == "\r" || testChar == "+" || testChar == "|" || testChar == "(" || testChar == ")");
                            }
                        }
                        // else if string opens with -"something" then it is a negated phrase, keep isNegatedPhrase as true

                        if (!isNegatedPhrase)
                        {
                            // Found a phrase!
                            foundPhrase = origSource.Substring(openingPhraseMarker + 1, endingPhraseMarker - openingPhraseMarker - 1).Trim();
                            if (foundPhrase.Length > 0)
                                phrases.Add(foundPhrase); // if phrase-neatening were needed, could do: Regex.Replace(foundPhrase, @"\s+", " ")); // collapse multiple spaces between phrase words to a single one as necessary
                            if (openingPhraseMarker - 1 > workVal)
                                retVal += origSource.Substring(workVal, openingPhraseMarker - 1 - workVal); // save part of source before opening "
                        }
                        else
                        {
                            // Drop opening -" rather than just the opening " for negated phrases.
                            if (openingPhraseMarker - 2 > workVal)
                                retVal += origSource.Substring(workVal, openingPhraseMarker - 2 - workVal); // save part of source before opening -"
                        }
                        workVal = endingPhraseMarker + 1; // continue after the end of the phrase
                    }
                    else
                    {
                        // if only 1 unpaired " is found, ignore it.  Exit loop.
                        break;
                    }
                }
                else
                {
                    // No (more) " found; exit loop.
                    break;
                }
            }
            if (workVal < origSource.Length)
                // Tack on to our saved source string with the phrases extracted.
                retVal += origSource.Substring(workVal, origSource.Length - workVal);
            return retVal;
        }

        /// <summary>
        /// Looks for instances of query terms within the given transcript text.
        /// </summary>
        /// <param name="queryTerms">A string of query terms separated by whitespace.</param>
        /// <param name="transcriptText">The segment's transcript text.</param>
        /// <returns>A list of MatchTerm instances on success; null otherwise.</returns>
        /// <remarks>Wilcard after a prefix (term*), negated term (-term) and phrase search ("phraseword1 phraseword2") all supported.</remarks>
        private async Task<List<MatchTerm>> FindMatches(string queryTerms, string transcriptText)
        {
            var matches = new List<MatchTerm>();
            // If query and/or transcript are empty, no work is needed: there are no matches.
            if (queryTerms != null && queryTerms.Trim().Length > 0 && transcriptText != null && transcriptText.Trim().Length > 0)
            {
                // In the Lucene-based simple query parser syntax, "-" at the start of a term negates that term,
                // e.g., buffalo -soldier matches buffalo and not soldier (with default and'ing of terms active).
                // Do not analyze any terms starting with - (as they won't match anyway, but save a bit of work).
                //
                // More importantly, if a term ends with *, do a prefix search, e.g., buff* matches buffalo, buffet, etc.
                // Handle prefix searching in its own loop below.
                //
                // Also, do not tokenize and expand out terms within a phrase.  Instead, just match the phrase in the transcript.

                var prefixTerms = new List<string>();
                var phraseTerms = new List<string>();

                string cleanedQueryTerms = "";

                if (!queryTerms.Contains("*") && !queryTerms.Contains("-") && !queryTerms.Contains("\""))
                {
                    // a quick check that there are no prefix wildcard searches with * and no negated terms with - and no phrases with "
                    cleanedQueryTerms = queryTerms;
                }
                else
                { // process query with its potential use of negated search, prefix/wildcard search, and/or phrase search

                    // TODO: By being very literal with phrase search, we will match "real union hall" if those 3 words are in order in a transcript,
                    // and will not match "real 7union hall" if that phrase is not, but we mess up on whatever Azure Search is doing with a query
                    // like "real &union hall" - more tuning would be necessary to accommodate unusual input cases.
                    queryTerms = HandlePhraseExtractionWithNegation(queryTerms, phraseTerms);

                    // TODO: Not sure what to do about the operator characters +|() ...for now, treating them as query term separators.
                    char[] termSplitters = { ' ', '\t', '\n', '\r', '+', '|', '(', ')' };

                    var listOfQueryTerms = queryTerms.Split(termSplitters, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var term in listOfQueryTerms)
                    {
                        if (!term.StartsWith("-"))
                        {
                            if (term.EndsWith("*"))
                            {
                                prefixTerms.Add(term.Substring(0, term.Length - 1)); // add term without its ending "*" as a prefix to match later
                            }
                            else
                            {
                                cleanedQueryTerms += " " + term; // Note: extra " " at start of non-empty cleanedQueryTerms will do no harm later
                            }
                        }
                        // else do not consider a negated term further.
                    }
                }

                // Find and sort all the tokens which match with the query terms (or match the start of the prefix terms) (or match a phrase in phrase list).
                var tokens = new SortedList<int, TokenInfo>();

                if (phraseTerms.Count > 0)
                {
                    // Also find all the prefix matches
                    char[] phraseTermSplitters = { ' ' }; // based on how phrases are put together (in HandlePhraseExtractionWithNegation) this is kept simple

                    string[] listOfPhraseTerms;
                    string patternToMatchForPhrase;
                    foreach (var phrase in phraseTerms)
                    {
                        // TODO: This exact matching is likely more strict than what Azure Search allows.  The phrase words need to be 
                        // in order, but likely they can be separated by 1 or more term break characters rather than exactly one space,
                        // as is stored in phraseTerms via HandlePhraseExtractionWithNegation().
                        // What is done here: take phrase, break it into individual words.  Use RegEx to match those words in order separated 
                        // by one or more non-word characters (\W+).  
                        listOfPhraseTerms = phrase.Split(phraseTermSplitters, StringSplitOptions.RemoveEmptyEntries);
                        patternToMatchForPhrase = "";
                        foreach (var phraseWord in listOfPhraseTerms)
                            patternToMatchForPhrase += phraseWord + @"\W+";
                        // Get a collection of matches.
                        MatchCollection matchesToPhrase = Regex.Matches(transcriptText, patternToMatchForPhrase, RegexOptions.IgnoreCase);

                        foreach (Match match in matchesToPhrase)
                        {
                            foreach (Capture capture in match.Captures)
                            {
                                Console.WriteLine("Index={0}, Value={1}", capture.Index, capture.Value);
                                tokens.Add(capture.Index, new TokenInfo(capture.Value, capture.Index, capture.Index + capture.Length - 1));
                            }
                        }
                    }
                }

                // NOTE: there might only be a phrase search;
                // if so, there will be no prefix terms and no terms to search, and hence no need for a transcript tokenizing, either.
                // Only tokenize if there are follow-up steps.
                if (prefixTerms.Count > 0 || cleanedQueryTerms.Length > 0)
                {
                    AnalyzeResult transcriptAnalysis = await azureSearch.AnalyzeText(transcriptText);

                    // Each word in the transcript may contain multiple tokens, for instance
                    // running becomes both running and run.  Colored becomes colored, color, and colour.
                    // This dictionary will aggregate like tokens together for faster recall.
                    var dictionary = new Dictionary<string, List<TokenInfo>>();
                    foreach (var t in transcriptAnalysis.Tokens)
                    {
                        if (dictionary.ContainsKey(t.Token))
                        {
                            dictionary[t.Token].Add(t);
                        }
                        else
                        {
                            dictionary.Add(t.Token, new List<TokenInfo> { t });
                        }
                    }

                    if (cleanedQueryTerms.Length > 0)
                    {
                        AnalyzeResult queryTermsAnalysis = await azureSearch.AnalyzeText(cleanedQueryTerms);
                        // Find and sort all the tokens which match with the query terms.  We only care about
                        // where the match occurred so ignore additional word forms that match with a given position.
                        foreach (var q in queryTermsAnalysis.Tokens)
                        {
                            if (dictionary.ContainsKey(q.Token))
                            {
                                // Put each new instance of a word into the list of matching tokens.
                                foreach (TokenInfo t in dictionary[q.Token])
                                {
                                    // Check if there is already a match at this transcript position first...
                                    int position = t.Position ?? 0;
                                    if (!tokens.ContainsKey(position))
                                    {
                                        tokens.Add(position, t);
                                    }
                                }
                            }
                        }
                    }

                    // Also find all the prefix matches
                    foreach (var prefix in prefixTerms)
                    {
                        foreach (var k in dictionary.Keys)
                        {
                            if (k.StartsWith(prefix))
                            {
                                // Put each new instance of a word into the list of matching tokens.
                                foreach (TokenInfo t in dictionary[k])
                                {
                                    // Check if there is already a match at this transcript position first...
                                    int position = t.Position ?? 0;
                                    if (!tokens.ContainsKey(position))
                                    {
                                        tokens.Add(position, t);
                                    }
                                }
                            }
                        }
                    }
                }
                // Convert the list of tokens into the the proper return type.
                foreach (var t in tokens)
                {
                    matches.Add(new MatchTerm
                    {
                        StartOffset = t.Value.StartOffset ?? 0,
                        EndOffset = t.Value.EndOffset ?? 0
                    });
                }
            }

            return matches;
        }
    }
}
