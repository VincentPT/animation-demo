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
    /// Interaction logic for AnimationsView.xaml
    /// </summary>
    public partial class AnimationsView : UserControl
    {
        sealed class ImageSourceSetter : Animation.FrameHandler
        {
            internal Image ImageControl;
            public void OnIncommingFrame(FrameStream frameStream)
            {
                ImageControl.Source = frameStream.CurrentFrame;
            }
        }
        Animation.FrameController animationController;
        public Animation.FrameController AnimationController { get => animationController; set => animationController = value; }

        public AnimationsView()
        {
            InitializeComponent();
        }

        private void Canvas_Drop(object sender, DragEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Canvas_Drop " + this.Name);
            allowDrop = true;
            if (dragImage != null)
            {
                dragImage.Opacity = 1.0f;
                Animation.FrameAnimation animation = new Animation.FrameAnimation(gifSource);
                animation.Loop = true;
                if (animationController != null)
                {
                    animationController.AddAnimation(animation, new ImageSourceSetter { ImageControl = dragImage });
                }
            }
            gifSource = null;
            dragImage = null;
        }

        private void Canvas_DragLeave(object sender, DragEventArgs e)
        {
            allowDrop = true;
            gifSource = null;
            if (dragImage != null)
            {
                myCanvas.Children.Remove(dragImage);
            }
            dragImage = null;
            System.Diagnostics.Debug.WriteLine("Canvas_DragLeave " + this.Name);
        }

        bool allowDrop = true;
        Animation.KeyFramesAnimation gifSource;
        Image dragImage = null;

        private void Canvas_DragOver(object sender, DragEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Canvas_DragOver " + this.Name);
            e.Effects = allowDrop ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
            if (gifSource != null && gifSource.KeyFrames.Length > 0 && dragImage == null)
            {
                dragImage = new Image();
                dragImage.Opacity = 0.25;
                dragImage.Source = gifSource.KeyFrames[0].Frame;
                dragImage.IsHitTestVisible = false;
                myCanvas.Children.Add(dragImage);
            }
            if (dragImage != null)
            {
                var position = e.GetPosition(myCanvas);
                Canvas.SetLeft(dragImage, position.X - gifSource.FrameWidth / 2);
                Canvas.SetTop(dragImage, position.Y - gifSource.FrameHeight / 2);
            }
        }

        private void Canvas_DragEnter(object sender, DragEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Canvas_DragEnter " + this.Name);

            string gifFile = GetSingleGifFile(e);
            allowDrop = gifFile != null;
            e.Effects = allowDrop ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;

            if (allowDrop)
            {
                var decodeGifTask = Animation.Gif.GifDecodeHelper.DecodeFramesAysnc(gifFile);
                decodeGifTask.ContinueWith((t) => gifSource = t.Result);
            }
        }

        // If the data object in args is a single file, this method will return the filename.
        // Otherwise, it returns null.
        private string GetSingleGifFile(DragEventArgs args)
        {
            // Check for files in the hovering data object.
            if (args.Data.GetDataPresent(DataFormats.FileDrop, true))
            {
                var fileNames = args.Data.GetData(DataFormats.FileDrop, true) as string[];
                // Check fo a single file or folder.
                if (fileNames.Length == 1)
                {
                    var fileName = fileNames[0];
                    // Check for a file (a directory will return false).
                    if (File.Exists(fileName))
                    {
                        // At this point we know there is a single file.
                        var ext = System.IO.Path.GetExtension(fileName).ToLower();
                        if (ext == ".gif")
                        {
                            return fileName;
                        }
                    }
                }
            }
            return null;
        }

        public void Play()
        {
            if (animationController != null)
            {
                animationController.Play();
            }
        }

        public void Pause()
        {
            if (animationController != null)
            {
                animationController.Pause();
            }
        }

        public void Start()
        {
            if (animationController != null)
            {
                animationController.Start();
            }
        }

        public void Stop()
        {
            if (animationController != null)
            {
                animationController.Stop();
            }
        }
    }
}
