-- =============================================
-- 1. Bảng TaiKhoan
-- =============================================
CREATE TABLE TaiKhoan (
    idTaiKhoan   INT            IDENTITY(1,1) NOT NULL PRIMARY KEY,
    tenDangNhap  VARCHAR(50)    NOT NULL,
    matKhau      VARCHAR(255)   NOT NULL,
    hoTen        NVARCHAR(100)   NOT NULL,
    vaiTro       VARCHAR(20)    NOT NULL,
    email        VARCHAR(100)   NOT NULL,
    soDienThoai  VARCHAR(20)    NOT NULL,
    anhDaiDien   VARCHAR(255)   NULL,
    trangThai    VARCHAR(20)    NOT NULL,
    ngayTao      DATETIME       NOT NULL DEFAULT GETDATE(),
    ngayCapNhat  DATETIME       NOT NULL DEFAULT GETDATE(),

    CONSTRAINT UQ_TaiKhoan_tenDangNhap UNIQUE (tenDangNhap),
    CONSTRAINT UQ_TaiKhoan_email       UNIQUE (email),
    CONSTRAINT UQ_TaiKhoan_soDienThoai UNIQUE (soDienThoai)
);

-- =============================================
-- 2. Bảng OTP
-- =============================================
CREATE TABLE OTP (
    otp_id       INT            IDENTITY(1,1) NOT NULL PRIMARY KEY,
    idTaiKhoan   INT            NOT NULL,
    otp_code     VARCHAR(10)    NOT NULL,
    ngayHetHan   DATETIME       NOT NULL,
    ngayTao      DATETIME       NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_OTP_TaiKhoan
      FOREIGN KEY(idTaiKhoan) REFERENCES TaiKhoan(idTaiKhoan)
);

-- =============================================
-- 3. Bảng Khoa
-- =============================================
CREATE TABLE Khoa (
    idKhoa       INT             IDENTITY(1,1) NOT NULL PRIMARY KEY,
    tenKhoa      NVARCHAR(100)    NOT NULL,
    moTa         NVARCHAR(255)   NULL,            -- rút gọn mô tả
    ngayTao      DATETIME        NOT NULL DEFAULT GETDATE(),
    ngayCapNhat  DATETIME        NOT NULL DEFAULT GETDATE()
);

-- =============================================
-- 4. Bảng BoMon
-- =============================================
CREATE TABLE BoMon (
    idBoMon      INT             IDENTITY(1,1) NOT NULL PRIMARY KEY,
    tenBoMon     NVARCHAR(100)    NOT NULL,
    moTa         NVARCHAR(255)   NULL,            -- rút gọn mô tả
    idKhoa       INT             NOT NULL,
    ngayTao      DATETIME        NOT NULL DEFAULT GETDATE(),
    ngayCapNhat  DATETIME        NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_BoMon_Khoa
      FOREIGN KEY(idKhoa) REFERENCES Khoa(idKhoa)
);

-- =============================================
-- 5. Bảng HocPhan
-- =============================================
CREATE TABLE HocPhan (
    idHocPhan    INT             IDENTITY(1,1) NOT NULL PRIMARY KEY,
    tenHocPhan   NVARCHAR(100)    NOT NULL,
    moTa         NVARCHAR(255)   NULL,            -- rút gọn mô tả
    trangThai    VARCHAR(20)     NOT NULL,
    idBoMon      INT             NOT NULL,
    ngayTao      DATETIME        NOT NULL DEFAULT GETDATE(),
    ngayCapNhat  DATETIME        NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_HocPhan_BoMon
      FOREIGN KEY(idBoMon) REFERENCES BoMon(idBoMon)
);

-- =============================================
-- 6. Bảng BaiGiang
-- =============================================
CREATE TABLE BaiGiang (
    idBaiGiang   INT             IDENTITY(1,1) NOT NULL PRIMARY KEY,
    tieuDe       NVARCHAR(255)    NOT NULL,
    moTa         NVARCHAR(255)   NOT NULL,
    ngayTao      DATETIME        NOT NULL DEFAULT GETDATE(),
    ngayCapNhat  DATETIME        NOT NULL DEFAULT GETDATE(),
    idTaiKhoan   INT             NOT NULL,  -- người soạn

    CONSTRAINT FK_BaiGiang_TaiKhoan
      FOREIGN KEY(idTaiKhoan) REFERENCES TaiKhoan(idTaiKhoan)
);

-- =============================================
-- 7. Bảng Chuong
-- =============================================
CREATE TABLE Chuong (
    idChuong     INT             IDENTITY(1,1) NOT NULL PRIMARY KEY,
    tenChuong    NVARCHAR(100)    NOT NULL,
    ngayTao      DATETIME        NOT NULL DEFAULT GETDATE(),
    ngayCapNhat  DATETIME        NOT NULL DEFAULT GETDATE(),
    idBaiGiang   INT             NOT NULL,

    CONSTRAINT FK_Chuong_BaiGiang
      FOREIGN KEY(idBaiGiang) REFERENCES BaiGiang(idBaiGiang)
);

