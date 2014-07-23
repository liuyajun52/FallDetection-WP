using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using FallDetection.Resources;
using Microsoft.Devices.Sensors;
using Microsoft.Xna.Framework;
using System.Windows.Media;

namespace FallDetection
{
    public partial class MainPage : PhoneApplicationPage
    {
        Motion motion;
        DataArray datas;
        const int DATA_LENGTH=200;
        float zThreshold = -1.0f; // z轴加速的的阈值
        int counter = 0; // 发现超出阈值之后计数的计数器
        bool isCounting = false; // 是否开始计数
        bool isListening = true;
        // 构造函数
        public MainPage()
        {
            InitializeComponent();
            
            // 用于本地化 ApplicationBar 的示例代码
            //BuildLocalizedApplicationBar();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if(e.NavigationMode== System.Windows.Navigation.NavigationMode.New&& NavigationContext.QueryString.ContainsKey("homeFromThird")) 
            { NavigationService.RemoveBackEntry();
                 NavigationService.RemoveBackEntry();// Remove ThirdPageNavigationService.RemoveBackEntry(); 
                 NavigationService.RemoveBackEntry();// Remove SecondPageNavigationService.RemoveBackEntry();
                 NavigationService.RemoveBackEntry();// Remove original MainPage
            }
            SwitchButton.Background = new SolidColorBrush(Colors.Red);
            if (!Motion.IsSupported)
            {
                MessageBox.Show("该设备不支持 Motion API");
                return;
            }
            // If the Motion object is null, initialize it and add a CurrentValueChanged
            // event handler.
            if (motion == null)
            {
                motion = new Motion();
                motion.TimeBetweenUpdates = TimeSpan.FromMilliseconds(5); //设置刷新频率
                motion.CurrentValueChanged +=
                    new EventHandler<SensorReadingEventArgs<MotionReading>>(motion_CurrentValueChanged);
            }

            // Try to start the Motion API.
            try
            {
                datas = new DataArray(DATA_LENGTH);
                motion.Start();
                isListening = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("无法启动 Motion API.");
            }
        }

        void motion_CurrentValueChanged(object sender, SensorReadingEventArgs<MotionReading> e)
        {
            // This event arrives on a background thread. Use BeginInvoke to call
            // CurrentValueChanged on the UI thread.
            Dispatcher.BeginInvoke(() => CurrentValueChanged(e.SensorReading));
        }

        private void CurrentValueChanged(MotionReading e)
        {
            // Check to see if the Motion data is valid.
            if (motion.IsDataValid)
            {
                //手机的重力数据
                float[] currentG=new float[3];
                currentG[0] = e.Gravity.X;
                currentG[1] = e.Gravity.Y;
                currentG[2] = e.Gravity.Z;


                //手机的相对坐标系加速度数据
                float[] currentA = new float[3];
                currentA[0] = e.DeviceAcceleration.X;
                currentA[1] = e.DeviceAcceleration.Y;
                currentA[2] = e.DeviceAcceleration.Z;

                //手机的姿态数据
                float xw =MathHelper.ToDegrees( e.Attitude.Pitch);
                float yw =MathHelper.ToDegrees( e.Attitude.Yaw);
                float zw =MathHelper.ToDegrees( e.Attitude.Roll);

                // z轴变换
                float tempx = currentA[0];
                float tempy = currentA[1];
                float tempz = currentA[2];

                currentA[0] = (float)(tempx * Math.Cos(zw) - tempy
                        * Math.Sin(zw));
                currentA[1] = (float)(tempx * Math.Sin(zw) + tempy
                        * Math.Cos(zw));

                tempx = currentA[0];
                tempy = currentA[1];
                tempz = currentA[2];
                // x轴变换
                currentA[1] = (float)(tempy * Math.Cos(xw) - tempz
                        * Math.Sin(xw));
                currentA[2] = (float)(tempy * Math.Sin(xw) + tempz
                        * Math.Cos(xw));

                tempx = currentA[0];
                tempy = currentA[1];
                tempz = currentA[2];
                // y轴变换
                currentA[2] = (float)(tempz * Math.Cos(yw) - tempx
                        * Math.Sin(yw));
                currentA[0] = (float)(tempz * Math.Sin(yw) + tempx
                        * Math.Cos(yw));

                DataStruct data = new DataStruct(e.Timestamp,currentA,currentG);
                datas.addData(data);

                if ((tempz<zThreshold||tempz>-zThreshold)&&!isCounting)
                {
                  
                    isCounting = true; // 当发现有数据超过阈值时开始计数
                }

               if (isCounting && ++counter == DATA_LENGTH / 2) {
				isCounting = false;
				DataAnalysiser analysiser = new DataAnalysiser(datas);
				bool analysisResult = analysiser.analysis();
				if (analysisResult) {
					onFirstWarming();
				}
				// 数据收集满了之后，将数据提交到数据分析模块
				datas = new DataArray(DATA_LENGTH); // 清空数据
				counter = 0;
			}
            }
        }

        private void onFirstWarming()
        {
            this.NavigationService.Navigate(new Uri("/WarmingPage.xaml", UriKind.Relative));
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new Uri("/SettingPage.xaml", UriKind.Relative));
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new Uri("/AboutPage.xaml", UriKind.Relative));
        }

        private void SwitchButton_Click(object sender, RoutedEventArgs e)
        {
            if (isListening)
            {
                isListening = false;
                motion.Stop();
                SwitchButton.Background=new SolidColorBrush(Colors.Blue);
            }
            else 
            {
                isListening = true;
                motion.Start();
                SwitchButton.Background = new SolidColorBrush(Colors.Red);
            }
        }

        private void GuideButton_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new Uri("/GuidePage.xaml",UriKind.Relative));
        }

        // 用于生成本地化 ApplicationBar 的示例代码
        //private void BuildLocalizedApplicationBar()
        //{
        //    // 将页面的 ApplicationBar 设置为 ApplicationBar 的新实例。
        //    ApplicationBar = new ApplicationBar();

        //    // 创建新按钮并将文本值设置为 AppResources 中的本地化字符串。
        //    ApplicationBarIconButton appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.add.rest.png", UriKind.Relative));
        //    appBarButton.Text = AppResources.AppBarButtonText;
        //    ApplicationBar.Buttons.Add(appBarButton);

        //    // 使用 AppResources 中的本地化字符串创建新菜单项。
        //    ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem(AppResources.AppBarMenuItemText);
        //    ApplicationBar.MenuItems.Add(appBarMenuItem);
        //}
    }
}