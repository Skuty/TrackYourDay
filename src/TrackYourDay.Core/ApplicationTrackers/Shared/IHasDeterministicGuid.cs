namespace TrackYourDay.Core.ApplicationTrackers.Shared
{
    /// <summary>
    /// Marker interface for activities with deterministic GUID generation.
    /// </summary>
    public interface IHasDeterministicGuid
    {
        /// <summary>
        /// Gets the deterministic GUID based on upstream identifier.
        /// </summary>
        Guid Guid { get; }
        
        /// <summary>
        /// Gets the upstream identifier used for GUID generation.
        /// </summary>
        string UpstreamId { get; }
    }
}
