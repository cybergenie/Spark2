using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Windows.Devices.Bluetooth;
using BLEAdapter;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Threading.Tasks;

namespace Spark2.Views
{
    /// <summary>
    /// Client.xaml 的交互逻辑
    /// </summary>
    public partial class Client : BasePage
    {
        private List<BluetoothLEDevice> bluetoothLeDevice = new List<BluetoothLEDevice>();        
        string ServiceGuid = "0000ffe5-0000-1000-8000-00805f9a34fb";
        string NotifyCharacteristicGuid = "0000FFE4-0000-1000-8000-00805F9A34FB";
        string WriteCharacteristicGuid = "0000FFE9-0000-1000-8000-00805F9A34FB";
        BluetoothLEAdapter[] BLEDevices = new BluetoothLEAdapter[3];
        List<TextBlock> tbList = new List<TextBlock>();



        struct Data
        {
            public DateTime dateTime;
            public string value;
        }

        private List<Data>[] datas = { new List<Data>(), new List<Data>(), new List<Data>() };
        public Client()
        {
            InitializeComponent();
            tbList.Add(CharacteristicLatestValue1);
            tbList.Add(CharacteristicLatestValue2);
            tbList.Add(CharacteristicLatestValue3);
        }
      

        private bool ClearBluetoothLEDevice()
        {
            try
            {
                foreach (var _BLEDevices in BLEDevices)
                {
                    if (_BLEDevices != null)
                    {
                        _BLEDevices.Dispose();
                    }
                }
                return true;
            }
           
           catch
            {
                return false;
            }
        }

       
        private void WriteToFile(string filepath)
        {           
           
            RadioButton[,] radioButtons = new RadioButton[3, 3] { { rbLL1, rbRL1, rbMI1 }, { rbLL2, rbRL2, rbMI2 }, { rbLL3, rbRL3, rbMI3 } };
            string[] position = new string[3];

            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (radioButtons[i, j].IsChecked == true)
                    {
                        position[i] = radioButtons[i, j].Content.ToString();
                    }
                }
            }


            try
            {
                for (int i = 0; i < datas.Length; i++)
                {
                    if (i < ParentWindow.SelectedBleDeviceId.Count)
                    {
                        DirectoryInfo fileDir = new DirectoryInfo(filepath);
                        var fileName = filepath+ $"\\{tbName.Text}-{position[i]}-{ParentWindow.SelectedBleDeviceName[i]}-{i + 1}.csv";
                        int fp = 0;
                        while (System.IO.File.Exists(fileName))
                        {
                            fileName= filepath + $"\\{tbName.Text}-{position[i]}-{ParentWindow.SelectedBleDeviceName[i]}-{i + 1}({fp++}).csv";
                        }

                        var fs = new FileStream(fileName, FileMode.CreateNew);
                        using (StreamWriter writer = new StreamWriter(fs, Encoding.UTF8))
                        {
                            writer.WriteLine("time" + "," + "value");
                            foreach (var dt in datas[i])
                            {
                                writer.WriteLine(dt.dateTime.ToString("hh:mm:ss.fff") + "," + dt.value);
                            }

                        }
                    }

                }
                ParentWindow.NotifyUser("文件保存成功.", MainWindow.NotifyType.StatusMessage);
            }

