﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SFA.DAS.ApplyService.Application.Apply.Roatp;
using SFA.DAS.ApplyService.Domain.Sectors;
using SFA.DAS.ApplyService.InternalApi.Services.Assessor;
using SFA.DAS.ApplyService.InternalApi.Types.Assessor;

namespace SFA.DAS.ApplyService.InternalApi.UnitTests.Services.Assessor
{
    [TestFixture]
    public class AssessorSectorDetailsServiceTests
    {
        private AssessorSectorDetailsService _service;
        private Mock<IAssessorPageService> _assessorPageService;
        private Mock<IAssessorLookupService> _assessorLookupService;
        private Mock<IExtractAnswerValueService> _extractAnswerValueService;
        private SectorQuestionIds _sectorQuestionIds;

        private readonly string _firstPageId = "57610AA";
        private readonly string _secondPageId = "57610A"; 
        private readonly string _thirdPageId = "57610B";
        private readonly string _fourthPageId = "57610";
        private readonly string _fifthPageId = "57611";
        private readonly string _sixthPageId = "57612";
        private readonly string _seventhPageId = "57613";
        private readonly Guid _applicationId = Guid.NewGuid();
        private readonly int _sequenceId = RoatpWorkflowSequenceIds.DeliveringApprenticeshipTraining;

        private readonly int _sectionId =
            RoatpWorkflowSectionIds.DeliveringApprenticeshipTraining.YourSectorsAndEmployees;

        [SetUp]
        public void TestSetup()
        {
            _assessorPageService = new Mock<IAssessorPageService>();
            _assessorLookupService = new Mock<IAssessorLookupService>();
            _extractAnswerValueService = new Mock<IExtractAnswerValueService>();
            _sectorQuestionIds = new SectorQuestionIds();

            _assessorPageService.Setup(x => x.GetPage(_applicationId, _sequenceId, _sectionId, _firstPageId))
                .ReturnsAsync(new AssessorPage { PageId = _firstPageId, Answers = new List<AssessorAnswer> { new AssessorAnswer() }, NextPageId = _secondPageId });
            _assessorPageService.Setup(x => x.GetPage(_applicationId, _sequenceId, _sectionId, _secondPageId))
                .ReturnsAsync(new AssessorPage { PageId = _secondPageId, Answers = new List<AssessorAnswer> { new AssessorAnswer() }, NextPageId = _thirdPageId });
            _assessorPageService.Setup(x => x.GetPage(_applicationId, _sequenceId, _sectionId, _thirdPageId))
                .ReturnsAsync(new AssessorPage { PageId = _thirdPageId, Answers = new List<AssessorAnswer> { new AssessorAnswer() }, NextPageId = _fourthPageId });
            _assessorPageService.Setup(x => x.GetPage(_applicationId, _sequenceId, _sectionId, _fourthPageId))
                .ReturnsAsync(new AssessorPage { PageId = _fourthPageId, Answers = new List<AssessorAnswer> { new AssessorAnswer() }, NextPageId = _fifthPageId});
            _assessorPageService.Setup(x => x.GetPage(_applicationId, _sequenceId, _sectionId, _fifthPageId))
                .ReturnsAsync(new AssessorPage { PageId = _fifthPageId, Answers = new List<AssessorAnswer> { new AssessorAnswer() }, NextPageId = _sixthPageId });

            _assessorPageService.Setup(x => x.GetPage(_applicationId, _sequenceId, _sectionId, _sixthPageId))
                .ReturnsAsync(new AssessorPage { PageId = _sixthPageId, Answers = new List<AssessorAnswer> { new AssessorAnswer() }, NextPageId = _seventhPageId });

            _assessorPageService.Setup(x => x.GetPage(_applicationId, _sequenceId, _sectionId, _seventhPageId))
                .ReturnsAsync(new AssessorPage { PageId = _seventhPageId, Answers = new List<AssessorAnswer> { new AssessorAnswer() } });
            _assessorLookupService.Setup(x => x.GetSectorQuestionIdsForSectorPageId(It.IsAny<string>())).Returns(_sectorQuestionIds);

            _sectorQuestionIds.HowHaveTheyDeliveredTraining = "HowHaveTheyDeliveredId";
            var howHaveTheyDelivered = "delivery mechanism";

            _extractAnswerValueService
                .Setup(a => a.ExtractAnswerValueFromQuestionId(It.IsAny<List<AssessorAnswer>>(), _sectorQuestionIds.HowHaveTheyDeliveredTraining))
                .Returns(howHaveTheyDelivered);
            _service = new AssessorSectorDetailsService(_assessorLookupService.Object,_assessorPageService.Object,_extractAnswerValueService.Object);
        }


