namespace iCoreService.Models
{
    /// <summary>
    /// A heirarchical list of tags used by the search index.
    /// </summary>
    public class TagTree
    {
        /// <summary>
        /// All the branches within the tag heirarchy.
        /// </summary>
        public TagBranch[] Branches { get; set; }
    }

    /// <summary>
    /// A branch within the tag heirarchy.
    /// </summary>
    public class TagBranch
    {
        /// <summary>
        /// The branch identifier.
        /// </summary>
        public string BranchID { get; set; }

        /// <summary>
        /// The name of this branch.
        /// </summary>
        public string BranchName { get; set; }

        /// <summary>
        /// The tags contained within this branch.
        /// </summary>
        public BranchValue[] BranchValues { get; set; }
    }

    /// <summary>
    /// A single tag within the heirarchy.
    /// </summary>
    public class BranchValue
    {
        /// <summary>
        /// The tag identifier as used by the search index.
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// The human readable label for this tag.
        /// </summary>
        public string Label { get; set; }
    }
}