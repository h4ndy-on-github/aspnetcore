// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Net.Http;
using System.Net.Http.HPack;
using System.Runtime.ExceptionServices;
using System.Text;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

// https://datatracker.ietf.org/doc/html/rfc8441
public class Http2WebSocketTests : Http2TestBase
{
    [Fact]
    public async Task HEADERS_Received_ExtendedCONNECTMethod_Received()
    {
        await InitializeConnectionAsync(async context =>
        {
            var connectFeature = context.Features.Get<IHttpExtendedConnectFeature>();
            Assert.True(connectFeature.IsExtendedConnect);
            Assert.Equal(HttpMethods.Connect, context.Request.Method);
            Assert.Equal("websocket", connectFeature.Protocol);
            Assert.False(context.Request.Headers.TryGetValue(":protocol", out var _));
            Assert.Equal("http", context.Request.Scheme);
            Assert.Equal("/chat", context.Request.Path.Value);
            Assert.Equal("server.example.com", context.Request.Host.Value);
            Assert.Equal("chat, superchat", context.Request.Headers.WebSocketSubProtocols);
            Assert.Equal("permessage-deflate", context.Request.Headers.SecWebSocketExtensions);
            Assert.Equal("13", context.Request.Headers.SecWebSocketVersion);
            Assert.Equal("http://www.example.com", context.Request.Headers.Origin);

            Assert.Equal(0, await context.Request.Body.ReadAsync(new byte[1]));
        });

        // HEADERS + END_HEADERS
        // :method = CONNECT
        // :protocol = websocket
        // :scheme = https
        // :path = /chat
        // :authority = server.example.com
        // sec-websocket-protocol = chat, superchat
        // sec-websocket-extensions = permessage-deflate
        // sec-websocket-version = 13
        // origin = http://www.example.com
        var headers = new[]
        {
            new KeyValuePair<string, string>(HeaderNames.Method, "CONNECT"),
            new KeyValuePair<string, string>(HeaderNames.Protocol, "websocket"),
            new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(HeaderNames.Path, "/chat"),
            new KeyValuePair<string, string>(HeaderNames.Authority, "server.example.com"),
            new KeyValuePair<string, string>(HeaderNames.WebSocketSubProtocols, "chat, superchat"),
            new KeyValuePair<string, string>(HeaderNames.SecWebSocketExtensions, "permessage-deflate"),
            new KeyValuePair<string, string>(HeaderNames.SecWebSocketVersion, "13"),
            new KeyValuePair<string, string>(HeaderNames.Origin, "http://www.example.com"),
        };
        await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS, headers);
        await SendDataAsync(1, Array.Empty<byte>(), endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 36,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: false, handler: this);

