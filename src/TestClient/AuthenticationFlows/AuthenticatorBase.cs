using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Extensions.CognitoAuthentication;

namespace TestClient.AuthenticationFlows
{
    public class AuthenticatorBase
    {
        /// <summary>
        /// Handles additional challenges
        /// </summary>
        protected static async Task<AuthFlowResponse> HandleAdditionalChallenges(CognitoUser user, AuthFlowResponse authResponse)
        {
            // Authenticating with Multiple Forms of Authentication
            while (authResponse.AuthenticationResult == null)
            {
                if (authResponse.ChallengeName == ChallengeNameType.NEW_PASSWORD_REQUIRED)
                {
                    Console.WriteLine("Enter your desired new password:");
                    string newPassword = Console.ReadLine() ?? string.Empty;

                    authResponse = await user.RespondToNewPasswordRequiredAsync(new RespondToNewPasswordRequiredRequest()
                    {
                        SessionID = authResponse.SessionID,
                        NewPassword = newPassword
                    }).ConfigureAwait(false);
                }
                else if (authResponse.ChallengeName == ChallengeNameType.SMS_MFA)
                {
                    Console.WriteLine("Enter the MFA Code sent to your device:");
                    string mfaCode = Console.ReadLine() ?? string.Empty;

                    authResponse = await user.RespondToSmsMfaAuthAsync(new RespondToSmsMfaRequest()
                    {
                        SessionID = authResponse.SessionID,
                        MfaCode = mfaCode
                    }).ConfigureAwait(false);
                }
                else if (authResponse.ChallengeName == ChallengeNameType.CUSTOM_CHALLENGE)
                {
                    Console.WriteLine("Enter the secret code: (Hint: Enter 123456 for this demo)"); // since the same is configured in CreateAuthChallenge lambda function

                    string secretCode = Console.ReadLine() ?? string.Empty;

                    var challengeParameters = new Dictionary<string, string>
                    {
                        { "USERNAME", user.Username },
                        { "ANSWER", secretCode }
                    };

                    authResponse = await user.RespondToCustomAuthAsync(new RespondToCustomChallengeRequest()
                    {
                        SessionID = authResponse.SessionID,
                        ChallengeParameters = challengeParameters
                    }).ConfigureAwait(false);
                }
                else
                {
                    Console.WriteLine("Unrecognized authentication challenge.");
                    break;
                }
            }

            return authResponse;
        }

        /// <summary>
        /// Prints authentication result
        /// </summary>
        protected void PrintSuccessResult(AuthFlowType authFlowType, AuthenticationResultType authenticationResult)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.WriteLine($"Authentication successful for {authFlowType}");
            Console.ResetColor();

            //You get ID_Token and Access_Token here
            //Console.WriteLine(JsonSerializer.Serialize(authenticationResult));
        }

        /// <summary>
        /// Prints error message in red color
        /// </summary>
        protected static void WriteError(string buffer)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.WriteLine(buffer);
            Console.ResetColor();
        }
    }
}
