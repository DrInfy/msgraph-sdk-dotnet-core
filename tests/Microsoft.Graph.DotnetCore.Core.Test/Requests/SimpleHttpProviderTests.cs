﻿// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

namespace Microsoft.Graph.DotnetCore.Core.Test.Requests
{
    using Microsoft.Graph.DotnetCore.Core.Test.Mocks;
    using Moq;
    using System;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;
    public class SimpleHttpProviderTests:IDisposable
    {
        private SimpleHttpProvider simpleHttpProvider;
        private readonly MockSerializer serializer;
        private readonly TestHttpMessageHandler testHttpMessageHandler;
        private readonly MockAuthenticationProvider authProvider;

        public SimpleHttpProviderTests()
        {
            this.testHttpMessageHandler = new TestHttpMessageHandler();
            this.authProvider = new MockAuthenticationProvider();
            this.serializer = new MockSerializer();

            var defaultHandlers = GraphClientFactory.CreateDefaultHandlers(authProvider.Object);
            var httpClient = GraphClientFactory.Create(handlers: defaultHandlers, finalHandler: testHttpMessageHandler);

            this.simpleHttpProvider = new SimpleHttpProvider(httpClient, this.serializer.Object);
        }

        public void Dispose()
        {
            this.simpleHttpProvider.Dispose();
        }

        [Fact]
        public async Task SendAsync()
        {
            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://localhost"))
            using (var httpResponseMessage = new HttpResponseMessage())
            {
                this.testHttpMessageHandler.AddResponseMapping(httpRequestMessage.RequestUri.ToString(), httpResponseMessage);
                var returnedResponseMessage = await this.simpleHttpProvider.SendAsync(httpRequestMessage);
                Assert.True(returnedResponseMessage.RequestMessage.Headers.Contains(CoreConstants.Headers.FeatureFlag));
                Assert.Equal(httpResponseMessage, returnedResponseMessage);
            }
        }


        [Fact]
        public async Task SendAsync_ThrowsClientGeneralException()
        {
            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://localhost"))
            {
                this.simpleHttpProvider.Dispose();
                var clientException = new Exception();

                var defaultHandlers = GraphClientFactory.CreateDefaultHandlers(authProvider.Object);
                var httpClient = GraphClientFactory.Create(handlers: defaultHandlers, finalHandler: new ExceptionHttpMessageHandler(clientException));
                this.simpleHttpProvider = new SimpleHttpProvider(httpClient, this.serializer.Object);

                ServiceException exception = await Assert.ThrowsAsync<ServiceException>(async () => await this.simpleHttpProvider.SendAsync(
                    httpRequestMessage, HttpCompletionOption.ResponseContentRead, CancellationToken.None));

                Assert.True(exception.IsMatch(ErrorConstants.Codes.GeneralException));
                Assert.Equal(ErrorConstants.Messages.UnexpectedExceptionOnSend, exception.Error.Message);
                Assert.Equal(clientException, exception.InnerException);
            }
        }

        [Fact]

        public async Task SendAsync_ThrowsClientTimeoutException()
        {
            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://localhost"))
            {
                this.simpleHttpProvider.Dispose();

                var clientException = new TaskCanceledException();
                var defaultHandlers = GraphClientFactory.CreateDefaultHandlers(authProvider.Object);
                var httpClient = GraphClientFactory.Create(handlers: defaultHandlers, finalHandler: new ExceptionHttpMessageHandler(clientException));
                this.simpleHttpProvider = new SimpleHttpProvider(httpClient, this.serializer.Object);

                ServiceException exception = await Assert.ThrowsAsync<ServiceException>(async () => await this.simpleHttpProvider.SendAsync(
                    httpRequestMessage, HttpCompletionOption.ResponseContentRead, CancellationToken.None));

                Assert.True(exception.IsMatch(ErrorConstants.Codes.Timeout));
                Assert.Equal(ErrorConstants.Messages.RequestTimedOut, exception.Error.Message);
                Assert.Equal(clientException, exception.InnerException);
            }
        }

        [Fact]
        public async Task SendAsync_ThrowsServiceExceptionOnInvalidRedirectResponse()
        {
            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://localhost"))
            using (var httpResponseMessage = new HttpResponseMessage())
            {
                httpResponseMessage.StatusCode = HttpStatusCode.Redirect;
                httpResponseMessage.RequestMessage = httpRequestMessage;
                this.testHttpMessageHandler.AddResponseMapping(httpRequestMessage.RequestUri.ToString(), httpResponseMessage);

                ServiceException exception = await Assert.ThrowsAsync<ServiceException>(async () => await this.simpleHttpProvider.SendAsync(httpRequestMessage));
                Assert.True(exception.IsMatch(ErrorConstants.Codes.GeneralException));
                Assert.Equal(
                    ErrorConstants.Messages.LocationHeaderNotSetOnRedirect,
                    exception.Error.Message);
            }
        }

        [Fact]
        public async Task SendAsync_VerifiesHeadersOnRedirect()
        {
            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://localhost"))
            using (var redirectResponseMessage = new HttpResponseMessage())
            using (var finalResponseMessage = new HttpResponseMessage())
            {
                httpRequestMessage.Headers.Add("testHeader", "testValue");

                redirectResponseMessage.StatusCode = HttpStatusCode.Redirect;
                redirectResponseMessage.Headers.Location = new Uri("https://localhost/redirect");
                redirectResponseMessage.RequestMessage = httpRequestMessage;

                this.testHttpMessageHandler.AddResponseMapping(httpRequestMessage.RequestUri.ToString(), redirectResponseMessage);
                this.testHttpMessageHandler.AddResponseMapping(redirectResponseMessage.Headers.Location.ToString(), finalResponseMessage);

                var returnedResponseMessage = await this.simpleHttpProvider.SendAsync(httpRequestMessage);

                Assert.Equal(6, finalResponseMessage.RequestMessage.Headers.Count());

                foreach (var header in httpRequestMessage.Headers)
                {
                    var actualValues = finalResponseMessage.RequestMessage.Headers.GetValues(header.Key);

                    var enumerable = actualValues as string[] ?? actualValues.ToArray();
                    Assert.Equal(enumerable.Length, header.Value.Count());

                    foreach (var headerValue in header.Value)
                    {
                        Assert.Contains(headerValue, enumerable);
                    }
                }

                Assert.Equal(finalResponseMessage, returnedResponseMessage);
            }
        }

