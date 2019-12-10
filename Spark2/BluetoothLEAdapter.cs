using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Security.Cryptography;

namespace BLEAdapter
{
    public class BluetoothLEAdapter
    {
        //存储检测的设备MAC。
        public string CurrentDeviceID { get; set; }
        //存储检测到的设备。
        public BluetoothLEDevice CurrentDevice { get; set; }
        //存储检测到的主服务。
        public GattDeviceService CurrentService { get; set; }
        //存储检测到的写特征对象。
        public GattCharacteristic CurrentWriteCharacteristic { get; set; }
        //存储检测到的通知特征对象。
        public GattCharacteristic CurrentNotifyCharacteristic { get; set; }

        public string ServiceGuid { get; set; }

        public string WriteCharacteristicGuid { get; set; }
        public string NotifyCharacteristicGuid { get; set; }


        private const int CHARACTERISTIC_INDEX = 0;
        //特性通知类型通知启用
        private const GattClientCharacteristicConfigurationDescriptorValue CHARACTERISTIC_NOTIFICATION_TYPE = GattClientCharacteristicConfigurationDescriptorValue.Notify;


        private Boolean asyncLock = false;

        //private DeviceWatcher deviceWatcher;



        //定义一个委托
        public delegate void eventRun(MsgType type, string str, string deviceID = null);
        //定义一个事件
        public event eventRun ValueChanged;



        public BluetoothLEAdapter(string serviceGuid, string writeCharacteristicGuid, string notifyCharacteristicGuid)
        {
            ServiceGuid = serviceGuid;
            WriteCharacteristicGuid = writeCharacteristicGuid;
            NotifyCharacteristicGuid = notifyCharacteristicGuid;
        }

        /// <summary>
        /// 按MAC地址查找系统中配对设备
        /// </summary>
        /// <param name="MAC"></param>
        public async Task SelectDevice(string bleDevice)
        {
            CurrentDevice = await BluetoothLEDevice.FromIdAsync(bleDevice);

            if (CurrentDevice == null)
            {
                string msg = "没有发现设备";
                ValueChanged(MsgType.Error, msg, CurrentDevice == null ? null : CurrentDevice.DeviceId);
            }
            else
            {
                await Connect();
            }

        }
        /// <summary>
        /// 按MAC地址直接组装设备ID查找设备
        /// </summary>
        /// <param name="MAC"></param>
        /// <returns></returns>
        public async Task SelectDeviceFromIdAsync(string bleDevice)
        {
            CurrentDevice = await BluetoothLEDevice.FromIdAsync(bleDevice);            
            await Connect();

        }
        
        private async Task Connect()
        {
            string msg = "正在连接设备<" + CurrentDeviceID + ">..";
            ValueChanged(MsgType.NotifyTxt, msg, CurrentDevice==null?null:CurrentDevice.DeviceId);
            CurrentDevice.ConnectionStatusChanged += CurrentDevice_ConnectionStatusChanged;
            await SelectDeviceService();

        }


        /// <summary>
        /// 主动断开连接
        /// </summary>
        /// <returns></returns>
        public void Dispose()
        {

            CurrentDeviceID = null;
            CurrentService?.Dispose();
            CurrentDevice?.Dispose();
            CurrentDevice = null;
            CurrentService = null;
            CurrentWriteCharacteristic = null;
            CurrentNotifyCharacteristic = null;
            ValueChanged(MsgType.NotifyTxt, "主动断开连接", CurrentDevice == null ? null : CurrentDevice.DeviceId);

        }

        private void CurrentDevice_ConnectionStatusChanged(BluetoothLEDevice sender, object args)
        {
            if (sender.ConnectionStatus == BluetoothConnectionStatus.Disconnected && CurrentDeviceID != null)
            {
                string msg = "设备已断开,自动重连";
                ValueChanged(MsgType.NotifyTxt, msg, CurrentDevice == null ? null : CurrentDevice.DeviceId);
                if (!asyncLock)
                {
                    asyncLock = true;
                    CurrentDevice.Dispose();
                    CurrentDevice = null;
                    CurrentService = null;
                    CurrentWriteCharacteristic = null;
                    CurrentNotifyCharacteristic = null;
                    SelectDeviceFromIdAsync(CurrentDeviceID);
                }

            }
            else
            {
                string msg = "设备已连接";
                ValueChanged(MsgType.NotifyTxt, msg, CurrentDevice == null ? null : CurrentDevice.DeviceId);
            }
        }
        /// <summary>
        /// 按GUID 查找主服务
        /// </summary>
        /// <param name="characteristic">GUID 字符串</param>
        /// <returns></returns>
        public async Task SelectDeviceService()
        {
            Guid guid = new Guid(ServiceGuid);

          

            //foreach (var CurrentService in rr.GetResults().Services)
            //{ Debug.WriteLine(CurrentService.Uuid.ToString()); }

            CurrentDevice.GetGattServicesForUuidAsync(guid).Completed = (asyncInfo, asyncStatus) =>
            {
                if (asyncStatus == AsyncStatus.Completed)
                {
                    try
                    {
                        GattDeviceServicesResult result = asyncInfo.GetResults();
                        string msg = "主服务=" + CurrentDevice.ConnectionStatus;


                        ValueChanged(MsgType.NotifyTxt, msg, CurrentDevice == null ? null : CurrentDevice.DeviceId);
                        if (result.Services.Count > 0)
                        {
                            //Debug.WriteLine(result.Services[0].Uuid.ToString());
                            CurrentService = result.Services[CHARACTERISTIC_INDEX];
                            
                            if (CurrentService != null)
                            {
                                asyncLock = true;
                                //GetCurrentWriteCharacteristic();
                                GetCurrentNotifyCharacteristic();

                            }
                        }
                        else
                        {
                            msg = "没有发现服务,自动重试中";
                            ValueChanged(MsgType.NotifyTxt, msg, CurrentDevice == null ? null : CurrentDevice.DeviceId);
                            SelectDeviceService();
                        }
                    }
                    catch (Exception e)
                    {
                        ValueChanged(MsgType.NotifyTxt, "没有发现服务,自动重试中", CurrentDevice == null ? null : CurrentDevice.DeviceId);
                        SelectDeviceService();

                    }
                }
            };
        }


