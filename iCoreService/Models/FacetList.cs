namespace iCoreService.Models
{
    /// <summary>
    /// A list of facets used by the search index.
    /// </summary>
    public class FacetList
    {
        /// <summary>
        /// Full list of maker category facet values.
        /// </summary>
        public FacetValue[] MakerCategories { get; set; }

        /// <summary>
        /// Full list of occupation type facet values.
        /// </summary>
        public FacetValue[] OccupationTypes { get; set; }
    }

    /// <summary>
    /// Represents a single facet value.
    /// </summary>
    public class FacetValue
    {
        /// <summary>
        /// The identifier for this facet used by the search index.
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// A short UI label for this value.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// The full description of this value.
        /// </summary>
        public string Description { get; set; }
    }
}