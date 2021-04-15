using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;

namespace FinancialDiaryApi.Middleware
{
	// You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
	public class AuthorizationMiddleware
	{
		private readonly RequestDelegate _next;

		public AuthorizationMiddleware(RequestDelegate next)
		{
			_next = next;
		}

		public Task Invoke(HttpContext httpContext)
		{
			try
			{
				var idToken = httpContext.Request.Headers["Authorization"];
				var user = httpContext.Request.Headers["User"];
				var validPayload = GoogleJsonWebSignature.ValidateAsync(idToken).Result ?? throw new UnauthorizedAccessException("Token is invalid or expired.");
				if (user.Equals(validPayload.Email))
				{
					return _next(httpContext);
				}

				httpContext.Response.Clear();
				httpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
				httpContext.Response.WriteAsync("UnAuthorized. API is accessible only through application");

			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				httpContext.Response.Clear();
				httpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
				httpContext.Response.WriteAsync("UnAuthorized. API is accessible only through application");
			}

			return null;
		}
	}

	// Extension method used to add the middleware to the HTTP request pipeline.
	public static class AuthorizationMiddlewareExtensions
	{
		public static IApplicationBuilder UseAuthorizationMiddleware(this IApplicationBuilder builder)
		{
			return builder.UseMiddleware<AuthorizationMiddleware>();
		}
	}
}
