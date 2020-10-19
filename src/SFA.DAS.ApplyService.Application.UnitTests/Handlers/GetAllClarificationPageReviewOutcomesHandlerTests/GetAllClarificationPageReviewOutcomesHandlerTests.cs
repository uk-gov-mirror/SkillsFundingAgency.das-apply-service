﻿using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.ApplyService.Application.Apply;
using SFA.DAS.ApplyService.Application.Apply.Clarification;
using SFA.DAS.ApplyService.Domain.Apply.Clarification;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SFA.DAS.ApplyService.Application.UnitTests.Handlers.GetAllClarificationPageReviewOutcomesHandlerTests
{
    [TestFixture]
    public class GetAllClarificationPageReviewOutcomesHandlerTests
    {
        protected Mock<IClarificationRepository> _repository;
        protected GetAllClarificationPageReviewOutcomesHandler _handler;

        [SetUp]
        public void TestSetup()
        {
            _repository = new Mock<IClarificationRepository>();
            _handler = new GetAllClarificationPageReviewOutcomesHandler(_repository.Object, Mock.Of<ILogger<GetAllClarificationPageReviewOutcomesHandler>>());
        }

        [Test]
        public async Task GetAllClarificationReviewOutcomesHandler_returns__List_of_PageReviewOutcome()
        {
            var expectedApplicationId = Guid.NewGuid();
            var expectedSequenceNumber = 1;
            var expectedSectionNumber = 2;
            var expectedPageId = "30";
            var expectedUserId = "4fs7f-userId-7gfhh";
            var expectedStatus = "Fail";
            var expectedComment = "Very bad";

            var expectedResult = new List<ClarificationPageReviewOutcome>
            {
                new ClarificationPageReviewOutcome
                {
                    ApplicationId = expectedApplicationId,
                    SequenceNumber = expectedSequenceNumber,
                    SectionNumber = expectedSectionNumber,
                    PageId = expectedPageId,
                    ModeratorUserId = expectedUserId,
                    ModeratorReviewStatus = expectedStatus,
                    ModeratorReviewComment = expectedComment,
                    UserId = expectedUserId,
                    Status = expectedStatus,
                    Comment = expectedComment
                }
            };

            _repository.Setup(x => x.GetAllClarificationPageReviewOutcomes(expectedApplicationId)).ReturnsAsync(expectedResult);

            var actualResult = await _handler.Handle(new GetAllClarificationPageReviewOutcomesRequest(expectedApplicationId, expectedUserId), new CancellationToken());

            Assert.AreSame(expectedResult, actualResult);
        }
    }
}
