using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Shopping.Aggregator
{
    /// <summary>
    /// Defines the BearerTokenHandler type.
    /// </summary>
    public sealed class BearerTokenHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _accessor;
        /// <summary>
        /// Executes the BearerTokenHandler operation.
        /// </summary>
        public BearerTokenHandler(IHttpContextAccessor accessor) => _accessor = accessor;
        /// <summary>
        /// Describes the SendAsync operation.
        /// </summary>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var context = _accessor.HttpContext;
            var token = context is null ? null : await context.GetTokenAsync("access_token");
            if (!string.IsNullOrEmpty(token)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
