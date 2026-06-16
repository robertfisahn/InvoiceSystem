using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentAssertions;
using InvoiceSystem.Tests.Integration.Helpers;
using InvoiceSystem.Web.Domain.Entities;
using InvoiceSystem.Web.Infrastructure.Database;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InvoiceSystem.Tests.Integration.Features.Auth
{
    public class AuthIntegrationTests : IntegrationTestBase
    {
        private HttpClient CreateClient(bool skipAuth = false, bool allowAutoRedirect = false)
        {
            var options = new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = allowAutoRedirect
            };
            var client = _factory.CreateClient(options);
            
            if (skipAuth)
            {
                client.DefaultRequestHeaders.Add("X-Skip-Test-Auth", "true");
            }

            return client;
        }

        private static string ExtractAntiforgeryToken(string html)
        {
            var match = Regex.Match(html, @"<input name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""");
            if (!match.Success)
            {
                throw new Exception("Antiforgery token not found in the HTML content.");
            }
            return match.Groups[1].Value;
        }

        [Fact]
        public async Task Login_Get_ReturnsLoginForm_WhenNotAuthenticated()
        {
            // Arrange
            var client = CreateClient(skipAuth: true);

            // Act
            var response = await client.GetAsync("/auth/login");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("name=\"Username\"");
            content.Should().Contain("name=\"Password\"");
            content.Should().Contain("__RequestVerificationToken");
        }

        [Fact]
        public async Task Login_Get_RedirectsToDashboard_WhenAlreadyAuthenticated()
        {
            // Arrange
            var client = CreateClient(skipAuth: false);

            // Act
            var response = await client.GetAsync("/auth/login");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Redirect);
            response.Headers.Location?.ToString().Should().Be("/dashboard");
        }

        [Fact]
        public async Task Login_Post_RedirectsToDashboard_WhenCredentialsAreValid()
        {
            // Arrange
            var username = $"admin_{Guid.NewGuid():N}";
            var password = "Password123!";

            using (var scope = _factory.Services.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
                var user = new AppUser
                {
                    UserName = username,
                    Email = $"{username}@example.com",
                    FullName = "Test Auth Admin",
                    EmailConfirmed = true
                };
                var createResult = await userManager.CreateAsync(user, password);
                createResult.Succeeded.Should().BeTrue();
            }

            var client = CreateClient(skipAuth: true, allowAutoRedirect: false);

            // Get antiforgery token
            var loginPageResponse = await client.GetAsync("/auth/login");
            var loginHtml = await loginPageResponse.Content.ReadAsStringAsync();
            var token = ExtractAntiforgeryToken(loginHtml);

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Username", username),
                new KeyValuePair<string, string>("Password", password),
                new KeyValuePair<string, string>("__RequestVerificationToken", token)
            });

            // Act
            var response = await client.PostAsync("/auth/login", formContent);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Redirect);
            response.Headers.Location?.ToString().Should().Be("/dashboard");
        }

        [Fact]
        public async Task Login_Post_ReturnsFormWithValidationErrors_WhenFieldsAreEmpty()
        {
            // Arrange
            var client = CreateClient(skipAuth: true, allowAutoRedirect: false);

            var loginPageResponse = await client.GetAsync("/auth/login");
            var loginHtml = await loginPageResponse.Content.ReadAsStringAsync();
            var token = ExtractAntiforgeryToken(loginHtml);

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Username", ""),
                new KeyValuePair<string, string>("Password", ""),
                new KeyValuePair<string, string>("__RequestVerificationToken", token)
            });

            // Act
            var response = await client.PostAsync("/auth/login", formContent);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var decodedContent = System.Net.WebUtility.HtmlDecode(content);
            decodedContent.Should().Contain("Podaj nazwę użytkownika.");
            decodedContent.Should().Contain("Podaj hasło.");
        }

        [Fact]
        public async Task Login_Post_ReturnsFormWithModelError_WhenCredentialsAreInvalid()
        {
            // Arrange
            var client = CreateClient(skipAuth: true, allowAutoRedirect: false);

            var loginPageResponse = await client.GetAsync("/auth/login");
            var loginHtml = await loginPageResponse.Content.ReadAsStringAsync();
            var token = ExtractAntiforgeryToken(loginHtml);

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Username", "nonexistentuser"),
                new KeyValuePair<string, string>("Password", "WrongPassword!"),
                new KeyValuePair<string, string>("__RequestVerificationToken", token)
            });

            // Act
            var response = await client.PostAsync("/auth/login", formContent);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var decodedContent = System.Net.WebUtility.HtmlDecode(content);
            decodedContent.Should().Contain("Nieprawidłowa nazwa użytkownika lub hasło.");
        }

        [Fact]
        public async Task Logout_Post_RedirectsToLogin()
        {
            // Arrange
            var client = CreateClient(skipAuth: false, allowAutoRedirect: false);

            // Get antiforgery token from the dashboard page (since we are authenticated)
            var dashboardResponse = await client.GetAsync("/dashboard");
            dashboardResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var dashboardHtml = await dashboardResponse.Content.ReadAsStringAsync();
            var token = ExtractAntiforgeryToken(dashboardHtml);

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("__RequestVerificationToken", token)
            });

            // Act
            var response = await client.PostAsync("/auth/logout", formContent);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Redirect);
            response.Headers.Location?.ToString().Should().Be("/auth/login");
        }

        [Fact]
        public async Task GlobalAuth_ReturnsUnauthorized_WhenNotAuthenticated()
        {
            // Arrange
            var client = CreateClient(skipAuth: true, allowAutoRedirect: false);

            // Act
            var response = await client.GetAsync("/dashboard");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}
