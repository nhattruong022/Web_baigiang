using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lecture_web.Models;

namespace Lecture_web.Service
{
    public class ExcelService
    {
        public class ExcelUserData
        {
            public string TenDangNhap { get; set; }
            public string MatKhau { get; set; }
            public string HoTen { get; set; }
            public string VaiTro { get; set; }
            public string Email { get; set; }
            public string SoDienThoai { get; set; }
        }

        public class ImportResult
        {
            public List<ExcelUserData> ValidUsers { get; set; } = new List<ExcelUserData>();
            public List<string> Errors { get; set; } = new List<string>();
            public int TotalRows { get; set; }
            public int ValidRows { get; set; }
            public int ErrorRows { get; set; }
        }

        public class InviteStudentRow
        {
            public string Email { get; set; }
            public string TenDangNhap { get; set; }
        }
        public class InviteStudentImportResult
        {
            public List<(string email, string tenDangNhap, string hoTen)> Valid { get; set; } = new();
            public List<string> Invalid { get; set; } = new();
        }

        public ImportResult ImportUsersFromExcel(Stream fileStream)
        {
            var result = new ImportResult();
            
            // Set EPPlus license context
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            try
            {
                using (var package = new ExcelPackage(fileStream))
                {
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                    if (worksheet == null)
                    {
                        result.Errors.Add("Không tìm thấy worksheet trong file Excel");
                        return result;
                    }

                    int rowCount = worksheet.Dimension?.Rows ?? 0;
                    if (rowCount < 2) // Cần ít nhất header + 1 dòng dữ liệu
                    {
                        result.Errors.Add("File Excel không có dữ liệu hoặc chỉ có header");
                        return result;
                    }

                    result.TotalRows = rowCount - 1; // Trừ đi header

                    // Đọc từ dòng 2 (sau header)
                    for (int row = 2; row <= rowCount; row++)
                    {
                        try
                        {
                            var userData = new ExcelUserData
                            {
                                TenDangNhap = GetCellValue(worksheet, row, 1),
                                MatKhau = GetCellValue(worksheet, row, 2),
                                HoTen = GetCellValue(worksheet, row, 3),
                                VaiTro = GetCellValue(worksheet, row, 4),
                                Email = GetCellValue(worksheet, row, 5),
                                SoDienThoai = GetCellValue(worksheet, row, 6)
                            };

                            // Validate dữ liệu
                            var validationErrors = ValidateUserData(userData, row);
                            if (validationErrors.Any())
                            {
                                result.Errors.AddRange(validationErrors);
                                result.ErrorRows++;
                            }
                            else
                            {
                                result.ValidUsers.Add(userData);
                                result.ValidRows++;
                            }
                        }
                        catch (Exception ex)
                        {
                            result.Errors.Add($"Lỗi ở dòng {row}: {ex.Message}");
                            result.ErrorRows++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Lỗi đọc file Excel: {ex.Message}");
            }

            return result;
        }

        private string GetCellValue(ExcelWorksheet worksheet, int row, int col)
        {
            var cell = worksheet.Cells[row, col];
            return cell?.Value?.ToString()?.Trim() ?? "";
        }

        private List<string> ValidateUserData(ExcelUserData userData, int row)
        {
            var errors = new List<string>();

            // Kiểm tra các trường bắt buộc
            if (string.IsNullOrWhiteSpace(userData.TenDangNhap))
                errors.Add($"Dòng {row}: Tên đăng nhập không được để trống");

            if (string.IsNullOrWhiteSpace(userData.MatKhau))
                errors.Add($"Dòng {row}: Mật khẩu không được để trống");

            if (string.IsNullOrWhiteSpace(userData.HoTen))
                errors.Add($"Dòng {row}: Họ và tên không được để trống");

            if (string.IsNullOrWhiteSpace(userData.VaiTro))
                errors.Add($"Dòng {row}: Vai trò không được để trống");

            if (string.IsNullOrWhiteSpace(userData.Email))
                errors.Add($"Dòng {row}: Email không được để trống");

            if (string.IsNullOrWhiteSpace(userData.SoDienThoai))
                errors.Add($"Dòng {row}: Số điện thoại không được để trống");

            // Kiểm tra định dạng email
            if (!string.IsNullOrWhiteSpace(userData.Email) && !IsValidEmail(userData.Email))
                errors.Add($"Dòng {row}: Email không đúng định dạng");

            // Kiểm tra vai trò hợp lệ
            if (!string.IsNullOrWhiteSpace(userData.VaiTro))
            {
                var validRoles = new[] { "Admin", "Giangvien", "Sinhvien" };
                if (!validRoles.Contains(userData.VaiTro))
                    errors.Add($"Dòng {row}: Vai trò không hợp lệ. Chỉ chấp nhận: Admin, Giangvien, Sinhvien");
            }

            // Kiểm tra độ dài
            if (userData.TenDangNhap?.Length > 50)
                errors.Add($"Dòng {row}: Tên đăng nhập không được quá 50 ký tự");

            if (userData.HoTen?.Length > 100)
                errors.Add($"Dòng {row}: Họ và tên không được quá 100 ký tự");

            if (userData.Email?.Length > 100)
                errors.Add($"Dòng {row}: Email không được quá 100 ký tự");

            if (userData.SoDienThoai?.Length > 20)
                errors.Add($"Dòng {row}: Số điện thoại không được quá 20 ký tự");

            return errors;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public byte[] GenerateExcelTemplate()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Users");

                // Header
                worksheet.Cells[1, 1].Value = "Tên đăng nhập";
                worksheet.Cells[1, 2].Value = "Mật khẩu";
                worksheet.Cells[1, 3].Value = "Họ và tên";
                worksheet.Cells[1, 4].Value = "Vai trò";
                worksheet.Cells[1, 5].Value = "Email";
                worksheet.Cells[1, 6].Value = "Số điện thoại";

                // Format header
                using (var range = worksheet.Cells[1, 1, 1, 6])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    range.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                }

                // Auto-fit columns
                worksheet.Cells.AutoFitColumns();

                // Add sample data
                worksheet.Cells[2, 1].Value = "user1";
                worksheet.Cells[2, 2].Value = "password123";
                worksheet.Cells[2, 3].Value = "Nguyễn Văn A";
                worksheet.Cells[2, 4].Value = "Sinhvien";
                worksheet.Cells[2, 5].Value = "user1@example.com";
                worksheet.Cells[2, 6].Value = "0123456789";
                worksheet.Cells[3, 1].Value = "teacher1";
                worksheet.Cells[3, 2].Value = "password123";
                worksheet.Cells[3, 3].Value = "Trần Thị B";
                worksheet.Cells[3, 4].Value = "Giangvien";
                worksheet.Cells[3, 5].Value = "teacher1@example.com";
                worksheet.Cells[3, 6].Value = "0987654321";

                return package.GetAsByteArray();
            }
        }

        public InviteStudentImportResult ImportInviteStudentsFromExcel(Stream fileStream)
        {
            var result = new InviteStudentImportResult();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            try
            {
                using (var package = new ExcelPackage(fileStream))
                {
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                    if (worksheet == null)
                    {
                        result.Invalid.Add("Không tìm thấy worksheet trong file Excel");
                        return result;
                    }
                    int rowCount = worksheet.Dimension?.Rows ?? 0;
                    if (rowCount < 2)
                    {
                        result.Invalid.Add("File Excel không có dữ liệu hoặc chỉ có header");
                        return result;
                    }
                    for (int row = 2; row <= rowCount; row++)
                    {
                        var email = GetCellValue(worksheet, row, 1);
                        var tenDangNhap = GetCellValue(worksheet, row, 2);
                        var hoTen = GetCellValue(worksheet, row, 3);
                        var vaiTro = worksheet.Dimension.Columns >= 4 ? GetCellValue(worksheet, row, 4) : "";
                        var trangThai = worksheet.Dimension.Columns >= 5 ? GetCellValue(worksheet, row, 5) : "";
                        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(tenDangNhap))
                        {
                            // Nếu tất cả các cột đều trống thì bỏ qua, không báo lỗi
                            if (string.IsNullOrWhiteSpace(hoTen) && string.IsNullOrWhiteSpace(vaiTro) && string.IsNullOrWhiteSpace(trangThai))
                                continue;
                            result.Invalid.Add($"Dòng {row}: Email hoặc Tên đăng nhập không được để trống");
                            continue;
                        }
                        // Kiểm tra vai trò
                        if (!string.IsNullOrWhiteSpace(vaiTro))
                        {
                            var role = vaiTro.Trim().ToLowerInvariant().Replace(" ", "");
                            if (role != "sinhvien")
                            {
                                result.Invalid.Add($"Dòng {row}: Vai trò phải là 'Sinhvien'");
                                continue;
                            }
                        }
                        // Kiểm tra trạng thái
                        if (!string.IsNullOrWhiteSpace(trangThai))
                        {
                            var status = trangThai.Trim().ToLowerInvariant().Replace(" ", "");
                            if (status != "hoatdong")
                            {
                                result.Invalid.Add($"Dòng {row}: Trạng thái phải là 'HoatDong'");
                                continue;
                            }
                        }
                        result.Valid.Add((email, tenDangNhap, hoTen));
                    }
                }
            }
            catch (Exception ex)
            {
                result.Invalid.Add($"Lỗi đọc file Excel: {ex.Message}");
            }
            return result;
        }

        public byte[] GenerateInviteStudentTemplate()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("InviteStudents");
                worksheet.Cells[1, 1].Value = "Email";
                worksheet.Cells[1, 2].Value = "Tên đăng nhập";
                using (var range = worksheet.Cells[1, 1, 1, 2])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    range.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                }
                worksheet.Cells[2, 1].Value = "sv01@example.com";
                worksheet.Cells[2, 2].Value = "sv01";
                worksheet.Cells[3, 1].Value = "";
                worksheet.Cells[3, 2].Value = "sv02";
                worksheet.Cells[4, 1].Value = "sv03@example.com";
                worksheet.Cells[4, 2].Value = "";
                worksheet.Cells.AutoFitColumns();
                return package.GetAsByteArray();
            }
        }
    }
} 