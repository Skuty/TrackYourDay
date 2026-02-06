namespace TrackYourDay.Core.Persistence.Specifications
{
    /// <summary>
    /// Combines two specifications with AND logic.
    /// Both specifications must be satisfied for the entity to pass.
    /// </summary>
    public class AndSpecification<T> : ISpecification<T> where T : class
    {
        private readonly ISpecification<T> left;
        private readonly ISpecification<T> right;

        public AndSpecification(ISpecification<T> left, ISpecification<T> right)
        {
            this.left = left ?? throw new ArgumentNullException(nameof(left));
            this.right = right ?? throw new ArgumentNullException(nameof(right));
        }

        public string GetSqlWhereClause()
        {
            var leftClause = left.GetSqlWhereClause();
            var rightClause = right.GetSqlWhereClause();
            
            return $"({leftClause}) AND ({rightClause})";
        }

        public Dictionary<string, object> GetSqlParameters()
        {
            var parameters = new Dictionary<string, object>();
            
            foreach (var param in left.GetSqlParameters())
            {
                parameters[param.Key] = param.Value;
            }
            
            foreach (var param in right.GetSqlParameters())
            {
                parameters[param.Key] = param.Value;
            }
            
            return parameters;
        }

        public bool IsSatisfiedBy(T entity)
        {
            return left.IsSatisfiedBy(entity) && right.IsSatisfiedBy(entity);
        }
    }
}
