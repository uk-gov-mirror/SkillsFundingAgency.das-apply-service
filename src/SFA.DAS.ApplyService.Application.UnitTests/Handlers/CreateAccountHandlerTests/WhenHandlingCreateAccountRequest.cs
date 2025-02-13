﻿using System;
using System.Threading;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SFA.DAS.ApplyService.Application.Interfaces;
using SFA.DAS.ApplyService.Application.Users;
using SFA.DAS.ApplyService.Application.Users.CreateAccount;
using SFA.DAS.ApplyService.Domain.Entities;
using Microsoft.Extensions.Logging;
using SFA.DAS.ApplyService.Domain.Interfaces;

namespace SFA.DAS.ApplyService.Application.UnitTests.Handlers.CreateAccountHandlerTests
{
    [TestFixture]
    public class WhenHandlingCreateAccountRequest
    {
        private Mock<IContactRepository> _userRepository;
        private CreateAccountHandler _handler;
        private Mock<IDfeSignInService> _dfeSignInService;

        [SetUp]
        public void Setup()
        {
            _userRepository = new Mock<IContactRepository>();
            _dfeSignInService = new Mock<IDfeSignInService>();
            
            _handler = new CreateAccountHandler(_userRepository.Object, _dfeSignInService.Object, new Mock<ILogger<CreateAccountHandler>>().Object);
        }
        
        [Test]
        public void ThenANewUserIsCreated()
        {
            _handler.Handle(new CreateAccountRequest("name@email.com", "James", "Jones"), new CancellationToken());
            
            _userRepository.Verify(r => r.CreateContact("name@email.com", "James", "Jones", "ASLogin"));
        }

        [Test]
        public void ThenAnExistingUserIsNotCreated()
        {
            _userRepository.Setup(r => r.GetContact("name@email.com")).ReturnsAsync(new Contact());
            _handler.Handle(new CreateAccountRequest("name@email.com", "James", "Jones"), new CancellationToken());
            
            _userRepository.Verify(r => r.CreateContact("name@email.com", "James", "Jones", "ASLogin"), Times.Never);
        }

        [Test]
        public void ThenANewUserIsInvitedToDfeSignin()
        {
            var newUserId = Guid.NewGuid();

            _userRepository.Setup(r => r.CreateContact("name@email.com", "James", "Jones", "ASLogin"))
                .ReturnsAsync(new Contact() {Id = newUserId});
            
            _handler.Handle(new CreateAccountRequest("name@email.com", "James", "Jones"), new CancellationToken());

            _dfeSignInService.Verify(s => s.InviteUser("name@email.com", "James", "Jones", newUserId));
        }

        [Test]
        public void And_AndErrorIsReturnedFromDfeService_ReturnFalse()
        {
            _dfeSignInService.Setup(dfe => dfe.InviteUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>()))
                .ReturnsAsync(new InviteUserResponse() {IsSuccess = false});
            
            var newUserId = Guid.NewGuid();

            _userRepository.Setup(r => r.CreateContact("name@email.com", "James", "Jones", "ASLogin"))
                .ReturnsAsync(new Contact() {Id = newUserId});
            
            var result = _handler.Handle(new CreateAccountRequest("name@email.com", "James", "Jones"), new CancellationToken()).Result;

            result.Should().Be(false);
        }

        [Test]
        public void ThenAnExistingUserIsInvitedToDfeSignin()
        {
            _userRepository.Setup(r => r.GetContact("name@email.com")).ReturnsAsync(new Contact{SigninId = Guid.NewGuid()});
            
            _handler.Handle(new CreateAccountRequest("name@email.com", "James", "Jones"), new CancellationToken());

            _dfeSignInService.Verify(s => s.InviteUser("name@email.com", "James", "Jones", It.IsAny<Guid>()));
        }


        
    }
}