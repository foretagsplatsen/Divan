namespace Divan
{
    public interface IReconcilingDocument: ICouchDocument
    {
        ReconcileStrategy ReconcileBy { get; set; }
        void SaveCommited();

        /// <summary>
        /// Called by the runtime when a conflict is detected during save. The supplied parameter
        /// is the database copy of the document being saved.
        /// </summary>
        /// <param name="databaseCopy"></param>
        void Reconcile(ICouchDocument databaseCopy);

        IReconcilingDocument GetDatabaseCopy(ICouchDatabase db);
    }
}