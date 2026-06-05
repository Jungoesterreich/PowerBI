// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

using System.Net;
using System.Text;

namespace JoeSync.Importers.Tests;

/// <summary>
/// Test double for <see cref="HttpMessageHandler"/> that answers each request
/// from a caller-supplied responder. Records every requested URI so tests can
/// assert how many calls the client made (e.g. paging termination).
/// </summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        _responder = responder;
    }

    public List<string> RequestedUris { get; } = [];

    public int RequestCount => RequestedUris.Count;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        RequestedUris.Add(request.RequestUri!.ToString());
        return Task.FromResult(_responder(request));
    }

    public static HttpResponseMessage Json(string body)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
    }
}
