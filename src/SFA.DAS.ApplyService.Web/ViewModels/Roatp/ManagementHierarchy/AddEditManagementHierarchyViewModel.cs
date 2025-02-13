﻿using System;
using System.ComponentModel.DataAnnotations;
using SFA.DAS.ApplyService.Web.ViewModels.Roatp.ManagementHierarchy;

namespace SFA.DAS.ApplyService.Web.ViewModels.Roatp
{
    public class AddEditManagementHierarchyViewModel : ManagementHierarchyViewModel, IPageViewModel
    {
        public Guid ApplicationId { get; set; }

        public string Identifier { get; set; }

        [Required(ErrorMessage = "Enter a first name")]
        [MaxLength(255, ErrorMessage = "Enter a first name using 255 characters or less")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Enter a last name")]
        [MaxLength(255, ErrorMessage = "Enter a last name using 255 characters or less")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Enter a job role")]
        [MaxLength(255, ErrorMessage = "Enter a job role using 255 characters or less")]
        public string JobRole { get; set; }

        public string TimeInRoleMonths { get; set; }
        public string TimeInRoleYears { get; set; }

        public string IsPartOfOtherOrgThatGetsFunding { get; set; }

        public string OtherOrgName { get; set; }


        public string DobMonth { get; set; }
        public string DobYear { get; set; }

        public string Email { get; set; }

        public string ContactNumber { get; set; }
        public string Title { get; set; }
        public int SequenceId { get; set; }
        public int SectionId { get; set; }
        public string PageId { get; set; }
        public string GetHelpQuestion { get; set; }
        public bool GetHelpQuerySubmitted { get; set; }
        public string GetHelpErrorMessage { get; set; }
        public string GetHelpAction { get; set; }


        public int Index { get; set; }
    }
}
