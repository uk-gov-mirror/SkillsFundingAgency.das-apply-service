﻿using System;

namespace SFA.DAS.ApplyService.Application.Apply.Oversight.Queries.GetAppeal
{
    public class GetAppealQueryResult
    {
        public string Message { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}