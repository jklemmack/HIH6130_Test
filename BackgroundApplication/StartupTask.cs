using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;
using System.Threading.Tasks;
using Windows.System.Threading;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace BackgroundApplication
{
    public sealed class StartupTask : IBackgroundTask
    {
        I2cDevice sensor;
        private ThreadPoolTimer timer;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            try
            {
                // initialize I2C communications
                string deviceSelector = I2cDevice.GetDeviceSelector();
                var i2cDeviceControllers = await DeviceInformation.FindAllAsync(deviceSelector);

                var i2cSettings = new I2cConnectionSettings(0x27);
                i2cSettings.BusSpeed = I2cBusSpeed.FastMode;
                sensor = await I2cDevice.FromIdAsync(i2cDeviceControllers[0].Id, i2cSettings);

                Task.Delay(2000).Wait();
                timer = ThreadPoolTimer.CreatePeriodicTimer(Timer_Tick, TimeSpan.FromMilliseconds(5000));

                Task.Delay(-1).Wait();
            }
            catch (Exception ex)
            {

            }
        }

        private void Timer_Tick(ThreadPoolTimer timer)
        {
            double RH, T_C;
            GetTemperatureAndHumidity(out RH, out T_C);
            System.Diagnostics.Debug.WriteLine("{0:F2}% {1:F2} C", RH, T_C);
        }

        private void GetTemperatureAndHumidity(out double RH, out double T_C)
        {
            RH = 0d;
            T_C = 0d;
            try
            {
                byte[] write = new byte[] { (0x27 << 1) | 0x01 };
                byte[] read = new byte[4];

                // Inspired by http://www.phanderson.com/arduino/hih6130.html
                // --------------------------------------------------------------
                // copyright, Peter H Anderson, Baltimore, MD, Nov, '11
                // You may use it, but please give credit.  
                // --------------------------------------------------------------
                sensor.WriteRead(write, read);

                byte Hum_H = read[0], Hum_L = read[1], Temp_H = read[2], Temp_L = read[3];

                byte _status = (byte)((Hum_H >> 0x06) & 0x03);  // Get top 2 bytes
                Hum_H = (byte)(Hum_H & 0x3f);                   // Get lower 6 bytes
                UInt16 H_Dat = (UInt16)((Hum_H << 8) | Hum_L);
                UInt16 T_Dat = (UInt16)((Temp_H << 8) | Temp_L);
                T_Dat = (UInt16)(T_Dat >> 2);

                RH = H_Dat * 6.10e-3;
                T_C = T_Dat * 1.007e-2 - 40.0;
            }
            catch (Exception ex)
            {

            }

        }
    }
}
