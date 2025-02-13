﻿using MediatR;
using SFA.DAS.ApplyService.Domain.Apply;
using System.Collections.Generic;

namespace SFA.DAS.ApplyService.Application.Apply.Gateway.Applications
{
    public class NewGatewayApplicationsRequest : IRequest<List<RoatpGatewaySummaryItem>>
    {
    }
}
