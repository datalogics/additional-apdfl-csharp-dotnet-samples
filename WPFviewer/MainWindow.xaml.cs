using System.Windows;
using System.Windows.Controls.Primitives;
using SkiaSharp.Views.WPF;
using Datalogics.PDFL;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using System.Windows.Media;


namespace WPFviewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const double resolution = 96.0; // standard resolution.
        string? filename = null;
        private readonly Library lib;
        private Document? doc;
        int currentPage = 0;
        
        private System.Windows.Media.Imaging.WriteableBitmap wbmp;
        
        private bool dragStarted = false;

        private void Slider_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            Page_display();
            this.dragStarted = false;
        }

        private void Slider_DragStarted(object sender, DragStartedEventArgs e)
        {
            this.dragStarted = true;
        }

        private void Slider_ValueChanged(
            object sender,
            RoutedPropertyChangedEventArgs<double> e)
        {
            if (!dragStarted)
                Page_display();
        }
        public string Page_Num{ get { return currentPage.ToString(); } }

        public MainWindow()
        {
            InitializeComponent();
            lib = new Library();
            filename = null;
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            if (doc != null)
            {
                doc.Close();
                doc.Dispose();
                doc = null;
            }
            lib.Dispose();
        }

        private void OnLoad(object sender, RoutedEventArgs e)
        {
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            // Configure open file dialog box
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                FileName = "Document", // Default file name
                DefaultExt = ".pdf", // Default file extension
                Filter = "PDF documents (.pdf)|*.pdf" // Filter files by extension
            };

            // Show open file dialog box
            bool? result = dialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                filename = dialog.FileName;
                doc = new Document(filename);
                currentPage = 0;
                PageNum.Text = String.Format("{0} of {1} Pages", currentPage + 1, doc.NumPages);
                ResolutionControl.Value = 1.0;
                
                Page_display();
            }
        }

        private void Page_display()
        {
            if (doc == null)
                return;

            double curResolution = ResolutionControl.Value * resolution;
            PageImageParams pip = new()
            {
                VerticalResolution = curResolution,
                HorizontalResolution = curResolution,
                PageDrawFlags = DrawFlags.DoLazyErase | DrawFlags.UseAnnotFaces,
                ThinLineHeuristics = true,
                Smoothing = SmoothFlags.Text | SmoothFlags.LineArt | SmoothFlags.Image
            };

            using var pg = doc.GetPage(currentPage);
            using Datalogics.PDFL.Image pgImage = pg.GetImage(pg.CropBox, pip);
            wbmp = WPFExtensions.ToWriteableBitmap(pgImage.SKBitmap);
            PageViewer.Source = wbmp;
        }

         private void Back_Click(object sender, RoutedEventArgs e)
        {
            currentPage = Math.Max(0,currentPage - 1);
            if (doc != null)
            {
                PageNum.Text = String.Format("{0} of {1} Pages", currentPage + 1, doc.NumPages);
                Page_display();
            }
        }

        private void Forward_Click(object sender, RoutedEventArgs e)
        {
            if (doc != null)
            {
                currentPage = Math.Min(doc.NumPages-1, currentPage + 1);
                PageNum.Text = String.Format("{0} of {1} Pages",currentPage+1,doc.NumPages);
                Page_display();
            }
        }
        
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            PageNum.Text = "";
            if (doc != null)
            {
                doc.Close();
                doc.Dispose();
                doc = null;
            }
            filename = null;
            wbmp = new WriteableBitmap((int)PageViewer.ActualWidth, (int)PageViewer.ActualHeight, 96, 96, PixelFormats.Bgra32, BitmapPalettes.Halftone256Transparent);
            PageViewer.Source = wbmp;
        }
    }
}