using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SFA.DAS.ApplyService.Application.Apply.Roatp;
using SFA.DAS.ApplyService.Application.Apply.Start;
using SFA.DAS.ApplyService.Configuration;
using SFA.DAS.ApplyService.Domain.Apply;
using SFA.DAS.ApplyService.Domain.Entities;
using SFA.DAS.ApplyService.Domain.Roatp;
using SFA.DAS.ApplyService.Session;
using SFA.DAS.ApplyService.Web.Infrastructure;
using SFA.DAS.ApplyService.Web.Infrastructure.Interfaces;
using SFA.DAS.ApplyService.Web.Orchestrators;
using SFA.DAS.ApplyService.Web.ViewModels;

namespace SFA.DAS.ApplyService.Web.Controllers
{
    using Configuration;
    using Microsoft.Extensions.Options;
    using MoreLinq;
    using SFA.DAS.ApplyService.EmailService.Interfaces;
    using SFA.DAS.ApplyService.Web.Controllers.Roatp;
    using SFA.DAS.ApplyService.Web.Infrastructure.Validations;
    using SFA.DAS.ApplyService.Web.Services;
    using SFA.DAS.ApplyService.Web.Validators;
    using ViewModels.Roatp;

    [Authorize]
    public class RoatpApplicationController : RoatpApplyControllerBase
    {
        private readonly IApplicationApiClient _apiClient;
        private readonly ILogger<RoatpApplicationController> _logger;
        private readonly IUsersApiClient _usersApiClient;
        private readonly IConfigurationService _configService;
        private readonly IUserService _userService;
        private readonly IQnaApiClient _qnaApiClient;
        private readonly IQuestionPropertyTokeniser _questionPropertyTokeniser;
        private readonly IPageNavigationTrackingService _pageNavigationTrackingService;
        private readonly List<QnaPageOverrideConfiguration> _pageOverrideConfiguration;
        private readonly List<QnaLinksConfiguration> _qnaLinks;
        private readonly ICustomValidatorFactory _customValidatorFactory;
        private readonly IRoatpApiClient _roatpApiClient;
        private readonly ISubmitApplicationConfirmationEmailService _submitApplicationEmailService;
        private readonly ITabularDataRepository _tabularDataRepository;
        private readonly IPagesWithSectionsFlowService _pagesWithSectionsFlowService;
        private readonly IRoatpTaskListWorkflowService _roatpTaskListWorkflowService;
        private readonly IRoatpOrganisationVerificationService _organisationVerificationService;
        private readonly ITaskListOrchestrator _taskListOrchestrator;
        private readonly IUkrlpApiClient _ukrlpApiClient;
        private readonly IApplicationApiClient _applicationApiClient;

        private const string InputClassUpperCase = "app-uppercase";
        private const string NotApplicableAnswerText = "None of the above";
        private const string InvalidCheckBoxListSelectionErrorMessage = "If your answer is 'none of the above', you must only select that option";

        public RoatpApplicationController(IApplicationApiClient apiClient, ILogger<RoatpApplicationController> logger,
            ISessionService sessionService, IConfigurationService configService, IUserService userService, IUsersApiClient usersApiClient,
            IQnaApiClient qnaApiClient,
            IPagesWithSectionsFlowService pagesWithSectionsFlowService,
            IQuestionPropertyTokeniser questionPropertyTokeniser, IOptions<List<QnaPageOverrideConfiguration>> pageOverrideConfiguration, 
            IPageNavigationTrackingService pageNavigationTrackingService, IOptions<List<QnaLinksConfiguration>> qnaLinks, 
            ICustomValidatorFactory customValidatorFactory,  
            IRoatpApiClient roatpApiClient, ISubmitApplicationConfirmationEmailService submitApplicationEmailService,
            ITabularDataRepository tabularDataRepository, IRoatpTaskListWorkflowService roatpTaskListWorkflowService,
            IRoatpOrganisationVerificationService organisationVerificationService, ITaskListOrchestrator taskListOrchestrator, IUkrlpApiClient ukrlpApiClient, IApplicationApiClient applicationApiClient)
            :base(sessionService)
        {
            _apiClient = apiClient;
            _logger = logger;
            _sessionService = sessionService;
            _configService = configService;
            _userService = userService;
            _usersApiClient = usersApiClient;
            _qnaApiClient = qnaApiClient;
            _pagesWithSectionsFlowService = pagesWithSectionsFlowService;
            _questionPropertyTokeniser = questionPropertyTokeniser;
            _pageNavigationTrackingService = pageNavigationTrackingService;
            _qnaLinks = qnaLinks.Value;
            _pageOverrideConfiguration = pageOverrideConfiguration.Value;
            _customValidatorFactory = customValidatorFactory;
            _roatpApiClient = roatpApiClient;
            _submitApplicationEmailService = submitApplicationEmailService;
            _tabularDataRepository = tabularDataRepository;
            _roatpTaskListWorkflowService = roatpTaskListWorkflowService;
            _organisationVerificationService = organisationVerificationService;
            _taskListOrchestrator = taskListOrchestrator;
            _ukrlpApiClient = ukrlpApiClient;
            _applicationApiClient = applicationApiClient;
        }

        [HttpGet]
        public async Task<IActionResult> Applications()
        {
            var user = User.Identity.Name;

            if (!await _userService.ValidateUser(user))
                return RedirectToAction("PostSignIn", "Users");

            _logger.LogDebug($"Got LoggedInUser from Session: {user}");

            var signinId = await _userService.GetSignInId();
            var applications = await GetInFlightApplicationsForSignInId(signinId);

            var application = new Apply();
            Guid applicationId;
            string applicationStatus;

            if (applications.Count > 1)
            {
                _logger.LogError($"Multiple in flight applications found for userId: {signinId}");
                return View("~/Views/Roatp/Applications.cshtml", applications);
            }
            if (applications.Count == 1)
            {
                application = applications.Single();
                applicationId = application.ApplicationId;
                applicationStatus = application.ApplicationStatus;
                
            }
            else
            {
                applicationId = await StartApplication(signinId);
                applicationStatus = ApplicationStatus.InProgress;

                if (applicationId == Guid.Empty)
                {
                    return RedirectToAction("EnterApplicationUkprn", "RoatpApplicationPreamble");
                }
            }
            
            _logger.LogDebug("Applications controller action completed");

            switch (applicationStatus)
            {
                case ApplicationStatus.New:
                case ApplicationStatus.InProgress:
                    return RedirectToAction("TaskList", new { applicationId });
                case ApplicationStatus.Approved:
                    return View("~/Views/Application/Approved.cshtml", application);
                case ApplicationStatus.Rejected:
                    return RedirectToAction("ApplicationRejected", new { applicationId });
                case ApplicationStatus.FeedbackAdded:
                    return View("~/Views/Application/FeedbackIntro.cshtml", applicationId);
                case ApplicationStatus.Withdrawn:
                    return RedirectToAction("ApplicationWithdrawn", new { applicationId });
                case ApplicationStatus.Removed:
                    return RedirectToAction("ApplicationRemoved", new { applicationId });
                case ApplicationStatus.GatewayAssessed:
                    if(application.GatewayReviewStatus == GatewayReviewStatus.Reject)
                        return RedirectToAction("ApplicationRejected", new { applicationId });
                    else
                        return RedirectToAction("ApplicationSubmitted", new { applicationId });
                case ApplicationStatus.Submitted:
                case ApplicationStatus.Resubmitted:
                    return RedirectToAction("ApplicationSubmitted", new { applicationId });
                default:
                    return RedirectToAction("TaskList", new { applicationId });
            }
        }

        private async Task<List<Apply>> GetInFlightApplicationsForSignInId(Guid signinId)
        {
            var applications = await _apiClient.GetApplications(signinId, false);

            var statusFilter = new[] { ApplicationStatus.Cancelled };

            return applications.Where(app => !statusFilter.Contains(app.ApplicationStatus)).OrderByDescending(app => app.CreatedAt).ToList();
        }

