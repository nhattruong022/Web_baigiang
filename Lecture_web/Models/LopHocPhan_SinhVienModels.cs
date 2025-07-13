using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Lecture_web.Models
{
    public class LopHocPhan_SinhVienModels
    {
        [Key]
        public int idLop_SinhVien { get; set; }
        public int IdLopHocPhan { get; set; }
        public int IdTaiKhoan { get; set; }

        public string ThongBaoDaDocIds { get; set; } = ""; // Lưu danh sách ID thông báo đã đọc, phân tách bằng dấu phẩy, mặc định rỗng

        [ForeignKey(nameof(IdLopHocPhan))]
        public LopHocPhanModels LopHocPhan { get; set; }

        [ForeignKey(nameof(IdTaiKhoan))]
        public TaiKhoanModels TaiKhoan { get; set; }
    }
}
