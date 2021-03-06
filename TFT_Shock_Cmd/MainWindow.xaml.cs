using System;
using System.IO;
using System.IO.Ports;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ExcelDataReader;
using System.Data;
using System.Windows.Navigation;
//using System.Windows.Shapes;

using DiCon.Instrument.HP;

namespace TFT_Shock_Cmd
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<string> Items_TLS_WL, Items_All;
        List<List<string>> Items_DUTs;
        public SerialPort port = new SerialPort("COM24", 115200);
        HPTLS tls;

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                tls = new HPTLS();
                tls.BoardNumber = 0;
                tls.Addr = 24;
                tls.init();
            }
            catch
            {
                MessageBox.Show("Init TLS failed");
            }

            Items_DUTs = new List<List<string>>();
            Items_TLS_WL = new List<string>();
            Items_All = new List<string>();

            try
            {
                Read_CSV(Path.Combine(Directory.GetCurrentDirectory(), "Cmd.csv"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.StackTrace.ToString());
            }

            //foreach (string s in Items_DUT_1) { ComBox_DUT_WL.Items.Add(s); }
            foreach (string s in Items_TLS_WL) { ComBox_TLS_WL.Items.Add(s); }
            foreach (string s in Items_All) { ComBox_All.Items.Add(s); }
        }

        public void Read_CSV(string path)
        {
            DataSet ds;
            if (path.Length > 0)
            {
                if (System.IO.Path.GetExtension(path) == string.Empty) path = path + ".csv";

                //Find Ref.xlsx file
                if (System.IO.File.Exists(path))
                {
                    var extension = System.IO.Path.GetExtension(path).ToLower();
                    using (var stream = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite))
                    {
                        #region 判斷格式套用讀取方法
                        IExcelDataReader reader = null;
                        if (extension == ".xls")
                        {
                            Console.WriteLine(" => XLS格式");
                            reader = ExcelReaderFactory.CreateBinaryReader(stream, new ExcelReaderConfiguration()
                            {
                                FallbackEncoding = Encoding.GetEncoding("big5")
                            });
                        }
                        else if (extension == ".xlsx")
                        {
                            Console.WriteLine(" => XLSX格式");
                            reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                        }
                        else if (extension == ".csv")
                        {
                            Console.WriteLine(" => CSV格式");
                            reader = ExcelReaderFactory.CreateCsvReader(stream, new ExcelReaderConfiguration()
                            {
                                FallbackEncoding = Encoding.GetEncoding("big5")
                            });
                        }
                        else if (extension == ".txt")
                        {
                            Console.WriteLine(" => Text(Tab Separated)格式");
                            reader = ExcelReaderFactory.CreateCsvReader(stream, new ExcelReaderConfiguration()
                            {
                                FallbackEncoding = Encoding.GetEncoding("big5"),
                                AutodetectSeparators = new char[] { '\t' }
                            });
                        }

                        //沒有對應產生任何格式
                        if (reader == null)
                        {
                            Console.WriteLine("未知的處理檔案：" + extension);
                        }
                        Console.WriteLine(" => 轉換中");
                        #endregion

                        //System.Windows.MessageBox.Show(path);

                        //顯示已讀取資料
                        using (reader)
                        {

                            ds = reader.AsDataSet(new ExcelDataSetConfiguration()
                            {
                                UseColumnDataType = false,
                                ConfigureDataTable = (tableReader) => new ExcelDataTableConfiguration()
                                {
                                    //設定讀取資料時是否忽略標題(True為不使用Header, False為使用Header)
                                    UseHeaderRow = false
                                }
                            });

                            //Turn DataTable into Dictionary type
                            string data;
                            var table = ds.Tables[0];

                            Dictionary<string, List<string>> CSV_keyValuePair = new Dictionary<string, List<string>>();

                            for (var col = 0; col < table.Columns.Count; col++)
                            {
                                //List<string> listValue = new List<string>();

                                if (col >= 2)
                                {
                                    Items_DUTs.Add(new List<string>());
                                }

                                if (table.Rows.Count <= 1) return;
                                for (int row = 1; row < table.Rows.Count; row++)
                                {
                                    data = table.Rows[row][col].ToString();
                                    //listValue.Add(data);

                                    switch (col)
                                    {
                                        case 0:
                                            Items_All.Add(data);
                                            break;
                                        case 1:
                                            Items_TLS_WL.Add(data);
                                            break;
                                    }

                                    if (col >= 2)
                                        Items_DUTs[col - 2].Add(data);

                                }

                                //if (!CSV_keyValuePair.ContainsKey(table.Rows[0][col].ToString()))
                                //    CSV_keyValuePair.Add(table.Rows[0][col].ToString(), listValue);
                            }

                            for (int i = 0; i < Items_DUTs.Count; i++)
                            {
                                ComBox_DUT_Batch.Items.Add("Batch " + (i + 1).ToString());
                            }

                            string[] CSV_Header = CSV_keyValuePair.Keys.ToArray();

                        }
                    }
                }
                else MessageBox.Show("檔案 " + path + " 不存在!");
            }
            else MessageBox.Show("沒有提供Path參數!");
        }

        private void btn_connect_Click(object sender, RoutedEventArgs e)
        {
            if (!port.IsOpen)
            {
                port.Open();
                port.DiscardInBuffer();
                port.DiscardOutBuffer();

                if (port.IsOpen) btn_connect.Content = "Disconnect";
            }
            else
            {
                port.DiscardInBuffer();
                port.DiscardOutBuffer();
                port.Close();

                if (!port.IsOpen) btn_connect.Content = "Connect";
            }
        }

        private void ComBox_Com_DropDownOpened(object sender, EventArgs e)
        {
            combox_comport.Items.Clear();
            string[] myPorts = SerialPort.GetPortNames();

            Task.Delay(10);

            foreach (string s in myPorts)
            {
                string comport_and_ID = s;

                combox_comport.Items.Add(comport_and_ID);  //寫入所有取得的com
            }
        }

        private void combox_comport_DropDownClosed(object sender, EventArgs e)
        {
            if (combox_comport.SelectedItem != null)
            {
                try
                {
                    string com = combox_comport.SelectedItem.ToString();
                    //port = new SerialPort(com, 115200);
                    port.PortName = com;

                    //vm.Ini_Write("Connection_Setting", "PortName", com);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.StackTrace.ToString());
                }
            }
        }

        private async void ComBox_DUT_WL_DropDownClosed(object sender, EventArgs e)
        {
            if (port != null)
            {
                if (port.IsOpen)
                {
                    int batch = ComBox_DUT_Batch.SelectedIndex;

                    port.WriteLine(Items_DUTs[batch][ComBox_TLS_WL.SelectedIndex]);
                    await Task.Delay(120);
                    Port_Read();

                    //port.WriteLine(" ");
                }
            }
        }

        private string Port_Read()
        {
            try
            {
                string msg = "";
                int size = port.BytesToRead;
                byte[] dataBuffer = new byte[size];
                int length = port.Read(dataBuffer, 0, size);

                if (length > 0)
                {
                    msg = Encoding.ASCII.GetString(dataBuffer);
                }
                return msg;
            }
            catch { return ""; }
        }

        private void ComBox_TLS_WL_DropDownClosed(object sender, EventArgs e)
        {
            try
            {
                if (ComBox_TLS_WL.SelectedIndex == -1) return;
                if (tls == null) return;

                double wl = 1548;
                if (double.TryParse(Items_TLS_WL[ComBox_TLS_WL.SelectedIndex], out wl))
                    tls.SetWL(wl);
            }
            catch { }
        }

        private async void ComBox_All_DropDownClosed(object sender, EventArgs e)
        {
            try
            {
                #region TLS cmd
                if (tls == null)
                {
                    MessageBox.Show("Init TLS failed");
                    return;
                }

                double wl = 1548;
                if (double.TryParse(Items_TLS_WL[ComBox_TLS_WL.SelectedIndex], out wl))
                {
                    tls.SetWL(wl);
                }
                #endregion

                #region Wirte cmd to port
                if (port != null)
                {
                    if (port.IsOpen)
                    {
                        int batch = ComBox_DUT_Batch.SelectedIndex;

                        port.WriteLine(Items_DUTs[batch][ComBox_TLS_WL.SelectedIndex]);
                        await Task.Delay(120);
                        Port_Read();

                        //port.WriteLine(" ");
                    }
                }
                #endregion
            }
            catch (Exception ex){ MessageBox.Show(ex.StackTrace.ToString()); }
        }

        public static string Creat_New_CSV(string fileName, List<string> list_column_titles)
        {
            try
            {
                string folder = @"D:\";

                System.Windows.Forms.SaveFileDialog saveFileDialog = new System.Windows.Forms.SaveFileDialog();
                saveFileDialog.Title = "Save Data";
                saveFileDialog.InitialDirectory = @"D:\";
                saveFileDialog.FileName = fileName + DateTime.Today.Year + DateTime.Today.Month.ToString("00") + DateTime.Today.Day.ToString("00") + ".csv";
                saveFileDialog.Filter = "CSV (*.csv)|*.csv|TXT (*.txt)|*.txt|All files (*.*)|*.*";

                string BoardData_filePath = @"D:\" + fileName + @"_001.csv";
                if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    BoardData_filePath = saveFileDialog.FileName;
                    folder = Path.GetDirectoryName(BoardData_filePath);

                    string extension = Path.GetExtension(BoardData_filePath);

                    string filepath = "";

                    #region String Builder
                    StringBuilder _stringBuilder = new StringBuilder();

                    //Header Title
                    for (int i = 0; i < list_column_titles.Count; i++)
                    {
                        _stringBuilder.Append(list_column_titles[i]);

                        if (i != list_column_titles.Count - 1)
                            _stringBuilder.Append(",");
                    }

                    _stringBuilder.AppendLine();  //換行                                                         
                    #endregion

                    if (extension.Equals(".csv"))
                    {
                        #region Save as csv

                        filepath = Path.Combine(folder, Path.GetFileNameWithoutExtension(BoardData_filePath)) + ".csv";


                        if (!File.Exists(filepath))
                        {
                            File.AppendAllText(filepath, "");  //Creat a csv file
                        }

                        //Clear file content and lock the file
                        FileStream fileStream = File.Open(filepath, FileMode.Open);
                        fileStream.SetLength(0);
                        fileStream.Close();

                        File.AppendAllText(filepath, _stringBuilder.ToString());  //Save string builder to csv
                        #endregion
                    }

                    else if (extension.Equals(".txt"))
                    {
                        #region Save as txt
                        filepath = Path.Combine(folder, Path.GetFileNameWithoutExtension(BoardData_filePath)) + ".txt";
                        if (File.Exists(filepath))
                            File.Delete(filepath);

                        File.AppendAllText(filepath, _stringBuilder.ToString());  //Save string builder to csv

                        #endregion
                    }

                    return filepath;
                }
                return "";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.StackTrace.ToString());
                return "Creat CSV Failed";

            }
        }

    }
}