        [Test]
        public async Task Get_sector_details_for_page1_answers()
        {
            _sectorQuestionIds.WhatStandardsOffered = "WhatStandardsOfferedQuestionId";

            var whatStandardsOffered = "standards offered";

            _extractAnswerValueService
                .Setup(a => a.ExtractAnswerValueFromQuestionId(It.IsAny<List<AssessorAnswer>>(), _sectorQuestionIds.WhatStandardsOffered))
                .Returns(whatStandardsOffered);

            var actualSectorDetails = await _service.GetSectorDetails(_applicationId, _firstPageId);
            Assert.AreEqual(whatStandardsOffered, actualSectorDetails.WhatStandardsOffered);
        }

        [Test]
        public async Task Get_sector_details_for_page2_answers()
        {
            _sectorQuestionIds.HowManyStarts = "HowManyStartsQuestionId";
            
            var howManyStarts = "1";
            
            _extractAnswerValueService
                .Setup(a => a.ExtractAnswerValueFromQuestionId(It.IsAny<List<AssessorAnswer>>(), _sectorQuestionIds.HowManyStarts))
                .Returns(howManyStarts);
            
            var actualSectorDetails = await _service.GetSectorDetails(_applicationId, _firstPageId);
            Assert.AreEqual(howManyStarts, actualSectorDetails.HowManyStarts);
        }

        [Test]
        public async Task Get_sector_details_for_page3_answers()
        {
            _sectorQuestionIds.HowManyStarts = "HowManyEmployeesQuestionId";

            var howManyEmployees = "2";

            _extractAnswerValueService
                .Setup(a => a.ExtractAnswerValueFromQuestionId(It.IsAny<List<AssessorAnswer>>(), _sectorQuestionIds.HowManyEmployees))
                .Returns(howManyEmployees);

            var actualSectorDetails = await _service.GetSectorDetails(_applicationId, _firstPageId);
            Assert.AreEqual(howManyEmployees, actualSectorDetails.HowManyEmployees);
        }

        [Test]
        public async Task Get_sector_details_for_page4_answers()
        {
            _sectorQuestionIds.FirstName = "FirstNameQuestionId";
            _sectorQuestionIds.LastName = "LastNameQuestionId";
            _sectorQuestionIds.JobRole = "JobRoleId";
            _sectorQuestionIds.TimeInRole = "TimeInRoleId";
            _sectorQuestionIds.IsPartOfAnyOtherOrganisations = "PartOfOtherOrgsId";
            var firstName = "FirstName Answer";
            var lastName = "LastName Answer";

            var jobRole = "Job Role Description";
            var timeInRole = "Time in role";
            var isPartOfOtherOrgs = "Yes";
            var organisationDetails = "other organisations";
        
            _extractAnswerValueService
                .Setup(a => a.ExtractAnswerValueFromQuestionId(It.IsAny<List<AssessorAnswer>>(), _sectorQuestionIds.FirstName))
                .Returns(firstName);
            _extractAnswerValueService
                .Setup(a => a.ExtractAnswerValueFromQuestionId(It.IsAny<List<AssessorAnswer>>(), _sectorQuestionIds.LastName))
                .Returns(lastName);
            _extractAnswerValueService
                .Setup(a => a.ExtractAnswerValueFromQuestionId(It.IsAny<List<AssessorAnswer>>(), _sectorQuestionIds.JobRole))
                .Returns(jobRole);
            _extractAnswerValueService
                .Setup(a => a.ExtractAnswerValueFromQuestionId(It.IsAny<List<AssessorAnswer>>(), _sectorQuestionIds.TimeInRole))
                .Returns(timeInRole);
            _extractAnswerValueService
                .Setup(a => a.ExtractAnswerValueFromQuestionId(It.IsAny<List<AssessorAnswer>>(), _sectorQuestionIds.IsPartOfAnyOtherOrganisations))
                .Returns(isPartOfOtherOrgs);
            _extractAnswerValueService
                .Setup(a => a.ExtractFurtherQuestionAnswerValueFromQuestionId(It.IsAny<AssessorPage>(), _sectorQuestionIds.IsPartOfAnyOtherOrganisations))
                .Returns(organisationDetails);
            
            var actualSectorDetails = await _service.GetSectorDetails(_applicationId, _firstPageId);
            Assert.AreEqual(firstName, actualSectorDetails.FirstName);
            Assert.AreEqual(lastName, actualSectorDetails.LastName);
            Assert.AreEqual(jobRole, actualSectorDetails.JobRole);
            Assert.AreEqual(timeInRole, actualSectorDetails.TimeInRole);
            Assert.AreEqual(isPartOfOtherOrgs, actualSectorDetails.IsPartOfAnyOtherOrganisations);
            Assert.AreEqual(organisationDetails, actualSectorDetails.OtherOrganisations );
        }