            catch (Exception ex)
            {
                ParentWindow.NotifyUser("文件保存错误:" + ex.ToString(), MainWindow.NotifyType.ErrorMessage);
            }



        }

       

        
        private async void  ConnectButton1_Click(object sender, RoutedEventArgs e)
        {
            

            ConnectButton1.IsEnabled = false;
            //subscribedForNotifications = false;

            if (ParentWindow.SelectedBleDeviceId != null)
            {
                Task.Run(() =>
                {
                    var bluetooth = new BluetoothLEAdapter(ServiceGuid, WriteCharacteristicGuid, NotifyCharacteristicGuid);
                    BLEDevices[0] = bluetooth;
                    bluetooth.SelectDevice(ParentWindow.SelectedBleDeviceId[0]);
                    bluetooth.ValueChanged += Bluetooth_ValueChanged;
                });
            }

            ConnectButton1.IsEnabled = true;

        }

        
        
        private async void Bluetooth_ValueChanged(MsgType type, string str, string deviceID = null)
        {
            Data SensorResult;
            SensorResult.dateTime = DateTime.Now;
            SensorResult.value = str;
            int i = 0;
            foreach(var _deviceID in ParentWindow.SelectedBleDeviceId)
            {
                if(deviceID == _deviceID)
                {
                    datas[i].Add(SensorResult);
                    string message = $"设备:{ParentWindow.SelectedBleDeviceName[i]}     时间 {SensorResult.dateTime:hh:mm:ss.FFF}     数值:{SensorResult.value}";
                    switch (type)
                    {
                        case MsgType.BLEData:
                            Debug.WriteLine("时间:" + DateTime.Now + "数据:" + str);
                            await Dispatcher.BeginInvoke(DispatcherPriority.Input,new Action(() =>
                            {
                                tbList[i].Text = message;
                            }));
                            break;
                        case MsgType.NotifyTxt:
                            ParentWindow.NotifyUser($"设备{i+1}:" + str, MainWindow.NotifyType.StatusMessage);
                            break;
                        case MsgType.Error:
                            ParentWindow.NotifyUser($"设备{i+1}:" + str, MainWindow.NotifyType.ErrorMessage);
                            break;
                        default:
                            ParentWindow.NotifyUser($"{str}", MainWindow.NotifyType.ErrorMessage);
                            break;

                    }
                    break;

                }
                i++;
            }
            
            
        }


        private async void ConnectButton2_Click(object sender, RoutedEventArgs e)
        {
            ConnectButton2.IsEnabled = false;
            //subscribedForNotifications[2] = false;

            if (ParentWindow.SelectedBleDeviceId != null)
            {
                Task.Run(() =>
                {
                    var bluetooth = new BluetoothLEAdapter(ServiceGuid, WriteCharacteristicGuid, NotifyCharacteristicGuid);
                    BLEDevices[1] = bluetooth;
                    bluetooth.SelectDevice(ParentWindow.SelectedBleDeviceId[1]);
                    bluetooth.ValueChanged += Bluetooth_ValueChanged;
                }
                  );
            }

            ConnectButton2.IsEnabled = true;
        }
        

        private async void ConnectButton3_Click(object sender, RoutedEventArgs e)
        {
            ConnectButton3.IsEnabled = false;
            //subscribedForNotifications = false;

            if (ParentWindow.SelectedBleDeviceId != null)
            {
                Task.Run(() =>
                {
                    var bluetooth = new BluetoothLEAdapter(ServiceGuid, WriteCharacteristicGuid, NotifyCharacteristicGuid);
                    BLEDevices[3] = bluetooth;
                    bluetooth.SelectDevice(ParentWindow.SelectedBleDeviceId[2]);
                    bluetooth.ValueChanged += Bluetooth_ValueChanged;
                });
            }

            ConnectButton2.IsEnabled = true;

        }        

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {

            try
            {

                ClearBluetoothLEDevice();

                ParentWindow.NotifyUser("停止读取传感器数据成功", MainWindow.NotifyType.StatusMessage);
                if (this.CharacteristicLatestValue4.Text.Trim().Length>0)
                {
                    var filepath = this.CharacteristicLatestValue4.Text.Trim();
                    WriteToFile(filepath);
                }
                else
                {
                    ParentWindow.NotifyUser("请选择文件保存路径。" , MainWindow.NotifyType.ErrorMessage);
                }
                

            }
            catch (UnauthorizedAccessException ex)
            {
                // This usually happens when a device reports that it support notify, but it actually doesn't.
                ParentWindow.NotifyUser(ex.Message, MainWindow.NotifyType.ErrorMessage);
            }

        }

        private void Client_Loaded(object sender, RoutedEventArgs e)
        {
            if (ParentWindow.SelectedBleDeviceId != null)
            {
                SelectedDeviceRun.Text = ParentWindow.SelectedBleDeviceName.Count.ToString();     

                switch (ParentWindow.SelectedBleDeviceId.Count)
                {
                    case 1:
                        ConnectButton1.IsEnabled = true;
                        ConnectButton2.IsEnabled = false;
                        ConnectButton3.IsEnabled = false;
                        break;
                    case 2:
                        ConnectButton1.IsEnabled = true;
                        ConnectButton2.IsEnabled = true;
                        ConnectButton3.IsEnabled = false;
                        break;
                    case 3:
                        ConnectButton1.IsEnabled = true;
                        ConnectButton2.IsEnabled = true;
                        ConnectButton3.IsEnabled = true;
                        break;
                    default:
                        ConnectButton1.IsEnabled = false;
                        ConnectButton2.IsEnabled = false;
                        ConnectButton3.IsEnabled = false;
                        break;

                }
            }
            else
            {
                ParentWindow.NotifyUser("没有选择蓝牙设备.", MainWindow. NotifyType.ErrorMessage);
                ConnectButton1.IsEnabled = false;
                ConnectButton2.IsEnabled = false;
                ConnectButton3.IsEnabled = false;
            }
        }

        private void Client_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearBluetoothLEDevice();
                datas = null;
            }
            catch
            {               
                    ParentWindow.NotifyUser("错误: 无法重置软件状态", MainWindow.NotifyType.ErrorMessage);
                
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;//设置为选择文件夹
            dialog.ShowDialog();
            try
            {
                this.CharacteristicLatestValue4.Text = dialog.FileName.Trim();
            }
            catch(Exception)
            {
                this.CharacteristicLatestValue4.Text = "";
            }
        }
    }
}