        [Fact]
        public async Task SendAsync_TThrowsServiceExceptionOnMaxRedirects()
        {
            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://localhost"))
            using (var redirectResponseMessage = new HttpResponseMessage())
            using (var tooManyRedirectsResponseMessage = new HttpResponseMessage())
            {
                redirectResponseMessage.StatusCode = HttpStatusCode.Redirect;
                redirectResponseMessage.Headers.Location = new Uri("https://localhost/redirect");
                tooManyRedirectsResponseMessage.StatusCode = HttpStatusCode.Redirect;
                tooManyRedirectsResponseMessage.Headers.Location = new Uri("https://localhost");

                redirectResponseMessage.RequestMessage = httpRequestMessage;

                this.testHttpMessageHandler.AddResponseMapping(httpRequestMessage.RequestUri.ToString(), redirectResponseMessage);
                this.testHttpMessageHandler.AddResponseMapping(redirectResponseMessage.Headers.Location.ToString(), tooManyRedirectsResponseMessage);

                httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue(CoreConstants.Headers.Bearer, "ticket");

                ServiceException exception = await Assert.ThrowsAsync<ServiceException>(async () => await this.simpleHttpProvider.SendAsync(
                    httpRequestMessage,
                    HttpCompletionOption.ResponseContentRead,
                    CancellationToken.None));

                Assert.True(exception.IsMatch(ErrorConstants.Codes.TooManyRedirects));
                Assert.Equal(
                    string.Format(ErrorConstants.Messages.TooManyRedirectsFormatString, "5"),
                    exception.Error.Message);
            }
        }

        [Fact]
        public async Task SendAsync_ThrowsServiceExceptionWithEmptyMessageOnHTTPNotFoundWithoutErrorBody()
        {
            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "https://localhost"))
            using (var stringContent = new StringContent("test"))
            using (var httpResponseMessage = new HttpResponseMessage())
            {
                httpResponseMessage.Content = stringContent;
                httpResponseMessage.StatusCode = HttpStatusCode.NotFound;

                this.testHttpMessageHandler.AddResponseMapping(httpRequestMessage.RequestUri.ToString(), httpResponseMessage);
                this.serializer.Setup(
                        mySerializer => mySerializer.DeserializeObject<ErrorResponse>(
                            It.IsAny<Stream>()))
                    .Returns((ErrorResponse)null);

                ServiceException exception = await Assert.ThrowsAsync<ServiceException>(async () => await this.simpleHttpProvider.SendAsync(httpRequestMessage));
                Assert.True(exception.IsMatch(ErrorConstants.Codes.ItemNotFound));
                Assert.True(string.IsNullOrEmpty(exception.Error.Message));
            }
        }

        [Fact]
        public async Task SendAsync_ThrowsServiceExceptionWithMessageOnHTTPNotFoundWithBody()
        {
            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://localhost"))
            using (var stringContent = new StringContent("test"))
            using (var httpResponseMessage = new HttpResponseMessage())
            {
                httpResponseMessage.Content = stringContent;
                httpResponseMessage.StatusCode = HttpStatusCode.InternalServerError;

                this.testHttpMessageHandler.AddResponseMapping(httpRequestMessage.RequestUri.ToString(), httpResponseMessage);
                var expectedError = new ErrorResponse
                {
                    Error = new Error
                    {
                        Code = ErrorConstants.Codes.ItemNotFound,
                        Message = "Error message"
                    }
                };

                this.serializer.Setup(mySerializer => mySerializer.DeserializeObject<ErrorResponse>(It.IsAny<Stream>())).Returns(expectedError);

                ServiceException exception = await Assert.ThrowsAsync<ServiceException>(async () => await this.simpleHttpProvider.SendAsync(httpRequestMessage));
                Assert.Equal(expectedError.Error.Code, exception.Error.Code);
                Assert.Equal(expectedError.Error.Message, exception.Error.Message);
            }
        }

        [Fact]
        public async Task SendAsync_ThrowsServiceExceptionWithThrowSiteHeader()
        {
            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://localhost"))
            using (var httpResponseMessage = new HttpResponseMessage())
            {
                const string throwSite = "throw site";

                httpResponseMessage.StatusCode = HttpStatusCode.BadRequest;
                httpResponseMessage.Headers.Add(CoreConstants.Headers.ThrowSiteHeaderName, throwSite);
                httpResponseMessage.RequestMessage = httpRequestMessage;

                this.testHttpMessageHandler.AddResponseMapping(httpRequestMessage.RequestUri.ToString(), httpResponseMessage);

                this.serializer.Setup(
                        mySerializer => mySerializer.DeserializeObject<ErrorResponse>(
                            It.IsAny<Stream>()))
                    .Returns(new ErrorResponse { Error = new Error() });

                ServiceException exception = await Assert.ThrowsAsync<ServiceException>(async () => await this.simpleHttpProvider.SendAsync(httpRequestMessage));
                Assert.NotNull(exception.Error);
                Assert.Equal(throwSite, exception.Error.ThrowSite);
            }
        }

    }
}
