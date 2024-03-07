using Amazon.Lambda.Annotations;
using Amazon.Lambda.CognitoEvents;
using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.Logging;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace CustomAuthLambdas;

public class Functions
{

    /* ======== CUSTOM_AUTH references ===========
    / https://docs.aws.amazon.com/cognito/latest/developerguide/user-pool-lambda-challenge.html
    / https://docs.aws.amazon.com/cognito/latest/developerguide/amazon-cognito-user-pools-authentication-flow.html#Using-SRP-password-verification-in-custom-authentication-flow
    */

    /// <summary>
    /// Constant PASSWORD_VERIFIER for ChallengeNameType
    /// </summary>
    const string PASSWORD_VERIFIER = "PASSWORD_VERIFIER";

    /// <summary>
    /// Constant CUSTOM_CHALLENGE for ChallengeNameType
    /// </summary>
    const string CUSTOM_CHALLENGE = "CUSTOM_CHALLENGE";

    /// <summary>
    /// This is the decider function that manages the authentication flow. 
    /// In the session array that's provided to this Lambda function (event.request.session), the entire state of the authentication flow is present.
    /// 
    /// If it's empty, the custom authentication flow just started. If it has items, the custom authentication flow is underway,
    /// a challenge was presented to the user, the user provided an answer, and it was verified to be right or wrong. 
    /// In either case, the decider function has to decide what to do next.
    /// </summary>
    [Logging(LogEvent = true)]
    [LambdaFunction(MemorySize = 1024, PackageType = LambdaPackageType.Zip, ResourceName = "DefineAuthChallenge")]
    public CognitoDefineAuthChallengeEvent DefineAuthChallenge(CognitoDefineAuthChallengeEvent challengeEvent)
    {
        try
        {
            var previousChallenges = challengeEvent.Request.Session ?? new List<ChallengeResultElement>();

            #region To support SRP_A password verification with CUSTOM_AUTH flow

            if (previousChallenges.Count == 1 && previousChallenges[0].ChallengeName == "SRP_A")
            {
                challengeEvent.Response.ChallengeName = PASSWORD_VERIFIER;
                challengeEvent.Response.IssueTokens = false;
                challengeEvent.Response.FailAuthentication = false;

                return challengeEvent;
            }

            if (previousChallenges.Count == 2 && previousChallenges[1].ChallengeName == PASSWORD_VERIFIER)
            {
                //kick-off custom flow
                challengeEvent.Response.ChallengeName = CUSTOM_CHALLENGE;
                challengeEvent.Response.IssueTokens = false;
                challengeEvent.Response.FailAuthentication = false;

                return challengeEvent;
            }

            #endregion

            int maxChallengesAllowed = 3;
            if (previousChallenges.Count >= 2 && previousChallenges[1].ChallengeName == PASSWORD_VERIFIER)
            {
                maxChallengesAllowed += 2; // since initial 2 challenges were for SRP authentication
            }


            if (previousChallenges.Count == 0)
            {
                // This will executed first time, when the auth process starts
                challengeEvent.Response.ChallengeName = CUSTOM_CHALLENGE;
                challengeEvent.Response.IssueTokens = false;
                challengeEvent.Response.FailAuthentication = false;
            }
            else if (previousChallenges.Count <= maxChallengesAllowed)
            {
                // This block will be executed after VerifyAuthChallengeResponse lambda is executed (user has responded to the challenge)

                bool success = challengeEvent.Request.Session.Last().ChallengeResult;   // The ChallengeResult is whatever the VerifyAuthChallengeResponse returned in Response.AnswerCorrect

                if (success)
                {
                    // All good, issue tokens
                    challengeEvent.Response.IssueTokens = true;
                    challengeEvent.Response.FailAuthentication = false;
                }
                else
                {
                    // issue the challenge again
                    challengeEvent.Response.ChallengeName = CUSTOM_CHALLENGE;
                    challengeEvent.Response.IssueTokens = false;
                    challengeEvent.Response.FailAuthentication = false;
                }
            }
            else
            {
                // The user provided a wrong answer 3 times; terminte the current auth process
                challengeEvent.Response.IssueTokens = false;
                challengeEvent.Response.FailAuthentication = true;
            }

            return challengeEvent;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);

            throw;
        }
    }

    /// <summary>
    /// This Lambda function is invoked, based on the instruction of the "Define Auth Challenge" trigger, to create a unique challenge for the user. 
    /// We'll use it to generate a one-time login code and send it to the user.
    /// </summary>
    [Logging(LogEvent = true)]
    [LambdaFunction(MemorySize = 1024, PackageType = LambdaPackageType.Zip, ResourceName = "CreateAuthChallenge")]
    public CognitoCreateAuthChallengeEvent CreateAuthChallenge(CognitoCreateAuthChallengeEvent challengeEvent)
    {
        try
        {
            var previousChallenges = challengeEvent.Request.Session ?? new List<ChallengeResultElement>();

            if (challengeEvent.Request.ChallengeName != CUSTOM_CHALLENGE)
            {
                return challengeEvent;
            }

            string secretLoginCode = string.Empty;
            string email = string.Empty;

            if (previousChallenges.Count == 0 || (previousChallenges.Count == 2 && previousChallenges[1].ChallengeName == PASSWORD_VERIFIER))
            {
                // This is a new auth session, generate a new secret login code and send it to the user

                // ACTUAL FLOW: Uncomment this
                //secretLoginCode = new Random(100000).Next(999999).ToString();
                //email = challengeEvent.Request.UserAttributes["email"];
                //SendEmail(challengeEvent.Request.UserAttributes["email"], secretLoginCode);

                // TEST FLOW
                secretLoginCode = "123456";
                email = "dummy@domain.com";
            }
            else
            {
                // This block will be executed when a user responds to the challenge, but answers incorrectly.
                // Since this is an existing session, no need to generate a new secret code and send it again to the user.
                // Retrieve the secret code from the previous challenge from the 'challengeMetadata' property.
                secretLoginCode = previousChallenges.Last().ChallengeMetadata;
            }

            // It is safe to create new object as child properties might be null
            challengeEvent.Response = new CognitoCreateAuthChallengeResponse();

            // This is sent back to the client app
            challengeEvent.Response.PublicChallengeParameters.Add("Message", $"A 6 digit code has been sent to {email}");

            // Add the secret login code to the private challenge parameters.
            // So it can be verified by the "Verify Auth Challenge Response" trigger
            challengeEvent.Response.PrivateChallengeParameters.Add("SecretLoginCode", secretLoginCode);

            // "ChallengeMetadata" field will be persisted across multiple calls to Create Auth Challenge.
            // so, we can use this property to store current session's secret code.
            // However, the pupose of this property is provide custom challenge a specific name such as CAPTCHA_CHALLENGE.
            challengeEvent.Response.ChallengeMetadata = secretLoginCode;

            return challengeEvent;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);

            throw;
        }
    }

    /// <summary>
    /// This Lambda function is invoked by the user pool when the user provides the answer to the challenge. Its only job is to determine if that answer is correct.
    /// </summary>
    [Logging(LogEvent = true)]
    [LambdaFunction(MemorySize = 1024, PackageType = LambdaPackageType.Zip, ResourceName = "VerifyAuthChallenge")]
    public CognitoVerifyAuthChallengeEvent VerifyAuthChallenge(CognitoVerifyAuthChallengeEvent challengeEvent)
    {
        try
        {
            string expectedAnswer = challengeEvent.Request.PrivateChallengeParameters["SecretLoginCode"];

            if (challengeEvent.Request.ChallengeAnswer == expectedAnswer)
            {
                challengeEvent.Response.AnswerCorrect = true;
            }
            else
            {
                challengeEvent.Response.AnswerCorrect = false;
            }

            return challengeEvent;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);

            throw;
        }
    }
}
