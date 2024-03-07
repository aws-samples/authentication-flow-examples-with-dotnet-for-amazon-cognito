using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;

namespace TestClient.AuthenticationFlows
{

    /// <summary>
    /// This class provides examples of USER_PASSWORD_AUTH
    /// </summary>
    public class UserPasswordAuthenticator : AuthenticatorBase
    {
        private readonly IAmazonCognitoIdentityProvider cognitoProvider;

        public UserPasswordAuthenticator(IAmazonCognitoIdentityProvider cognitoProvider)
        {
            this.cognitoProvider = cognitoProvider;
        }

        /// <summary>
        /// Example of USER_PASSWORD_AUTH using AWSSDK.CognitoIdentityProvider
        /// </summary>
        public async Task Authenticate(string username, string password, string clientId)
        {
            try
            {
                var authParameters = new Dictionary<string, string>
                {
                    { "USERNAME", username },
                    { "PASSWORD", password }
                };

                var authRequest = new InitiateAuthRequest
                {
                    ClientId = clientId,
                    AuthParameters = authParameters,
                    AuthFlow = AuthFlowType.USER_PASSWORD_AUTH,
                };

                var authResponse = await cognitoProvider.InitiateAuthAsync(authRequest);

                if (authResponse.AuthenticationResult != null)
                {
                    PrintSuccessResult(AuthFlowType.USER_PASSWORD_AUTH, authResponse.AuthenticationResult);
                }
                else
                {
                    // RespondToAuthChallenge is required for the next challenge i.e. SMS_MFA, MFA_SETUP, etc. 
                    Console.WriteLine($"Additional challenge {authResponse.ChallengeName} is required");
                }
            }
            catch (Exception ex)
            {
                WriteError(ex.Message);
            }
        }
    }
}
