using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Timberborn.SingletonSystem;
using UnityEngine;

namespace LouisPaulet.AiHarness {
  public class AiHarnessServer : ILoadableSingleton, IUnloadableSingleton {

    private const string Prefix = "http://localhost:8080/";

    private readonly IAiHarnessRequestHandler _requestHandler;
    private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

    private TcpListener? _listener;

    public AiHarnessServer(IAiHarnessRequestHandler requestHandler) {
      _requestHandler = requestHandler;
    }

    public void Load() {
      try {
        _listener = new TcpListener(IPAddress.Loopback, 8080);
        _listener.Start();
        _ = Task.Run(Listen);
        Debug.Log("[LouisPaulet.AiHarness] AI Harness HTTP server listening on " + Prefix);
      } catch (Exception exception) {
        Debug.LogError("[LouisPaulet.AiHarness] Failed to start AI Harness HTTP server on " + Prefix + ": " + exception.Message);
        Debug.LogException(exception);
      }
    }

    public void Unload() {
      _cancellationTokenSource.Cancel();
      if (_listener == null) {
        return;
      }

      try {
        _listener.Stop();
      } catch (Exception exception) {
        Debug.LogException(exception);
      }
    }

    private async Task Listen() {
      while (!_cancellationTokenSource.IsCancellationRequested && _listener != null) {
        TcpClient client;
        try {
          client = await _listener.AcceptTcpClientAsync();
        } catch (ObjectDisposedException) {
          return;
        } catch (SocketException) {
          if (_cancellationTokenSource.IsCancellationRequested) {
            return;
          }

          throw;
        } catch (Exception exception) {
          Debug.LogException(exception);
          continue;
        }

        _ = Task.Run(() => Handle(client));
      }
    }

    private void Handle(TcpClient client) {
      using (client) {
        NetworkStream stream = client.GetStream();
        HttpRequest request = ReadRequest(stream);
        if (request.Path == null) {
          WritePlainText(stream, 400, "Bad request");
          return;
        }

        AiHarnessResponse response;
        try {
          response = _requestHandler.HandleRequest(request.Method, request.Path, name => {
            request.Query.TryGetValue(name, out string? value);
            return value;
          });
        } catch (Exception exception) {
          Debug.LogException(exception);
          WritePlainText(stream, 500, exception.Message);
          return;
        }

        WriteJson(stream, AiHarnessResponse.StatusCodeFor(response), response);
      }
    }

    private static HttpRequest ReadRequest(Stream stream) {
      using (var reader = new StreamReader(stream, System.Text.Encoding.UTF8, false, 1024, true)) {
        string? requestLine = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(requestLine)) {
          return HttpRequest.BadRequest();
        }

        string[] parts = requestLine.Split(' ');
        if (parts.Length < 2) {
          return HttpRequest.BadRequest();
        }

        while (!string.IsNullOrEmpty(reader.ReadLine())) {
        }

        string method = parts[0];
        string target = parts[1];
        int queryIndex = target.IndexOf('?');
        string path = queryIndex >= 0 ? target.Substring(0, queryIndex) : target;
        string queryText = queryIndex >= 0 ? target.Substring(queryIndex + 1) : "";
        return new HttpRequest(method, path.TrimEnd('/'), ParseQuery(queryText));
      }
    }

    private static Dictionary<string, string> ParseQuery(string queryText) {
      var query = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
      if (string.IsNullOrWhiteSpace(queryText)) {
        return query;
      }

      string[] pairs = queryText.Split('&');
      foreach (string pair in pairs) {
        if (string.IsNullOrWhiteSpace(pair)) {
          continue;
        }

        int equalsIndex = pair.IndexOf('=');
        string name = equalsIndex >= 0 ? pair.Substring(0, equalsIndex) : pair;
        string value = equalsIndex >= 0 ? pair.Substring(equalsIndex + 1) : "";
        query[Decode(name)] = Decode(value);
      }

      return query;
    }

    private static string Decode(string value) {
      return Uri.UnescapeDataString(value.Replace("+", " "));
    }

    private static void WriteJson(Stream stream, int statusCode, AiHarnessResponse response) {
      string json = AiHarnessJson.Serialize(response);
      WriteResponse(stream, statusCode, "application/json; charset=utf-8", json);
    }

    private static void WritePlainText(Stream stream, int statusCode, string text) {
      WriteResponse(stream, statusCode, "text/plain; charset=utf-8", text);
    }

    private static void WriteResponse(Stream stream, int statusCode, string contentType, string body) {
      byte[] bodyBytes = System.Text.Encoding.UTF8.GetBytes(body);
      string headers = "HTTP/1.1 " + statusCode + " " + ReasonPhrase(statusCode) + "\r\n"
          + "Content-Type: " + contentType + "\r\n"
          + "Content-Length: " + bodyBytes.Length + "\r\n"
          + "Connection: close\r\n"
          + "\r\n";
      byte[] headerBytes = System.Text.Encoding.ASCII.GetBytes(headers);
      stream.Write(headerBytes, 0, headerBytes.Length);
      stream.Write(bodyBytes, 0, bodyBytes.Length);
      stream.Flush();
    }

    private static string ReasonPhrase(int statusCode) {
      switch (statusCode) {
        case 200:
          return "OK";
        case 400:
          return "Bad Request";
        case 404:
          return "Not Found";
        case 500:
          return "Internal Server Error";
        default:
          return "OK";
      }
    }

    private sealed class HttpRequest {

      public HttpRequest(string method, string path, Dictionary<string, string> query) {
        Method = method;
        Path = path;
        Query = query;
      }

      public string Method { get; }
      public string? Path { get; }
      public Dictionary<string, string> Query { get; }

      public static HttpRequest BadRequest() {
        return new HttpRequest("GET", null!, new Dictionary<string, string>());
      }

    }

  }
}
