namespace RYHMÄROJEKTI;

public static class ApiClient
{
    private static readonly Lazy<HttpClient> _http = new(() =>
    {
        string baseUrl;
#if ANDROID
        baseUrl = "http://10.0.2.2:5026";
#else
        baseUrl = "http://localhost:5026";
#endif
        return new HttpClient { BaseAddress = new Uri(baseUrl) };
    });

    public static HttpClient Http => _http.Value;
}
