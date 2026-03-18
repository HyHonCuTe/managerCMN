using managerCMN.Attributes;
using System.ComponentModel.DataAnnotations;

namespace managerCMN.Models.ViewModels;

public class AttendanceImportViewModel
{
    [ValidateFile(".xlsx,.xls", true)]
    [Display(Name = "File Excel")]
    public IFormFile? ExcelFile { get; set; }

    public int? Year { get; set; }
    public int? Month { get; set; }
}