        private async Task<Guid> StartApplication(Guid signinId)
        {
            _logger.LogDebug("StartApplication method invoked");

            var applications = await GetInFlightApplicationsForSignInId(signinId);

            if (applications.Any())
            {
                _logger.LogError($"Multiple in flight applications found for userId: {signinId}");
                return applications.First().ApplicationId;
            }

            var applicationDetails = _sessionService.Get<ApplicationDetails>(ApplicationDetailsKey);
            if (applicationDetails is null)
            {
                return Guid.Empty;
            }

            _logger.LogDebug($"Application Details:: Ukprn: [{applicationDetails?.UKPRN}], ProviderName: [{applicationDetails?.UkrlpLookupDetails?.ProviderName}], RouteId: [{applicationDetails?.ApplicationRoute?.Id}]");
            var providerRoute = applicationDetails.ApplicationRoute.Id;

            var startApplicationData = new JObject
            {
                ["OrganisationReferenceId"] = applicationDetails.UKPRN.ToString(),
                ["OrganisationName"] = applicationDetails.UkrlpLookupDetails.ProviderName,
                ["ApplyProviderRoute"] = providerRoute.ToString()
            };

            var user = await _usersApiClient.GetUserBySignInId(signinId.ToString());

            var applicationType = ApplicationTypes.RegisterTrainingProviders;
            var startApplicationJson = JsonConvert.SerializeObject(startApplicationData);
            _logger.LogDebug($"RoatpApplicationController.StartApplication:: Checking applicationStartResponse PRE: userid: [{user.Id.ToString()}], applicationType: [{applicationType}], startApplicationJson: [{startApplicationJson}]");
            var qnaResponse = await _qnaApiClient.StartApplication(user.Id.ToString(), applicationType, startApplicationJson);
            _logger.LogDebug($"RoatpApplicationController.StartApplication:: Checking applicationStartResponse POST: applicationId: [{qnaResponse?.ApplicationId}]");

            if (qnaResponse != null)
            {
                var allQnaSequencesTask = _qnaApiClient.GetSequences(qnaResponse.ApplicationId);
                var allQnaSectionsTask = _qnaApiClient.GetSections(qnaResponse.ApplicationId);

                await Task.WhenAll(allQnaSequencesTask, allQnaSectionsTask);

                var allQnaSequences = await allQnaSequencesTask;
                var allQnaSections = await allQnaSectionsTask;

                var startApplicationRequest = BuildStartApplicationRequest(qnaResponse.ApplicationId, user.Id, providerRoute, applicationDetails.RoatpRegisterStatus.ProviderTypeId, allQnaSequences, allQnaSections);

                var applicationId = await _apiClient.StartApplication(startApplicationRequest);
                _logger.LogDebug($"RoatpApplicationController.StartApplication:: Checking response from StartApplication POST: applicationId: [{applicationId}]");

                if (applicationId != Guid.Empty)
                {
                   await SavePreambleInformation(applicationId, applicationDetails);
                   _logger.LogDebug("Preamble information saved");

                    if (applicationDetails.UkrlpLookupDetails.VerifiedByCompaniesHouse)
                    {
                        await SaveCompaniesHouseInformation(applicationId, applicationDetails);
                        _logger.LogDebug("Companies House information saved");
                    }

                    if (applicationDetails.UkrlpLookupDetails.VerifiedbyCharityCommission)
                    {
                        await SaveCharityCommissionInformation(applicationId, applicationDetails);
                        _logger.LogDebug("Save Charity Commission information saved");
                    }
                }
                
                _logger.LogDebug("StartApplication method completed");

                return applicationId;
            }

            return Guid.Empty;
        }

        private string GetRouteName(int? routeId)
        {
            switch (routeId)
            {
                case null:
                    return string.Empty;
                case 1:
                    return "Main provider";
                case 2:
                    return "Employer provider";
                case 3:
                    return "Supporting provider";
                default:
                    throw new ArgumentException(nameof(routeId));
            }
        }

        private StartApplicationRequest BuildStartApplicationRequest(Guid qnaApplicationId, Guid creatingContactId, int providerRoute, int? providerRouteOnRegister, IEnumerable<ApplicationSequence> qnaSequences, IEnumerable<ApplicationSection> qnaSections)
        {
            return new StartApplicationRequest
            {
                ApplicationId = qnaApplicationId,
                CreatingContactId = creatingContactId,
                ProviderRoute = providerRoute,
                ProviderRouteName = GetRouteName(providerRoute),
                ProviderRouteOnRegister = providerRouteOnRegister,
                ProviderRouteNameOnRegister = GetRouteName(providerRouteOnRegister),
                ApplySequences = qnaSequences.Select(sequence => new ApplySequence
                {
                    SequenceId = sequence.Id,
                    SequenceNo = sequence.SequenceId,
                    Sections = qnaSections.Where(x => x.SequenceId == sequence.SequenceId).Select(section => new ApplySection
                    {
                        SectionId = section.Id,
                        SectionNo = section.SectionId,
                        //Status = "Draft",
                        //RequestedFeedbackAnswered = false
                    }).OrderBy(section => section.SectionNo).ToList(),
                    //Status = "Draft",
                    //IsActive = sequence.IsActive,                    
                    //Sequential = false
                }).OrderBy(sequence => sequence.SequenceNo).ToList()
            };
        }

        [HttpGet]
        public async Task<IActionResult> SequenceSignPost(Guid applicationId)
        {
            var application = await _apiClient.GetApplication(applicationId);
            if (application is null)
            {
                return RedirectToAction("Applications");
            }

            if (application.ApplicationStatus == ApplicationStatus.Approved)
            {
                return View("~/Views/Application/Approved.cshtml", application);
            }

            if (application.ApplicationStatus == ApplicationStatus.Rejected)
            {
                return View("~/Views/Application/Rejected.cshtml", application);
            }

            if (application.ApplicationStatus == ApplicationStatus.FeedbackAdded)
            {
                return View("~/Views/Application/FeedbackIntro.cshtml", application.ApplicationId);
            }

            return RedirectToAction("TaskList", new { applicationId = application.ApplicationId });
        }

        [HttpGet]
        public async Task<IActionResult> Back(Guid applicationId, int sequenceId, int sectionId, string pageId, string redirectAction)
        {
            string previousPageId = await _pageNavigationTrackingService.GetBackNavigationPageId(applicationId, sequenceId, sectionId, pageId);

            if (previousPageId == null)
            {
                return RedirectToAction("TaskList", "RoatpApplication", new { applicationId }, $"Sequence_{sequenceId}");
            }

            return RedirectToAction("Page", new { applicationId, sequenceId, sectionId, pageId = previousPageId, redirectAction });
        }

        [HttpGet]
        public async Task<IActionResult> Section(Guid applicationId, int sequenceId, int sectionId)
        {
            var canUpdate = await CanUpdateApplication(applicationId, sequenceId, sectionId);
            if (!canUpdate)
            {
                RedirectToAction("TaskList", "RoatpApplication", new {applicationId}, $"Sequence_{sequenceId}");
            }
            
            var section = await _qnaApiClient.GetSectionBySectionNo(applicationId, sequenceId, sectionId);

            if (section?.DisplayType == SectionDisplayType.PagesWithSections)
            {
                var applicationSection = _pagesWithSectionsFlowService.ProcessPagesInSectionsForStatusText(section);
                return View("~/Views/Application/PagesWithSections.cshtml", applicationSection);
            }

            var pageId = section.QnAData.Pages.FirstOrDefault()?.PageId;

            return await Page(applicationId, sequenceId, sectionId, pageId, "TaskList", null);
        }

