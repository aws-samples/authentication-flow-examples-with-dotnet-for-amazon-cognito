using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Extensions.CognitoAuthentication;

namespace TestClient.AuthenticationFlows
{
    /// <summary>
    /// This class provides examples of CUSTOM_AUTH
    /// </summary>
    public class CustomAuthenticator : AuthenticatorBase
    {
        private readonly IAmazonCognitoIdentityProvider cognitoProvider;

        public CustomAuthenticator(IAmazonCognitoIdentityProvider cognitoProvider)
        {
            this.cognitoProvider = cognitoProvider;
        }

        /// <summary>
        /// Example of CUSTOM_AUTH using AWSSDK.CognitoIdentityProvider
        /// </summary>
        public async Task Authenticate(string username, string clientId)
        {
            try
            {
                var authParameters = new Dictionary<string, string>
                {
                    { "USERNAME", username }
                };

                var authRequest = new InitiateAuthRequest
                {
                    ClientId = clientId,
                    AuthParameters = authParameters,
                    AuthFlow = AuthFlowType.CUSTOM_AUTH,
                };

                var authResponse = await cognitoProvider.InitiateAuthAsync(authRequest);

                if (authResponse.AuthenticationResult == null)
                {
                    if (authResponse.ChallengeName == ChallengeNameType.CUSTOM_CHALLENGE)
                    {
                        // just set few properties to make this while loop work properly
                        var challengeAuthResponse = new RespondToAuthChallengeResponse();
                        challengeAuthResponse.AuthenticationResult = null;
                        challengeAuthResponse.Session = authResponse.Session;

                        while (challengeAuthResponse.AuthenticationResult == null)
                        {
                            Console.WriteLine("Enter the secret code: (Hint: Enter 123456 for this demo)");  // since the same is configured in CreateAuthChallenge lambda function

                            string secretCode = Console.ReadLine() ?? string.Empty;

                            var challengeResponse = new Dictionary<string, string>
                            {
                                { "USERNAME", username },
                                { "ANSWER", secretCode }
                            };

                            var challengeRequest = new RespondToAuthChallengeRequest
                            {
                                ChallengeName = authResponse.ChallengeName,
                                ClientId = clientId,
                                ChallengeResponses = challengeResponse,
                                Session = challengeAuthResponse.Session
                            };

                            challengeAuthResponse = await cognitoProvider.RespondToAuthChallengeAsync(challengeRequest);
                        }

                        if (challengeAuthResponse.AuthenticationResult != null)
                        {
                            PrintSuccessResult(AuthFlowType.CUSTOM_AUTH, challengeAuthResponse.AuthenticationResult);
                        }
                        else
                        {
                            // though, this is never supposed to execute, as you'll get exception after certain attempts
                            Console.WriteLine($"Additional challenge {challengeAuthResponse.ChallengeName} is required");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Unrecognized authentication challenge.");
                    }
                }
                else
                {
                    Console.WriteLine($"Invalid setup. You're not supposed to get the token at this stage.");
                }
            }
            catch (Exception ex)
            {
                WriteError(ex.Message);
            }
        }

        /// <summary>
        /// Example of CUSTOM_AUTH using Amazon.Extensions.CognitoAuthentication
        /// </summary>
        public async Task AuthenticateWithExtensionLibrary(string username, string clientId, string userpoolId)
        {
            try
            {
                var userPool = new CognitoUserPool(userpoolId, clientId, cognitoProvider);
                var user = new CognitoUser(username, clientId, userPool, cognitoProvider);

                var authParameters = new Dictionary<string, string>
                {
                    { "USERNAME", username }
                };

                AuthFlowResponse authResponse = await user.StartWithCustomAuthAsync(new InitiateCustomAuthRequest()
                {
                    AuthParameters = authParameters,
                    ClientMetadata = new Dictionary<string, string>()
                }).ConfigureAwait(false);


                authResponse = await HandleAdditionalChallenges(user, authResponse).ConfigureAwait(false);

                if (authResponse.AuthenticationResult != null)
                {
                    PrintSuccessResult(AuthFlowType.CUSTOM_AUTH, authResponse.AuthenticationResult);
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
