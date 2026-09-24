using System.Net.Http.Json;

namespace KmKiolvasasMaui.Email
{
    public static class EmailKuld
    {
        private static readonly HttpClient Http = new();

        private const string EmailApiUrl =
            "https://script.google.com/macros/s/AKfycbyJgKE0CFjMkWly9eOpgs5YLI_CPtImm8Ax4PMwLzMb2NZ8UpwewnvWX57XMD2bdAzb/exec";
        // API_KEY Script Property értéke.
        private const string ApiKey =
            "BzMrtn0412FmjgO34Ughdo239f9G14fDmBh2gDx4my63g2t8dxFdnV12";


        public static async Task KuldesCsvAsync(
            string requestId,
            string userName,
            string csvTartalom,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(requestId))
                throw new ArgumentException(
                    "Hiányzik a requestId.",
                    nameof(requestId));


            if (string.IsNullOrWhiteSpace(csvTartalom))
                throw new ArgumentException(
                    "A CSV tartalma üres.",
                    nameof(csvTartalom));


            var request = new EmailRequest
            {
                ApiKey = ApiKey,
                RequestId = requestId,
                UserName = userName,
                Csv = csvTartalom
            };


            using HttpResponseMessage response =
                await Http.PostAsJsonAsync(
                    EmailApiUrl,
                    request,
                    cancellationToken);


            response.EnsureSuccessStatusCode();


            EmailResponse? result =
                await response.Content
                    .ReadFromJsonAsync<EmailResponse>(
                        cancellationToken:
                            cancellationToken);


            if (result == null)
            {
                throw new InvalidOperationException(
                    "Nem érkezett válasz az e-mail szolgáltatástól.");
            }


            if (!result.Success)
            {
                throw new InvalidOperationException(
                    result.Error ??
                    "Az e-mail küldése sikertelen.");
            }
        }


        private sealed class EmailRequest
        {
            public string ApiKey { get; set; } = "";

            public string RequestId { get; set; } = "";

            public string UserName { get; set; } = "";

            public string Csv { get; set; } = "";
        }


        private sealed class EmailResponse
        {
            public bool Success { get; set; }

            public bool AlreadySent { get; set; }

            public string? Error { get; set; }
        }
    }
}