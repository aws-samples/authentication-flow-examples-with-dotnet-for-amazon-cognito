using Amazon.CognitoIdentityProvider;
using Amazon.Extensions.CognitoAuthentication;

namespace TestClient.AuthenticationFlows
{
    /// <summary>
    /// This class provides examples of USER_SRP_AUTH
    /// </summary>
    public class UserSrpAuthenticator : AuthenticatorBase
    {
        private readonly IAmazonCognitoIdentityProvider cognitoProvider;

        public UserSrpAuthenticator(IAmazonCognitoIdentityProvider cognitoProvider)
        {
            this.cognitoProvider = cognitoProvider;
        }

        /// <summary>
        /// Example of USER_SRP_AUTH using Amazon.Extensions.CognitoAuthentication
        /// </summary>
        public async Task AuthenticateWithExtensionLibrary(string username, string password, string clientId, string userpoolId)
        {
            try
            {
                var userPool = new CognitoUserPool(userpoolId, clientId, cognitoProvider);
                var user = new CognitoUser(username, clientId, userPool, cognitoProvider);

                AuthFlowResponse authResponse = await user.StartWithSrpAuthAsync(new InitiateSrpAuthRequest()
                {
                    Password = password
                }).ConfigureAwait(false);


                authResponse = await HandleAdditionalChallenges(user, authResponse).ConfigureAwait(false);

                if (authResponse.AuthenticationResult != null)
                {
                    PrintSuccessResult(AuthFlowType.USER_SRP_AUTH, authResponse.AuthenticationResult);
                }
                else
                {
                    Console.WriteLine("Failed to authenticate");
                }
            }
            catch (Exception ex)
            {
                WriteError(ex.Message);
            }
        }
    }
}
