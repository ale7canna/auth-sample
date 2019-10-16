using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using PemUtils;

namespace ResourceServer
{
  class Program
  {
    private static HttpListener _listener;
    private static RsaSecurityKey _authKey;

    static async Task Main(string[] args)
    {
      IdentityModelEventSource.ShowPII = true;

      var handler = new HttpClientHandler
      {
        ServerCertificateCustomValidationCallback = delegate { return true; }
      };
      using var authClient = new HttpClient(handler)
      {
        BaseAddress = new Uri("https://localhost:7001")
      };
      using var authPublicKeyStream = await authClient.GetAsync("connect/public")
        .ContinueWith(r => r.Result.Content.ReadAsStreamAsync().Result);
      using var pemReader = new PemReader(authPublicKeyStream);
//      using var pemReader = new PemReader(File.OpenRead(@"C:\Users\canale\.ssh\id_rsa"));
      _authKey = new RsaSecurityKey(pemReader.ReadRsaKey());
      _listener = new HttpListener();
      _listener.Prefixes.Add("http://localhost:7002/");
      _listener.Start();
      var listenerThread = new Thread(ListenProcedure);
      listenerThread.Start();

      Console.ReadLine();
      _listener.Close();
    }

    private static void ListenProcedure(object obj)
    {
      while (_listener.IsListening)
      {
        try
        {
          HttpListenerContext ctx = _listener.GetContext();

          HttpListenerRequest request = ctx.Request;
          HttpListenerResponse response = ctx.Response;

          if (ctx.Request.Url.LocalPath != "/protected-resource")
          {
            response.StatusCode = 404;
            var buffer = Encoding.UTF8.GetBytes("Resource not found");

            response.OutputStream.Write(buffer);
            response.OutputStream.Close();
          }
          else
          {
            try
            {
              var token = request.Headers["Authorization"].Replace("Bearer ", string.Empty);
              TokenValidationParameters validationParameters =
                new TokenValidationParameters
                {
                  ValidIssuer = "https://localhost:7001/",
                  ValidAudiences = new[] { "resource_server" },
                  IssuerSigningKeys = new []{ _authKey },
                };
              JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();

              var user = handler.ValidateToken(token, validationParameters, out var validatedToken);
              Console.WriteLine($"Token is valid till: {validatedToken.ValidTo}");
              byte[] buffer = Encoding.UTF8.GetBytes("Response body");

              response.OutputStream.Write(buffer);
              response.OutputStream.Close();
            }
            catch (Exception e)
            {
              response.StatusCode = 401;
              var buffer = Encoding.UTF8.GetBytes("Unauthorized");

              response.OutputStream.Write(buffer);
              response.OutputStream.Close();
            }
          }
        }
        catch (Exception e)
        {
          Console.WriteLine(e);
        }
      }
    }
  }
}