using System.ComponentModel.DataAnnotations;

namespace managerCMN.Models.Enums;

public enum ContractType
{
    [Display(Name = "Thử việc")]
    Probation = 0,

    [Display(Name = "Xác định thời hạn")]
    FixedTerm = 1,

    [Display(Name = "Không xác định thời hạn")]
    Indefinite = 2,

    [Display(Name = "Thời vụ")]
    Seasonal = 3
}
