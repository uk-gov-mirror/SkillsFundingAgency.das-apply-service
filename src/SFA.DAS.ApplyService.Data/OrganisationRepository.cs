﻿using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Newtonsoft.Json;
using SFA.DAS.ApplyService.Application.Organisations;
using SFA.DAS.ApplyService.Configuration;
using SFA.DAS.ApplyService.Data.DapperTypeHandlers;
using SFA.DAS.ApplyService.Domain.Entities;

namespace SFA.DAS.ApplyService.Data
{
    public class OrganisationRepository : IOrganisationRepository
    {
        private readonly IApplyConfig _config;

        public OrganisationRepository(IConfigurationService configurationService)
        {
            _config = configurationService.GetConfig().Result;
            SqlMapper.AddTypeHandler(typeof(OrganisationDetails), new OrganisationDetailsHandler());
        }

        public async Task<Organisation> CreateOrganisation(Organisation organisation)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync();

                var orgData = JsonConvert.SerializeObject(organisation.OrganisationDetails);
                bool roEPAOApproved = "RoEPAO".Equals(organisation.OrganisationDetails?.OrganisationReferenceType, StringComparison.InvariantCultureIgnoreCase);
                bool roATPApproved = "RoATP".Equals(organisation.OrganisationDetails?.OrganisationReferenceType, StringComparison.InvariantCultureIgnoreCase);

                connection.Execute(
                    "INSERT INTO [Organisations] ([Id],[Name],[OrganisationType],[OrganisationUKPRN], " +
                    "[OrganisationDetails],[Status],[CreatedAt],[CreatedBy],[RoEPAOApproved],[RoATPApproved]) " +
                    "VALUES (NEWID(), @Name, @OrganisationType, @OrganisationUkprn, @orgData, 'New', GETUTCDATE(), @CreatedBy, @roEPAOApproved, @roATPApproved)",
                    new { organisation.Name, organisation.OrganisationType, organisation.OrganisationUkprn, orgData, organisation.CreatedBy, roEPAOApproved, roATPApproved });

                return await GetOrganisationByName(organisation.Name);
            }
        }

        public async Task<Organisation> UpdateOrganisation(Organisation organisation)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync();

                var orgData = JsonConvert.SerializeObject(organisation.OrganisationDetails);

                connection.Execute(
                    "UPDATE [Organisations] " +
                    "SET [UpdatedAt] = GETUTCDATE(), [UpdatedBy] = @UpdatedBy, [Name] = @Name, " +
                    "[OrganisationType] = @OrganisationType, [OrganisationUKPRN] = @OrganisationUkprn, " +
                    "[OrganisationDetails] = @orgData, [RoEPAOApproved] = @RoEPAOApproved, [RoATPApproved] = @RoATPApproved " +
                    "WHERE [Id] = @Id",
                    new { organisation.Id, organisation.Name, organisation.OrganisationType, organisation.OrganisationUkprn, orgData, organisation.UpdatedBy, organisation.RoEPAOApproved, organisation.RoATPApproved });

                return await GetOrganisationByName(organisation.Name);
            }
        }

        public async Task<Organisation> GetOrganisationByName(string name)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync();

                var sql =
                    "SELECT * " +
                    "FROM [Organisations] " +
                    "WHERE Name LIKE @name";

                var orgs = await connection.QueryAsync<Organisation>(sql, new { name });
                var org = orgs.FirstOrDefault();
                return org;
            }
        }

        public async Task<Organisation> GetOrganisationByContactEmail(string email)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync();

                var sql =
                    "SELECT org.* " +
                    "FROM [Organisations] org " +
                    "INNER JOIN [Contacts] con on org.Id = con.ApplyOrganisationId " +
                    "WHERE con.Email = @email";

                var orgs = await connection.QueryAsync<Organisation>(sql, new { email });
                var org = orgs.FirstOrDefault();
                return org;
            }
        }
    }
}