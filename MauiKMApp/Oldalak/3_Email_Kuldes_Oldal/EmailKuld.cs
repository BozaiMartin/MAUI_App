using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KmKiolvasasMaui.Email
{
    public static class EmailKuld
    {
        private static readonly HttpClient Http = new();

        private const string EmailApiUrl = "https://script.google.com/macros/s/AKfycbyJgKE0CFjMkWly9eOpgs5YLI_CPtImm8Ax4PMwLzMb2NZ8UpwewnvWX57XMD2bdAzb/exec";
        // API_KEY Script Property értéke.
        private const string ApiKey = "5692A9D7844FFEC77B0130F6F09C6AA4A7CE262C049DFC40687939C3BC32FE48";

        public static async Task<bool> KuldesCsvAsync(
            string requestId,
            string userName,
            string csvTartalom,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(requestId))
            {
                throw new ArgumentException(
                    "Hiányzik a requestId.",
                    nameof(requestId));
            }

            if (string.IsNullOrWhiteSpace(csvTartalom))
            {
                throw new ArgumentException(
                    "A CSV tartalma üres.",
                    nameof(csvTartalom));
            }

            EmailRequest request = new()
            {
                ApiKey = ApiKey,
                RequestId = requestId,
                UserName = userName,
                Csv = csvTartalom
            };

            string json =
                JsonSerializer.Serialize(request);

            using StringContent content =
                new(
                    json,
                    Encoding.UTF8,
                    "application/json");

            using HttpResponseMessage response =
                await Http.PostAsync(
                    EmailApiUrl,
                    content,
                    cancellationToken);

            string valasz =
                await response.Content
                    .ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Az API HTTP hibát adott: " +
                    $"{(int)response.StatusCode}. " +
                    $"Válasz: {Rovidit(valasz)}");
            }

            ApiResponse? apiResponse;

            try
            {
                apiResponse =
                    JsonSerializer.Deserialize<ApiResponse>(
                        valasz);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    "Az API nem értelmezhető JSON választ adott. " +
                    $"Válasz: {Rovidit(valasz)}",
                    ex);
            }

            if (apiResponse == null)
            {
                throw new InvalidOperationException(
                    "Az API üres választ adott.");
            }

            if (!apiResponse.Success)
            {
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(apiResponse.Error)
                        ? "Ismeretlen API-hiba."
                        : apiResponse.Error);
            }

            return apiResponse.AlreadySent;
        }

        private static string Rovidit(string? szoveg)
        {
            if (string.IsNullOrWhiteSpace(szoveg))
                return "(üres válasz)";

            return szoveg.Length <= 300
                ? szoveg
                : szoveg[..300] + "...";
        }

        private sealed class EmailRequest
        {
            [JsonPropertyName("apiKey")]
            public string ApiKey { get; set; } = "";

            [JsonPropertyName("requestId")]
            public string RequestId { get; set; } = "";

            [JsonPropertyName("userName")]
            public string UserName { get; set; } = "";

            [JsonPropertyName("csv")]
            public string Csv { get; set; } = "";
        }

        private sealed class ApiResponse
        {
            [JsonPropertyName("success")]
            public bool Success { get; set; }

            [JsonPropertyName("alreadySent")]
            public bool AlreadySent { get; set; }

            [JsonPropertyName("error")]
            public string? Error { get; set; }
        }
    }
}