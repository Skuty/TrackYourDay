﻿namespace TrackYourDay.Core.Breaks
{
    // TODO: Verify after time what went better, raw passed types or passed object
    public record class RevokedBreak(EndedBreak EndedBreak, DateTime BreakRevokedAt)
    {
        public Guid BreakGuid => EndedBreak.BreakGuid;
    }
}