﻿using System;
using System.Net;
using CSharpTradeOffers;
using Moq;
using NUnit.Framework;

namespace CSharpTradeOffers_Tests
{
    [TestFixture]
    public sealed class WebTests
    {
        [SetUp]
        public void BeforeEachTest()
        {
            // Create mock objects for the web.
            // --------------------------------
            // Our main goal here is to create a Web object which doesn't talk over the internet.
            // For this, we need to create a mock IWebRequestHandler.
            // This is because Web's constructor expects an IWebRequestHandler. It is 'dependent' on this object.
            // However, it's a bit tricky creating a mock IWebRequestHandler. Below is the steps taken to create this mock. 
            // -----------------------------------------------------------------------------------------------------------

            // You can tell mock to return something from an input using Setup().Returns().
            // Here I'm telling it, whenever 'ReadStream()' is called, return a known response (the SteamStreamResponse string defined below)
            _mockSteamStream = new Mock<IResponseStream>();
            _mockSteamStream.Setup(x => x.ReadStream()).Returns(SteamStreamResponse);

            // Again, here I want to make a mock IResponse object.
            // To make my tests pass, all I want it to do is return a mock stream (defined above).
            // Notice I have to call _mockSteamStream.Object. calling '.Object' gets the underlying mocked up object created (the IResponseStream).
            _mockResponse = new Mock<IResponse>();
            _mockResponse.Setup(x => x.GetResponseStream()).Returns(_mockSteamStream.Object);

            // Once more, I need one more mock object before testing the Web Class.
            // I don't want Web to actually talk over the internet, so I'm creating a mock WebRequestHandler.
            // This mock request handler is only set up to return pre-determined values (set up above).
            // For example, when given a request of 'HandleWebRequest(Url, Method, null, null, true, "")', it will always return a mock response (defined above)
            _mockRequestHandler = new Mock<IWebRequestHandler<IResponse>>();
            _mockRequestHandler.Setup(x => x.HandleWebRequest(Url, Method, null, null, true, "")).Returns(_mockResponse.Object);

            // So, follow this mock structure from bottom to top...
            // 1) Mock Web Request Contains a mock response
            // 2) Mock Response ontains a Mock Stream.
            // 3) Mock Stream will always return 'SteamStreamResponse', i.e. "Response From Stream".
            _web = new Web(_mockRequestHandler.Object);
        }

        private const string Url = "url";
        private const string Method = "method";

        /// <summary>
        /// What we expect the stream to contain.
        /// </summary>
        private const string SteamStreamResponse = "Response From Steam";

        private Mock<IWebRequestHandler<IResponse>> _mockRequestHandler;
        private Mock<IResponseStream> _mockSteamStream;
        private Mock<IResponse> _mockResponse;
        private Web _web;

        [Test]
        public void FetchStream_ShouldFetchStream()
        {
            IResponseStream steamStream = _web.FetchStream(Url, Method);

            Assert.AreEqual(steamStream, _mockSteamStream.Object);
        }

        [Test]
        public void RetryFetch_ReturnsNull_WhenExceedingRetryCount()
        {
            _mockRequestHandler.Setup(x => x.HandleWebRequest(Url, Method, null, null, true, "")).Throws<WebException>();

            string response = _web.RetryFetch(new TimeSpan(1), 2, Url, Method);
            Assert.IsNull(response);
        }

        [Test]
        public void RetryFetch_ReturnsSteamStreamResponse_WhenNotExceedingRetryCount()
        {
            string response = _web.RetryFetch(new TimeSpan(1), 2, Url, Method);
            Assert.AreEqual(response, SteamStreamResponse);
        }

        [Test]
        public void RetryFetchStream_ReturnsNull_WhenExceedingRetryCount()
        {
            // Here I can ask the mock handler to always throw a web exception. This is to test the web method's implementation.
            _mockRequestHandler.Setup(x => x.HandleWebRequest(Url, Method, null, null, true, "")).Throws<WebException>();

            IResponseStream steamStream = _web.RetryFetchStream(new TimeSpan(1), 2, Url, Method);

            Assert.IsNull(steamStream);
        }

        [Test]
        public void RetryFetchStream_ReturnsSteamStream_WhenNotExceedingRetryCount()
        {
            IResponseStream steamStream = _web.RetryFetchStream(new TimeSpan(1), 2, Url, Method);

            Assert.AreEqual(steamStream, _mockSteamStream.Object);
        }

        [Test]
        public void WebRequest_Fetch_ReturnsResponseStream()
        {
            string responseStream = _web.Fetch(Url, Method);

            Assert.AreEqual(responseStream, SteamStreamResponse);
        }

        [Test]
        public void WebRequest_ShouldReturnResponse()
        {
            IResponse response = _web.Request(Url, Method);

            Assert.AreEqual(response, _mockResponse.Object);
        }
    }
}