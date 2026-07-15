namespace CMS.API.Models;

/// <summary>
/// Profile returned on a successful login. Deliberately carries no <c>PasswordHash</c> —
/// only the identity and the signed JWT the client sends back on later requests.
/// </summary>
public sealed class LoginResponse
{
    public string UserId { get; set; } = "";
    public string UserName { get; set; } = "";
    public string AccessToken { get; set; } = "";
}
