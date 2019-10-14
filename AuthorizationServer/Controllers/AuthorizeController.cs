using System.IO;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AspNet.Security.OpenIdConnect.Extensions;
using AspNet.Security.OpenIdConnect.Primitives;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Core;
using OpenIddict.EntityFrameworkCore.Models;
using OpenIddict.Mvc.Internal;
using OpenIddict.Server;
using Shared;

namespace AuthorizationServer.Controllers
{
  public class AuthorizeController : Controller
  {
    private readonly OpenIddictApplicationManager<OpenIddictApplication> _applicationManager;

    public AuthorizeController(OpenIddictApplicationManager<OpenIddictApplication> applicationManager)
    {
      _applicationManager = applicationManager;
    }

    [HttpPost("~/connect/token"), Produces("application/json")]
    public async Task<IActionResult> Exchange([ModelBinder(typeof(OpenIddictMvcBinder))] OpenIdConnectRequest request)
    {
      if (!request.IsClientCredentialsGrantType())
        return BadRequest(new OpenIdConnectResponse
        {
          Error = OpenIdConnectConstants.Errors.UnsupportedGrantType,
          ErrorDescription = "The specified grant type is not supported."
        });

      var application = await _applicationManager.FindByClientIdAsync(request.ClientId, HttpContext.RequestAborted);
      if (application == null)
      {
        return BadRequest(new OpenIdConnectResponse
        {
          Error = OpenIdConnectConstants.Errors.InvalidClient,
          ErrorDescription = "The client application was not found in the database."
        });
      }

      var ticket = CreateTicket(application);
      return SignIn(ticket.Principal, ticket.Properties, ticket.AuthenticationScheme);
    }

    private AuthenticationTicket CreateTicket(OpenIddictApplication application)
    {
      // Create a new ClaimsIdentity containing the claims that
      // will be used to create an id_token, a token or a code.
      var identity = new ClaimsIdentity(
        OpenIddictServerDefaults.AuthenticationScheme,
        OpenIdConnectConstants.Claims.Name,
        OpenIdConnectConstants.Claims.Role);

      // Use the client_id as the subject identifier.
      identity.AddClaim(OpenIdConnectConstants.Claims.Subject, application.ClientId,
        OpenIdConnectConstants.Destinations.AccessToken,
        OpenIdConnectConstants.Destinations.IdentityToken);

      identity.AddClaim(OpenIdConnectConstants.Claims.Name, application.DisplayName,
        OpenIdConnectConstants.Destinations.AccessToken,
        OpenIdConnectConstants.Destinations.IdentityToken);

      // Create a new authentication ticket holding the user identity.
      var ticket = new AuthenticationTicket(
        new ClaimsPrincipal(identity),
        new AuthenticationProperties(),
        OpenIddictServerDefaults.AuthenticationScheme);

      ticket.SetResources("resource_server");

      return ticket;
    }

    [HttpGet("~/connect/public")]
    public string PublicKey()
    {
      var outputStream = new StreamWriter(new MemoryStream());
      Utils.ExportPublicKey(Startup.RSAInstance, outputStream);
      outputStream.Flush();
      outputStream.BaseStream.Seek(0, SeekOrigin.Begin);
      var reader = new StreamReader(outputStream.BaseStream);
      return reader.ReadToEnd();
    }

    [HttpGet("~/connect/private")]
    public string PrivateKey()
    {
      var outputStream = new StreamWriter(new MemoryStream());
      Utils.ExportPrivateKey(Startup.RSAInstance, outputStream);
      outputStream.Flush();
      outputStream.BaseStream.Seek(0, SeekOrigin.Begin);
      var reader = new StreamReader(outputStream.BaseStream);
      return reader.ReadToEnd();
    }
  }
}