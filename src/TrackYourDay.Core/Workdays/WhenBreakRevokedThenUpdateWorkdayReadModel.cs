﻿using MediatR;
using TrackYourDay.Core.Breaks.Events;

namespace TrackYourDay.Core.Workdays
{
    internal class WhenBreakRevokedThenUpdateWorkdayReadModel 
        : INotificationHandler<BreakRevokedEvent>
    {
        private readonly WorkdayReadModelRepository workdayReadModelRepository;

        public WhenBreakRevokedThenUpdateWorkdayReadModel(WorkdayReadModelRepository workdayReadModelRepository)
        {
            this.workdayReadModelRepository = workdayReadModelRepository;
        }

        public Task Handle(BreakRevokedEvent notification, CancellationToken cancellationToken)
        {
            var workday = this.workdayReadModelRepository.Get(DateOnly.FromDateTime(DateTime.Today));
            var newWorkday = workday.Include(notification.RevokedBreak);
            this.workdayReadModelRepository.AddOrUpdate(newWorkday);

            return Task.CompletedTask;
        }
    }
}
