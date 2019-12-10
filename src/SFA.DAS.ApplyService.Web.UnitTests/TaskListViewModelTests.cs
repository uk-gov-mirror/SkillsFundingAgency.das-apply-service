﻿namespace SFA.DAS.ApplyService.Web.UnitTests
{
    using System;
    using Domain.Entities;
    using NUnit.Framework;
    using System.Collections.Generic;
    using Application.Apply.Roatp;
    using SFA.DAS.ApplyService.Web.Services;
    using Domain.Apply;
    using FluentAssertions;
    using ViewModels.Roatp;
    using Moq;
    using SFA.DAS.ApplyService.Web.Infrastructure;

    [TestFixture]
    public class TaskListViewModelTests
    {
        private Guid _applicationId;
        private List<ApplicationSequence> _applicationSequences;
        private ApplicationSequence _yourApplicationSequence;
        private ApplicationSection _providerRouteSection;
        private ApplicationSection _whatYouNeedSection;
        private RoatpTaskListWorkflowService _roatpTaskListWorkflowService;
        private Mock<IQnaApiClient> _qnaApiClient;

        private const string MainProviderRoute = "1";
        private const string EmployerProviderRoute = "2";
        private const string SupportingProviderRoute = "3";

        [SetUp]
        public void Before_each_test()
        {
            _applicationId = Guid.NewGuid();
            _applicationSequences = new List<ApplicationSequence>();
            _yourApplicationSequence = new ApplicationSequence
            {
                Id = Guid.NewGuid(),
                ApplicationId = _applicationId,
                SequenceId = RoatpWorkflowSequenceIds.YourOrganisation,
                Sections = new List<ApplicationSection>(),
                Sequential = true
            };
            _roatpTaskListWorkflowService = new RoatpTaskListWorkflowService();
            _providerRouteSection = new ApplicationSection
            {
                SequenceId = RoatpWorkflowSequenceIds.YourOrganisation,
                SectionId = RoatpWorkflowSectionIds.YourOrganisation.ProviderRoute
            };
            _whatYouNeedSection = new ApplicationSection
            {
                SequenceId = RoatpWorkflowSequenceIds.YourOrganisation,
                SectionId = RoatpWorkflowSectionIds.YourOrganisation.WhatYouWillNeed
            };

            _yourApplicationSequence.Sections.Add(_providerRouteSection);
            _yourApplicationSequence.Sections.Add(_whatYouNeedSection);
            _applicationSequences.Add(_yourApplicationSequence);
            _qnaApiClient = new Mock<IQnaApiClient>();
        }

        [Test]
        public void Task_list_shows_next_for_provider_route_if_not_complete()
        {
            // This should be prefilled in the pre-amble but test in case the sequencing changes

            _providerRouteSection.QnAData = new QnAData
            {
                Pages = new List<Page>
                {
                    new Page
                    {
                        PageId = "1",
                        Questions = new List<Question>
                        {
                            new Question
                            {
                                QuestionId = "YO-1"
                            }
                        },
                        PageOfAnswers = new List<PageOfAnswers>()
                    }
                }
            };

            _whatYouNeedSection.QnAData = new QnAData
            {
                Pages = new List<Page>
                {
                    new Page
                    {
                        PageId = "2",
                        Questions = new List<Question>
                        {
                            new Question
                            {
                                QuestionId = "YO-2"
                            }
                        },
                        PageOfAnswers = new List<PageOfAnswers>()
                    }
                }
            };

            var model = new TaskListViewModel(_qnaApiClient.Object)
            {
                ApplicationId = _applicationId,
                ApplicationSequences = _applicationSequences,
                UKPRN = "10001234",
                OrganisationName = "Org Name"
            };

            model.SectionStatus(RoatpWorkflowSequenceIds.YourOrganisation,
                RoatpWorkflowSectionIds.YourOrganisation.ProviderRoute).Should().Be("Next");
            model.SectionStatus(RoatpWorkflowSequenceIds.YourOrganisation,
                RoatpWorkflowSectionIds.YourOrganisation.WhatYouWillNeed).ToLower().Should().Be("");
        }

        [Test]
        public void Task_list_shows_next_for_your_organisation_what_you_need_if_not_complete()
        {
            _providerRouteSection.QnAData = new QnAData
            {
                Pages = new List<Page>
                {
                    new Page
                    {
                        PageId = "1",
                        Questions = new List<Question>
                        {
                            new Question
                            {
                                QuestionId = "YO-1"
                            }
                        },
                        PageOfAnswers = new List<PageOfAnswers>
                        {
                            new PageOfAnswers
                            {
                                Id = Guid.NewGuid(),
                                Answers = new List<Answer>
                                {
                                    new Answer {QuestionId = "YO-1", Value = "1"}
                                }
                            }
                        },
                        Active = true,
                        Complete = true
                    }
                    
                }
            };

            _whatYouNeedSection.QnAData = new QnAData
            {
                Pages = new List<Page>
                {
                    new Page
                    {
                        PageId = "2",
                        Questions = new List<Question>
                        {
                            new Question
                            {
                                QuestionId = "YO-2"
                            }
                        },
                        PageOfAnswers = new List<PageOfAnswers>()
                    }
                }
            };

            var model = new TaskListViewModel(_qnaApiClient.Object)
            {
                ApplicationId = _applicationId,
                ApplicationSequences = _applicationSequences,
                UKPRN = "10001234",
                OrganisationName = "Org Name"
            };

            model.SectionStatus(RoatpWorkflowSequenceIds.YourOrganisation,
                RoatpWorkflowSectionIds.YourOrganisation.ProviderRoute).Should().Be("Completed");
            model.SectionStatus(RoatpWorkflowSequenceIds.YourOrganisation,
                RoatpWorkflowSectionIds.YourOrganisation.WhatYouWillNeed).Should().Be("Next");
        }

        [Test]
        public void Task_list_shows_completed_your_organisation_section()
        {
            _providerRouteSection.QnAData = new QnAData
            {
                Pages = new List<Page>
                {
                    new Page
                    {
                        PageId = "1",
                        Questions = new List<Question>
                        {
                            new Question
                            {
                                QuestionId = "YO-1"
                            }
                        },
                        PageOfAnswers = new List<PageOfAnswers>
                        {
                            new PageOfAnswers
                            {
                                Id = Guid.NewGuid(),
                                Answers = new List<Answer>
                                {
                                    new Answer {QuestionId = "YO-1", Value = "1"}
                                }
                            }
                        },
                        Active = true,
                        Complete = true
                    }
                }
            };

            _whatYouNeedSection.QnAData = new QnAData
            {
                Pages = new List<Page>
                {
                    new Page
                    {
                        PageId = "2",
                        Questions = new List<Question>
                        {
                            new Question
                            {
                                QuestionId = "YO-2"
                            }
                        },
                        PageOfAnswers = new List<PageOfAnswers>
                        {
                            new PageOfAnswers
                            {
                                Id = Guid.NewGuid(),
                                Answers = new List<Answer>
                                {
                                    new Answer {QuestionId = "YO-2", Value = "1"}
                                }
                            }
                        },
                        Active = true,
                        Complete = true
                    }
                }
            };

            var model = new TaskListViewModel(_qnaApiClient.Object)
            {
                ApplicationId = _applicationId,
                ApplicationSequences = _applicationSequences,
                UKPRN = "10001234",
                OrganisationName = "Org Name"
            };

            model.SectionStatus(RoatpWorkflowSequenceIds.YourOrganisation,
                RoatpWorkflowSectionIds.YourOrganisation.ProviderRoute).Should().Be("Completed");
            model.SectionStatus(RoatpWorkflowSequenceIds.YourOrganisation,
                RoatpWorkflowSectionIds.YourOrganisation.WhatYouWillNeed).Should().Be("Completed");
        }

        [Test]
        public void Task_list_shows_correct_status_for_incomplete_non_sequential_section()
        {
            var criminalComplianceSequence = new ApplicationSequence
            {
                Id = Guid.NewGuid(),
                ApplicationId = _applicationId,
                SequenceId = RoatpWorkflowSequenceIds.CriminalComplianceChecks,
                Sections = new List<ApplicationSection>()
            };
            var criminalWhatYouNeedSection = new ApplicationSection
            {
                SequenceId = RoatpWorkflowSequenceIds.CriminalComplianceChecks,
                SectionId = RoatpWorkflowSectionIds.CriminalComplianceChecks.WhatYouWillNeed,
                QnAData = new QnAData
                {
                    Pages = new List<Page>
                    {
                        new Page
                        {
                            PageId = "1",
                            Questions = new List<Question>
                            {
                                new Question
                                {
                                    QuestionId = "CC-1"
                                }
                            },
                            PageOfAnswers = new List<PageOfAnswers>
                            {
                                new PageOfAnswers
                                {
                                    Id = Guid.NewGuid(),
                                    Answers = new List<Answer>
                                    {
                                        new Answer {QuestionId = "CC-1", Value = "1"}
                                    }
                                }
                            },
                            Active = true,
                            Complete = true
                        }
                    }
                }
            };

            var criminalOrganisationChecksSection = new ApplicationSection
            {
                SequenceId = RoatpWorkflowSequenceIds.CriminalComplianceChecks,
                SectionId = RoatpWorkflowSectionIds.CriminalComplianceChecks.ChecksOnYourOrganisation,
                QnAData = new QnAData
                {
                    Pages = new List<Page>
                    {
                        new Page
                        {
                            PageId = "1",
                            Questions = new List<Question>
                            {
                                new Question
                                {
                                    QuestionId = "CC-1"
                                },
                                new Question
                                {
                                    QuestionId = "CC-2"
                                },
                                new Question
                                {
                                    QuestionId = "CC-3"
                                }
                            },
                            PageOfAnswers = new List<PageOfAnswers>
                            {
                                new PageOfAnswers
                                {
                                    Id = Guid.NewGuid(),
                                    Answers = new List<Answer>
                                    {
                                        new Answer {QuestionId = "CC-1", Value = "1"},
                                        new Answer {QuestionId = "CC-2", Value = "1"}
                                    }
                                }
                            }
                        }
                    }
                }
            };
            var criminalIndividualChecksSection = new ApplicationSection
            {
                SequenceId = RoatpWorkflowSequenceIds.CriminalComplianceChecks,
                SectionId = RoatpWorkflowSectionIds.CriminalComplianceChecks.CheckOnWhosInControl,
                QnAData = new QnAData
                {
                    Pages = new List<Page>
                    {
                        new Page
                        {
                            PageId = "1",
                            Questions = new List<Question>
                            {
                                new Question
                                {
                                    QuestionId = "CC-10"
                                },
                                new Question
                                {
                                    QuestionId = "CC-20"
                                },
                                new Question
                                {
                                    QuestionId = "CC-30"
                                }
                            },
                            PageOfAnswers = new List<PageOfAnswers>()
                        }
                    }
                }
            };
           criminalComplianceSequence.Sections.Add(criminalWhatYouNeedSection);
           criminalComplianceSequence.Sections.Add(criminalOrganisationChecksSection);
            criminalComplianceSequence.Sections.Add(criminalIndividualChecksSection);
            _applicationSequences.Add(criminalComplianceSequence);

            var model = new TaskListViewModel(_qnaApiClient.Object)
            {
                ApplicationId = _applicationId,
                ApplicationSequences = _applicationSequences,
                UKPRN = "10001234",
                OrganisationName = "Org Name"
            };

            model.SectionStatus(RoatpWorkflowSequenceIds.CriminalComplianceChecks,
                RoatpWorkflowSectionIds.CriminalComplianceChecks.WhatYouWillNeed).Should().Be("Completed");
            model.SectionStatus(RoatpWorkflowSequenceIds.CriminalComplianceChecks,
                RoatpWorkflowSectionIds.CriminalComplianceChecks.ChecksOnYourOrganisation).Should().Be("In Progress");
            model.SectionStatus(RoatpWorkflowSequenceIds.CriminalComplianceChecks,
                RoatpWorkflowSectionIds.CriminalComplianceChecks.CheckOnWhosInControl).Should().Be("");
        }

        [Test]
        public void Whos_in_control_section_status_shows_as_next_if_companies_house_verified_and_not_confirmed()
        {
            var model = new TaskListViewModel(_qnaApiClient.Object)
            {
                VerifiedCompaniesHouse = true,
                VerifiedCharityCommission = false,
                CompaniesHouseDataConfirmed = false,
                CharityCommissionDataConfirmed = false,
                WhosInControlConfirmed = false
            };

            model.WhosInControlSectionStatus.Should().Be("Next");
        }

        [Test]
        public void Whos_in_control_section_status_shows_as_next_if_charity_commission_verified_and_not_confirmed()
        {
            var model = new TaskListViewModel(_qnaApiClient.Object)
            {
                VerifiedCompaniesHouse = false,
                VerifiedCharityCommission = true,
                CompaniesHouseDataConfirmed = false,
                CharityCommissionDataConfirmed = false,
                WhosInControlConfirmed = false
            };

            model.WhosInControlSectionStatus.Should().Be("Next");
        }


        [Test]
        public void Whos_in_control_section_status_shows_as_complete_if_companies_house_verified_and_confirmed()
        {
            var model = new TaskListViewModel(_qnaApiClient.Object)
            {
                VerifiedCompaniesHouse = true,
                VerifiedCharityCommission = false,
                CompaniesHouseDataConfirmed = true,
                CharityCommissionDataConfirmed = false,
                WhosInControlConfirmed = false
            };

            model.WhosInControlSectionStatus.Should().Be("Completed");
        }

        [Test]
        public void Whos_in_control_section_status_shows_as_in_progress_if_companies_house_and_charity_commission_verified_and_only_confirmed_company()
        {
            var model = new TaskListViewModel(_qnaApiClient.Object)
            {
                VerifiedCompaniesHouse = true,
                VerifiedCharityCommission = true,
                CompaniesHouseDataConfirmed = true,
                CharityCommissionDataConfirmed = false,
                WhosInControlConfirmed = false
            };

            model.WhosInControlSectionStatus.Should().Be("In Progress");
        }

        [Test]
        public void Whos_in_control_section_status_shows_as_in_progress_if_companies_house_and_charity_commission_verified_and_only_confirmed_charity()
        {
            var model = new TaskListViewModel(_qnaApiClient.Object)
            {
                VerifiedCompaniesHouse = true,
                VerifiedCharityCommission = true,
                CompaniesHouseDataConfirmed = false,
                CharityCommissionDataConfirmed = true,
                WhosInControlConfirmed = false
            };

            model.WhosInControlSectionStatus.Should().Be("In Progress");
        }

        [Test]
        public void Whos_in_control_section_status_shows_as_completed_if_companies_house_and_charity_commission_verified_and_both_confirmed()
        {
            var model = new TaskListViewModel(_qnaApiClient.Object)
            {
                VerifiedCompaniesHouse = true,
                VerifiedCharityCommission = true,
                CompaniesHouseDataConfirmed = true,
                CharityCommissionDataConfirmed = true,
                WhosInControlConfirmed = false
            };

            model.WhosInControlSectionStatus.Should().Be("Completed");
        }

        [Test]
        public void Whos_in_control_section_status_shows_as_next_if_not_verified_by_companies_house_or_charity_commission_and_whos_in_control_not_confirmed()
        {
            var model = new TaskListViewModel(_qnaApiClient.Object)
            {
                VerifiedCompaniesHouse = false,
                VerifiedCharityCommission = false,
                CompaniesHouseDataConfirmed = false,
                CharityCommissionDataConfirmed = false,
                WhosInControlConfirmed = false
            };

            model.WhosInControlSectionStatus.Should().Be("Next");
        }

        [Test]
        public void Whos_in_control_section_status_shows_as_completed_if_not_verified_by_companies_house_or_charity_commission_and_whos_in_control_confirmed()
        {
            var model = new TaskListViewModel(_qnaApiClient.Object)
            {
                VerifiedCompaniesHouse = false,
                VerifiedCharityCommission = false,
                CompaniesHouseDataConfirmed = false,
                CharityCommissionDataConfirmed = false,
                WhosInControlConfirmed = true
            };

            model.WhosInControlSectionStatus.Should().Be("Completed");
        }

        [Test]
        public void Finish_application_checks_shows_blank_if_not_all_sequences_completed()
        {
            var applicationSequences = new List<ApplicationSequence>
            {
                new ApplicationSequence
                {
                    ApplicationId = Guid.NewGuid(),
                    SequenceId = RoatpWorkflowSequenceIds.YourOrganisation,
                    Sections = new List<ApplicationSection>
                    {
                        new ApplicationSection
                        {
                            SectionId = RoatpWorkflowSectionIds.YourOrganisation.OrganisationDetails,
                            QnAData = new QnAData
                            {
                                Pages = new List<Page>
                                {
                                    new Page
                                    {                                        
                                        Active = true,
                                        Complete = true,
                                        Questions = new List<Question>()
                                    },
                                    new Page
                                    {
                                        Active = true,
                                        Complete = false,
                                        Questions = new List<Question>()
                                    }
                                }
                            }
                        }
                    }
                },
                new ApplicationSequence
                {
                    ApplicationId = Guid.NewGuid(),
                    SequenceId = RoatpWorkflowSequenceIds.Finish,
                    Sections = new List<ApplicationSection>
                    {
                        new ApplicationSection
                        {
                            SectionId = RoatpWorkflowSectionIds.Finish.TermsAndConditions,
                            QnAData = new QnAData
                            {
                                Pages = new List<Page>
                                {
                                    new Page
                                    {
                                        Active = true,
                                        Complete = false,
                                        Questions = new List<Question>()
                                    },
                                    new Page
                                    {
                                        Active = true,
                                        Complete = false,
                                        Questions = new List<Question>()
                                    }
                                }
                            }
                        }
                    }
                }
            };

            var model = new TaskListViewModel(_qnaApiClient.Object)
            {
                ApplicationSequences = applicationSequences
            };

            var status = model.FinishSectionStatus(RoatpWorkflowSectionIds.Finish.ApplicationPermissionsAndChecks);
            status.Should().Be(string.Empty);
            var css = model.FinishCss(RoatpWorkflowSectionIds.Finish.ApplicationPermissionsAndChecks);
            css.Should().Be("hidden");
        }

        [Test]
        public void Finish_application_checks_shows_next_if_all_sequences_completed_and_section_not_started()
        {
            var applicationSequences = new List<ApplicationSequence>
            {
                new ApplicationSequence
                {
                    ApplicationId = Guid.NewGuid(),
                    SequenceId = RoatpWorkflowSequenceIds.EvaluatingApprenticeshipTraining,
                    Sections = new List<ApplicationSection>
                    {
                        new ApplicationSection
                        {
                            SectionId = 1,
                            QnAData = new QnAData
                            {
                                Pages = new List<Page>
                                {
                                    new Page
                                    {
                                        Active = true,
                                        Complete = true,
                                        Questions = new List<Question>()
                                    },
                                    new Page
                                    {
                                        Active = true,
                                        Complete = true,
                                        Questions = new List<Question>()
                                    }
                                }
                            }
                        }
                    }
                },
                new ApplicationSequence
                {
                    ApplicationId = Guid.NewGuid(),
                    SequenceId = RoatpWorkflowSequenceIds.Finish,
                    Sequential = true,
                    Sections = new List<ApplicationSection>
                    {
                        new ApplicationSection
                        {
                            SectionId = RoatpWorkflowSectionIds.Finish.ApplicationPermissionsAndChecks,
                            QnAData = new QnAData
                            {
                                Pages = new List<Page>
                                {
                                    new Page
                                    {
                                        Active = true,
                                        Complete = false,
                                        Questions = new List<Question>()
                                    },
                                    new Page
                                    {
                                        Active = true,
                                        Complete = false,
                                        Questions = new List<Question>()
                                    }
                                }
                            }
                        }
                    }
                }
            };

            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishPermissionPersonalDetails)).ReturnsAsync(new Answer { Value = null });
            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishAccuratePersonalDetails)).ReturnsAsync(new Answer { Value = null });
            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishPermissionSubmitApplication)).ReturnsAsync(new Answer { Value = null });

            var model = new TaskListViewModel(_qnaApiClient.Object)
            {
                ApplicationSequences = applicationSequences
            };

            var status = model.FinishSectionStatus(RoatpWorkflowSectionIds.Finish.ApplicationPermissionsAndChecks);
            status.Should().Be("Next");
            var css = model.FinishCss(RoatpWorkflowSectionIds.Finish.ApplicationPermissionsAndChecks);
            css.Should().Be("next");
        }

        [Test]
        public void Finish_application_checks_shows_in_progress_if_all_not_questions_answered()
        {
            var applicationSequences = new List<ApplicationSequence>
            {
                new ApplicationSequence
                {
                    ApplicationId = Guid.NewGuid(),
                    SequenceId = RoatpWorkflowSequenceIds.EvaluatingApprenticeshipTraining,
                    Sections = new List<ApplicationSection>
                    {
                        new ApplicationSection
                        {
                            SectionId = 1,
                            QnAData = new QnAData
                            {
                                Pages = new List<Page>
                                {
                                    new Page
                                    {
                                        Active = true,
                                        Complete = true,
                                        Questions = new List<Question>()
                                    },
                                    new Page
                                    {
                                        Active = true,
                                        Complete = true,
                                        Questions = new List<Question>()
                                    }
                                }
                            }
                        }
                    }
                },
                new ApplicationSequence
                {
                    ApplicationId = Guid.NewGuid(),
                    SequenceId = RoatpWorkflowSequenceIds.Finish,
                    Sequential = true,
                    Sections = new List<ApplicationSection>
                    {
                        new ApplicationSection
                        {
                            SectionId = RoatpWorkflowSectionIds.Finish.ApplicationPermissionsAndChecks,
                            QnAData = new QnAData
                            {
                                Pages = new List<Page>
                                {
                                    new Page
                                    {
                                        Active = true,
                                        Complete = true,
                                        Questions = new List<Question>()
                                    },
                                    new Page
                                    {
                                        Active = true,
                                        Complete = true,
                                        Questions = new List<Question>()
                                    }
                                }
                            }
                        }
                    }
                }
            };

            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishPermissionPersonalDetails)).ReturnsAsync(new Answer { Value = "Yes" });
            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishAccuratePersonalDetails)).ReturnsAsync(new Answer { Value = null });
            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishPermissionSubmitApplication)).ReturnsAsync(new Answer { Value = null });
            
            var model = new TaskListViewModel(_qnaApiClient.Object)
            {
                ApplicationSequences = applicationSequences
            };

            var status = model.FinishSectionStatus(RoatpWorkflowSectionIds.Finish.ApplicationPermissionsAndChecks);
            status.Should().Be("In Progress");
            var css = model.FinishCss(RoatpWorkflowSectionIds.Finish.ApplicationPermissionsAndChecks);
            css.Should().Be("inprogress");
        }

        [Test]
        public void Finish_application_checks_shows_in_progress_if_questions_answered_and_shutter_page_shown_for_conditions_not_met()
        {
            var applicationSequences = new List<ApplicationSequence>
            {
                new ApplicationSequence
                {
                    ApplicationId = Guid.NewGuid(),
                    SequenceId = RoatpWorkflowSequenceIds.YourOrganisation,
                    Sections = new List<ApplicationSection>
                    {
                        new ApplicationSection
                        {
                            SectionId = RoatpWorkflowSectionIds.YourOrganisation.OrganisationDetails,
                            QnAData = new QnAData
                            {
                                Pages = new List<Page>
                                {
                                    new Page
                                    {
                                        Active = true,
                                        Complete = true,
                                        Questions = new List<Question>()
                                    },
                                    new Page
                                    {
                                        Active = true,
                                        Complete = true,
                                        Questions = new List<Question>()
                                    }
                                }
                            }
                        }
                    }
                },
                new ApplicationSequence
                {
                    ApplicationId = Guid.NewGuid(),
                    SequenceId = RoatpWorkflowSequenceIds.Finish,
                    Sections = new List<ApplicationSection>
                    {
                        new ApplicationSection
                        {
                            SectionId = RoatpWorkflowSectionIds.Finish.TermsAndConditions,
                            QnAData = new QnAData
                            {
                                Pages = new List<Page>
                                {
                                    new Page
                                    {
                                        Active = true,
                                        Complete = true,
                                        Questions = new List<Question>()
                                    },
                                    new Page
                                    {
                                        Active = true,
                                        Complete = false,
                                        Questions = new List<Question>()
                                    }

                                }
                            }
                        }
                    }
                }
            };

            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishPermissionPersonalDetails)).ReturnsAsync(new Answer { Value = "Yes" });
            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishAccuratePersonalDetails)).ReturnsAsync(new Answer { Value = "No" });
            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishPermissionSubmitApplication)).ReturnsAsync(new Answer { Value = null });

            var model = new TaskListViewModel(_qnaApiClient.Object)
            {
                ApplicationSequences = applicationSequences
            };

            var status = model.FinishSectionStatus(RoatpWorkflowSectionIds.Finish.ApplicationPermissionsAndChecks);
            status.Should().Be("In Progress");
            var css = model.FinishCss(RoatpWorkflowSectionIds.Finish.ApplicationPermissionsAndChecks);
            css.Should().Be("inprogress");
        }

        [Test]
        public void Finish_commercial_in_confidence_shows_blank_if_previous_section_not_complete()
        {
            var applicationSequences = new List<ApplicationSequence>
            {
                new ApplicationSequence
                {
                    ApplicationId = Guid.NewGuid(),
                    SequenceId = RoatpWorkflowSequenceIds.YourOrganisation,
                    Sections = new List<ApplicationSection>
                    {
                        new ApplicationSection
                        {
                            SectionId = RoatpWorkflowSectionIds.YourOrganisation.OrganisationDetails,
                            QnAData = new QnAData
                            {
                                Pages = new List<Page>
                                {
                                    new Page
                                    {
                                        Active = true,
                                        Complete = true,
                                        Questions = new List<Question>()
                                    },
                                    new Page
                                    {
                                        Active = true,
                                        Complete = true,
                                        Questions = new List<Question>()
                                    }
                                }
                            }
                        }
                    }
                },
                new ApplicationSequence
                {
                    ApplicationId = Guid.NewGuid(),
                    SequenceId = RoatpWorkflowSequenceIds.Finish,
                    Sections = new List<ApplicationSection>
                    {
                        new ApplicationSection
                        {
                            SectionId = RoatpWorkflowSectionIds.Finish.TermsAndConditions,
                            QnAData = new QnAData
                            {
                                Pages = new List<Page>
                                {
                                    new Page
                                    {
                                        Active = true,
                                        Complete = true,
                                        Questions = new List<Question>()
                                    },
                                    new Page
                                    {
                                        Active = true,
                                        Complete = false,
                                        Questions = new List<Question>()
                                    }

                                }
                            }
                        },
                        new ApplicationSection
                        {
                            SectionId = RoatpWorkflowSectionIds.Finish.CommercialInConfidenceInformation,
                            QnAData = new QnAData
                            {
                                Pages = new List<Page>
                                {
                                    new Page
                                    {
                                        Active = true,
                                        Complete = false,
                                        Questions = new List<Question>()
                                    }
                                }
                            }
                        }
                    }
                }
            };
            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishPermissionPersonalDetails)).ReturnsAsync(new Answer { Value = "Yes" });
            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishAccuratePersonalDetails)).ReturnsAsync(new Answer { Value = "No" });
            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishPermissionSubmitApplication)).ReturnsAsync(new Answer { Value = null });

            var model = new TaskListViewModel(_qnaApiClient.Object)
            {
                ApplicationSequences = applicationSequences
            };

            var status = model.FinishSectionStatus(RoatpWorkflowSectionIds.Finish.CommercialInConfidenceInformation);
            status.Should().Be(string.Empty);
            var css = model.FinishCss(RoatpWorkflowSectionIds.Finish.CommercialInConfidenceInformation);
            css.Should().Be("hidden");
        }

        [Test]
        public void Finish_commercial_in_confidence_shows_next_if_no_questions_answered_and_previous_section_complete()
        {
            var applicationSequences = new List<ApplicationSequence>
            {
                new ApplicationSequence
                {
                    ApplicationId = Guid.NewGuid(),
                    SequenceId = RoatpWorkflowSequenceIds.YourOrganisation,
                    Sections = new List<ApplicationSection>
                    {
                        new ApplicationSection
                        {
                            SectionId = RoatpWorkflowSectionIds.YourOrganisation.OrganisationDetails,
                            QnAData = new QnAData
                            {
                                Pages = new List<Page>
                                {
                                    new Page
                                    {
                                        Active = true,
                                        Complete = true,
                                        Questions = new List<Question>()
                                    },
                                    new Page
                                    {
                                        Active = true,
                                        Complete = true,
                                        Questions = new List<Question>()
                                    }
                                }
                            }
                        }
                    }
                },
                new ApplicationSequence
                {
                    ApplicationId = Guid.NewGuid(),
                    SequenceId = RoatpWorkflowSequenceIds.Finish,
                    Sections = new List<ApplicationSection>
                    {
                        new ApplicationSection
                        {
                            SectionId = RoatpWorkflowSectionIds.Finish.TermsAndConditions,
                            QnAData = new QnAData
                            {
                                Pages = new List<Page>
                                {
                                    new Page
                                    {
                                        Active = true,
                                        Complete = true,
                                        Questions = new List<Question>()
                                    },
                                    new Page
                                    {
                                        Active = true,
                                        Complete = false,
                                        Questions = new List<Question>()
                                    }

                                }
                            }
                        },
                        new ApplicationSection
                        {
                            SectionId = RoatpWorkflowSectionIds.Finish.CommercialInConfidenceInformation,
                            QnAData = new QnAData
                            {
                                Pages = new List<Page>
                                {
                                    new Page
                                    {
                                        Active = true,
                                        Complete = false,
                                        Questions = new List<Question>()
                                    }
                                }
                            }
                        }
                    }
                }
            };
            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishPermissionPersonalDetails)).ReturnsAsync(new Answer { Value = "Yes" });
            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishAccuratePersonalDetails)).ReturnsAsync(new Answer { Value = "Yes" });
            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishPermissionSubmitApplication)).ReturnsAsync(new Answer { Value = "Yes" });

            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishCommercialInConfidence)).ReturnsAsync(new Answer { Value = null });

            var model = new TaskListViewModel(_qnaApiClient.Object)
            {
                ApplicationSequences = applicationSequences
            };

            var status = model.FinishSectionStatus(RoatpWorkflowSectionIds.Finish.CommercialInConfidenceInformation);
            status.Should().Be("Next");
            var css = model.FinishCss(RoatpWorkflowSectionIds.Finish.CommercialInConfidenceInformation);
            css.Should().Be("next");
        }

        [TestCase("Yes")]
        [TestCase("No")]
        public void Finish_commercial_in_confidence_shows_completed_if_answer_supplied(string answerValue)
        {
            var applicationSequences = new List<ApplicationSequence>
            {
                new ApplicationSequence
                {
                    ApplicationId = Guid.NewGuid(),
                    SequenceId = RoatpWorkflowSequenceIds.YourOrganisation,
                    Sections = new List<ApplicationSection>
                    {
                        new ApplicationSection
                        {
                            SectionId = RoatpWorkflowSectionIds.YourOrganisation.OrganisationDetails,
                            QnAData = new QnAData
                            {
                                Pages = new List<Page>
                                {
                                    new Page
                                    {
                                        Active = true,
                                        Complete = true,
                                        Questions = new List<Question>()
                                    },
                                    new Page
                                    {
                                        Active = true,
                                        Complete = true,
                                        Questions = new List<Question>()
                                    }
                                }
                            }
                        }
                    }
                },
                new ApplicationSequence
                {
                    ApplicationId = Guid.NewGuid(),
                    SequenceId = RoatpWorkflowSequenceIds.Finish,
                    Sections = new List<ApplicationSection>
                    {
                        new ApplicationSection
                        {
                            SectionId = RoatpWorkflowSectionIds.Finish.TermsAndConditions,
                            QnAData = new QnAData
                            {
                                Pages = new List<Page>
                                {
                                    new Page
                                    {
                                        Active = true,
                                        Complete = true,
                                        Questions = new List<Question>()
                                    },
                                    new Page
                                    {
                                        Active = true,
                                        Complete = false,
                                        Questions = new List<Question>()
                                    }

                                }
                            }
                        },
                        new ApplicationSection
                        {
                            SectionId = RoatpWorkflowSectionIds.Finish.CommercialInConfidenceInformation,
                            QnAData = new QnAData
                            {
                                Pages = new List<Page>
                                {
                                    new Page
                                    {
                                        Active = true,
                                        Complete = false,
                                        Questions = new List<Question>()
                                    }
                                }
                            }
                        }
                    }
                }
            };
            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishPermissionPersonalDetails)).ReturnsAsync(new Answer { Value = "Yes" });
            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishAccuratePersonalDetails)).ReturnsAsync(new Answer { Value = "Yes" });
            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishPermissionSubmitApplication)).ReturnsAsync(new Answer { Value = "Yes" });

            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishCommercialInConfidence)).ReturnsAsync(new Answer { Value = answerValue });

            var model = new TaskListViewModel(_qnaApiClient.Object)
            {
                ApplicationSequences = applicationSequences
            };

            var status = model.FinishSectionStatus(RoatpWorkflowSectionIds.Finish.CommercialInConfidenceInformation);
            status.Should().Be("Completed");
            var css = model.FinishCss(RoatpWorkflowSectionIds.Finish.CommercialInConfidenceInformation);
            css.Should().Be("completed");
        }

        [Test]
        public void Finish_terms_and_conditions_shows_blank_if_previous_section_not_complete()
        {
            var applicationSequences = new List<ApplicationSequence>
            {
                new ApplicationSequence
                {
                    ApplicationId = Guid.NewGuid(),
                    SequenceId = RoatpWorkflowSequenceIds.YourOrganisation,
                    Sections = new List<ApplicationSection>
                    {
                        new ApplicationSection
                        {
                            SectionId = RoatpWorkflowSectionIds.YourOrganisation.OrganisationDetails,
                            QnAData = new QnAData
                            {
                                Pages = new List<Page>
                                {
                                    new Page
                                    {
                                        Active = true,
                                        Complete = true,
                                        Questions = new List<Question>()
                                    },
                                    new Page
                                    {
                                        Active = true,
                                        Complete = true,
                                        Questions = new List<Question>()
                                    }
                                }
                            }
                        }
                    }
                },
                new ApplicationSequence
                {
                    ApplicationId = Guid.NewGuid(),
                    SequenceId = RoatpWorkflowSequenceIds.Finish,
                    Sections = new List<ApplicationSection>
                    {
                        new ApplicationSection
                        {
                            SectionId = RoatpWorkflowSectionIds.Finish.TermsAndConditions,
                            QnAData = new QnAData
                            {
                                Pages = new List<Page>
                                {
                                    new Page
                                    {
                                        Active = true,
                                        Complete = true,
                                        Questions = new List<Question>()
                                    },
                                    new Page
                                    {
                                        Active = true,
                                        Complete = false,
                                        Questions = new List<Question>()
                                    }

                                }
                            }
                        },
                        new ApplicationSection
                        {
                            SectionId = RoatpWorkflowSectionIds.Finish.CommercialInConfidenceInformation,
                            QnAData = new QnAData
                            {
                                Pages = new List<Page>
                                {
                                    new Page
                                    {
                                        Active = true,
                                        Complete = false,
                                        Questions = new List<Question>()
                                    }
                                }
                            }
                        }
                    }
                }
            };
            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishPermissionPersonalDetails)).ReturnsAsync(new Answer { Value = "Yes" });
            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishAccuratePersonalDetails)).ReturnsAsync(new Answer { Value = "Yes" });
            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishPermissionSubmitApplication)).ReturnsAsync(new Answer { Value = "Yes" });

            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishCommercialInConfidence)).ReturnsAsync(new Answer { Value = null });

            var model = new TaskListViewModel(_qnaApiClient.Object)
            {
                ApplicationSequences = applicationSequences
            };

            var status = model.FinishSectionStatus(RoatpWorkflowSectionIds.Finish.TermsAndConditions);
            status.Should().Be(string.Empty);
            var css = model.FinishCss(RoatpWorkflowSectionIds.Finish.TermsAndConditions);
            css.Should().Be("hidden");
        }

        [Test]
        public void Finish_terms_and_conditions_shows_next_if_no_questions_answered_and_previous_section_complete()
        {
            var applicationSequences = new List<ApplicationSequence>
            {
                new ApplicationSequence
                {
                    ApplicationId = Guid.NewGuid(),
                    SequenceId = RoatpWorkflowSequenceIds.YourOrganisation,
                    Sections = new List<ApplicationSection>
                    {
                        new ApplicationSection
                        {
                            SectionId = RoatpWorkflowSectionIds.YourOrganisation.OrganisationDetails,
                            QnAData = new QnAData
                            {
                                Pages = new List<Page>
                                {
                                    new Page
                                    {
                                        Active = true,
                                        Complete = true,
                                        Questions = new List<Question>()
                                    },
                                    new Page
                                    {
                                        Active = true,
                                        Complete = true,
                                        Questions = new List<Question>()
                                    }
                                }
                            }
                        }
                    }
                },
                new ApplicationSequence
                {
                    ApplicationId = Guid.NewGuid(),
                    SequenceId = RoatpWorkflowSequenceIds.Finish,
                    Sections = new List<ApplicationSection>
                    {
                        new ApplicationSection
                        {
                            SectionId = RoatpWorkflowSectionIds.Finish.TermsAndConditions,
                            QnAData = new QnAData
                            {
                                Pages = new List<Page>
                                {
                                    new Page
                                    {
                                        Active = true,
                                        Complete = true,
                                        Questions = new List<Question>()
                                    },
                                    new Page
                                    {
                                        Active = true,
                                        Complete = false,
                                        Questions = new List<Question>()
                                    }

                                }
                            }
                        },
                        new ApplicationSection
                        {
                            SectionId = RoatpWorkflowSectionIds.Finish.CommercialInConfidenceInformation,
                            QnAData = new QnAData
                            {
                                Pages = new List<Page>
                                {
                                    new Page
                                    {
                                        Active = true,
                                        Complete = true,
                                        Questions = new List<Question>()
                                    }
                                }
                            }
                        },
                        new ApplicationSection
                        {
                            SectionId = RoatpWorkflowSectionIds.Finish.TermsAndConditions,
                            QnAData = new QnAData
                            {
                                Pages = new List<Page>
                                {
                                    new Page
                                    {
                                        Active = true,
                                        Complete = false,
                                        Questions = new List<Question>()
                                    }
                                }
                            }
                        }
                    }
                }
            };
            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishPermissionPersonalDetails)).ReturnsAsync(new Answer { Value = "Yes" });
            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishAccuratePersonalDetails)).ReturnsAsync(new Answer { Value = "Yes" });
            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishPermissionSubmitApplication)).ReturnsAsync(new Answer { Value = "Yes" });

            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishCommercialInConfidence)).ReturnsAsync(new Answer { Value = "No" });

            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishCOA2MainEmployer)).ReturnsAsync(new Answer { Value = null });
            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishCOA3MainEmployer)).ReturnsAsync(new Answer { Value = null });

            var model = new TaskListViewModel(_qnaApiClient.Object)
            {
                ApplicationSequences = applicationSequences,
                ApplicationRouteId = MainProviderRoute
            };

            var status = model.FinishSectionStatus(RoatpWorkflowSectionIds.Finish.TermsAndConditions);
            status.Should().Be("Next");
            var css = model.FinishCss(RoatpWorkflowSectionIds.Finish.TermsAndConditions);
            css.Should().Be("next");
        }

        [Test]
        public void Finish_terms_and_conditions_shows_in_progress_if_not_all_questions_answered()
        {
            var applicationSequences = new List<ApplicationSequence>
            {
                new ApplicationSequence
                {
                    ApplicationId = Guid.NewGuid(),
                    SequenceId = RoatpWorkflowSequenceIds.YourOrganisation,
                    Sections = new List<ApplicationSection>
                    {
                        new ApplicationSection
                        {
                            SectionId = RoatpWorkflowSectionIds.YourOrganisation.OrganisationDetails,
                            QnAData = new QnAData
                            {
                                Pages = new List<Page>
                                {
                                    new Page
                                    {
                                        Active = true,
                                        Complete = true,
                                        Questions = new List<Question>()
                                    },
                                    new Page
                                    {
                                        Active = true,
                                        Complete = true,
                                        Questions = new List<Question>()
                                    }
                                }
                            }
                        }
                    }
                },
                new ApplicationSequence
                {
                    ApplicationId = Guid.NewGuid(),
                    SequenceId = RoatpWorkflowSequenceIds.Finish,
                    Sections = new List<ApplicationSection>
                    {
                        new ApplicationSection
                        {
                            SectionId = RoatpWorkflowSectionIds.Finish.TermsAndConditions,
                            QnAData = new QnAData
                            {
                                Pages = new List<Page>
                                {
                                    new Page
                                    {
                                        Active = true,
                                        Complete = true,
                                        Questions = new List<Question>()
                                    },
                                    new Page
                                    {
                                        Active = true,
                                        Complete = false,
                                        Questions = new List<Question>()
                                    }

                                }
                            }
                        },
                        new ApplicationSection
                        {
                            SectionId = RoatpWorkflowSectionIds.Finish.CommercialInConfidenceInformation,
                            QnAData = new QnAData
                            {
                                Pages = new List<Page>
                                {
                                    new Page
                                    {
                                        Active = true,
                                        Complete = true,
                                        Questions = new List<Question>()
                                    }
                                }
                            }
                        },
                        new ApplicationSection
                        {
                            SectionId = RoatpWorkflowSectionIds.Finish.TermsAndConditions,
                            QnAData = new QnAData
                            {
                                Pages = new List<Page>
                                {
                                    new Page
                                    {
                                        Active = true,
                                        Complete = false,
                                        Questions = new List<Question>()
                                    }
                                }
                            }
                        }
                    }
                }
            };
            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishPermissionPersonalDetails)).ReturnsAsync(new Answer { Value = "Yes" });
            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishAccuratePersonalDetails)).ReturnsAsync(new Answer { Value = "Yes" });
            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishPermissionSubmitApplication)).ReturnsAsync(new Answer { Value = "Yes" });

            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishCommercialInConfidence)).ReturnsAsync(new Answer { Value = "No" });

            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishCOA2Supporting)).ReturnsAsync(new Answer { Value = "Yes" });
            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishCOA3Supporting)).ReturnsAsync(new Answer { Value = null });

            var model = new TaskListViewModel(_qnaApiClient.Object)
            {
                ApplicationSequences = applicationSequences,
                ApplicationRouteId = SupportingProviderRoute
            };

            var status = model.FinishSectionStatus(RoatpWorkflowSectionIds.Finish.TermsAndConditions);
            status.Should().Be("In Progress");
            var css = model.FinishCss(RoatpWorkflowSectionIds.Finish.TermsAndConditions);
            css.Should().Be("inprogress");
        }

        [Test]
        public void Finish_terms_and_conditions_shows_in_progress_if_questions_answered_and_shutter_page_shown_for_conditions_not_met()
        {
            var applicationSequences = new List<ApplicationSequence>
            {
                new ApplicationSequence
                {
                    ApplicationId = Guid.NewGuid(),
                    SequenceId = RoatpWorkflowSequenceIds.YourOrganisation,
                    Sections = new List<ApplicationSection>
                    {
                        new ApplicationSection
                        {
                            SectionId = RoatpWorkflowSectionIds.YourOrganisation.OrganisationDetails,
                            QnAData = new QnAData
                            {
                                Pages = new List<Page>
                                {
                                    new Page
                                    {
                                        Active = true,
                                        Complete = true,
                                        Questions = new List<Question>()
                                    },
                                    new Page
                                    {
                                        Active = true,
                                        Complete = true,
                                        Questions = new List<Question>()
                                    }
                                }
                            }
                        }
                    }
                },
                new ApplicationSequence
                {
                    ApplicationId = Guid.NewGuid(),
                    SequenceId = RoatpWorkflowSequenceIds.Finish,
                    Sections = new List<ApplicationSection>
                    {
                        new ApplicationSection
                        {
                            SectionId = RoatpWorkflowSectionIds.Finish.TermsAndConditions,
                            QnAData = new QnAData
                            {
                                Pages = new List<Page>
                                {
                                    new Page
                                    {
                                        Active = true,
                                        Complete = true,
                                        Questions = new List<Question>()
                                    },
                                    new Page
                                    {
                                        Active = true,
                                        Complete = false,
                                        Questions = new List<Question>()
                                    }

                                }
                            }
                        },
                        new ApplicationSection
                        {
                            SectionId = RoatpWorkflowSectionIds.Finish.CommercialInConfidenceInformation,
                            QnAData = new QnAData
                            {
                                Pages = new List<Page>
                                {
                                    new Page
                                    {
                                        Active = true,
                                        Complete = true,
                                        Questions = new List<Question>()
                                    }
                                }
                            }
                        },
                        new ApplicationSection
                        {
                            SectionId = RoatpWorkflowSectionIds.Finish.TermsAndConditions,
                            QnAData = new QnAData
                            {
                                Pages = new List<Page>
                                {
                                    new Page
                                    {
                                        Active = true,
                                        Complete = false,
                                        Questions = new List<Question>()
                                    }
                                }
                            }
                        }
                    }
                }
            };
            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishPermissionPersonalDetails)).ReturnsAsync(new Answer { Value = "Yes" });
            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishAccuratePersonalDetails)).ReturnsAsync(new Answer { Value = "Yes" });
            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishPermissionSubmitApplication)).ReturnsAsync(new Answer { Value = "Yes" });

            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishCommercialInConfidence)).ReturnsAsync(new Answer { Value = "No" });

            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishCOA2Supporting)).ReturnsAsync(new Answer { Value = "Yes" });
            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishCOA3Supporting)).ReturnsAsync(new Answer { Value = "No" });

            var model = new TaskListViewModel(_qnaApiClient.Object)
            {
                ApplicationSequences = applicationSequences,
                ApplicationRouteId = SupportingProviderRoute
            };

            var status = model.FinishSectionStatus(RoatpWorkflowSectionIds.Finish.TermsAndConditions);
            status.Should().Be("In Progress");
            var css = model.FinishCss(RoatpWorkflowSectionIds.Finish.TermsAndConditions);
            css.Should().Be("inprogress");
        }

        [Test]
        public void Finish_terms_and_conditions_shows_completed_if_all_questions_answered()
        {
            var applicationSequences = new List<ApplicationSequence>
            {
                new ApplicationSequence
                {
                    ApplicationId = Guid.NewGuid(),
                    SequenceId = RoatpWorkflowSequenceIds.YourOrganisation,
                    Sections = new List<ApplicationSection>
                    {
                        new ApplicationSection
                        {
                            SectionId = RoatpWorkflowSectionIds.YourOrganisation.OrganisationDetails,
                            QnAData = new QnAData
                            {
                                Pages = new List<Page>
                                {
                                    new Page
                                    {
                                        Active = true,
                                        Complete = true,
                                        Questions = new List<Question>()
                                    },
                                    new Page
                                    {
                                        Active = true,
                                        Complete = true,
                                        Questions = new List<Question>()
                                    }
                                }
                            }
                        }
                    }
                },
                new ApplicationSequence
                {
                    ApplicationId = Guid.NewGuid(),
                    SequenceId = RoatpWorkflowSequenceIds.Finish,
                    Sections = new List<ApplicationSection>
                    {
                        new ApplicationSection
                        {
                            SectionId = RoatpWorkflowSectionIds.Finish.TermsAndConditions,
                            QnAData = new QnAData
                            {
                                Pages = new List<Page>
                                {
                                    new Page
                                    {
                                        Active = true,
                                        Complete = true,
                                        Questions = new List<Question>()
                                    },
                                    new Page
                                    {
                                        Active = true,
                                        Complete = false,
                                        Questions = new List<Question>()
                                    }

                                }
                            }
                        },
                        new ApplicationSection
                        {
                            SectionId = RoatpWorkflowSectionIds.Finish.CommercialInConfidenceInformation,
                            QnAData = new QnAData
                            {
                                Pages = new List<Page>
                                {
                                    new Page
                                    {
                                        Active = true,
                                        Complete = true,
                                        Questions = new List<Question>()
                                    }
                                }
                            }
                        },
                        new ApplicationSection
                        {
                            SectionId = RoatpWorkflowSectionIds.Finish.TermsAndConditions,
                            QnAData = new QnAData
                            {
                                Pages = new List<Page>
                                {
                                    new Page
                                    {
                                        Active = true,
                                        Complete = false,
                                        Questions = new List<Question>()
                                    }
                                }
                            }
                        }
                    }
                }
            };
            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishPermissionPersonalDetails)).ReturnsAsync(new Answer { Value = "Yes" });
            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishAccuratePersonalDetails)).ReturnsAsync(new Answer { Value = "Yes" });
            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishPermissionSubmitApplication)).ReturnsAsync(new Answer { Value = "Yes" });

            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishCommercialInConfidence)).ReturnsAsync(new Answer { Value = "No" });

            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishCOA2MainEmployer)).ReturnsAsync(new Answer { Value = "Yes" });
            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishCOA3MainEmployer)).ReturnsAsync(new Answer { Value = "Yes" });

            var model = new TaskListViewModel(_qnaApiClient.Object)
            {
                ApplicationSequences = applicationSequences,
                ApplicationRouteId = EmployerProviderRoute
            };

            var status = model.FinishSectionStatus(RoatpWorkflowSectionIds.Finish.TermsAndConditions);
            status.Should().Be("Completed");
            var css = model.FinishCss(RoatpWorkflowSectionIds.Finish.TermsAndConditions);
            css.Should().Be("completed");
        }

        [Test]
        public void Finish_submit_application_shows_next_if_previous_section_completed()
        {
            var applicationSequences = new List<ApplicationSequence>
            {
                new ApplicationSequence
                {
                    ApplicationId = Guid.NewGuid(),
                    SequenceId = RoatpWorkflowSequenceIds.YourOrganisation,
                    Sections = new List<ApplicationSection>
                    {
                        new ApplicationSection
                        {
                            SectionId = RoatpWorkflowSectionIds.YourOrganisation.OrganisationDetails,
                            QnAData = new QnAData
                            {
                                Pages = new List<Page>
                                {
                                    new Page
                                    {
                                        Active = true,
                                        Complete = true,
                                        Questions = new List<Question>()
                                    },
                                    new Page
                                    {
                                        Active = true,
                                        Complete = true,
                                        Questions = new List<Question>()
                                    }
                                }
                            }
                        }
                    }
                },
                new ApplicationSequence
                {
                    ApplicationId = Guid.NewGuid(),
                    SequenceId = RoatpWorkflowSequenceIds.Finish,
                    Sections = new List<ApplicationSection>
                    {
                        new ApplicationSection
                        {
                            SectionId = RoatpWorkflowSectionIds.Finish.TermsAndConditions,
                            QnAData = new QnAData
                            {
                                Pages = new List<Page>
                                {
                                    new Page
                                    {
                                        Active = true,
                                        Complete = true,
                                        Questions = new List<Question>()
                                    },
                                    new Page
                                    {
                                        Active = true,
                                        Complete = false,
                                        Questions = new List<Question>()
                                    }

                                }
                            }
                        },
                        new ApplicationSection
                        {
                            SectionId = RoatpWorkflowSectionIds.Finish.CommercialInConfidenceInformation,
                            QnAData = new QnAData
                            {
                                Pages = new List<Page>
                                {
                                    new Page
                                    {
                                        Active = true,
                                        Complete = true,
                                        Questions = new List<Question>()
                                    }
                                }
                            }
                        },
                        new ApplicationSection
                        {
                            SectionId = RoatpWorkflowSectionIds.Finish.TermsAndConditions,
                            QnAData = new QnAData
                            {
                                Pages = new List<Page>
                                {
                                    new Page
                                    {
                                        Active = true,
                                        Complete = false,
                                        Questions = new List<Question>()
                                    }
                                }
                            }
                        }
                    }
                }
            };
            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishPermissionPersonalDetails)).ReturnsAsync(new Answer { Value = "Yes" });
            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishAccuratePersonalDetails)).ReturnsAsync(new Answer { Value = "Yes" });
            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishPermissionSubmitApplication)).ReturnsAsync(new Answer { Value = "Yes" });

            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishCommercialInConfidence)).ReturnsAsync(new Answer { Value = "No" });

            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishCOA2Supporting)).ReturnsAsync(new Answer { Value = "Yes" });
            _qnaApiClient.Setup(x => x.GetAnswerByTag(It.IsAny<Guid>(), RoatpWorkflowQuestionTags.FinishCOA3Supporting)).ReturnsAsync(new Answer { Value = "Yes" });

            var model = new TaskListViewModel(_qnaApiClient.Object)
            {
                ApplicationSequences = applicationSequences,
                ApplicationRouteId = SupportingProviderRoute
            };

            var status = model.FinishSectionStatus(RoatpWorkflowSectionIds.Finish.SubmitApplication);
            status.Should().Be("Next");
            var css = model.FinishCss(RoatpWorkflowSectionIds.Finish.SubmitApplication);
            css.Should().Be("next");
        }
    }
}
