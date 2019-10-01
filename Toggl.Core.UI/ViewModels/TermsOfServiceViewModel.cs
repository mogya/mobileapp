using System.Threading.Tasks;
using Toggl.Core.Services;
using Toggl.Core.UI.Navigation;
using Toggl.Shared;
using Toggl.Shared.Extensions;
using Xamarin.Essentials;

namespace Toggl.Core.UI.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class TermsOfServiceViewModel : ViewModelWithOutput<bool>
    {
        private const string privacyPolicyUrl = "https://toggl.com/legal/privacy/";
        private const string termsOfServiceUrl = "https://toggl.com/legal/terms/";

        public ViewAction ViewTermsOfService { get; }
        public ViewAction ViewPrivacyPolicy { get; }

        public TermsOfServiceViewModel(IRxActionFactory rxActionFactory, INavigationService navigationService)
            : base(navigationService)
        {
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));

            ViewPrivacyPolicy = rxActionFactory.FromAsync(openPrivacyPolicy);
            ViewTermsOfService = rxActionFactory.FromAsync(openTermsOfService);
        }

        private Task openPrivacyPolicy()
            => Browser.OpenAsync(privacyPolicyUrl, BrowserLaunchMode.External);

        private Task openTermsOfService()
            => Browser.OpenAsync(termsOfServiceUrl, BrowserLaunchMode.External);
    }
}