        [HttpGet]
        [ModelStatePersist(ModelStatePersist.RestoreEntry)]
        public async Task<IActionResult> Page(Guid applicationId, int sequenceId, int sectionId, string pageId, string redirectAction, List<Question> answeredQuestions)
        {
            var canUpdate = await CanUpdateApplication(applicationId, sequenceId, sectionId, pageId);
            if (!canUpdate)
            {
                return RedirectToAction("TaskList", "RoatpApplication", new { applicationId }, $"Sequence_{sequenceId}");
            }

            _pageNavigationTrackingService.AddPageToNavigationStack(pageId);

            var selectedSection = await _qnaApiClient.GetSectionBySectionNo(applicationId, sequenceId, sectionId);
            var sectionTitle = selectedSection.LinkTitle;

            PageViewModel viewModel = null;
            var returnUrl = Request.Headers["Referer"].ToString();

            string pageContext = string.Empty;

            if (!ModelState.IsValid)
            {
                // when the model state has errors the page will be displayed with the values which failed validation
                var page = selectedSection.GetPage(pageId);
                var pageOfAnswers = JsonConvert.DeserializeObject<List<PageOfAnswers>>((string)TempData["InvalidPageOfAnswers"]);
                page.PageOfAnswers = pageOfAnswers;

                var peopleInControlDetails = await GetPeopleInControlDetails(applicationId, sequenceId, sectionId);

                var errorMessages = !ModelState.IsValid
                    ? ModelState.SelectMany(k => k.Value.Errors.Select(e => new ValidationErrorDetail()
                    {
                        ErrorMessage = e.ErrorMessage,
                        Field = k.Key
                    })).DistinctBy(f => f.Field).ToList()
                    : null;

                viewModel = new PageViewModel(applicationId, sequenceId, sectionId, pageId, page, pageContext, redirectAction,
                    returnUrl, errorMessages, _pageOverrideConfiguration, _qnaLinks, sectionTitle, peopleInControlDetails);
            }
            else
            {
                var page = selectedSection.GetPage(pageId);
                
                if (page == null || page.Questions == null)
                {
                    return RedirectToAction("TaskList","RoatpApplication", new { applicationId },$"Sequence_{sequenceId}");
                }

                page = await GetDataFedOptions(applicationId, page);
                var peopleInControlDetails = await GetPeopleInControlDetails(applicationId, sequenceId, sectionId);

                viewModel = new PageViewModel(applicationId, sequenceId, sectionId, pageId, page, pageContext, redirectAction,
                    returnUrl, null, _pageOverrideConfiguration, _qnaLinks, sectionTitle, peopleInControlDetails);

            }

            viewModel = await TokeniseViewModelProperties(viewModel);

            PopulateGetHelpWithQuestion(viewModel, pageId);

            if (viewModel.DisplayType == PageDisplayType.MultipleFileUpload)
            {
                return View("~/Views/Application/Pages/MultipleFileUpload.cshtml", viewModel);
            }
            else
            {
                return View("~/Views/Application/Pages/Index.cshtml", viewModel);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Skip(Guid applicationId, int sequenceId, int sectionId, string pageId, string redirectAction)
        {
            var canUpdate = await CanUpdateApplication(applicationId, sequenceId, sectionId, pageId);
            if (!canUpdate)
            {
                return RedirectToAction("TaskList", "RoatpApplication", new { applicationId }, $"Sequence_{sequenceId}");
            }

            var nextAction = await _qnaApiClient.SkipPageBySectionNo(applicationId, sequenceId, sectionId, pageId);
            var nextPageId = nextAction?.NextActionId;

            // Note that SkipPage could have updated the section within QnA, so you must get the latest version!
            var section = await _qnaApiClient.GetSectionBySectionNo(applicationId, sequenceId, sectionId);

            if (nextPageId == null || section.QnAData.Pages.FirstOrDefault(x => x.PageId == nextPageId) == null)
                return RedirectToAction("TaskList", "RoatpApplication", new { applicationId }, $"Sequence_{sequenceId}");

            return RedirectToAction("Page", new
            {
                applicationId,
                sequenceId,
                sectionId,
                pageId = nextPageId,
                redirectAction
            });
        }

        [HttpPost]
        [ModelStatePersist(ModelStatePersist.Store)]
        public async Task<IActionResult> SaveAnswers(PageViewModel vm, string formAction)
        {
            var applicationId = vm.ApplicationId;
            var sequenceId = vm.SequenceId;
            var sectionId = vm.SectionId;
            var pageId = vm.PageId;
            var redirectAction = vm.RedirectAction;

            var canUpdate = await CanUpdateApplication(applicationId, sequenceId, sectionId, pageId);
            if (!canUpdate)
            {
                return RedirectToAction("TaskList", "RoatpApplication", new { applicationId }, $"Sequence_{sequenceId}");
            }

            _pageNavigationTrackingService.AddPageToNavigationStack(pageId);

            var selectedSection = await _qnaApiClient.GetSectionBySectionNo(applicationId, sequenceId, sectionId);

            // TODO: Do we have anything in the viewmodel that'll trigger this?
            if (!ModelState.IsValid)
            {
                // when the model state has errors the page will be displayed with the values which failed validation
                var pageInvalid = selectedSection.GetPage(pageId);
                var pageOfAnswers = JsonConvert.DeserializeObject<List<PageOfAnswers>>((string)TempData["InvalidPageOfAnswers"]);
                pageInvalid.PageOfAnswers = pageOfAnswers;

                var returnUrl = Request.Headers["Referer"].ToString();

                var errorMessages = !ModelState.IsValid
                    ? ModelState.SelectMany(k => k.Value.Errors.Select(e => new ValidationErrorDetail()
                    {
                        ErrorMessage = e.ErrorMessage,
                        Field = k.Key
                    })).DistinctBy(f => f.Field).ToList()
                    : null;

                var peopleInControlDetails = await GetPeopleInControlDetails(applicationId, sequenceId, sectionId);

                var viewModel = new PageViewModel(applicationId, sequenceId, sectionId, pageId, pageInvalid, string.Empty, redirectAction,
                    returnUrl, errorMessages, _pageOverrideConfiguration, _qnaLinks, selectedSection.Title, peopleInControlDetails);

                viewModel = await TokeniseViewModelProperties(viewModel);

                if (viewModel.DisplayType == PageDisplayType.MultipleFileUpload)
                {
                    return View("~/Views/Application/Pages/MultipleFileUpload.cshtml", viewModel);
                }
                else
                {
                    return View("~/Views/Application/Pages/Index.cshtml", viewModel);
                }
            }

            // when the model state has no errors the page will be displayed with the last valid values which were saved
            var page = selectedSection.GetPage(pageId);
            
            if (page == null)
            {
                RedirectToAction("TaskList", "RoatpApplication", new {applicationId}, $"Sequence_{sequenceId}");
            }

            return await SaveAnswersGiven(applicationId, selectedSection.Id, selectedSection.SectionId, selectedSection.SequenceId, pageId, page, redirectAction, formAction);
        }

        [Route("apply-training-provider-tasklist")]
        [HttpGet]
        public async Task<IActionResult> TaskList(Guid applicationId)
        {
            var canUpdate = await CanUpdateApplication(applicationId);
            if (!canUpdate)
            {
                return RedirectToAction("Applications");
            }

            var viewModel = await _taskListOrchestrator.GetTaskListViewModel(applicationId, User.GetUserId());
            return View("~/Views/Roatp/TaskList.cshtml", viewModel);
        }

        [Route("change-ukprn")]
        [HttpGet]
        public IActionResult ChangeUkprn(Guid applicationId)
        {
            var model = new ChangeUkprnViewModel { ApplicationId = applicationId };

            return View("~/Views/Roatp/ChangeUkprn.cshtml", model);
        }

        [HttpPost]
        [Route("change-ukprn")]
        public IActionResult ChangeUkprn(ChangeUkprnViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Roatp/ChangeUkprn.cshtml", model);
            }

            if (!model.Confirmed.Value)
            {
                return RedirectToAction("TaskList", new {model.ApplicationId});
            }

            return RedirectToAction("EnterNewUkprn", new {model.ApplicationId});
        }

        [Route("change-ukprn/enter-new-ukprn")]
        [HttpGet]
        public async Task<IActionResult> EnterNewUkprn(Guid applicationId)
        {
            var applicationDetails = await _apiClient.GetOrganisationByUserId(User.GetUserId());
            var model = new EnterNewUkprnViewModel{CurrentUkprn = applicationDetails.OrganisationDetails.UKRLPDetails.UKPRN};
            PopulateGetHelpWithQuestion(model, "UKPRN");
            return View("~/Views/Roatp/EnterNewUkprn.cshtml", model);
        }

        [HttpPost]
        [Route("change-ukprn/enter-new-ukprn")]
        public async Task<IActionResult> EnterNewUkprn(EnterNewUkprnViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Roatp/EnterNewUkprn.cshtml", model);
            }

            UkprnValidator.IsValidUkprn(model.Ukprn, out var ukprn);
            var ukrlpLookupResults = await _ukrlpApiClient.GetTrainingProviderByUkprn(ukprn);

            if (ukrlpLookupResults?.Results is null || !ukrlpLookupResults.Success)
            {
                return RedirectToAction("UkrlpNotAvailable", "RoatpShutterPages");
            }

            if (ukrlpLookupResults.Results.Any())
            {
                var applicationDetails = new ApplicationDetails
                {
                    UKPRN = ukprn,
                    UkrlpLookupDetails = ukrlpLookupResults.Results.FirstOrDefault()
                };

                var userId  = User.GetUserId().ToString();
                await _applicationApiClient.UpdateApplicationStatus(model.ApplicationId, ApplicationStatus.Cancelled, userId);
                _sessionService.Remove(ApplicationDetailsKey);

                _sessionService.Set(ApplicationDetailsKey, applicationDetails);

                return RedirectToAction("ConfirmOrganisation", "RoatpApplicationPreamble");
            }
            else
            {
                var applicationDetails = new ApplicationDetails
                {
                    UKPRN = ukprn
                };

                _sessionService.Set(ApplicationDetailsKey, applicationDetails);
                return RedirectToAction("UkprnNotFound", "RoatpShutterPages");
            }
        }


