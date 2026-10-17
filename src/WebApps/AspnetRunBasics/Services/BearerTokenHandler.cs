using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace AspnetRunBasics.Services
{
    /// <summary>
    /// Defines the BearerTokenHandler type.
    /// </summary>
    public sealed class BearerTokenHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        /// <summary>
        /// Executes the BearerTokenHandler operation.
        /// </summary>
        public BearerTokenHandler(IHttpContextAccessor httpContextAccessor) => _httpContextAccessor = httpContextAccessor;
        /// <summary>
        /// Describes the SendAsync operation.
        /// </summary>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var context = _httpContextAccessor.HttpContext;
            if (context != null)
            {
                var token = await context.GetTokenAsync("access_token");
                if (!string.IsNullOrEmpty(token)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
