namespace SP.Runtime.Core.Services.AuthorizationService
{
    public interface IAuthorizationCallbacks
    {
        void OnPlayerAuthorized();
        void OnPlayerUnauthorized();
    }
}