        //TODO: Move this method to the API rather than pulling all of the application back over the wire then checking.
        private async Task<bool> CanUpdateApplication(Guid applicationId, int? sequenceId = null, int? sectionId = null, string pageId = null)
        {
            bool canUpdate = false;

            var signInId = await _userService.GetSignInId();
            var application = await _apiClient.GetApplicationByUserId(applicationId, signInId);

            var validApplicationStatuses = new string[] { ApplicationStatus.InProgress, ApplicationStatus.FeedbackAdded };

            if (application != null && application.ApplyData != null && validApplicationStatuses.Contains(application.ApplicationStatus))
            {
                if (sequenceId.HasValue)
                {
                    var sequence = application.ApplyData.Sequences?.FirstOrDefault(seq => /*!seq.NotRequired && seq.IsActive &&*/ seq.SequenceNo == sequenceId);

                    if (sequence != null)
                    {
                        if (sectionId.HasValue)
                        {
                            var section = sequence.Sections.FirstOrDefault(sec => /*!sec.NotRequired && */ sec.SectionNo == sectionId);

                            if (section != null)
                            {
                                if (!string.IsNullOrWhiteSpace(pageId))
                                {
                                    canUpdate = await _qnaApiClient.CanUpdatePage(applicationId, section.SectionId, pageId);
                                }
                                else
                                {
                                    // No need to check the page
                                    canUpdate = true;
                                }
                            }
                        }
                        else
                        {
                            // No need to check the section
                            canUpdate = true;
                        }
                    }
                }
                else
                {
                    // No need to check the sequence
                    canUpdate = true;
                }
            }

            return canUpdate;
        }

        private async Task<Page> GetDataFedOptions(Guid applicationId, Page page)
        {
            if (page != null)
            {
                foreach (var question in page.Questions)
                {
                    if (question.Input.Type.StartsWith("DataFed_"))
                    {
                        var questionOptions = await _apiClient.GetQuestionDataFedOptions(question.Input.DataEndpoint);
                        // Get data from API using question.Input.DataEndpoint
                        question.Input.Options = questionOptions;
                        question.Input.Type = question.Input.Type.Replace("DataFed_", "");
                    }
                    if (QuestionType.TabularData.Equals(question.Input.Type, StringComparison.InvariantCultureIgnoreCase))
                    {
                        var answer = await _qnaApiClient.GetAnswerByTag(applicationId, question.QuestionTag, question.QuestionId);
                        if (page.PageOfAnswers == null || page.PageOfAnswers.Count < 1)
                        {
                            page.PageOfAnswers = new List<PageOfAnswers>();

                            var pageOfAnswers = new PageOfAnswers { Id = Guid.NewGuid(), Answers = new List<Answer>() };
                            page.PageOfAnswers.Add(pageOfAnswers);
                        }
                        var autoFilledAnswer = new Answer { QuestionId = question.QuestionId, Value = answer.Value };
                        var answersCollection = page.PageOfAnswers.First().Answers;
                        if (answersCollection.FirstOrDefault(x => x.QuestionId == question.QuestionId) == null)
                        {
                            answersCollection.Add(autoFilledAnswer);
                        }
                    }
                }

            }

            return page;
        }

        private async Task<IActionResult> SaveAnswersGiven(Guid applicationId, Guid sectionId, int sectionNo, int sequenceNo, string pageId, Page page, string redirectAction, string formAction)
        {
            var isFileUploadPage = page.Questions.Any(q => QuestionType.FileUpload.Equals(q.Input.Type, StringComparison.InvariantCultureIgnoreCase));

            var answers = isFileUploadPage ? GetAnswersFromFiles() : GetAnswersFromForm(page);

            ApplyFormattingToAnswers(answers, page);

            if(formAction == "Upload" && page.DisplayType == PageDisplayType.MultipleFileUpload)
            {
                ValidateFileHasBeenSelectedForMultipleFileUpload(page, answers);
            }

            RunCustomValidations(page, answers);
            if (!ModelState.IsValid)
            {
                //Can this be made common? What about DataFedOptions?
                page = await _qnaApiClient.GetPage(applicationId, sectionId, pageId);
                this.TempData["InvalidPageOfAnswers"] = JsonConvert.SerializeObject(page.PageOfAnswers);
                return RedirectToAction("Page", new { applicationId, sequenceId = sequenceNo, sectionId = sectionNo, pageId, redirectAction });
            }

            //todo: Should we convert this to a custom validation?
            var checkBoxListQuestions = GetCheckBoxListQuestionsFromPage(page);
            if (checkBoxListQuestions.Any())
            {
                var checkBoxListQuestionId = CheckBoxListHasInvalidSelections(checkBoxListQuestions, answers);
                if (!string.IsNullOrWhiteSpace(checkBoxListQuestionId))
                {
                    ModelState.AddModelError(checkBoxListQuestionId, InvalidCheckBoxListSelectionErrorMessage);

                    //Can this be made common? What about DataFedOptions?
                    page = await _qnaApiClient.GetPage(applicationId, sectionId, pageId);
                    this.TempData["InvalidPageOfAnswers"] = JsonConvert.SerializeObject(page.PageOfAnswers);
                    return RedirectToAction("Page", new { applicationId, sequenceId = sequenceNo, sectionId = sectionNo, pageId, redirectAction });
                }
            }

            bool validationPassed;
            List<KeyValuePair<string, string>> validationErrors;
            string nextAction;
            string nextActionId;

            if (isFileUploadPage)
            {
                var uploadFileResult = await _qnaApiClient.Upload(applicationId, sectionId, pageId, HttpContext.Request.Form.Files);
                validationPassed = uploadFileResult.ValidationPassed;
                validationErrors = uploadFileResult.ValidationErrors;
                nextAction = uploadFileResult.NextAction;
                nextActionId = uploadFileResult.NextActionId;
            }
            else
            {
                var updatePageResult = await _qnaApiClient.UpdatePageAnswers(applicationId, sectionId, pageId, answers);
                validationPassed = updatePageResult.ValidationPassed;
                validationErrors = updatePageResult.ValidationErrors;
                nextAction = updatePageResult.NextAction;
                nextActionId = updatePageResult.NextActionId;
            }

            if (validationPassed)
            {
                // Any answer that is saved will affect the NotRequiredOverrides
                await _roatpTaskListWorkflowService.RefreshNotRequiredOverrides(applicationId);

                if(formAction == "Upload" && page.DisplayType == PageDisplayType.MultipleFileUpload)
                {
                    return RedirectToAction("Page", new
                    {
                        applicationId,
                        sequenceId = sequenceNo,
                        sectionId = sectionNo,
                        pageId = pageId,
                        redirectAction
                    });
                }

                if (redirectAction == "Feedback")
                {
                    return RedirectToAction("Feedback", new { applicationId });
                }

                if (NextAction.ReturnToSection.Equals(nextAction, StringComparison.InvariantCultureIgnoreCase) && (page.DisplayType == SectionDisplayType.PagesWithSections || page.DisplayType == "OtherPagesInPagesWithSections"))
                {
                    return await Section(applicationId, sequenceNo, sectionNo);
                }

                if (string.IsNullOrEmpty(nextActionId) || !NextAction.NextPage.Equals(nextAction, StringComparison.InvariantCultureIgnoreCase))
                {
                    return RedirectToAction("TaskList", "RoatpApplication", new { applicationId }, $"Sequence_{sequenceNo}");
                }

                return RedirectToAction("Page", new
                {
                    applicationId,
                    sequenceId = sequenceNo,
                    sectionId = sectionNo,
                    pageId = nextActionId,
                    redirectAction
                });
            }

            if (validationErrors != null)
            {
                foreach (var error in validationErrors)
                {
                    ModelState.AddModelError(error.Key, error.Value);
                }
            }

            page = await _qnaApiClient.GetPage(applicationId, sectionId, pageId);

            if (!isFileUploadPage)
            {
                page = StoreEnteredAnswers(answers, page);
            }

            var invalidPage = await GetDataFedOptions(applicationId, page);
            this.TempData["InvalidPageOfAnswers"] = JsonConvert.SerializeObject(invalidPage.PageOfAnswers);

            return RedirectToAction("Page", new { applicationId, sequenceId = sequenceNo, sectionId = sectionNo, pageId, redirectAction });
        }

