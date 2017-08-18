using SunfounderSensorKit.ViewModels;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SunfounderSensorKit
{
    /// <summary>
    ///     An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage
    {
        public MainPage()
        {
            InitializeComponent();

            //new DualLed01();
//            new Test();
            new Thermistor80();
            //new PassiveBuzzer10();
            //new RgbLed02();
            //DataContext = new I2CLcd160230();
        }
    }
}