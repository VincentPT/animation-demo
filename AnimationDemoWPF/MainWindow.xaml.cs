using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Animation;

namespace AnimationDemoWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            animationView1.AnimationController  = new Animation.ThreadingFrameController();
            animationView2.AnimationController = new Animation.TimingFrameController();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            animationView1.Start();
            animationView2.Start();
            playPauseButton.IsEnabled = true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            animationView1.Stop();
            animationView1.Stop();
        }        
        
        private void playPauseButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if ("Play".Equals(button.Content))
            {
                button.Content = "Pause";
                animationView1.Play();
                animationView2.Play();
            }
            else
            {
                button.Content = "Play";

                animationView1.Pause();
                animationView2.Pause();
            }
        }
    }
}
