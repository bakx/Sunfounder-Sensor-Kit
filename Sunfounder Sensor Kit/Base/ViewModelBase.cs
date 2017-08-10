using System.ComponentModel;
using Windows.Devices.Gpio;
using Windows.Devices.Pwm;
using FD.Base;

namespace SunfounderSensorKit.Base
{
    public class ViewModelBase : CommandBase, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}