        private static Page StoreEnteredAnswers(List<Answer> answers, Page page)
        {
            if (answers != null && answers.Any())
            {
                if (page.PageOfAnswers is null || !page.PageOfAnswers.Any())
                {
                    page.PageOfAnswers = new List<PageOfAnswers> { new PageOfAnswers { Answers = new List<Answer>() } };
                }

                page.PageOfAnswers.Add(new PageOfAnswers { Answers = answers });
            }

            return page;
        }

        private static void ApplyFormattingToAnswers(List<Answer> answers, Page page)
        {
            foreach (var answer in answers)
            {
                var question = page.Questions.FirstOrDefault(x => x.QuestionId == answer.QuestionId);
                if (question != null && question.Input != null
                                     && !string.IsNullOrWhiteSpace(question.Input.InputClasses)
                                     && question.Input.InputClasses.Contains(InputClassUpperCase))
                {
                    answer.Value = answer.Value.ToUpper();
                }
            }
        }

        private static IEnumerable<Question> GetCheckBoxListQuestionsFromPage(Page page)
        {
            return page.Questions.Where(q => QuestionType.CheckboxList.Equals(q.Input.Type, StringComparison.InvariantCultureIgnoreCase)
                                          || QuestionType.ComplexCheckboxList.Equals(q.Input.Type, StringComparison.InvariantCultureIgnoreCase));
        }

        private static string CheckBoxListHasInvalidSelections(IEnumerable<Question> checkBoxListQuestions, List<Answer> answers)
        {
            foreach (var question in checkBoxListQuestions)
            {
                var checkBoxListAnswer = answers.FirstOrDefault(x => x.QuestionId == question.QuestionId);
                if (checkBoxListAnswer != null)
                {
                    if (checkBoxListAnswer.Value.Contains(NotApplicableAnswerText)
                        && checkBoxListAnswer.Value != NotApplicableAnswerText)
                    {
                        return checkBoxListAnswer.QuestionId;
                    }
                }
            }

            return null;
        }

        // TODO This needs isolating into a testable service
        private List<Answer> GetAnswersFromForm(Page page)
        {
            List<Answer> answers = new List<Answer>();

            // These are special in that they drive other things and thus should not be deemed as an answer
            var excludedInputs = new List<string> { "formAction", "postcodeSearch", "checkAll", "ApplicationId", "RedirectAction" };

            // Add answers from the Form post
            foreach (var keyValuePair in HttpContext.Request.Form.Where(f => !f.Key.StartsWith("__") && !excludedInputs.Contains(f.Key, StringComparer.InvariantCultureIgnoreCase)))
            {
                answers.Add(new Answer() { QuestionId = keyValuePair.Key, Value = keyValuePair.Value });
            }

            // Check if any Page Question is missing and add the default answer
            foreach (var questionId in page.Questions.Select(q => q.QuestionId))
            {
                if (!answers.Any(a => a.QuestionId == questionId))
                {
                    // Add default answer if it's missing
                    answers.Add(new Answer { QuestionId = questionId, Value = string.Empty });
                }
            }

            #region FurtherQuestion_Processing
            // Get all questions that have FurtherQuestions in a ComplexRadio
            var questionsWithFutherQuestions = GetCheckBoxListQuestionsFromPage(page).Where(x => x.Input.Options != null && x.Input.Options.Any(o => o.FurtherQuestions != null && o.FurtherQuestions.Any()));

            foreach (var question in questionsWithFutherQuestions)
            {
                if (QuestionType.ComplexRadio.Equals(question.Input.Type, StringComparison.InvariantCultureIgnoreCase))
                {
                    var answerForQuestion = answers.FirstOrDefault(a => a.QuestionId == question.QuestionId);


                    // Remove FurtherQuestion answers to all other Options as they were not selected and thus should not be stored
                    foreach (var furtherQuestion in question.Input.Options
                        .Where(opt => opt.Value != answerForQuestion?.Value && opt.FurtherQuestions != null)
                        .SelectMany(opt => opt.FurtherQuestions))
                    {
                        foreach (var answer in answers.Where(a => a.QuestionId == furtherQuestion.QuestionId))
                        {
                            answer.Value = string.Empty;
                        }
                    }
                }
                else if (QuestionType.ComplexCheckboxList.Equals(question.Input.Type, StringComparison.InvariantCultureIgnoreCase))
                {
                    var answerForQuestion = answers.FirstOrDefault(a => a.QuestionId == question.QuestionId);

                    if (answerForQuestion?.Value == null) continue;

                    // This different funcationality required as the checkbox may and will return a comma delimited list of responses
                    var splitAnswers = answerForQuestion.Value.Split(",", StringSplitOptions.RemoveEmptyEntries);
                    // Remove FurtherQuestion answers to all other Options as they were not selected and thus should not be stored
                    foreach (var option in question.Input.Options
                        .Where(opt => opt.FurtherQuestions != null))
                    {
                        foreach (var furtherQuestion in option.FurtherQuestions)
                            if (!splitAnswers.Contains(option.Value))
                            {
                                foreach (var answer in answers.Where(a => a.QuestionId == furtherQuestion.QuestionId))
                                {
                                    answer.Value = string.Empty;
                                }
                            }
                    }
                }
            }
            #endregion FurtherQuestion_Processing

            // Address inputs require special processing
            if (page.Questions.Any(x => QuestionType.Address.Equals(x.Input.Type, StringComparison.InvariantCultureIgnoreCase)))
            {
                answers = ProcessPageVmQuestionsForAddress(page, answers);
            }

            return answers;
        }

        private List<Answer> GetAnswersFromFiles()
        {
            List<Answer> answers = new List<Answer>();

            // Add answers from the Files sent within the Form post
            if (HttpContext.Request.Form.Files != null)
            {
                foreach (var file in HttpContext.Request.Form.Files)
                {
                    answers.Add(new Answer() { QuestionId = file.Name, Value = file.FileName });
                }

            }

            return answers;
        }

        private static List<Answer> ProcessPageVmQuestionsForAddress(Page page, List<Answer> answers)
        {

            if (page.Questions.Any(x => QuestionType.Address.Equals(x.Input.Type, StringComparison.InvariantCultureIgnoreCase)))
            {
                Dictionary<string, JObject> answerValueDictionary = new Dictionary<string, JObject>();

                // Address input fields will contain _Key_
                foreach (var formVariable in answers.Where(x => x.QuestionId.Contains("_Key_")))
                {
                    var answerKey = formVariable.QuestionId.Split("_Key_");
                    if (!answerValueDictionary.ContainsKey(answerKey[0]))
                    {
                        answerValueDictionary.Add(answerKey[0], new JObject());
                    }

                    answerValueDictionary[answerKey[0]].Add(
                        answerKey.Count() == 1 ? string.Empty : answerKey[1],
                        formVariable.Value.ToString());
                }

                // Remove anything that contains _Key_ as it has now been processed correctly
                answers = answers.Where(x => !x.QuestionId.Contains("_Key_")).ToList();

                foreach (var answerValue in answerValueDictionary)
                {
                    if (answerValue.Value.Count > 1)
                    {
                        var answer = answers.FirstOrDefault(a => a.QuestionId == answerValue.Key);

                        if (answer is null)
                        {
                            answers.Add(new Answer() { QuestionId = answerValue.Key, Value = answerValue.Value.ToString() });
                        }
                        else
                        {
                            answer.Value = answerValue.Value.ToString();
                        }
                    }
                }

            }

            return answers;
        }

