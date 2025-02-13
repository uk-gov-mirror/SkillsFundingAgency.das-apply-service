﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.ApplyService.Configuration;
using SFA.DAS.ApplyService.InternalApi.Services.Files;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SFA.DAS.ApplyService.InternalApi.UnitTests.Services.Files
{
    [TestFixture]
#if (!DEBUG)
        [Ignore("Must be tested on local DEV machine as it uses local Azure storage")]
#endif
    public class FileStorageServiceTests
    {
        private const string _fileStorageConnectionString = "UseDevelopmentStorage=true;DevelopmentStorageProxyUri=http://127.0.0.1";
        private const string _fileStorageContainerName = "filestorage-unit-tests";

        private readonly Guid _applicationId = Guid.NewGuid();
        private readonly int _sequenceNumber = 1;
        private readonly int _sectionNumber = 1;
        private readonly string _pageId = $"{Guid.NewGuid()}";
        private readonly string _fileName = $"{Guid.NewGuid()}.txt";

        private readonly ContainerType _containerType = ContainerType.Assessor;

        private FileStorageService _fileStorageService;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            var config = new ApplyConfig { FileStorage = new FileStorageConfig { StorageConnectionString = _fileStorageConnectionString, AssessorContainerName = _fileStorageContainerName } };
            var _configurationService = new Mock<IConfigurationService>();
            _configurationService.Setup(x => x.GetConfig()).ReturnsAsync(config);

            var _fileEncryptionService = new Mock<IFileEncryptionService>();
            _fileEncryptionService.Setup(x => x.Decrypt(It.IsAny<Stream>())).Returns<Stream>(x => x); // Don't Decrypt stream
            _fileEncryptionService.Setup(x => x.Encrypt(It.IsAny<Stream>())).Returns<Stream>(x => x); // Don't Encrypt stream

            var logger = Mock.Of<ILogger<FileStorageService>>();
            _fileStorageService = new FileStorageService(logger, _configurationService.Object, _fileEncryptionService.Object);
        }

        [SetUp]
        public async Task Setup()
        {
            // Ensure a file is always in storage prior to running test
            var fileThatWillExistInStorage = new FormFileCollection { GenerateFile(_fileName) };
            await _fileStorageService.UploadFiles(_applicationId, _sequenceNumber, _sectionNumber, _pageId, fileThatWillExistInStorage, _containerType, new CancellationToken());
        }

        [TearDown]
        public async Task TearDown()
        {
            // Ensure the file is removed from storage after to running test
            await _fileStorageService.DeleteFile(_applicationId, _sequenceNumber, _sectionNumber, _pageId, _fileName, _containerType, new CancellationToken());
        }

        #region Page File List
        [Test]
        public async Task GetPageFileList_when_page_directory_exists_Then_it_lists_all_files()
        {
            var result = await _fileStorageService.GetPageFileList(_applicationId, _sequenceNumber, _sectionNumber, _pageId, _containerType, new CancellationToken());

            CollectionAssert.IsNotEmpty(result);
            CollectionAssert.Contains(result, _fileName);
        }

        [Test]
        public async Task GetFileList_when_page_directory_does_not_exist_Then_it_returns_empty()
        {
            var nameOfPageThatDoesNotExist = $"{Guid.NewGuid()}";
            var result = await _fileStorageService.GetPageFileList(_applicationId, _sequenceNumber, _sectionNumber, nameOfPageThatDoesNotExist, _containerType, new CancellationToken());

            CollectionAssert.IsEmpty(result);
        }
        #endregion

        #region Upload
        [Test]
        public async Task UploadFiles_when_all_files_successfully_upload_returns_true()
        {
            // Note this will intentionally overwrite the existing file uploaded in SetUp(). Done this way to ensure clean state.
            var filesToUpload = new FormFileCollection { GenerateFile(_fileName) };
            var result = await _fileStorageService.UploadFiles(_applicationId, _sequenceNumber, _sectionNumber, _pageId, filesToUpload, _containerType, new CancellationToken());

            Assert.IsTrue(result);
        }

        [Test]
        public async Task UploadFiles_when_no_files_specified_returns_false()
        {
            var filesToUpload = default(FormFileCollection);
            var result = await _fileStorageService.UploadFiles(_applicationId, _sequenceNumber, _sectionNumber, _pageId, filesToUpload, _containerType, new CancellationToken());

            Assert.False(result);
        }

        [Test]
        public async Task UploadFiles_when_unknown_ContainerType_specificed_returns_false()
        {
            var unknownContainerType = ContainerType.Unknown;
            var filesToUpload = new FormFileCollection { GenerateFile(_fileName) };
            var result = await _fileStorageService.UploadFiles(_applicationId, _sequenceNumber, _sectionNumber, _pageId, filesToUpload, unknownContainerType, new CancellationToken());

            Assert.False(result);
        }
        #endregion

        #region Delete
        [Test]
        public async Task DeleteFile_when_file_exists_Then_it_deletes_the_file()
        {
            var result = await _fileStorageService.DeleteFile(_applicationId, _sequenceNumber, _sectionNumber, _pageId, _fileName, _containerType, new CancellationToken());

            Assert.IsTrue(result);
        }

        [Test]
        public async Task DeleteFile_when_page_does_not_exists_Then_it_returns_false()
        {
            var nameOfPageThatDoesNotExist = $"{Guid.NewGuid()}";
            var result = await _fileStorageService.DeleteFile(_applicationId, _sequenceNumber, _sectionNumber, nameOfPageThatDoesNotExist, _fileName, _containerType, new CancellationToken());

            Assert.IsFalse(result);
        }

        [Test]
        public async Task DeleteFile_when_file_does_not_exists_Then_it_returns_false()
        {
            var nameOfFileThatDoesNotExist = $"{Guid.NewGuid()}.txt";
            var result = await _fileStorageService.DeleteFile(_applicationId, _sequenceNumber, _sectionNumber, _pageId, nameOfFileThatDoesNotExist, _containerType, new CancellationToken());

            Assert.IsFalse(result);
        }
        #endregion

        #region Download
        [Test]
        public async Task DownloadFile_when_file_exists_Then_it_returns_the_file()
        {
            var result = await _fileStorageService.DownloadFile(_applicationId, _sequenceNumber, _sectionNumber, _pageId, _fileName, _containerType, new CancellationToken()); ;

            Assert.IsNotNull(result);
            StringAssert.AreEqualIgnoringCase(_fileName, result.FileName);
        }

        [Test]
        public async Task DownloadFile_when_page_does_not_exists_Then_it_returns_null()
        {
            var nameOfPageThatDoesNotExist = $"{Guid.NewGuid()}";
            var result = await _fileStorageService.DownloadFile(_applicationId, _sequenceNumber, _sectionNumber, nameOfPageThatDoesNotExist, _fileName, _containerType, new CancellationToken());

            Assert.IsNull(result);
        }

        [Test]
        public async Task DownloadFile_when_file_does_not_exists_Then_it_returns_null()
        {
            var nameOfFileThatDoesNotExist = $"{Guid.NewGuid()}.txt";
            var result = await _fileStorageService.DownloadFile(_applicationId, _sequenceNumber, _sectionNumber, _pageId, nameOfFileThatDoesNotExist, _containerType, new CancellationToken());

            Assert.IsNull(result);
        }

        [Test]
        public async Task DownloadPageFiles_when_page_exists_Then_it_returns_the_files_as_a_zip()
        {
            var result = await _fileStorageService.DownloadPageFiles(_applicationId, _sequenceNumber, _sectionNumber, _pageId, _containerType, new CancellationToken());

            Assert.IsNotNull(result);
            StringAssert.EndsWith(".zip", result.FileName);
            StringAssert.AreEqualIgnoringCase("application/zip", result.ContentType);
        }

        [Test]
        public async Task DownloadPageFiles_when_page_does_not_exist_Then_it_returns_null()
        {
            var nameOfPageThatDoesNotExist = $"{Guid.NewGuid()}";
            var result = await _fileStorageService.DownloadPageFiles(_applicationId, _sequenceNumber, _sectionNumber, nameOfPageThatDoesNotExist, _containerType, new CancellationToken());

            Assert.IsNull(result);
        }
        #endregion

        private static FormFile GenerateFile(string fileName)
        {
            var content = "This is a dummy file";
            return new FormFile(new MemoryStream(Encoding.UTF8.GetBytes(content)), 0, content.Length, fileName, fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/octet-stream"
            };
        }
    }
}
