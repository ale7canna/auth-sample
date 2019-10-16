using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Core;
using OpenIddict.EntityFrameworkCore.Models;
using Shared;

namespace AuthorizationServer.Controllers
{
  [Route("account")]
  public class AccountController : Controller
  {
    private readonly OpenIddictApplicationManager<OpenIddictApplication> _manager;

    public AccountController(OpenIddictApplicationManager<OpenIddictApplication> manager)
    {
      _manager = manager;
    }

    [HttpPost]
    [Route("register")]
    public async Task<IActionResult> Register([FromBody]UserRegistration user)
    {
      //Check OTP
      await _manager.CreateAsync(new OpenIddictApplicationDescriptor
      {
        ClientId = user.ClientId,
        ClientSecret = user.Secret,
        DisplayName = user.Name,
        Permissions =
        {
          OpenIddictConstants.Permissions.Endpoints.Token,
          OpenIddictConstants.Permissions.GrantTypes.ClientCredentials
        }
      });
      return Created(Request.Host.Value, user.ClientId);
    }
  }
}