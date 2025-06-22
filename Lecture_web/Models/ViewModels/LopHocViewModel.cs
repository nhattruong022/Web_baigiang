namespace Lecture_web.Models.ViewModels
{
    public class LopHocViewModel
    {
        public int IdLopHocPhan { get; set; }
        public string TenLop { get; set; }
        public string TenHocPhan { get; set; }
        public int SoSinhVien { get; set; }
        public DateTime NgayTao { get; set; }
        public DateTime NgayCapNhat { get; set; }
        public string GiangVienName { get; set; }
        public string GiangVienAvatarUrl { get; set; }
        public string Mota {  get; set; }

        public int? BaiGiangId { get; set; }

    }
}
