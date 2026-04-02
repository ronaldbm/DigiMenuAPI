using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Application.DTOs.Update;

public class CompanyTabsUpdateDto
{
    public bool TabsEnabled { get; set; }

    [Range(0, 100)]
    public int DefaultMaxOpenTabs { get; set; } = 3;

    [Range(0, (double)decimal.MaxValue)]
    public decimal DefaultMaxTabAmount { get; set; } = 0;

    public bool TabRequiresManagerApproval { get; set; } = false;
}
