using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using InvoiceSystem.Web.Infrastructure.Behaviors;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace InvoiceSystem.Tests.Unit.Infrastructure.Behaviors
{
    public class PerformanceBehaviorTests
    {
        private readonly ILogger<PerformanceBehavior<TestRequest, TestResponse>> _logger;
        private readonly PerformanceBehavior<TestRequest, TestResponse> _behavior;

        public PerformanceBehaviorTests()
        {
            _logger = Substitute.For<ILogger<PerformanceBehavior<TestRequest, TestResponse>>>();
            _behavior = new PerformanceBehavior<TestRequest, TestResponse>(_logger);
        }

        public record TestRequest : IRequest<TestResponse>;
        public record TestResponse(string Value);

        [Fact]
        public async Task Handle_Should_Call_Next_And_Return_Response()
        {
            // Arrange
            var request = new TestRequest();
            var expectedResponse = new TestResponse("Success");
            RequestHandlerDelegate<TestResponse> next = () => Task.FromResult(expectedResponse);

            // Act
            var result = await _behavior.Handle(request, next, CancellationToken.None);

            // Assert
            result.Should().Be(expectedResponse);
        }

        [Fact]
        public async Task Handle_Should_Log_Warning_When_Execution_Exceeds_Threshold()
        {
            // Arrange
            var request = new TestRequest();
            var expectedResponse = new TestResponse("Success");
            RequestHandlerDelegate<TestResponse> next = async () =>
            {
                await Task.Delay(550); // Exceeds 500ms threshold
                return expectedResponse;
            };

            // Act
            var result = await _behavior.Handle(request, next, CancellationToken.None);

            // Assert
            result.Should().Be(expectedResponse);

            // Verify LogWarning was called
            _logger.Received(1).Log(
                LogLevel.Warning,
                Arg.Any<EventId>(),
                Arg.Is<object>(o => o.ToString()!.Contains("Wolne zapytanie")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>()
            );
        }

        [Fact]
        public async Task Handle_Should_Not_Log_Warning_When_Execution_Is_Under_Threshold()
        {
            // Arrange
            var request = new TestRequest();
            var expectedResponse = new TestResponse("Success");
            RequestHandlerDelegate<TestResponse> next = () => Task.FromResult(expectedResponse);

            // Act
            var result = await _behavior.Handle(request, next, CancellationToken.None);

            // Assert
            result.Should().Be(expectedResponse);

            // Verify LogWarning was NOT called
            _logger.DidNotReceive().Log(
                LogLevel.Warning,
                Arg.Any<EventId>(),
                Arg.Any<object>(),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>()
            );
        }
    }
}
