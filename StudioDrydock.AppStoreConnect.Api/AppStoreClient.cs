using System;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;

namespace StudioDrydock.AppStoreConnect.Api
{
	public partial class AppStoreClient
	{
		private static readonly JsonSerializerOptions JsonSerializerOptions = new()
		{
			// Null fields must not be serialized in requests, as the App Store API requires
			// that certain fields are not submitted if not updating (for example, whatsNew
			// text for an initial version).
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
		};

		private readonly Uri m_BaseUri = new("https://api.appstoreconnect.apple.com");
		private readonly HttpClient m_Client = new();
		private readonly HttpClient m_UploadClient = new();
		private readonly RateLimiter m_RateLimiter = new();
		private readonly IAppStoreClientTokenMaker m_TokenMaker;

		public AppStoreClient(IAppStoreClientTokenMaker tokenMaker)
		{
			m_TokenMaker = tokenMaker;
		}

		private static StringContent Serialize(object obj)
		{
			string text = JsonSerializer.Serialize(obj, options: JsonSerializerOptions);
			return new StringContent(text, encoding: Encoding.UTF8, mediaType: "application/json");
		}

		private async Task SendAsync(HttpRequestMessage request)
		{
			await SendInternal(request);
		}

		private async Task<T> SendAsync<T>(HttpRequestMessage request)
		{
			var response = await SendInternal(request);

			string responseText = await response.Content.ReadAsStringAsync();
			var responseObject = JsonSerializer.Deserialize<T>(responseText);
			if (responseObject == null)
			{
				Trace.TraceError(await response.Content.ReadAsStringAsync());
				throw new Exception($"Deserialization failed");
			}

			return responseObject;
		}

		private async Task<HttpResponseMessage> SendInternal(HttpRequestMessage request)
		{
			using var token = await m_RateLimiter.Begin();

			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", m_TokenMaker.MakeToken());

			Trace.TraceInformation($"{request.Method} {request.RequestUri}");

			var response = await m_Client.SendAsync(request);

			if (!response.IsSuccessStatusCode)
			{
				Trace.TraceError(await response.Content.ReadAsStringAsync());
				throw new Exception($"Status code {response.StatusCode}");
			}

			// see: https://developer.apple.com/documentation/appstoreconnectapi/identifying-rate-limits
			if (response.Headers.TryGetValues("X-Rate-Limit", out var contentRange))
			{
				var rateLimit = contentRange.LastOrDefault();
				if (rateLimit != null)
				{
					var match = Regex.Match(rateLimit, @"user-hour-lim:(\d+);user-hour-rem:(\d+);");
					if (match.Success)
					{
						var remaining = int.Parse(match.Groups[2].Value);
						Trace.TraceInformation($"Req left this hour: {remaining}");
						m_RateLimiter.SetRequestsRemainingThisHour(remaining);
					}
				}
			}

			return response;
		}

		public async Task UploadPortion(string method, string url, byte[] data, Dictionary<string, string> requestHeaders)
		{
			HttpMethod httpMethod;
			switch (method.ToUpperInvariant())
			{
				case "POST":
					httpMethod = HttpMethod.Post;
					break;
				case "PUT":
					httpMethod = HttpMethod.Put;
					break;
				default:
					throw new Exception($"Unknown method {method}");
			}

			var request = new HttpRequestMessage(httpMethod, url);
			request.Content = new ByteArrayContent(data);
			foreach (var header in requestHeaders)
			{
				if (header.Key == "Content-Type")
				{
					request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(header.Value);
					continue;
				}
				request.Headers.Add(header.Key, header.Value);
			}
			Trace.TraceInformation($"{request.Method} {request.RequestUri}");
			var response = await m_UploadClient.SendAsync(request);
			if (!response.IsSuccessStatusCode)
			{
				Trace.TraceError(await response.Content.ReadAsStringAsync());
				throw new Exception($"Status code {response.StatusCode}");
			}
		}

		public Task<T> GetNextPage<T>(T prevPage)
			where T : IHasNextLink
		{
			if (prevPage.links.next == null)
			{
				throw new Exception("No next page");
			}

			var message = new HttpRequestMessage(HttpMethod.Get, prevPage.links.next);
			return SendAsync<T>(message);
		}

		public Task PostAppPreviewSets(object v)
		{
			throw new NotImplementedException();
		}

		public interface IHasNextLink
		{
			PagedDocumentLinks links { get; }
		}

		public interface INextLink
		{
			string? next { get; }
		}

		public interface IRequestHeaders
		{
			public string? name { get; }
			public string? value { get; }
		}

		public interface IUploadOperations
		{
			public string? method { get; }
			public string? url { get; }
			public int? length { get; }
			public int? offset { get; }
			public IReadOnlyList<IRequestHeaders>? requestHeaders { get; }
		}

	}
}