        [Test]
        public async Task Get_sector_details_for_page5_answers()
        {
            _sectorQuestionIds.ExperienceOfDelivering = "ExperienceOfDeliveringId";
            _sectorQuestionIds.DoTheyHaveQualifications = "qualificationsId";
            _sectorQuestionIds.AwardingBodies = "awardingId";
            _sectorQuestionIds.TradeMemberships = "TradeId";

            var experienceOfDelivering = "experience of delivering";
            var experienceOfDeliveringFurther = "experience of delivering further";
            var doTheyHaveQualifications = "Do they have qualifications";
            var whatQualificationsDoTheyHave = "What qualifications";
            var approvedByAwardingBodies = "awarding bodies";
            var namesOfAwardingBodies = "names of awarding bodies";
            var doTheyHaveTradeBodyMemberships = "trade memberships";
            var namesOfTradeBodyMemberships = "names of trade memberships";
           _extractAnswerValueService
                .Setup(a => a.ExtractAnswerValueFromQuestionId(It.IsAny<List<AssessorAnswer>>(), _sectorQuestionIds.ExperienceOfDelivering))
                .Returns(experienceOfDelivering);
            _extractAnswerValueService
                .Setup(a => a.ExtractFurtherQuestionAnswerValueFromQuestionId(It.IsAny<AssessorPage>(), _sectorQuestionIds.ExperienceOfDelivering))
                .Returns(experienceOfDeliveringFurther);

            _extractAnswerValueService
                .Setup(a => a.ExtractAnswerValueFromQuestionId(It.IsAny<List<AssessorAnswer>>(), _sectorQuestionIds.DoTheyHaveQualifications))
                .Returns(doTheyHaveQualifications);
            _extractAnswerValueService
                .Setup(a => a.ExtractFurtherQuestionAnswerValueFromQuestionId(It.IsAny<AssessorPage>(), _sectorQuestionIds.DoTheyHaveQualifications))
                .Returns(whatQualificationsDoTheyHave);


            _extractAnswerValueService
                .Setup(a => a.ExtractAnswerValueFromQuestionId(It.IsAny<List<AssessorAnswer>>(), _sectorQuestionIds.AwardingBodies))
                .Returns(approvedByAwardingBodies);
            _extractAnswerValueService
                .Setup(a => a.ExtractFurtherQuestionAnswerValueFromQuestionId(It.IsAny<AssessorPage>(), _sectorQuestionIds.AwardingBodies))
                .Returns(namesOfAwardingBodies);


            _extractAnswerValueService
                .Setup(a => a.ExtractAnswerValueFromQuestionId(It.IsAny<List<AssessorAnswer>>(), _sectorQuestionIds.TradeMemberships))
                .Returns(doTheyHaveTradeBodyMemberships);
            _extractAnswerValueService
                .Setup(a => a.ExtractFurtherQuestionAnswerValueFromQuestionId(It.IsAny<AssessorPage>(), _sectorQuestionIds.TradeMemberships))
                .Returns(namesOfTradeBodyMemberships);

            var actualSectorDetails = await _service.GetSectorDetails(_applicationId, _firstPageId);
            Assert.AreEqual(experienceOfDelivering, actualSectorDetails.ExperienceOfDelivering);
            Assert.AreEqual(experienceOfDeliveringFurther, actualSectorDetails.WhereDidTheyGainThisExperience);
            Assert.AreEqual(doTheyHaveQualifications, actualSectorDetails.DoTheyHaveQualifications);
            Assert.AreEqual(whatQualificationsDoTheyHave, actualSectorDetails.WhatQualificationsDoTheyHave);
            Assert.AreEqual(approvedByAwardingBodies, actualSectorDetails.ApprovedByAwardingBodies);
            Assert.AreEqual(namesOfAwardingBodies, actualSectorDetails.NamesOfAwardingBodies);
            Assert.AreEqual(doTheyHaveTradeBodyMemberships, actualSectorDetails.DoTheyHaveTradeBodyMemberships);
            Assert.AreEqual(namesOfTradeBodyMemberships, actualSectorDetails.NamesOfTradeBodyMemberships);
        }

        [Test]
        public async Task Get_sector_details_for_page6_answers()
        {
            _sectorQuestionIds.WhatTypeOfTrainingDelivered = "WhatTypeOfTrainingId";

            var typeOfTraining = "type of training";

            _assessorLookupService.Setup(x => x.GetSectorQuestionIdsForSectorPageId(_sixthPageId)).Returns(_sectorQuestionIds);

            _extractAnswerValueService
                .Setup(a => a.ExtractAnswerValueFromQuestionId(It.IsAny<List<AssessorAnswer>>(),
                    _sectorQuestionIds.WhatTypeOfTrainingDelivered))
                .Returns(typeOfTraining);

            var actualSectorDetails = await _service.GetSectorDetails(_applicationId, _firstPageId);
            Assert.AreEqual(typeOfTraining, actualSectorDetails.WhatTypeOfTrainingDelivered);
        }

