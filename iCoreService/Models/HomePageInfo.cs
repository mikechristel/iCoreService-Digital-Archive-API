namespace iCoreService.Models
{
    /// <summary>
    /// Information used by the home view.
    /// </summary>
    public class HomePageInfo
    {
        /// <summary>
        /// The name of the Azure Search service being used by the web api.
        /// </summary>
        /// <remarks>
        /// For diagnostic purposes.
        /// </remarks>
        public string SearchServiceName { get; set; }

        /// <summary>
        /// The number of biographies in the corpus.
        /// </summary>
        public long BiographyCount { get; set; }

        /// <summary>
        /// The number of stories in the corpus.
        /// </summary>
        public long StoryCount { get; set; }
#if SCIENCEMAKERSONLY
        /// <summary>
        /// The number of ScienceMakers biographies in the corpus.
        /// </summary>
        public long ScienceMakersBiographyCount { get; set; }

        /// <summary>
        /// The number of stories from ScienceMakers in the corpus.
        /// </summary>
        public long ScienceMakersStoryCount { get; set; }
#endif
    }
}