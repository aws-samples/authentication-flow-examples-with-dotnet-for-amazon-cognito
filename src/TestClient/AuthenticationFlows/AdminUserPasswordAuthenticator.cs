using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Extensions.CognitoAuthentication;

namespace TestClient.AuthenticationFlows
{
    /// <summary>
    /// This class provides examples of ADMIN_USER_PASSWORD_AUTH
    /// </summary>
    public class AdminUserPasswordAuthenticator: AuthenticatorBase
    {
        private readonly IAmazonCognitoIdentityProvider cognitoProvider;

        public AdminUserPasswordAuthenticator( IAmazonCognitoIdentityProvider cognitoProvider)
        {
            this.cognitoProvider = cognitoProvider;
        }

        /// <summary>
        /// Example of ADMIN_USER_PASSWORD_AUTH using AWSSDK.CognitoIdentityProvider
        /// </summary>
        public async Task Authenticate(string username, string password, string clientId, string userpoolId)
        {
            try
            {
                var authParameters = new Dictionary<string, string>
                {
                    { "USERNAME", username },
                    { "PASSWORD", password }
                };

                var authRequest = new AdminInitiateAuthRequest
                {
                    ClientId = clientId,
                    UserPoolId = userpoolId,
                    AuthParameters = authParameters,
                    AuthFlow = AuthFlowType.ADMIN_USER_PASSWORD_AUTH,
                };

                var authResponse = await cognitoProvider.AdminInitiateAuthAsync(authRequest);

                if (authResponse.AuthenticationResult != null)
                {
                    PrintSuccessResult(AuthFlowType.ADMIN_USER_PASSWORD_AUTH, authResponse.AuthenticationResult);
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

        /// <summary>
        /// Example of ADMIN_USER_PASSWORD_AUTH using Amazon.Extensions.CognitoAuthentication
        /// </summary>
        public async Task AuthenticateWithExtensionLibrary(string username, string password, string clientId, string userpoolId)
        {
            try
            {
                var userPool = new CognitoUserPool(userpoolId, clientId, cognitoProvider);
                var user = new CognitoUser(username, clientId, userPool, cognitoProvider);

                AuthFlowResponse authResponse = await user.StartWithAdminNoSrpAuthAsync(new InitiateAdminNoSrpAuthRequest()
                {
                    Password = password
                }).ConfigureAwait(false);


                authResponse = await HandleAdditionalChallenges(user, authResponse).ConfigureAwait(false);

                if (authResponse.AuthenticationResult != null)
                {
                    PrintSuccessResult(AuthFlowType.ADMIN_USER_PASSWORD_AUTH, authResponse.AuthenticationResult);
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
