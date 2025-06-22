using System;
using System.Collections.Generic;

namespace Lecture_web.Models.ViewModels
{
    public class BinhLuanViewModel
    {
        public int IdBinhLuan { get; set; }
        public string NoiDung { get; set; }
        public string TenNguoiDung { get; set; }
        public string AnhDaiDien { get; set; }
        public DateTime? NgayTao { get; set; }
        public int? IdBinhLuanCha { get; set; }
        public List<BinhLuanViewModel> PhanHoi { get; set; }
    }

    public class ThemBinhLuanViewModel
    {
        public string NoiDung { get; set; }
        public int IdBai { get; set; }
        public int? IdBinhLuanCha { get; set; }
    }
} 