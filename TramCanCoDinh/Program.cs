using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using MySql.Data.MySqlClient;
using System.Threading;
using System.Data.Common;
using System.Data;
using System.Net;
using log4net;

namespace TramCanCoDinh
{
    /*
     * Số liệu của phiếu cân trong ví dụ dưới đây được mô tả theo thông tin của phiếu cân đính kèm với project này.
     * (Xem file MauPhieuCan.png trong project này để tham chiếu)
     * Các thông số kết nối server được đặt ở file App.config (Thông tin này sẽ được cung cấp sau)
     */
    class Program
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));

        public static SendData sendToServer = new SendData();     
        public static String imagePath = "";

        private static String downloadImg(String imgUrl)
        {
            String path = @imagePath + imgUrl.Split('=').Last() + ".jpg";

            byte[] imageBytes;
            HttpWebRequest imageRequest = (HttpWebRequest)WebRequest.Create(imgUrl);
            WebResponse imageResponse = imageRequest.GetResponse();
            Stream responseStream = imageResponse.GetResponseStream();
            BinaryReader br = new BinaryReader(responseStream);            
            imageBytes = br.ReadBytes(500000);
            br.Close();
            responseStream.Close();
            imageResponse.Close();

            FileStream fs = new FileStream(path, FileMode.Create);
            BinaryWriter bw = new BinaryWriter(fs);
            try
            {
                bw.Write(imageBytes);
            }
            finally
            {
                fs.Close();
                bw.Close();
            }
            return path;
        }

        private static PhieuCanEntity prepareData(int logcanxeId, ref bool isDone)
        {
            PhieuCanEntity phieuCanObj = new PhieuCanEntity();

            MySqlConnection conn2 = DBUtils.GetDBConnection();
            try
            {
                log.Debug("Openning Connection 2...");
                conn2.Open();
                log.Debug("Connection 2 successful!");

                //Query logcanxe                             
                MySqlCommand logcanxeCmd = conn2.CreateCommand();
                string logcanxeSql =
                    " SELECT logcanxe_id, logcanxe_thoidiem, logcanxe_V, logcanxe_bienso, " +
                    "   logcanxe_lanxe, logcanxe_imglanxe, logcanxe_imgbienso, " +
                    "   logcanxe_W, logcanxe_ghichu, logcanxe_biensoSMRM " +                    
                    " FROM tbl_logcanxe " +
                    " WHERE logcanxe_id = @logcanxeId ";
                logcanxeCmd.CommandText = logcanxeSql;

                MySqlParameter logcanxeIdParam = new MySqlParameter("@logcanxeId", SqlDbType.Int);
                logcanxeIdParam.Value = logcanxeId;
                logcanxeCmd.Parameters.Add(logcanxeIdParam);

                DbDataReader reader = logcanxeCmd.ExecuteReader();
                if (reader.HasRows && reader.Read())
                {
                    DateTime thoidiem = reader.GetDateTime(1);
                    double tocdo = reader.GetDouble(2);

                    string bienso = "";
                    if (!reader.IsDBNull(3))
                    {
                        bienso = reader.GetString(3).Replace("-","").Replace(".","");
                    }

                    int lanxe = reader.GetInt32(4);
                    string imgUrlLanxe = reader.GetString(5);
                    string imgUrlBienso = reader.GetString(6);
                    double khoiluongThucte = reader.GetDouble(7);
                    string ghichu = reader.GetString(8);

                    string biensoSMRM = "";
                    if (!reader.IsDBNull(9))
                    {
                        biensoSMRM = reader.GetString(9).Replace("-", "").Replace(".", "");
                    }

                    string[] arrGhichu = ghichu.Split('|');                    
                    string saiso = arrGhichu.Last().Split(',')[3].Split('=')[1];
                    string taitrongchophep = arrGhichu.Last().Split(',')[5].Split('=')[1];



                    //fill data to phieuCanEntity                                                   
                    String maPhieuCan = ConfigAccess.GetTramCanID() + "." + thoidiem.ToString("yyyyMMddHHmmss") + lanxe;
                    phieuCanObj.MaPhieuCan = maPhieuCan; //"TC341.20150131233009" - Mã phiếu theo format: MaTramCan.NamThangNgayGioPhutGiay để đảm bảo tính duy nhất cho toàn hệ thống. Nếu trạm có nhiều làn cân thì thêm số làn ở cuối.
                    phieuCanObj.MaTramCan = ConfigAccess.GetTramCanID();   //"TC341" - Liên hệ để được admin hệ thống cung cấp mã
                    phieuCanObj.BienSoXe = bienso; //"12C03435" - Biển số xe

                    /*----------------------- BẮT ĐẦU ADD THÔNG TIN ẢNH CHỤP CỦA XE --------------------------*/
                    //List đối tượng ảnh chụp xe (thông thường là 2 ảnh: Ảnh 1 là ảnh chụp làn xe từ camera trước + biển số trước; Ảnh 2 là ảnh chụp làn xe từ camera sau + biển số sau. Định dạnh jpg hoặc png dung lượng khoảng 200-300kb)
                    PhieuCanEntity.AnhPhieuCan imgObj1 = new PhieuCanEntity.AnhPhieuCan();
                    PhieuCanEntity.AnhPhieuCan imgObj2 = new PhieuCanEntity.AnhPhieuCan();
                    try
                    {
                        imgObj1.Anh = Common.FileToBase64(downloadImg(imgUrlLanxe)); //Biểu diễn ảnh dưới dạng chuỗi base64. Lưu ý dùng hàm convert ảnh sang base64 string trong class Common như ở ví dụ này
                                                                                     //"E:\\Pictures\\20151026102552203_1.jpg"                    
                        imgObj2.Anh = Common.FileToBase64(downloadImg(imgUrlBienso));   //Biểu diễn ảnh dưới dạng chuỗi base64. Lưu ý dùng hàm convert ảnh sang base64 string trong class Common như ở ví dụ này
                                                                                        //Add các ảnh vào trong list
                        phieuCanObj.ImagesList.Add(imgObj1);
                        phieuCanObj.ImagesList.Add(imgObj2);
                    }
                    catch (Exception ex)
                    {
                        log.Info("Error: " + ex.Message);
                    }
                    /*----------------------KẾT THÚC VIỆC ADD THÔNG TIN ẢNH CHỤP CỦA XE --------------------------*/

                    phieuCanObj.TocDoXe = tocdo;   //3.1 - Tốc độ xe đi qua trạm tính theo đơn vị km/h (Ví dụ này xe chạy qua trạm với tốc độ 3.1 km/h)
                    phieuCanObj.ThoiGianXeVaoTram = thoidiem.ToString("dd/MM/yyyy HH:mm:ss");   //Thời gian xe bắt đầu vào trạm cân. Ví dụ: 11/11/2015 23:29:01
                    phieuCanObj.NgayCan = thoidiem.ToString("dd/MM/yyyy");  //Ngày cân phiếu này. Ví dụ: 11/11/2015 
                    phieuCanObj.HinhThucCan = 3;    //1: Hình thức cân theo tải trọng cầu đường; 2: Hình thức cân theo tải trọng hàng hóa; 3: Hình thức cân tổng hợp (mặc định là 3 - theo thông tư 46)
                    phieuCanObj.KhoiLuongXeThucTe = khoiluongThucte; //49260 - Khối lượng cả xe cân được thực tế (tính bằng kg)                    
                    phieuCanObj.KhoiLuongXeSaiSo = double.Parse(saiso); //1970 - Khối lượng sai số cả xe cho phép (tính bằng kg)
                    phieuCanObj.KhoiLuongXeChoPhep = double.Parse(taitrongchophep); //48000 - Khối lượng xe cho phép chở tối đa (tính bằng kg)

                    /*---------------------- BẮT ĐẦU ADD THÔNG TIN CÁC BỘ TRỤC CỦA XE --------------------------*/
                    for (int i = 0; i < arrGhichu.Length - 1; i++)
                    {
                        string[] arrBotruc = arrGhichu[i].Split(',');
                        int loaitruc = Int32.Parse(arrBotruc[0].Split('=')[1]);

                        if (loaitruc == 1) {
                            //Bộ trục đơn
                            PhieuCanEntity.BoTruc boTrucDonObj = new PhieuCanEntity.BoTruc();
                            boTrucDonObj.KhoangCachTruc = "d=0m";   //với bộ trục đơn thì khoảng cách trục luôn bằng 0: "d=0m"
                            boTrucDonObj.KhoiLuongChoPhep = double.Parse(arrBotruc[5].Split('=')[1]);  //10000 - Khối lượng cho phép của trục này tính bằng đơn vị kg (ví dụ này trục cho phép chở 10 tấn)
                            boTrucDonObj.KhoiLuongTrucThucTe = double.Parse(arrBotruc[2].Split('=')[1]); //6150 - Khối lượng trục cân được thực tế (ví dụ này khối lượng trục cân được 6.15 tấn)
                            boTrucDonObj.KhoiLuongSaiSo = double.Parse(arrBotruc[3].Split('=')[1]);  //250 - Khối lượng sai số tính bằng kg (ví dụ này thì trục này cho phép sai số 0.25 tấn)
                            boTrucDonObj.LoaiBoTruc = 1;    //Bộ trục đơn ký hiệu là 1, bộ trục đôi là 2, bộ trục 3 là 3.
                            //Add các trục vào bộ trục
                            phieuCanObj.BoTrucs.Add(boTrucDonObj);
                        }
                        else if(loaitruc == 2)
                        {
                            //Bộ trục đôi
                            PhieuCanEntity.BoTruc boTrucDoiObj = new PhieuCanEntity.BoTruc();
                            boTrucDoiObj.KhoangCachTruc = "d>=1.3m";
                            boTrucDoiObj.KhoiLuongChoPhep = double.Parse(arrBotruc[5].Split('=')[1]);  //Khối lượng cho phép của trục này tính bằng đơn vị kg (ví dụ này trục cho phép chở 18 tấn)
                            boTrucDoiObj.KhoiLuongTrucThucTe = double.Parse(arrBotruc[2].Split('=')[1]); //Khối lượng trục cân được thực tế (ví dụ này khối lượng trục cân được 21.22 tấn)
                            boTrucDoiObj.KhoiLuongSaiSo = double.Parse(arrBotruc[3].Split('=')[1]); //Khối lượng sai số tính bằng kg (ví dụ này thì trục này cho phép sai số 0.85 tấn)
                            boTrucDoiObj.LoaiBoTruc = 2;    //Bộ trục đơn ký hiệu là 1, bộ trục đôi là 2, bộ trục 3 là 3.
                            //Add các trục vào bộ trục
                            phieuCanObj.BoTrucs.Add(boTrucDoiObj);
                        }
                        else if(loaitruc == 3)
                        {
                            //Bộ trục ba
                            PhieuCanEntity.BoTruc boTrucBaObj = new PhieuCanEntity.BoTruc();
                            boTrucBaObj.KhoangCachTruc = "d>1.3m";
                            boTrucBaObj.KhoiLuongChoPhep = double.Parse(arrBotruc[5].Split('=')[1]);  //Khối lượng cho phép của trục này tính bằng đơn vị kg (ví dụ này trục cho phép chở 24 tấn)
                            boTrucBaObj.KhoiLuongTrucThucTe = double.Parse(arrBotruc[2].Split('=')[1]); //Khối lượng trục cân được thực tế (ví dụ này khối lượng trục cân được 21.89 tấn)
                            boTrucBaObj.KhoiLuongSaiSo = double.Parse(arrBotruc[3].Split('=')[1]); //Khối lượng sai số tính bằng kg (ví dụ này thì trục này cho phép sai số 0.87 tấn)
                            boTrucBaObj.LoaiBoTruc = 3;    //Bộ trục đơn ký hiệu là 1, bộ trục đôi là 2, bộ trục 3 là 3.                            
                            //Add các trục vào bộ trục
                            phieuCanObj.BoTrucs.Add(boTrucBaObj);
                        }
                    }                                                                                            
                    /*----------------------KẾT THÚC VIỆC ADD THÔNG TIN CÁC BỘ TRỤC VÀO PHIẾU CÂN --------------------------*/

                    //Thêm các thông tin bổ sung
                    phieuCanObj.LanCan = lanxe.ToString();  //"P2" - Làn cân xe (P1,P2,P3,T1,T2,T3...)
                    phieuCanObj.LanCanXe = "1"; // Lần cân xe
                    phieuCanObj.BienSoRM = biensoSMRM; //"15R10886" - Biển số SMRM/RM
                    phieuCanObj.LaXeXiTec = 0; // Là xe xi téc chở chất lỏng: 1; Nếu không phải : 0;
                    phieuCanObj.HoTenLaiXe = ""; // Nguyễn Văn A - Họ tên lái xe
                    phieuCanObj.SoGPLHX = ""; // GPLHX số
                    phieuCanObj.GhiChu = ""; // Ghi chú
                    phieuCanObj.NguoiLapPhieu = ""; //Hoàng Văn B - Người lập phiếu
                    phieuCanObj.MauXe = ""; //Mầu xe
                    phieuCanObj.GPLX = ""; //GPLX
                    phieuCanObj.KichThuocBaoVuotD = 0; //(Chiều dài kích thước bao vượt – đơn vị mm)
                    phieuCanObj.KichThuocBaoVuotR = 0; //(Chiều rộng kích thước bao vượt – đơn vị mm)
                    phieuCanObj.KichThuocBaoVuotC = 0; //550 - (Chiều cao kích thước bao vượt – đơn vị mm)
                    phieuCanObj.KichThuocThungVuotD = 0; //(Chiều dài kích thước thùng vượt – đơn vị mm)
                    phieuCanObj.KichThuocThungVuotR = 0; //(Chiều rộng kích thước thùng vượt – đơn vị mm)
                    phieuCanObj.KichThuocThungVuotC = 0; //650 - (Chiều cao kích thước thùng vượt – đơn vị mm)
                }
                isDone = true;
            }
            catch (Exception e)
            {
                isDone = false;
                log.Info("Error: " + e.Message);                
            }
            finally
            {
                log.Debug("Closing Connection 2...");
                conn2.Close();
                log.Debug("Connection 2 is closed!");
            }                        

            return phieuCanObj;
        }


        static void Main(string[] args)
        {            
            IniFile myIni = new IniFile("config.ini");
            imagePath = myIni.Read("IMG_PATH", "Application Config");                        

            while (true)
            {
                log.Info("--------------------------------------------------");
                log.Debug("Getting Connection ...");
                MySqlConnection conn = DBUtils.GetDBConnection();                

                try
                {                    
                    log.Debug("Openning Connection ...");
                    conn.Open();
                    log.Debug("Connection successful!");


                    //Scan danh sách phiếu cân chưa đẩy
                    String scanSql = 
                        " SELECT guiphieucan_id, guiphieucan_logcanxe " +
                        " FROM tbl_guiphieucan " +
                        " WHERE guiphieucan_trangthai = 0 " +
                        " ORDER BY guiphieucan_thoigian ASC LIMIT 1";
                    MySqlCommand scanCmd = conn.CreateCommand();
                    scanCmd.CommandText = scanSql;

                    DbDataReader reader = scanCmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            int guiphieucanId = reader.GetInt32(0);
                            int logcanxeId = reader.GetInt32(1);
                            log.Info("guiphieucanId :" + guiphieucanId);
                            log.Info("logcanxeId :" + logcanxeId);

                            bool isFillData = true;
                            PhieuCanEntity phieuCan = prepareData(logcanxeId, ref isFillData);

                            MySqlConnection connUpdate = DBUtils.GetDBConnection();
                            
                            try
                            {
                                connUpdate.Open();
                                if (isFillData)
                                {
                                    //Đẩy thông tin phiếu cân lên server
                                    log.Info("Start pushing...");
                                    sendToServer.PushMessage(phieuCan);
                                    log.Info("Done");

                                    //update thông tin đã gửi phiếu cân                                
                                    MySqlCommand updateCmd = connUpdate.CreateCommand();
                                    updateCmd.CommandText = "UPDATE tbl_guiphieucan SET guiphieucan_trangthai = 1 WHERE guiphieucan_id =" + guiphieucanId + " AND guiphieucan_trangthai = 0";
                                    updateCmd.ExecuteNonQuery();
                                }
                                else
                                {
                                    log.Info("Data is invalid...");
                                    //update thông tin bản ghi lỗi 
                                    MySqlCommand updateCmd = connUpdate.CreateCommand();
                                    updateCmd.CommandText = "UPDATE tbl_guiphieucan SET guiphieucan_trangthai = 2 WHERE guiphieucan_id =" + guiphieucanId + " AND guiphieucan_trangthai = 0";
                                    updateCmd.ExecuteNonQuery();
                                }

                            }
                            catch (Exception e)
                            {
                                log.Info("Error: " + e.Message);
                            }
                            finally
                            {
                                connUpdate.Close();
                            }                            
                        }
                    }                                        
                }
                catch (Exception e)
                {
                    log.Info("Error: " + e.Message);                    
                }
                finally
                {                    
                        log.Debug("Closing Connection ...");
                        conn.Close();
                        log.Debug("Connection is closed!");
                }

                log.Info("Sleep in seconds...");
                Thread.Sleep(1000);
            }                                    
        }
    }
}
