﻿using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.ApplyService.Application.Apply.Gateway.ApplicationActions;
using SFA.DAS.ApplyService.Application.Apply.Gateway.Applications;
using SFA.DAS.ApplyService.Application.Apply.GetApplications;
using SFA.DAS.ApplyService.Configuration;
using SFA.DAS.ApplyService.Domain.Entities;
using SFA.DAS.ApplyService.Domain.Roatp;
using SFA.DAS.ApplyService.EmailService.Interfaces;
using SFA.DAS.ApplyService.InternalApi.Controllers;

namespace SFA.DAS.ApplyService.InternalApi.UnitTests.Controllers
{
    [TestFixture]
    public class GatewayReviewControllerTests
    {
        private GatewayReviewController _controller;
        private Mock<IMediator> _mediator;
        private Mock<IWithdrawApplicationConfirmationEmailService> _withdrawApplicationConfirmationEmailService;

        [SetUp]
        public void TestSetup()
        {
            _mediator = new Mock<IMediator>();
            _mediator.Setup(x => x.Send(It.IsAny<GetGatewayApplicationCountsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new GetGatewayApplicationCountsResponse
                {
                    NewApplicationsCount = 1,
                    InProgressApplicationsCount = 2,
                     ClosedApplicationsCount = 3
                });

            var logger = new Mock<ILogger<GatewayReviewController>>();

            var configurationService = new Mock<IConfigurationService>();
            configurationService.Setup(x => x.GetConfig()).ReturnsAsync(new ApplyConfig { SignInPage = "https://localhost/" });

            _withdrawApplicationConfirmationEmailService = new Mock<IWithdrawApplicationConfirmationEmailService>();

            _controller = new GatewayReviewController(_mediator.Object, logger.Object, configurationService.Object, _withdrawApplicationConfirmationEmailService.Object);
        }

        [Test]
        public async Task Get_ApplicationCounts_returns_expected_counts_for_the_user()
        {
            var result = await _controller.GetApplicationCounts();

            Assert.AreEqual(1, result.NewApplicationsCount);
            Assert.AreEqual(2, result.InProgressApplicationsCount);
            Assert.AreEqual(3, result.ClosedApplicationsCount);
        }

        [Test]
        public async Task WithdrawApplication_calls_mediator()
        {
            var applicationId = Guid.NewGuid();

            var request = new GatewayWithdrawApplicationRequest
            {
                UserId = "userId",
                UserName = "userName",
                Comments = "comments"
            };

            await _controller.WithdrawApplication(applicationId, request);

            _mediator.Verify(x => x.Send(It.Is<WithdrawApplicationRequest>(y => y.ApplicationId == applicationId), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task WithdrawApplication_sends_withdraw_email()
        {
            var applicationId = Guid.NewGuid();

            _mediator.Setup(x => x.Send(It.Is<GetContactForApplicationRequest>(y => y.ApplicationId == applicationId), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(() => new Contact
                    {
                        Email = "email@test.test",
                        GivenNames = "GivenNames",
                        FamilyName = "FamilyName"
                    });

            _mediator.Setup(x => x.Send(It.Is<WithdrawApplicationRequest>(y => y.ApplicationId == applicationId), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true);

            var request = new GatewayWithdrawApplicationRequest
            {
                UserId = "userId",
                UserName = "userName",
                Comments = "comments"
            };

            await _controller.WithdrawApplication(applicationId, request);

            _withdrawApplicationConfirmationEmailService.Verify(x => x.SendWithdrawConfirmationEmail(It.IsAny<ApplicationWithdrawConfirmation>()), Times.Once);
        }

        [Test]
        public async Task RemoveApplication_calls_mediator()
        {
            var applicationId = Guid.NewGuid();

            var request = new GatewayRemoveApplicationRequest
            {
                UserId = "userId",
                UserName = "userName",
                Comments = "comments",
                ExternalComments = "external comments"
            };

            await _controller.RemoveApplication(applicationId, request);

            _mediator.Verify(x => x.Send(It.Is<RemoveApplicationRequest>(y => y.ApplicationId == applicationId), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
