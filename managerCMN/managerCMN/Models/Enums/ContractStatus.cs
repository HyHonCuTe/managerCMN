using System.ComponentModel.DataAnnotations;

namespace managerCMN.Models.Enums;

public enum ContractStatus
{
    [Display(Name = "Đang hiệu lực")]
    Active = 0,

    [Display(Name = "Hết hạn")]
    Expired = 1,

    [Display(Name = "Thanh lý")]
    Terminated = 2,

    [Display(Name = "Đã gia hạn")]
    Renewed = 3
}
