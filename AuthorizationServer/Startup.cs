using System;
using System.Security.Cryptography;
using AspNet.Security.OpenIdConnect.Primitives;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;

namespace AuthorizationServer
{
  public class Startup
  {
    public Startup(IConfiguration configuration)
    {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
      IdentityModelEventSource.ShowPII = true;
      services.AddControllers();

      services.AddDbContext<SampleDbContext>();
      services.AddOpenIddict()
        .AddCore(options =>
        {
          options.UseEntityFrameworkCore()
            .UseDbContext<SampleDbContext>();
        })
        .AddServer(options =>
        {
          options.UseMvc();
          options.EnableTokenEndpoint("/connect/token")
            .EnableUserinfoEndpoint("/info/connect")
            .AllowClientCredentialsFlow()
            .UseJsonWebTokens();

          var cspParams = new CspParameters
          {
            KeyContainerName = "new_key_container",
            Flags = CspProviderFlags.UseDefaultKeyContainer
          };
          RSAInstance = new RSACryptoServiceProvider(2048, cspParams);
          var rsaSecurityKey = new RsaSecurityKey(RSAInstance);
          options.AddSigningKey(rsaSecurityKey);
        });

      services.AddAuthentication()
        .AddJwtBearer(options =>
        {
          options.Authority = "https://localhost:7001/";
          options.Audience = "server_service";
          options.RequireHttpsMetadata = false;
          options.TokenValidationParameters = new TokenValidationParameters
          {
            NameClaimType = OpenIdConnectConstants.Claims.Subject,
            RoleClaimType = OpenIdConnectConstants.Claims.Role,
            ClockSkew = TimeSpan.FromMinutes(5)
          };
        });
    }

    public static RSACryptoServiceProvider RSAInstance { get; set; }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
      }

      app.UseHttpsRedirection();

      app.UseRouting();

      app.UseAuthorization();
      app.UseAuthentication();

      app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
    }
  }
}