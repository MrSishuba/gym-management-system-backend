using av_motion_api.Models;
using ClosedXML.Excel;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace av_motion_api.Services
{
    public static class ExportHelper
    {
        // Method to export employee shifts to JSON format
        public static string ExportShiftsToJson(IEnumerable<EmployeeShift> employeeShifts)
        {
            var shifts = employeeShifts.Select(es => new
            {
                es.EmployeeShift_ID,
                es.Employee_ID,
                es.Shift_ID,
                es.Shift_Start_Time,
                es.Shift_End_Time
            });
            return JsonConvert.SerializeObject(shifts);
        }

        // Method to export employee shifts to Excel format
        public static byte[] ExportShiftsToExcel(IEnumerable<EmployeeShift> employeeShifts)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Shifts");

                // Adding column headers
                worksheet.Cell(1, 1).Value = "Employee Shift ID";
                worksheet.Cell(1, 2).Value = "Employee ID";
                worksheet.Cell(1, 3).Value = "Shift ID";
                worksheet.Cell(1, 4).Value = "Start Time";
                worksheet.Cell(1, 5).Value = "End Time";

                // Adding shift data
                int row = 2;
                foreach (var es in employeeShifts)
                {
                    worksheet.Cell(row, 1).Value = es.EmployeeShift_ID;
                    worksheet.Cell(row, 2).Value = es.Employee_ID;
                    worksheet.Cell(row, 3).Value = es.Shift_ID;
                    worksheet.Cell(row, 4).Value = es.Shift_Start_Time.ToString("yyyy-MM-dd HH:mm:ss");
                    worksheet.Cell(row, 5).Value = es.Shift_End_Time?.ToString("yyyy-MM-dd HH:mm:ss");
                    row++;
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }
    }

}
