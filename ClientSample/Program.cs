using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shared;

namespace ClientSample
{
  class Program
  {
    static async Task Main(string[] args)
    {
      var handler = new HttpClientHandler
      {
        ServerCertificateCustomValidationCallback = delegate { return true; }
      };
      using var authClient = new HttpClient(handler)
      {
        BaseAddress = new Uri("https://localhost:7001")
      };

      var csParams = new CspParameters
      {
        KeyContainerName = "client_secret_container",
        Flags = CspProviderFlags.UseDefaultKeyContainer,
      };
      using var rsa = new RSACryptoServiceProvider(2048, csParams) {PersistKeyInCsp = false};
      var publicKey = rsa.ExportPublicKey();
      rsa.PersistKeyInCsp = false;
      var computeHash = MD5.Create().ComputeHash(rsa.ExportRSAPublicKey());
      var username = Convert.ToBase64String(computeHash);
      Console.WriteLine($"username: {username}");
      string secret;
      if (File.Exists("secret.txt"))
        secret = File.ReadAllText("secret.txt");
      else
      {
        secret = Guid.NewGuid().ToString();
        File.WriteAllText("secret.txt", secret);
      }

      var user = new UserRegistration
      {
        ClientId = username,
        Secret = secret,
        Name = "Console app"
      };
      var content = new StringContent(JsonConvert.SerializeObject(user), Encoding.UTF8, "application/json");
      var result = await authClient.PostAsync("account/register", content);

      Console.WriteLine(result.StatusCode);
      Console.WriteLine(await result.Content.ReadAsStringAsync());
      if (result.StatusCode != HttpStatusCode.Created)
        throw new Exception(await result.Content.ReadAsStringAsync());

      var tokenContent = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>()
      {
        new KeyValuePair<string, string>("grant_type", "client_credentials"),
        new KeyValuePair<string, string>("client_id", username),
        new KeyValuePair<string, string>("client_secret", secret)
      });
      var login = await authClient.PostAsync("connect/token", tokenContent);
      var token = JsonConvert.DeserializeObject<JObject>(await login.Content.ReadAsStringAsync())
        .SelectToken("access_token")
        .Value<string>();

      using var resourceClient = new HttpClient(handler);
      resourceClient.BaseAddress = new Uri("http://localhost:7002");

      var resourceResult = await resourceClient.GetAsync("/protected-resource");
      if (resourceResult.StatusCode != HttpStatusCode.Unauthorized)
        throw new Exception("Request should be unauthorized");

      resourceClient.DefaultRequestHeaders.Authorization
        = new AuthenticationHeaderValue("Bearer", token);
      resourceResult = await resourceClient.GetAsync("/protected-resource");
      Console.WriteLine(await resourceResult.Content.ReadAsStringAsync());

      Console.ReadLine();
    }
  }
}