        [HttpPost]
        public async Task<IActionResult> Submit(Guid applicationId)
        {
            var canUpdate = await CanUpdateApplication(applicationId);
            if (!canUpdate)
            {
                return RedirectToAction("TaskList", new { applicationId });
            }

            var activeSequence = await _apiClient.GetSequence(applicationId, User.GetUserId());
            var errors = ValidateSubmit(activeSequence);
            if (errors.Any())
            {
                var sequenceVm = new SequenceViewModel(activeSequence, applicationId, errors);

                if (activeSequence.Status == ApplicationSequenceStatus.FeedbackAdded)
                {
                    return View("~/Views/Application/Feedback.cshtml", sequenceVm);
                }
                else
                {
                    return View("~/Views/Application/Sequence.cshtml", sequenceVm);
                }
            }

            var providerRoute = await _qnaApiClient.GetAnswerByTag(applicationId, RoatpWorkflowQuestionTags.ProviderRoute);

            var submitApplicationRequest = new Application.Apply.Submit.SubmitApplicationRequest
            {
                ApplicationId = applicationId,
                ProviderRoute = Convert.ToInt32(providerRoute.Value),
                SubmittingContactId = User.GetUserId()
            };

            if (await _apiClient.SubmitApplication(submitApplicationRequest))
            {
                return RedirectToAction("Submitted", new { applicationId });
            }
            else
            {
                // unable to submit
                return RedirectToAction("NotSubmitted", new { applicationId });
            }
        }

        private List<ValidationErrorDetail> ValidateSubmit(ApplicationSequence sequence)
        {
            var validationErrors = new List<ValidationErrorDetail>();

            if (sequence?.Sections is null)
            {
                var validationError = new ValidationErrorDetail(string.Empty, $"Cannot submit empty sequence");
                validationErrors.Add(validationError);
            }
            else if (sequence.Sections.Where(sec => sec.PagesComplete != sec.PagesActive).Any())
            {
                foreach (var sectionQuestionsNotYetCompleted in sequence.Sections.Where(sec => sec.PagesComplete != sec.PagesActive))
                {
                    var validationError = new ValidationErrorDetail(sectionQuestionsNotYetCompleted.Id.ToString(), $"You need to complete the '{sectionQuestionsNotYetCompleted.LinkTitle}' section");
                    validationErrors.Add(validationError);
                }
            }
            else if (sequence.Sections.Where(sec => sec.QnAData.RequestedFeedbackAnswered is false || sec.QnAData.Pages.Any(p => !p.AllFeedbackIsCompleted)).Any())
            {
                foreach (var sectionFeedbackNotYetCompleted in sequence.Sections.Where(sec => sec.QnAData.RequestedFeedbackAnswered is false || sec.QnAData.Pages.Any(p => !p.AllFeedbackIsCompleted)))
                {
                    var validationError = new ValidationErrorDetail(sectionFeedbackNotYetCompleted.Id.ToString(), $"You need to complete the '{sectionFeedbackNotYetCompleted.LinkTitle}' section");
                    validationErrors.Add(validationError);
                }
            }

            return validationErrors;
        }

        [HttpGet]
        public async Task<IActionResult> DeleteAnswer(Guid applicationId, int sequenceId, int sectionId, string pageId, Guid answerId, string redirectAction)
        {
            await _apiClient.DeleteAnswer(applicationId, sequenceId, sectionId, pageId, answerId, User.GetUserId());

            return RedirectToAction("Page", new { applicationId, sequenceId, sectionId, pageId, redirectAction });
        }

        [HttpGet]
        public async Task<IActionResult> Feedback(Guid applicationId)
        {
            var sequence = await _apiClient.GetSequence(applicationId, User.GetUserId());
            var sequenceVm = new SequenceViewModel(sequence, applicationId, null);
            return View("~/Views/Application/Feedback.cshtml", sequenceVm);
        }

        public async Task<IActionResult> Submitted(Guid applicationId)
        {
            var application = await _apiClient.GetApplication(applicationId);
            return View("~/Views/Application/Submitted.cshtml", new SubmittedViewModel
            {
                ReferenceNumber = application?.ApplyData?.ApplyDetails?.ReferenceNumber
            });
        }

        [HttpGet]
        public async Task<IActionResult> NotSubmitted(Guid applicationId)
        {
            var application = await _apiClient.GetApplication(applicationId);
            return View("~/Views/Application/NotSubmitted.cshtml", new SubmittedViewModel
            {
                ReferenceNumber = application?.ApplyData?.ApplyDetails?.ReferenceNumber
            });
        }

        private async Task UpdateQuestionsWithinQnA(Guid applicationId, List<PreambleAnswer> questions)
        {
            if (questions != null)
            {
                // Many answers for a particular page could have been added, so we need to group them up
                var pageAnswersDictionary = questions.GroupBy(q => q.PageId).ToDictionary(g => g.Key, g => g.ToList());

                // Now we can process each page
                foreach (var entry in pageAnswersDictionary)
                {
                    var answers = entry.Value; // Note: All answers have been grouped up

                    // Make sure we have answers within the page to update
                    if (answers != null && answers.Any())
                    {
                        var pageId = answers[0].PageId;

                        await _qnaApiClient.UpdatePageAnswers(applicationId, answers[0].SequenceId, answers[0].SectionId, pageId, answers.ToList<Answer>());
                    }
                }
            }
        }