        /// <summary>
        /// 设置写特征对象。
        /// </summary>
        /// <returns></returns>
        public async Task GetCurrentWriteCharacteristic()
        {

            string msg = "";
            Guid guid = new Guid(WriteCharacteristicGuid);
            CurrentService.GetCharacteristicsForUuidAsync(guid).Completed = async (asyncInfo, asyncStatus) =>
            {
                if (asyncStatus == AsyncStatus.Completed)
                {
                    GattCharacteristicsResult result = asyncInfo.GetResults();
                    msg = "特征对象=" + CurrentDevice.ConnectionStatus;
                    ValueChanged(MsgType.NotifyTxt, msg, CurrentDevice == null ? null : CurrentDevice.DeviceId);
                    if (result.Characteristics.Count > 0)
                    {
                        CurrentWriteCharacteristic = result.Characteristics[CHARACTERISTIC_INDEX];
                    }
                    else
                    {
                        msg = "没有发现特征对象,自动重试中";
                        ValueChanged(MsgType.NotifyTxt, msg, CurrentDevice == null ? null : CurrentDevice.DeviceId);
                        await GetCurrentWriteCharacteristic();
                    }
                }
            };
        }




        /// <summary>
        /// 发送数据接口
        /// </summary>
        /// <param name="characteristic"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task Write(byte[] data)
        {
            if (CurrentWriteCharacteristic != null)
            {
                CurrentWriteCharacteristic.WriteValueAsync(CryptographicBuffer.CreateFromByteArray(data), GattWriteOption.WriteWithResponse);
            }

        }

        /// <summary>
        /// 设置通知特征对象。
        /// </summary>
        /// <returns></returns>
        public async Task GetCurrentNotifyCharacteristic()
        {
            string msg = "";
            Guid guid = new Guid(NotifyCharacteristicGuid);
            CurrentService.GetCharacteristicsForUuidAsync(guid).Completed = async (asyncInfo, asyncStatus) =>
            {
                if (asyncStatus == AsyncStatus.Completed)
                {
                    GattCharacteristicsResult result = asyncInfo.GetResults();
                    msg = "特征对象=" + CurrentDevice.ConnectionStatus;
                    ValueChanged(MsgType.NotifyTxt, msg, CurrentDevice == null ? null : CurrentDevice.DeviceId);
                    if (result.Characteristics.Count > 0)
                    {
                        CurrentNotifyCharacteristic = result.Characteristics[CHARACTERISTIC_INDEX];
                        CurrentNotifyCharacteristic.ProtectionLevel = GattProtectionLevel.Plain;
                        CurrentNotifyCharacteristic.ValueChanged += Characteristic_ValueChanged;
                        await EnableNotifications(CurrentNotifyCharacteristic);

                    }
                    else
                    {
                        msg = "没有发现特征对象,自动重试中";
                        ValueChanged(MsgType.NotifyTxt, msg, CurrentDevice == null ? null : CurrentDevice.DeviceId);
                        await GetCurrentNotifyCharacteristic();
                    }
                }
            };
        }

        /// <summary>
        /// 设置特征对象为接收通知对象
        /// </summary>
        /// <param name="characteristic"></param>
        /// <returns></returns>
        public async Task EnableNotifications(GattCharacteristic characteristic)
        {
            string msg = "收通知对象=" + CurrentDevice.ConnectionStatus;
            ValueChanged(MsgType.NotifyTxt, msg, CurrentDevice == null ? null : CurrentDevice.DeviceId);
            var cccdValue = GattClientCharacteristicConfigurationDescriptorValue.Notify;

             characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(cccdValue).Completed = async (asyncInfo, asyncStatus) =>
            {
                
                if (asyncStatus == AsyncStatus.Completed)
                {
                    GattCommunicationStatus status = asyncInfo.GetResults();
                    if (status == GattCommunicationStatus.Unreachable)
                    {
                        
                        msg = "设备不可用";
                        ValueChanged(MsgType.Error, msg, CurrentDevice == null ? null : CurrentDevice.DeviceId);
                        if (CurrentNotifyCharacteristic != null && !asyncLock)
                        {
                            await EnableNotifications(CurrentNotifyCharacteristic);
                        }
                    }
                    asyncLock = false;
                    msg = "设备连接状态" + status;
                    ValueChanged(MsgType.NotifyTxt, msg, CurrentDevice == null ? null : CurrentDevice.DeviceId);
                }
            };
        }

        private void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            byte[] data;
            CryptographicBuffer.CopyToByteArray(args.CharacteristicValue, out data);
            if(sender!=null)
            {
                //string str = BitConverter.ToString(data) + sender.Service.Device.DeviceId;
                string str = sender.Service.Device.DeviceId;
                var sData = BitConverter.ToString(data);
                ValueChanged(MsgType.BLEData, sData, str);
            }
           
            

        }

    }

    public enum MsgType
    {
        NotifyTxt,
        Error,
        BLEData
    }
}
