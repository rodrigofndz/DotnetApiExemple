using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;

namespace Movies.Api.Sdk.Consumer;

public class AuthTokenProvider
{
    private readonly HttpClient _client;
    private string _cachedToken = string.Empty;
    private static readonly SemaphoreSlim Lock = new(1, 1);

    public AuthTokenProvider(HttpClient client)
    {
        _client = client;
    }

    public async Task<string> GetTokenAsync()
    {
        if (!string.IsNullOrEmpty(_cachedToken))
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(_cachedToken);
            var expiryTimeText = jwt.Claims.Single(claim => claim.Type == "exp").Value;
            var expiryDateTime = UnixTimesStampToDateTime(int.Parse(expiryTimeText));
            if (expiryDateTime > DateTime.UtcNow)
            {
                return _cachedToken;
            }
        }
        
        await Lock.WaitAsync();
        var response = await _client.PostAsJsonAsync("https://localhost:5003/token", new
        {
            userid = "d8566de3-b1a6-4a9b-b842-8e3887a82e41",
            email = "rodrigo@rodrigo.com",
            customClaims = new Dictionary<string, object>
            {
                {"admin", true},
                {"trusted_member", true},
            }
        });
        
        var newToken = await response.Content.ReadAsStringAsync();
        _cachedToken = newToken;
        Lock.Release();
        return newToken;
    }

    private static DateTime UnixTimesStampToDateTime(int unixTimeStamp)
    {
        var dateTime = new DateTime(1970,1,1,0,0,0,0, DateTimeKind.Utc);
        return dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
    }
}