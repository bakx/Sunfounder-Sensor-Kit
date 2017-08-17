using SunfounderSensorKit.Base;

namespace SunfounderSensorKit.Library
{
    public class TemperatureModel : ViewModelBase
    {
        private double temperature;
        private bool started;
        private string systemSymbol = "C";
        private string statusText = "";

        public double Temperature
        {
            get => temperature;
            set
            {
                if (value != temperature)
                {
                    temperature = value;
                    RaisePropertyChanged(nameof(Temperature));
                }
            }
        }

        public bool Started
        {
            get => started;
            set
            {
                if (value != started)
                {
                    started = value;
                    RaisePropertyChanged(nameof(Stopped));
                }
            }
        }
        public bool Stopped => !Started;

        public string StatusText
        {
            get => statusText;
            set
            {
                if (value != statusText)
                {
                    statusText = value;
                    RaisePropertyChanged(nameof(StatusText));
                }
            }
        }

        public string SystemSymbol
        {
            get => systemSymbol;
            set
            {
                if (value != systemSymbol)
                {
                    systemSymbol = value;
                    RaisePropertyChanged(nameof(SystemSymbol));
                }
            }
        }
    }
}
