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

        [ForeignKey(nameof(IdLopHocPhan))]
        public LopHocPhanModels LopHocPhan { get; set; }

        [ForeignKey(nameof(IdTaiKhoan))]
        public TaiKhoanModels TaiKhoan { get; set; }
    }
}
