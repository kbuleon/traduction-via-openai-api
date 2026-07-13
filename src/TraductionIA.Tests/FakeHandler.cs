using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace TraductionIA.Tests
{
    /// <summary>
    /// Faux HttpMessageHandler : renvoie une réponse fabriquée, sans réseau réel.
    /// Permet de prouver le mécanisme d'échange (succès + cas d'erreur) hors ligne.
    /// </summary>
    internal sealed class FakeHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public HttpRequestMessage LastRequest { get; private set; }
        public string LastBody { get; private set; }

        public FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastBody = request.Content == null
                ? ""
                : await request.Content.ReadAsStringAsync().ConfigureAwait(false);
            return _responder(request);
        }
    }
}