        Assert.Equal(3, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);
        Assert.Equal("0", _decodedHeaders["content-length"]);
    }

    [Fact]
    public async Task HEADERS_Received_ExtendedCONNECTMethod_Accepted()
    {
        await InitializeConnectionAsync(async context =>
        {
            var connectFeature = context.Features.Get<IHttpExtendedConnectFeature>();
            Assert.True(connectFeature.IsExtendedConnect);
            Assert.Equal(HttpMethods.Connect, context.Request.Method);
            Assert.Equal("websocket", connectFeature.Protocol);
            Assert.False(context.Request.Headers.TryGetValue(":protocol", out var _));
            Assert.Equal("http", context.Request.Scheme);
            Assert.Equal("/chat", context.Request.Path.Value);
            Assert.Equal("server.example.com", context.Request.Host.Value);
            Assert.Equal("chat, superchat", context.Request.Headers.WebSocketSubProtocols);
            Assert.Equal("permessage-deflate", context.Request.Headers.SecWebSocketExtensions);
            Assert.Equal("13", context.Request.Headers.SecWebSocketVersion);
            Assert.Equal("http://www.example.com", context.Request.Headers.Origin);

            Assert.Equal(0, await context.Request.Body.ReadAsync(new byte[1]));

            var stream = await connectFeature.AcceptAsync();
            Assert.Equal(0, await stream.ReadAsync(new byte[1]));
            await stream.WriteAsync(new byte[] { 0x01 });
        });

        // HEADERS + END_HEADERS
        // :method = CONNECT
        // :protocol = websocket
        // :scheme = https
        // :path = /chat
        // :authority = server.example.com
        // sec-websocket-protocol = chat, superchat
        // sec-websocket-extensions = permessage-deflate
        // sec-websocket-version = 13
        // origin = http://www.example.com
        var headers = new[]
        {
            new KeyValuePair<string, string>(HeaderNames.Method, "CONNECT"),
            new KeyValuePair<string, string>(HeaderNames.Protocol, "websocket"),
            new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(HeaderNames.Path, "/chat"),
            new KeyValuePair<string, string>(HeaderNames.Authority, "server.example.com"),
            new KeyValuePair<string, string>(HeaderNames.WebSocketSubProtocols, "chat, superchat"),
            new KeyValuePair<string, string>(HeaderNames.SecWebSocketExtensions, "permessage-deflate"),
            new KeyValuePair<string, string>(HeaderNames.SecWebSocketVersion, "13"),
            new KeyValuePair<string, string>(HeaderNames.Origin, "http://www.example.com"),
        };
        await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS, headers);
        await SendDataAsync(1, Array.Empty<byte>(), endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: false, handler: this);

        Assert.Equal(2, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);

        var dataFrame = await ExpectAsync(Http2FrameType.DATA,
            withLength: 1,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        Assert.Equal(0x01, dataFrame.Payload.Span[0]);

        dataFrame = await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
    }

    [Fact]
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/42133")]
    public async Task HEADERS_Received_SecondRequest_Accepted()
    {
        // Add stream to Http2Connection._completedStreams inline with SetResult().
        var appDelegateTcs = new TaskCompletionSource();
        await InitializeConnectionAsync(async context =>
        {
            var connectFeature = context.Features.Get<IHttpExtendedConnectFeature>();
            Assert.True(connectFeature.IsExtendedConnect);
            Assert.Equal(HttpMethods.Connect, context.Request.Method);
            Assert.Equal("websocket", connectFeature.Protocol);
            Assert.False(context.Request.Headers.TryGetValue(":protocol", out var _));
            Assert.Equal("http", context.Request.Scheme);
            Assert.Equal("/chat", context.Request.Path.Value);
            Assert.Equal("server.example.com", context.Request.Host.Value);
            Assert.Equal("chat, superchat", context.Request.Headers.WebSocketSubProtocols);
            Assert.Equal("permessage-deflate", context.Request.Headers.SecWebSocketExtensions);
            Assert.Equal("13", context.Request.Headers.SecWebSocketVersion);
            Assert.Equal("http://www.example.com", context.Request.Headers.Origin);

            Assert.Equal(0, await context.Request.Body.ReadAsync(new byte[1]));

            var stream = await connectFeature.AcceptAsync();
            Assert.Equal(0, await stream.ReadAsync(new byte[1]));
            await stream.WriteAsync(new byte[] { 0x01 });
            await appDelegateTcs.Task;
        });

        // HEADERS + END_HEADERS
        // :method = CONNECT
        // :protocol = websocket
        // :scheme = https
        // :path = /chat
        // :authority = server.example.com
        // sec-websocket-protocol = chat, superchat
        // sec-websocket-extensions = permessage-deflate
        // sec-websocket-version = 13
        // origin = http://www.example.com
        var headers = new[]
        {
            new KeyValuePair<string, string>(HeaderNames.Method, "CONNECT"),
            new KeyValuePair<string, string>(HeaderNames.Protocol, "websocket"),
            new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(HeaderNames.Path, "/chat"),
            new KeyValuePair<string, string>(HeaderNames.Authority, "server.example.com"),
            new KeyValuePair<string, string>(HeaderNames.WebSocketSubProtocols, "chat, superchat"),
            new KeyValuePair<string, string>(HeaderNames.SecWebSocketExtensions, "permessage-deflate"),
            new KeyValuePair<string, string>(HeaderNames.SecWebSocketVersion, "13"),
            new KeyValuePair<string, string>(HeaderNames.Origin, "http://www.example.com"),
        };
        await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS, headers);
        await SendDataAsync(1, Array.Empty<byte>(), endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: false, handler: this);

        Assert.Equal(2, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);

        var dataFrame = await ExpectAsync(Http2FrameType.DATA,
            withLength: 1,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        Assert.Equal(0x01, dataFrame.Payload.Span[0]);

        appDelegateTcs.TrySetResult();

        dataFrame = await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        // TriggerTick will trigger the stream to be returned to the pool so we can assert it
        TriggerTick();

        // Stream has been returned to the pool
        Assert.Equal(1, _connection.StreamPool.Count);
        Assert.True(_connection.StreamPool.TryPeek(out var pooledStream));

        await SendHeadersAsync(3, Http2HeadersFrameFlags.END_HEADERS, headers);
        await SendDataAsync(3, Array.Empty<byte>(), endStream: true);

        headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 2,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 3);

        _decodedHeaders.Clear();
        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: false, handler: this);

        Assert.Equal(2, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", _decodedHeaders[HeaderNames.Status]);

        dataFrame = await ExpectAsync(Http2FrameType.DATA,
            withLength: 1,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 3);
        Assert.Equal(0x01, dataFrame.Payload.Span[0]);

        dataFrame = await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 3);

        await StopConnectionAsync(expectedLastStreamId: 3, ignoreNonGoAwayFrames: false);
    }

    [Theory]
    [InlineData(":path", "/")]
    [InlineData(":scheme", "http")]
    public async Task HEADERS_Received_ExtendedCONNECTMethod_WithoutSchemeOrPath_Reset(string headerName, string value)
    {
        await InitializeConnectionAsync(_noopApplication);

        // :path and :scheme are required with :protocol, :authority is optional
        var headers = new[]
        {
            new KeyValuePair<string, string>(HeaderNames.Method, "CONNECT"),
            new KeyValuePair<string, string>(HeaderNames.Protocol, "WebSocket"),
            new KeyValuePair<string, string>(headerName, value)
        };
        await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM, headers);

        await WaitForStreamErrorAsync(expectedStreamId: 1, Http2ErrorCode.PROTOCOL_ERROR, CoreStrings.ConnectRequestsWithProtocolRequireSchemeAndPath);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
    }

    [Fact]
    public async Task HEADERS_Received_ProtocolWithoutCONNECTMethod_Reset()
    {
        await InitializeConnectionAsync(_noopApplication);

        var headers = new[]
        {
            new KeyValuePair<string, string>(HeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(HeaderNames.Path, "/"),
            new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(HeaderNames.Authority, "example.com"),
            new KeyValuePair<string, string>(HeaderNames.Protocol, "WebSocket")
        };
        await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM, headers);

        await WaitForStreamErrorAsync(expectedStreamId: 1, Http2ErrorCode.PROTOCOL_ERROR, CoreStrings.ProtocolRequiresConnect);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
    }
}
