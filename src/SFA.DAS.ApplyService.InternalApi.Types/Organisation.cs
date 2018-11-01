﻿namespace SFA.DAS.ApplyService.InternalApi.Types
{
    public class Organisation
    {
        public long? Ukprn { get; set; }

        public string Name { get; set; }
        public OrganisationAddress Address { get; set; }
        public OrganisationType Type { get; set; }
    }

    public class OrganisationAddress
    {
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Address3 { get; set; }
        public string City { get; set; }
        public string Postcode { get; set; }
    }

    public class OrganisationType
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
    }
}