        [Test]
        public async Task Get_sector_details_for_page7_answers()
        {
            _sectorQuestionIds.HowHaveTheyDeliveredTraining = "HowHaveTheyDeliveredId";
            _sectorQuestionIds.ExperienceOfDeliveringTraining = "ExperienceId";
            _sectorQuestionIds.TypicalDurationOfTraining = "DurationId";

            var howHaveTheyDelivered = "delivery mechanism";
            var experienceOfDelivering = "experience of delivering";
            var typicalDuration = "typical duration";
            _assessorLookupService.Setup(x => x.GetSectorQuestionIdsForSectorPageId(_fourthPageId)).Returns(_sectorQuestionIds);

            _extractAnswerValueService
                .Setup(a => a.ExtractAnswerValueFromQuestionId(It.IsAny<List<AssessorAnswer>>(), _sectorQuestionIds.HowHaveTheyDeliveredTraining))
                .Returns(howHaveTheyDelivered);

            _extractAnswerValueService
                .Setup(a => a.ExtractAnswerValueFromQuestionId(It.IsAny<List<AssessorAnswer>>(), _sectorQuestionIds.ExperienceOfDeliveringTraining))
                .Returns(experienceOfDelivering);

            _extractAnswerValueService
                .Setup(a => a.ExtractAnswerValueFromQuestionId(It.IsAny<List<AssessorAnswer>>(), _sectorQuestionIds.TypicalDurationOfTraining))
                .Returns(typicalDuration);

            var actualSectorDetails = await _service.GetSectorDetails(_applicationId, _firstPageId);
            Assert.AreEqual(howHaveTheyDelivered, actualSectorDetails.HowHaveTheyDeliveredTraining);
            Assert.AreEqual(experienceOfDelivering, actualSectorDetails.ExperienceOfDeliveringTraining);
            Assert.AreEqual(typicalDuration, actualSectorDetails.TypicalDurationOfTraining);
        }

        [Test]
        public async Task Get_sector_details_for_page7_answers_other()
        {
            _sectorQuestionIds.HowHaveTheyDeliveredTraining = "HowHaveTheyDeliveredId";
            _sectorQuestionIds.ExperienceOfDeliveringTraining = "ExperienceId";
            _sectorQuestionIds.TypicalDurationOfTraining = "DurationId";

            var howHaveTheyDeliveredDescription = "other delivery mechanism";
            var experienceOfDelivering = "experience of delivering";
            var typicalDuration = "typical duration";
            var otherHowHaveTheyDelivered = "delivery mechanism other";

            _assessorLookupService.Setup(x => x.GetSectorQuestionIdsForSectorPageId(_fourthPageId)).Returns(_sectorQuestionIds);

            _extractAnswerValueService
                .Setup(a => a.ExtractAnswerValueFromQuestionId(It.IsAny<List<AssessorAnswer>>(), _sectorQuestionIds.HowHaveTheyDeliveredTraining))
                .Returns(RoatpWorkflowPageIds.DeliveringApprenticeshipTraining.DeliveringTrainingOther);

            _extractAnswerValueService
                .Setup(a => a.ExtractAnswerValueFromQuestionId(It.IsAny<List<AssessorAnswer>>(), _sectorQuestionIds.HowHaveTheyDeliveredTrainingOther))
                .Returns(howHaveTheyDeliveredDescription);

            _extractAnswerValueService
                .Setup(a => a.ExtractAnswerValueFromQuestionId(It.IsAny<List<AssessorAnswer>>(), _sectorQuestionIds.ExperienceOfDeliveringTraining))
                .Returns(experienceOfDelivering);

            _extractAnswerValueService
                .Setup(a => a.ExtractAnswerValueFromQuestionId(It.IsAny<List<AssessorAnswer>>(), _sectorQuestionIds.TypicalDurationOfTraining))
                .Returns(typicalDuration);

            var actualSectorDetails = await _service.GetSectorDetails(_applicationId, _firstPageId);
            Assert.AreEqual(howHaveTheyDeliveredDescription, actualSectorDetails.HowHaveTheyDeliveredTraining);
            Assert.AreEqual(experienceOfDelivering, actualSectorDetails.ExperienceOfDeliveringTraining);
            Assert.AreEqual(typicalDuration, actualSectorDetails.TypicalDurationOfTraining);

        }
    }
}
