﻿namespace TrackYourDay.Core.Activities
{
    public interface IStartedActivityRecognizingStrategy
    {
        public ActivityType RecognizeActivity();
    }
}