-- =============================================
-- 8. Bảng Bai
-- =============================================
CREATE TABLE Bai (
    idBai        INT             IDENTITY(1,1) NOT NULL PRIMARY KEY,
    tieuDeBai    NVARCHAR(100)    NOT NULL,
    noiDungText  NVARCHAR(MAX)   NOT NULL,
    ngayTao      DATETIME        NOT NULL DEFAULT GETDATE(),
	ngayCapNhat  DATETIME        NOT NULL DEFAULT GETDATE(),
    idChuong     INT             NOT NULL,

    CONSTRAINT FK_Bai_Chuong
      FOREIGN KEY(idChuong) REFERENCES Chuong(idChuong)
);

-- =============================================
-- 9. Bảng LopHocPhan
-- =============================================
CREATE TABLE LopHocPhan (
    idLopHocPhan INT             IDENTITY(1,1) NOT NULL PRIMARY KEY,
    tenLop       VARCHAR(20)    NOT NULL,
    moTa         NVARCHAR(100)   NULL,            -- rút gọn mô tả
    idHocPhan    INT             NOT NULL,
    idTaiKhoan   INT             NOT NULL,  -- giảng viên dẫn dắt
    idBaiGiang   INT             NULL,      -- optional link

    ngayTao      DATETIME        NOT NULL DEFAULT GETDATE(),
    ngayCapNhat  DATETIME        NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_LopHocPhan_HocPhan
      FOREIGN KEY(idHocPhan) REFERENCES HocPhan(idHocPhan),
    CONSTRAINT FK_LopHocPhan_TaiKhoan
      FOREIGN KEY(idTaiKhoan) REFERENCES TaiKhoan(idTaiKhoan),
    CONSTRAINT FK_LopHocPhan_BaiGiang
      FOREIGN KEY(idBaiGiang) REFERENCES BaiGiang(idBaiGiang)
);

-- =============================================
-- 10. Bảng LopHocPhan_SinhVien
-- =============================================
CREATE TABLE LopHocPhan_SinhVien (
    idLop_SinhVien INT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
    idLopHocPhan INT             NOT NULL,
    idTaiKhoan   INT             NOT NULL,  -- sinh viên
	ThongBaoDaDocIds NVARCHAR(MAX)        NOT NULL,
 
    CONSTRAINT FK_LHPSV_LopHocPhan
      FOREIGN KEY(idLopHocPhan) REFERENCES LopHocPhan(idLopHocPhan)
	   ON DELETE CASCADE,
    CONSTRAINT FK_LHPSV_TaiKhoan
      FOREIGN KEY(idTaiKhoan) REFERENCES TaiKhoan(idTaiKhoan)
);

-- =============================================
-- 11. Bảng ThongBao
-- =============================================
CREATE TABLE ThongBao (
    idThongBao   INT             IDENTITY(1,1) NOT NULL PRIMARY KEY,
    noiDung      NVARCHAR(MAX)   NOT NULL,
    ngayTao      DATETIME        NOT NULL DEFAULT GETDATE(),
    ngayCapNhat  DATETIME        NOT NULL DEFAULT GETDATE(),
    idTaiKhoan   INT             NOT NULL,  -- người đăng (giảng viên)
    idLopHocPhan INT             NOT NULL,

    CONSTRAINT FK_ThongBao_TaiKhoan
      FOREIGN KEY(idTaiKhoan) REFERENCES TaiKhoan(idTaiKhoan),
    CONSTRAINT FK_ThongBao_LopHocPhan
      FOREIGN KEY(idLopHocPhan) REFERENCES LopHocPhan(idLopHocPhan)
	  ON DELETE CASCADE
);

-- =============================================
-- 12. Bảng BinhLuan
-- =============================================
CREATE TABLE BinhLuan (
    idBinhLuan    INT             IDENTITY(1,1) NOT NULL PRIMARY KEY,
    noiDung       NVARCHAR(255)   NOT NULL,
    ngayTao       DATETIME        NOT NULL DEFAULT GETDATE(),
    idTaiKhoan    INT             NOT NULL,   -- tác giả
    idBinhLuanCha INT             NULL,       -- reply
    idBai         INT             NULL,
    idThongBao    INT             NULL,       -- nếu comment lên thông báo
	idLopHocPhan  INT             NOT NULL,
    CONSTRAINT FK_BinhLuan_TaiKhoan
      FOREIGN KEY(idTaiKhoan) REFERENCES TaiKhoan(idTaiKhoan),
    CONSTRAINT FK_BinhLuan_Cha
      FOREIGN KEY(idBinhLuanCha) REFERENCES BinhLuan(idBinhLuan),
    CONSTRAINT FK_BinhLuan_Bai
      FOREIGN KEY(idBai) REFERENCES Bai(idBai),
    CONSTRAINT FK_BinhLuan_ThongBao
      FOREIGN KEY(idThongBao) REFERENCES ThongBao(idThongBao),
	CONSTRAINT FK_BinhLuan_LopHocPhan
	  FOREIGN KEY(idLopHocPhan) REFERENCES LopHocPhan(idLopHocPhan)
	  ON DELETE CASCADE
);
