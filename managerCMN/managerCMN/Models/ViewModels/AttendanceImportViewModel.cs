namespace managerCMN.Models.ViewModels;

public class AttendanceImportViewModel
{
    public IFormFile? ExcelFile { get; set; }
    public int? Year { get; set; }
    public int? Month { get; set; }
}
