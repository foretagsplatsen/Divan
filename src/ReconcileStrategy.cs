namespace Divan
{
    /// <summary>
    /// Conflict reconcilliation strategies supported by Divan
    /// </summary>
    public enum ReconcileStrategy
    {
        /// <summary>
        /// If a conflict occurs, the exception is propagated to the application
        /// </summary>
        None,
        /// <summary>
        /// The document will retain additional state information to identify which fields
        /// have been modified since the last save, and use that to merge with the database
        /// copy. The merge will be performed automatically, by reflecting over all public
        /// and non-public fields and properties of the document.
        /// </summary>
        AutoMergeFields,
        /// <summary>
        /// The document will retain additional state information to identify which fields
        /// have been modified since the last save, and use that to merge with the database
        /// copy. The merge will be delegated to the document instance, by invoking the 
        /// Reconcile() method with the database document.
        /// </summary>
        ManualMergeFields
    }
}