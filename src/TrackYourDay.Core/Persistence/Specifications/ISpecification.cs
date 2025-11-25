namespace TrackYourDay.Core.Persistence.Specifications
{
    /// <summary>
    /// Base specification interface for filtering entities.
    /// Implements the Specification Pattern for flexible querying.
    /// </summary>
    /// <typeparam name="T">The entity type to filter</typeparam>
    public interface ISpecification<T> where T : class
    {
        /// <summary>
        /// Gets the SQL WHERE clause condition for database queries using JSON extraction.
        /// </summary>
        string GetSqlWhereClause();

        /// <summary>
        /// Gets the SQL parameters for the WHERE clause.
        /// </summary>
        Dictionary<string, object> GetSqlParameters();

        /// <summary>
        /// Evaluates whether an entity satisfies the specification (for in-memory filtering).
        /// </summary>
        bool IsSatisfiedBy(T entity);
    }
}
