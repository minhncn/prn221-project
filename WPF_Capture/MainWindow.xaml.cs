using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Emgu.CV.Structure;
using Firebase.Storage;
using System.IO;

namespace WPF_Capture
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private VideoCapture _capture;
        private ObservableCollection<ImageInfo> imageList = new ObservableCollection<ImageInfo>();

        public class ImageInfo
        {
            public ImageSource ImageSource { get; set; }
            public string FilePath { get; set; }
        }
        public MainWindow()
        {
            InitializeComponent();
            InitializeCamera();
        }

        public void InitializeCamera()
        {
            _capture = new VideoCapture();
            _capture.ImageGrabbed += ProcessFrame;
            _capture.Start();
        }

        public void ProcessFrame(object sender, EventArgs e)
        {
            try
            {
                Mat frame = new Mat();
                _capture.Retrieve(frame);

                imageControl.Dispatcher.Invoke(() =>
                {
                    imageControl.Source = ToBitmapSource(frame);
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Something went wrong");
            }

        }

        public static BitmapSource ToBitmapSource(Mat frame)
        {
            BitmapSource bitmapSource = null;
            try
            {
                using (Image<Bgr, byte> image = frame.ToImage<Bgr, byte>())
                {
                    System.Drawing.Bitmap bitmap = image.ToBitmap();

                    using (System.IO.MemoryStream memory = new System.IO.MemoryStream())
                    {
                        bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                        memory.Position = 0;

                        BitmapImage bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.StreamSource = memory;
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.EndInit();
                        bitmapImage.Freeze();

                        //Set BitmapImage as the Source of Image
                        bitmapSource = bitmapImage;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Cannot convert Mat to BitmapSource: " + ex.Message);
            }
            return bitmapSource;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_capture != null && _capture.IsOpened)
            {
                _capture.Stop();
            }
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Title = "Select a folder to save the image";
            openFileDialog.Filter = "All Files (*.*)|*.*";
            openFileDialog.CheckFileExists = false;
            openFileDialog.CheckPathExists = true;
            openFileDialog.FileName = "Select Folder";

            bool? result = openFileDialog.ShowDialog();

            if (result == true)
            {
                // Display the selected folder path in the TextBox
                txtAddress.Text = System.IO.Path.GetDirectoryName(openFileDialog.FileName);
            }
        }

        private void btnCapture_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtAddress.Text))
            {
                MessageBox.Show("Please select folder");
                return;
            }
            Mat captureImage = new Mat();
            _capture.Retrieve(captureImage);

            //Create file dialog
            string fileName = System.IO.Path.Combine(txtAddress.Text, DateTime.Now.ToString("yyyyMMddHHmmss") + ".jpg");
            captureImage.Save(fileName);

            //Create image info object
            ImageInfo imageInfo = new ImageInfo()
            {
                ImageSource = ToBitmapSource(captureImage),
                FilePath = fileName
            };

            //Add to list
            imageList.Add(imageInfo);

            //Set to list view
            listView.ItemsSource = null;
            listView.ItemsSource = imageList;

            // After capturing the image, upload it to Firebase
            UploadImage(fileName);
        }

        private async void UploadImage(string filePath)
        {
            var firebase = new FirebaseStorage("prn221-f5853.appspot.com")
                .Child("folderName")
                .Child(System.IO.Path.GetFileName(filePath)); // use the actual file name

            var stream = File.Open(filePath, FileMode.Open);
            var task = firebase.PutAsync(stream);
            task.Progress.ProgressChanged += (s, e) => Console.WriteLine($"Progress: {e.Percentage} %");
            var downloadUrl = await task;
        }

        private async void listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Get the selected item
            var selectedItem = (ImageInfo)listView.SelectedItem;

            if (selectedItem != null)
            {
                MessageBoxResult result = MessageBox.Show("Do you want to delete this file?", "Confirmation", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    imageList.Remove(selectedItem);
                    File.Delete(selectedItem.FilePath);
                    var firebase = new FirebaseStorage("prn221-f5853.appspot.com")
                        .Child("folderName")
                        .Child(System.IO.Path.GetFileName(selectedItem.FilePath)); // use the actual file name
                    await firebase.DeleteAsync();

                    listView.ItemsSource = null;
                    listView.ItemsSource = imageList;
                }
            }
        }
    }
}
