﻿using Newtonsoft.Json;
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Toggl.Networking.Exceptions;
using Toggl.Networking.Helpers;
using Toggl.Networking.Models;
using Toggl.Networking.Network;
using Toggl.Networking.Serialization;
using Toggl.Networking.Serialization.Converters;
using Toggl.Shared;
using Toggl.Shared.Extensions;
using Toggl.Shared.Models;

namespace Toggl.Networking.ApiClients
{
    internal sealed class UserApi : BaseApi, IUserApi
    {
        private const string userAlreadyExistsApiErrorMessage = "user with this email already exists";

        private readonly UserEndpoints endPoints;
        private readonly IJsonSerializer serializer;

        public UserApi(Endpoints endPoints, IApiClient apiClient, IJsonSerializer serializer,
            Credentials credentials)
            : base(apiClient, serializer, credentials, endPoints.LoggedIn)
        {
            this.endPoints = endPoints.User;
            this.serializer = serializer;
        }

        public Task<IUser> Get()
            => SendRequest<User>(endPoints.Get, AuthHeader).Upcast<IUser, User>();

        public Task<IUser> GetWithGoogle()
            => SendRequest<User>(endPoints.GetWithGoogle, AuthHeader).Upcast<IUser, User>();

        public Task<IUser> Update(IUser user)
            => SendRequest(endPoints.Put, AuthHeader, user as User ?? new User(user), SerializationReason.Post)
            .Upcast<IUser, User>();

        public Task<string> ResetPassword(Email email)
        {
            var json = $"{{\"email\":\"{email}\"}}";
            return SendRequest(endPoints.ResetPassword, new HttpHeader[0], json)
                .ContinueWith(t => t.Result.Trim('"'));
        }

        public async Task<IUser> SignUp(
            Email email,
            Password password,
            bool termsAccepted,
            int countryId,
            string timezone
        )
        {
            if (!email.IsValid)
                throw new ArgumentException(nameof(email));

            var dto = new SignUpParameters
            {
                Email = email,
                Password = password,
                Workspace = new WorkspaceParameters
                {
                    InitialPricingPlan = PricingPlans.Free
                },
                TermsAccepted = termsAccepted,
                CountryId = countryId,
                Timezone = timezone
            };
            var json = serializer.Serialize(dto, SerializationReason.Post);
            try
            {
                var user = await SendRequest<User>(endPoints.Post, new HttpHeader[0], json)
                    .ConfigureAwait(false);
                return user;
            }
            catch (BadRequestException ex)
            when (ex.LocalizedApiErrorMessage == userAlreadyExistsApiErrorMessage)
            {
                throw new EmailIsAlreadyUsedException(ex);
            }
        }

        public Task<IUser> SignUpWithGoogle(string googleToken, bool termsAccepted, int countryId, string timezone)
        {
            Ensure.Argument.IsNotNull(googleToken, nameof(googleToken));
            var parameters = new GoogleSignUpParameters
            {
                GoogleAccessToken = googleToken,
                Workspace = new WorkspaceParameters
                {
                    InitialPricingPlan = PricingPlans.Free
                },
                TermsAccepted = termsAccepted,
                CountryId = countryId,
                Timezone = timezone
            };

            var json = serializer.Serialize(parameters, SerializationReason.Post);
            return SendRequest<User>(endPoints.PostWithGoogle, new HttpHeader[0], json)
                .Upcast<IUser, User>();
        }

        [Preserve(AllMembers = true)]
        internal class WorkspaceParameters
        {
            public string Name { get; set; } = null;

            public PricingPlans InitialPricingPlan { get; set; }
        }

        [Preserve(AllMembers = true)]
        private class SignUpParameters
        {
            [JsonConverter(typeof(EmailConverter))]
            public Email Email { get; set; }

            [JsonConverter(typeof(PasswordConverter))]
            public Password Password { get; set; }

            public WorkspaceParameters Workspace { get; set; }

            [JsonProperty("tos_accepted")]
            public bool TermsAccepted { get; set; }

            public int CountryId { get; set; }

            public string Timezone { get; set; }
        }

        [Preserve(AllMembers = true)]
        private class GoogleSignUpParameters
        {
            public string GoogleAccessToken { get; set; }

            public WorkspaceParameters Workspace { get; set; }

            [JsonProperty("tos_accepted")]
            public bool TermsAccepted { get; set; }

            public int CountryId { get; set; }

            public string Timezone { get; set; }
        }
    }
}
