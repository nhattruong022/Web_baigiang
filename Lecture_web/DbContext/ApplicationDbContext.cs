using Microsoft.EntityFrameworkCore;
using Lecture_web.Models;

namespace Lecture_web
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<TaiKhoanModels> TaiKhoan { get; set; }
        public DbSet<OtpModels> OTP { get; set; }
        public DbSet<KhoaModels> Khoa { get; set; }
        public DbSet<BoMonModels> BoMon { get; set; }
        public DbSet<HocPhanModels> HocPhan { get; set; }
        public DbSet<BaiGiangModels> BaiGiang { get; set; }
        public DbSet<ChuongModels> Chuong { get; set; }
        public DbSet<BaiModels> Bai { get; set; }
        public DbSet<LopHocPhanModels> LopHocPhan { get; set; }
        public DbSet<LopHocPhan_SinhVienModels> LopHocPhan_SinhVien { get; set; }
        public DbSet<ThongBaoModels> ThongBao { get; set; }
        public DbSet<BinhLuanModels> BinhLuan { get; set; }
    }


} 