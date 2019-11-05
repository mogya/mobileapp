using System;
using System.Collections.Generic;
using System.Reactive;
using Toggl.Core.Analytics;
using Toggl.Core.DTOs;
using Toggl.Core.Interactors.Generic;
using Toggl.Core.Models;
using Toggl.Core.Models.Interfaces;
using Toggl.Core.Suggestions;
using Toggl.Storage.Models;

namespace Toggl.Core.Interactors
{
    public sealed partial class InteractorFactory : IInteractorFactory
    {
        public IInteractor<IObservable<IThreadSafeTimeEntry>> CreateTimeEntry(ITimeEntryPrototype prototype, TimeEntryStartOrigin origin)
            => new CreateTimeEntryInteractor(
                idProvider,
                timeService,
                dataSource,
                analyticsService,
                prototype,
                syncManager,
                prototype.StartTime,
                prototype.Duration,
                origin);

        public IInteractor<IObservable<IThreadSafeTimeEntry>> ContinueTimeEntry(ITimeEntryPrototype prototype, ContinueTimeEntryMode continueMode)
            => new CreateTimeEntryInteractor(
                idProvider,
                timeService,
                dataSource,
                analyticsService,
                prototype,
                syncManager,
                timeService.CurrentDateTime,
                null,
                (TimeEntryStartOrigin)continueMode);

        public IInteractor<IObservable<IThreadSafeTimeEntry>> ContinueTimeEntryFromMainLog(ITimeEntryPrototype prototype,
            ContinueTimeEntryMode continueMode, int indexInLog, int dayInLog, int daysInThePast)
            => new ContinueTimeEntryFromMainLogInteractor(
                this,
                analyticsService,
                prototype,
                continueMode,
                indexInLog,
                dayInLog,
                daysInThePast);

        public IInteractor<IObservable<IThreadSafeTimeEntry>> StartSuggestion(Suggestion suggestion)
            => new CreateTimeEntryInteractor(
                idProvider,
                timeService,
                dataSource,
                analyticsService,
                suggestion,
                syncManager,
                timeService.CurrentDateTime,
                null,
            TimeEntryStartOrigin.Suggestion);

        public IInteractor<IObservable<IThreadSafeTimeEntry>> ContinueMostRecentTimeEntry()
            => new ContinueMostRecentTimeEntryInteractor(
                idProvider,
                timeService,
                dataSource,
                analyticsService,
                syncManager);

        public IInteractor<IObservable<Unit>> DeleteTimeEntry(long id)
            => new DeleteTimeEntryInteractor(timeService, dataSource.TimeEntries, this, id);

        public IInteractor<IObservable<Unit>> DeleteMultipleTimeEntries(long[] ids)
             => new DeleteMultipleTimeEntriesInteractor(dataSource.TimeEntries, this, ids);

        public IInteractor<IObservable<Unit>> SoftDeleteMultipleTimeEntries(long[] ids)
            => new SoftDeleteMultipleTimeEntriesInteractor(dataSource.TimeEntries, timeService, syncManager, this, ids);

        public IInteractor<IObservable<IThreadSafeTimeEntry>> GetTimeEntryById(long id)
            => new GetByIdInteractor<IThreadSafeTimeEntry, IDatabaseTimeEntry>(dataSource.TimeEntries, analyticsService, id)
                .TrackException<Exception, IThreadSafeTimeEntry>("GetTimeEntryById");

        public IInteractor<IObservable<IEnumerable<IThreadSafeTimeEntry>>> GetMultipleTimeEntriesById(long[] ids)
            => new GetMultipleByIdInteractor<IThreadSafeTimeEntry, IDatabaseTimeEntry>(dataSource.TimeEntries, ids);

        public IInteractor<IObservable<IEnumerable<IThreadSafeTimeEntry>>> GetAllTimeEntriesVisibleToTheUser()
            => new GetAllTimeEntriesVisibleToTheUserInteractor(dataSource.TimeEntries);

        public IInteractor<IObservable<IEnumerable<IThreadSafeTimeEntry>>> ObserveAllTimeEntriesVisibleToTheUser()
            => new ObserveAllTimeEntriesVisibleToTheUserInteractor(dataSource.TimeEntries, dataSource.Workspaces);

        public IInteractor<IObservable<IThreadSafeTimeEntry>> UpdateTimeEntry(EditTimeEntryDto dto)
            => new UpdateTimeEntryInteractor(timeService, dataSource, this, syncManager, dto);

        public IInteractor<IObservable<IEnumerable<IThreadSafeTimeEntry>>> UpdateMultipleTimeEntries(EditTimeEntryDto[] dtos)
            => new UpdateMultipleTimeEntriesInteractor(timeService, dataSource, this, syncManager, dtos);

        public IInteractor<IObservable<IThreadSafeTimeEntry>> StopTimeEntry(DateTimeOffset currentDateTime, TimeEntryStopOrigin origin)
            => new StopTimeEntryInteractor(timeService, dataSource.TimeEntries, currentDateTime, analyticsService, origin);

        public IInteractor<IObservable<Unit>> ObserveTimeEntriesChanges()
            => new ObserveTimeEntriesChangesInteractor(dataSource);

        public IInteractor<IObservable<TimeSpan>> ObserveTimeTrackedToday()
            => new ObserveTimeTrackedTodayInteractor(timeService, dataSource.TimeEntries);
    }
}
