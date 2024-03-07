using Amazon.CognitoIdentityProvider;
using Amazon.Runtime;
using TestClient.AuthenticationFlows;

// Replace these placeholders with their actual values
string userName = "<emailAddress>"; // use the email address of the cognito user
string password = "<password>"; //  use the password of the cognito user
string clientId = "<clientId>"; // get this from deployment output
string userpoolId = "<userpoolId>"; // get this from deployment output

// The Amazon Cognito service client with anonymous credentials
var cognitoProvider = new AmazonCognitoIdentityProviderClient(new AnonymousAWSCredentials(), FallbackRegionFactory.GetRegionEndpoint());

// The Amazon Cognito service client with developers IAM credentials, since, AdminInitiateAuth API is meant to be called from a back-end which has access to IAM credentials.
var cognitoProviderForAdmin = new AmazonCognitoIdentityProviderClient(FallbackRegionFactory.GetRegionEndpoint());


// USER_PASSWORD_AUTH
Console.WriteLine("USER_PASSWORD_AUTH Authentication Started");
await new UserPasswordAuthenticator(cognitoProvider).Authenticate(userName, password, clientId);
Console.WriteLine("USER_PASSWORD_AUTH Completed\n");


// USER_SRP_AUTH
Console.WriteLine("USER_SRP_AUTH Authentication Started");
await new UserSrpAuthenticator(cognitoProvider).AuthenticateWithExtensionLibrary(userName, password, clientId, userpoolId);
Console.WriteLine("USER_SRP_AUTH Completed\n");


// ADMIN_USER_PASSWORD_AUTH
Console.WriteLine("ADMIN_USER_PASSWORD_AUTH(1) Authentication Started");
await new AdminUserPasswordAuthenticator(cognitoProviderForAdmin).Authenticate(userName, password, clientId, userpoolId);
Console.WriteLine("ADMIN_USER_PASSWORD_AUTH(1) Completed\n");

Console.WriteLine("ADMIN_USER_PASSWORD_AUTH(2) Authentication Started");
await new AdminUserPasswordAuthenticator(cognitoProviderForAdmin).AuthenticateWithExtensionLibrary(userName, password, clientId, userpoolId);
Console.WriteLine("ADMIN_USER_PASSWORD_AUTH(2) Completed\n");


// CUSTOM_AUTH
Console.WriteLine("CUSTOM_AUTH(1) Authentication Started");
await new CustomAuthenticator(cognitoProvider).Authenticate(userName, clientId);
Console.WriteLine("CUSTOM_AUTH(1) Completed\n");

Console.WriteLine("CUSTOM_AUTH(2) Authentication Started");
await new CustomAuthenticator(cognitoProvider).AuthenticateWithExtensionLibrary(userName, clientId, userpoolId);
Console.WriteLine("CUSTOM_AUTH(2) Completed\n");


// CUSTOM_AUTH with SRP password verification
Console.WriteLine("CUSTOM_AUTH With SRP Authentication Started");
await new UserSrpCustomAuthenticator(cognitoProvider).AuthenticateWithExtensionLibrary(userName, password, clientId, userpoolId);
Console.WriteLine("CUSTOM_AUTH With SRP Completed \n");

Console.WriteLine("You're all done! You can now close the window.");
Console.ReadLine();

