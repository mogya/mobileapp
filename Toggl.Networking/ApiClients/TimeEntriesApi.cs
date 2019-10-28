using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using Toggl.Networking.Exceptions;
using Toggl.Networking.Models;
using Toggl.Networking.Network;
using Toggl.Networking.Serialization;
using Toggl.Shared.Models;

namespace Toggl.Networking.ApiClients
{
    internal sealed class TimeEntriesApi : BaseApi, ITimeEntriesApi
    {
        private readonly UserAgent userAgent;
        private readonly TimeEntryEndpoints endPoints;

        public TimeEntriesApi(Endpoints endPoints, IApiClient apiClient, IJsonSerializer serializer,
            Credentials credentials, UserAgent userAgent)
            : base(apiClient, serializer, credentials, endPoints.LoggedIn)
        {
            this.userAgent = userAgent;
            this.endPoints = endPoints.TimeEntries;
        }

        public IObservable<List<ITimeEntry>> GetAll()
            => SendRequest<TimeEntry, ITimeEntry>(endPoints.Get, AuthHeader);

        public IObservable<List<ITimeEntry>> GetAll(DateTimeOffset start, DateTimeOffset end)
        {
            if (start > end)
                throw new InvalidOperationException($"Start date ({start}) must be earlier than the end date ({end}).");

            return SendRequest<TimeEntry, ITimeEntry>(endPoints.GetBetween(start, end), AuthHeader)
                .Select(timeEntries => timeEntries ?? new List<ITimeEntry>());
        }

        public IObservable<List<ITimeEntry>> GetAllSince(DateTimeOffset threshold)
            => SendRequest<TimeEntry, ITimeEntry>(endPoints.GetSince(threshold), AuthHeader);

        public IObservable<ITimeEntry> Create(ITimeEntry timeEntry)
            => pushTimeEntry(endPoints.Post(timeEntry.WorkspaceId), timeEntry, SerializationReason.Post);

        public IObservable<ITimeEntry> Update(ITimeEntry timeEntry)
            => pushTimeEntry(endPoints.Put(timeEntry.WorkspaceId, timeEntry.Id), timeEntry, SerializationReason.Default);

        public IObservable<Unit> Delete(ITimeEntry timeEntry)
            => SendRequest<ITimeEntry>(endPoints.Delete(timeEntry.WorkspaceId, timeEntry.Id), AuthHeader)
                .SingleAsync()
                .Select(_ => Unit.Default)
                .Catch<Unit, NotFoundException>(_ => Observable.Return(Unit.Default));

        private IObservable<ITimeEntry> pushTimeEntry(Endpoint endPoint, ITimeEntry timeEntry, SerializationReason reason)
        {
            var timeEntryCopy = timeEntry as TimeEntry ?? new TimeEntry(timeEntry);
            if (reason == SerializationReason.Post)
            {
                timeEntryCopy.CreatedWith = userAgent.ToString();
            }

            var observable = SendRequest(endPoint, AuthHeader, timeEntryCopy, reason);
            return observable;
        }
    }
}