        [HttpGet]
        [Route("submit-application")]
        public async Task<IActionResult> SubmitApplication(Guid applicationId)
        {
            var model = new SubmitApplicationViewModel { ApplicationId = applicationId };

            var organisationName = await _qnaApiClient.GetAnswerByTag(applicationId, RoatpWorkflowQuestionTags.UkrlpLegalName);
            model.OrganisationName = organisationName.Value;
            model.EmailAddress = User.GetEmail();

            return View("~/Views/Roatp/SubmitApplication.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmSubmitApplication(SubmitApplicationViewModel model)
        {
            var canUpdate = await CanUpdateApplication(model.ApplicationId);
            if (!canUpdate)
            {
                return RedirectToAction("TaskList", new { applicationId = model.ApplicationId});
            }

            if (!ModelState.IsValid)
            {
                model.ErrorMessages = new List<ValidationErrorDetail>();

                foreach (var modelStateKey in ViewData.ModelState.Keys)
                {
                    var value = ViewData.ModelState[modelStateKey];
                    foreach (var error in value.Errors)
                    {
                        model.ErrorMessages.Add(new ValidationErrorDetail
                        {
                            Field = modelStateKey,
                            ErrorMessage = error.ErrorMessage
                        });
                    }
                }

                return View("~/Views/Roatp/SubmitApplication.cshtml", model);
            }

            // TODO: Validate all sections are completed (i.e all questions answered)
            // FUTURE WORK: Validate all sections have had requested feedback answered

            var providerRouteTask = _qnaApiClient.GetAnswerByTag(model.ApplicationId, RoatpWorkflowQuestionTags.ProviderRoute);
            var applicationTask = _apiClient.GetApplication(model.ApplicationId);
            var allSectionsTask = _qnaApiClient.GetSections(model.ApplicationId);
            var roatpSequencesTask = _apiClient.GetRoatpSequences();
            var organisationVerificationStatusTask = _organisationVerificationService.GetOrganisationVerificationStatus(model.ApplicationId);
            var applicationRoutesTask = _roatpApiClient.GetApplicationRoutes();
            var applicationDataTask = _qnaApiClient.GetApplicationData(model.ApplicationId);
            var addressTask = _qnaApiClient.GetPageBySectionNo(model.ApplicationId, RoatpWorkflowSequenceIds.PlanningApprenticeshipTraining, RoatpWorkflowSectionIds.PlanningApprenticeshipTraining.WhereWillYourApprenticesBeTrained, RoatpWorkflowPageIds.PlanningApprenticeshipTraining.AddressWhereApprenticesWillBeTrained);

            await Task.WhenAll(providerRouteTask, applicationTask, allSectionsTask, roatpSequencesTask, organisationVerificationStatusTask, applicationRoutesTask, addressTask);

            var providerRoute = await providerRouteTask;
            var application = await applicationTask;
            var allSections = await allSectionsTask;
            var roatpSequences = await roatpSequencesTask;
            var organisationVerificationStatus = await organisationVerificationStatusTask;
            var applicationRoutes = await applicationRoutesTask;
            var applicationData = (await applicationDataTask) as JObject;
            var address = await addressTask;

            await _roatpTaskListWorkflowService.RefreshNotRequiredOverrides(model.ApplicationId);
            var sequences = await _roatpTaskListWorkflowService.GetApplicationSequences(model.ApplicationId);

            foreach (var sequence in application.ApplyData.Sequences)
            {
                var applicationSequence = await _qnaApiClient.GetSequenceBySequenceNo(model.ApplicationId, sequence.SequenceNo);

                var sections = allSections.Where(x => x.SequenceId == sequence.SequenceNo).ToList();

                applicationSequence.Sections = sections;
                foreach (var section in sections)
                {
                    var applySection = sequence.Sections.FirstOrDefault(x => x.SectionNo == section.SectionId);
                    if (applySection != null)
                    {
                        applySection.NotRequired = SectionNotRequired(model.ApplicationId, applicationSequence, section.SectionId,
                                                                            roatpSequences, organisationVerificationStatus);
                    }
                }

                applicationSequence.Sections = sections.ToList();
                sequence.NotRequired = SequenceNotRequired(model.ApplicationId, applicationSequence, sequences,
                                                           organisationVerificationStatus);
            }

            var selectedProviderRoute = applicationRoutes.FirstOrDefault(p => p.Id.ToString() == providerRoute.Value);

            var submitApplicationRequest = new Application.Apply.Submit.SubmitApplicationRequest
            {
                ApplicationId = model.ApplicationId,
                ProviderRoute = Convert.ToInt32(providerRoute.Value),
                ProviderRouteName = selectedProviderRoute?.RouteName,
                SubmittingContactId = User.GetUserId(),
                ApplyData = application.ApplyData,
                OrganisationType = ExtractOrganisationType(applicationData),
                FinancialData = ExtractFinancialData(_logger, model.ApplicationId, applicationData),
                Address = ExtractAddress(address)
            };

            var submitResult = await _apiClient.SubmitApplication(submitApplicationRequest);

            if (submitResult)
            {
                var userDetails = await _usersApiClient.GetUserBySignInId(User.GetSignInId());
                var applicationSubmitConfirmation = new ApplicationSubmitConfirmation
                {
                    ApplicantFullName = $"{userDetails.GivenNames} {userDetails.FamilyName}",
                    ApplicationRouteId = providerRoute.Value,
                    EmailAddress = User.GetEmail()
                };

                await _submitApplicationEmailService.SendSubmitConfirmationEmail(applicationSubmitConfirmation);
                return RedirectToAction("ApplicationSubmitted", new { model.ApplicationId });
            }
            else
            {
                return RedirectToAction("TaskList", new { model.ApplicationId });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ApplicationSubmitted(Guid applicationId)
        {
            var application = await _apiClient.GetApplication(applicationId);
            var applicationData = application.ApplyData.ApplyDetails;

            var model = new ApplicationSummaryViewModel
            {
                ApplicationId = application.ApplicationId,
                UKPRN = applicationData.UKPRN,
                OrganisationName = applicationData.OrganisationName,
                TradingName = applicationData.TradingName,
                ApplicationRouteId = applicationData.ProviderRoute.ToString(),
                ApplicationReference = applicationData.ReferenceNumber,
                EmailAddress = User.GetEmail()
            };

            return View("~/Views/Roatp/ApplicationSubmitted.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> ApplicationWithdrawn(Guid applicationId)
        {
            var application = await _apiClient.GetApplication(applicationId);
            var applicationData = application.ApplyData.ApplyDetails;

            var model = new ApplicationSummaryViewModel
            {
                ApplicationId = application.ApplicationId,
                UKPRN = applicationData.UKPRN,
                OrganisationName = applicationData.OrganisationName,
                TradingName = applicationData.TradingName,
                ApplicationRouteId = applicationData.ProviderRoute.ToString(),
                ApplicationReference = applicationData.ReferenceNumber,
                EmailAddress = User.GetEmail(),
                SubmittedDate = applicationData.ApplicationSubmittedOn
            };

            return View("~/Views/Roatp/ApplicationWithdrawn.cshtml", model);
        }


        [HttpGet]
        public async Task<IActionResult> ApplicationRemoved(Guid applicationId)
        {
            var application = await _apiClient.GetApplication(applicationId);
            var applicationData = application.ApplyData.ApplyDetails;

            var model = new ApplicationSummaryViewModel
            {
                ApplicationId = application.ApplicationId,
                UKPRN = applicationData.UKPRN,
                OrganisationName = applicationData.OrganisationName,
                TradingName = applicationData.TradingName,
                ApplicationRouteId = applicationData.ProviderRoute.ToString(),
                ApplicationReference = applicationData.ReferenceNumber,
                EmailAddress = User.GetEmail(),
                SubmittedDate = applicationData.ApplicationSubmittedOn,
                ExternalComments = application.ExternalComments,
                HideEmailAddress = true
            };

            return View("~/Views/Roatp/ApplicationWithdrawnESFA.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> ApplicationRejected(Guid applicationId)
        {
            var application = await _apiClient.GetApplication(applicationId);
            var applicationData = application.ApplyData.ApplyDetails;

            var model = new ApplicationSummaryViewModel
            {
                ApplicationId = application.ApplicationId,
                UKPRN = applicationData.UKPRN,
                OrganisationName = applicationData.OrganisationName,
                TradingName = applicationData.TradingName,
                ApplicationRouteId = applicationData.ProviderRoute.ToString(),
                ApplicationReference = applicationData.ReferenceNumber,
                EmailAddress = User.GetEmail(),
                SubmittedDate = applicationData.ApplicationSubmittedOn,
                ExternalComments = application.ApplyData.GatewayReviewDetails.ExternalComments
            };

            return View("~/Views/Roatp/ApplicationRejected.cshtml", model);
        }

        private async Task SavePreambleInformation(Guid applicationId, ApplicationDetails applicationDetails)
        {
            var preambleQuestions = RoatpPreambleQuestionBuilder.CreatePreambleQuestions(applicationDetails);
            await UpdateQuestionsWithinQnA(applicationId, preambleQuestions);
        }

        private async Task SaveCompaniesHouseInformation(Guid applicationId, ApplicationDetails applicationDetails)
        {
            var directorsPscsQuestions = RoatpPreambleQuestionBuilder.CreateCompaniesHouseWhosInControlQuestions(applicationDetails);
            await UpdateQuestionsWithinQnA(applicationId, directorsPscsQuestions);
        }

        private async Task SaveCharityCommissionInformation(Guid applicationId, ApplicationDetails applicationDetails)
        {
            var trusteesQuestions = RoatpPreambleQuestionBuilder.CreateCharityCommissionWhosInControlQuestions(applicationDetails);
            await UpdateQuestionsWithinQnA(applicationId, trusteesQuestions);
        }

        private async Task<PageViewModel> TokeniseViewModelProperties(PageViewModel viewModel)
        {
            viewModel.Title =
                await _questionPropertyTokeniser.GetTokenisedValue(viewModel.ApplicationId, viewModel.Title);

            viewModel.BodyText =
                await _questionPropertyTokeniser.GetTokenisedValue(viewModel.ApplicationId, viewModel.BodyText);

            foreach (var questionModel in viewModel.Questions)
            {
                questionModel.Hint = await _questionPropertyTokeniser.GetTokenisedValue(viewModel.ApplicationId, questionModel.Hint);
                questionModel.Label = await _questionPropertyTokeniser.GetTokenisedValue(viewModel.ApplicationId, questionModel.Label);
                questionModel.QuestionBodyText = await _questionPropertyTokeniser.GetTokenisedValue(viewModel.ApplicationId, questionModel.QuestionBodyText);
                questionModel.ShortLabel = await _questionPropertyTokeniser.GetTokenisedValue(viewModel.ApplicationId, questionModel.ShortLabel);
            }

            return viewModel;
        }

        private void ValidateFileHasBeenSelectedForMultipleFileUpload(Page page, List<Answer> answers)
        {
            if (page.DisplayType == PageDisplayType.MultipleFileUpload && !answers.Any())
            {
                var fileUploadQuestionIds = page.Questions.Where(p => p.Input.Type == QuestionType.FileUpload).Select(q => q.QuestionId);

                var existingAnswers = page.PageOfAnswers?.SelectMany(poa => poa.Answers) ?? new List<Answer>();
                var existingAnswersQuestionIds = existingAnswers.Select(x => x.QuestionId);

                var questionIdForError = fileUploadQuestionIds.FirstOrDefault(x => !existingAnswersQuestionIds.Contains(x));
                ModelState.AddModelError(questionIdForError, "The selected file is empty");
            }
        }

        private void RunCustomValidations(Page page, List<Answer> answers)
        {
            var pagecustomValidators = _customValidatorFactory.GetCustomValidationsForPage(page, HttpContext.Request.Form.Files);
            
            foreach (var pageCustomValidation in pagecustomValidators)
            {
                var result = pageCustomValidation.Validate();

                if (!result.IsValid)
                {
                    ModelState.AddModelError(result.QuestionId, result.ErrorMessage);
                }
            }

            foreach (var answer in answers)
            {
                var customValidations = _customValidatorFactory.GetCustomValidationsForQuestion(page.PageId, answer.QuestionId, HttpContext.Request.Form.Files);

                foreach (var customValidation in customValidations)
                {
                    var result = customValidation.Validate();

                    if (!result.IsValid)
                    {
                        ModelState.AddModelError(result.QuestionId, result.ErrorMessage);
                    }
                }
            }
        }

        private async Task<List<TabularData>> GetPeopleInControlDetails(Guid applicationId, int sequenceId, int sectionId)
        {
            var result = new List<TabularData>();

            if (sequenceId == RoatpWorkflowSequenceIds.CriminalComplianceChecks && sectionId == RoatpWorkflowSectionIds.CriminalComplianceChecks.CheckOnWhosInControl)
            {
                var personData = await _tabularDataRepository.GetTabularDataAnswer(applicationId, RoatpWorkflowQuestionTags.AddPeopleInControl);
                if (personData != null && personData.DataRows != null && personData.DataRows.Count > 0)
                {
                    result.Add(personData);
                }

                var directorsData = await _tabularDataRepository.GetTabularDataAnswer(applicationId, RoatpWorkflowQuestionTags.CompaniesHouseDirectors);
                if (directorsData != null && directorsData.DataRows != null && directorsData.DataRows.Count > 0)
                {
                    result.Add(directorsData);
                }

                var pscsData = await _tabularDataRepository.GetTabularDataAnswer(applicationId, RoatpWorkflowQuestionTags.CompaniesHousePscs);
                if (pscsData != null && pscsData.DataRows != null && pscsData.DataRows.Count > 0)
                {
                    result.Add(pscsData);
                }

                var trusteesData = await _tabularDataRepository.GetTabularDataAnswer(applicationId, RoatpWorkflowQuestionTags.CharityCommissionTrustees);
                if (trusteesData != null && trusteesData.DataRows != null && trusteesData.DataRows.Count > 0)
                {
                    result.Add(trusteesData);
                }
            }

            return result;
        }

        private bool SequenceNotRequired(Guid applicationId, ApplicationSequence sequence, 
                                                IEnumerable<ApplicationSequence> applicationSequences,
                                                OrganisationVerificationStatus organisationVerificationStatus)
        {
            var sectionCount = sequence.Sections.Count;
            var notRequiredCount = 0;

            foreach (var section in sequence.Sections)
            {
                if (_roatpTaskListWorkflowService.SectionStatus(applicationId, sequence.SequenceId, section.SectionId, applicationSequences, organisationVerificationStatus) == TaskListSectionStatus.NotRequired) 
                {
                    notRequiredCount++;
                }
            }

            return (sectionCount == notRequiredCount);
        }


        private bool SectionNotRequired(Guid applicationId, ApplicationSequence sequence,
                                                    int sectionId, IEnumerable<RoatpSequences> roatpSequences, OrganisationVerificationStatus organisationVerificationStatus)
        {
            var sequences = new List<ApplicationSequence>
            {
                sequence
            };

            if (_roatpTaskListWorkflowService.SectionStatus(applicationId, sequence.SequenceId, sectionId, sequences, organisationVerificationStatus) == TaskListSectionStatus.NotRequired)
            {
                return true;
            }

            var currentSequence = roatpSequences.FirstOrDefault(x => x.Id == sequence.SequenceId);
            if (currentSequence?.ExcludeSections != null && currentSequence.ExcludeSections.Contains(sectionId.ToString()))
            {
                return true;
            }

            return false;
        }

        private static string ExtractOrganisationType(JObject applicationData)
        {
            return applicationData.GetValue("OrganisationEducationInstitute")?.Value<string>()
                   ?? applicationData.GetValue("OrganisationPublicBody")?.Value<string>()
                   ?? applicationData.GetValue("OrganisationTypeMainSupporting")?.Value<string>()
                   ?? applicationData.GetValue("OrganisationTypeEmployer")?.Value<string>();
        }

        private static string ExtractAddress(Page page)
        {
            var address = string.Empty;

            if (page != null && !page.NotRequired && page.PageOfAnswers.Any())
            {
                address = string.Join(", ", page.PageOfAnswers.First().Answers);
            }

            return address;
        }

        private static FinancialData ExtractFinancialData(ILogger<RoatpApplicationController> _logger, Guid applicationId, JObject applicationData)
        {
            try
            {
                var fundingTurnover5pc = applicationData.GetValue(RoatpWorkflowQuestionTags.FundingTurnover5Percent)?.Value<string>();

                if ("Yes".Equals(fundingTurnover5pc, StringComparison.OrdinalIgnoreCase))
                {
                    return new FinancialData
                    {
                        ApplicationId = applicationId,
                        AccountingReferenceDate = AccountingReferenceDate(applicationData),
                        AccountingPeriod = applicationData.GetValue(RoatpWorkflowQuestionTags.AccountingPeriod).Value<byte>()
                    };
                }
                else
                {
                    return new FinancialData
                    {
                        ApplicationId = applicationId,
                        TurnOver = applicationData.GetValue(RoatpWorkflowQuestionTags.Turnover).Value<long>(),
                        Depreciation = applicationData.GetValue(RoatpWorkflowQuestionTags.Depreciation).Value<long>(),
                        ProfitLoss = applicationData.GetValue(RoatpWorkflowQuestionTags.ProfitLoss).Value<long>(),
                        Dividends = applicationData.GetValue(RoatpWorkflowQuestionTags.Dividends).Value<long>(),
                        Assets = applicationData.GetValue(RoatpWorkflowQuestionTags.Assets).Value<long>(),
                        Liabilities = applicationData.GetValue(RoatpWorkflowQuestionTags.Liabilities).Value<long>(),
                        Borrowings = applicationData.GetValue(RoatpWorkflowQuestionTags.Borrowings).Value<long>(),
                        ShareholderFunds = applicationData.GetValue(RoatpWorkflowQuestionTags.ShareholderFunds).Value<long>(),
                        IntangibleAssets = applicationData.GetValue(RoatpWorkflowQuestionTags.IntangibleAssets).Value<long>(),
                        AccountingReferenceDate = AccountingReferenceDate(applicationData),
                        AccountingPeriod = applicationData.GetValue(RoatpWorkflowQuestionTags.AccountingPeriod).Value<byte>(),
                        AverageNumberofFTEEmployees = applicationData.GetValue(RoatpWorkflowQuestionTags.AverageNumberofFTEEmployees).Value<long>()
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unable to extract finanical data for application: {applicationId}");
                return new FinancialData { ApplicationId = applicationId };
            }            
        }

        private static DateTime? AccountingReferenceDate(JObject applicationData)
        {
            var rawDate = applicationData.GetValue(RoatpWorkflowQuestionTags.AccountingReferenceDate).Value<string>();

            return DateTime.TryParseExact(rawDate, new[] { @"d,M,yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date.Date : default(DateTime?);
        }
    }
}
