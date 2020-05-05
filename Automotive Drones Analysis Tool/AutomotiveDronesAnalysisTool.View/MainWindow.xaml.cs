using MetadataExtractor;
using MetadataExtractor.Formats.Xmp;
using System;
using System.Collections.Generic;
using System.Drawing;
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
using XmpCore;

namespace AutomotiveDronesAnalysisTool.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Testing
            // Get the EXIF metadata
            var list = new StringBuilder();
            var directories = ImageMetadataReader.ReadMetadata("E:\\WPF Projects\\Automotive Drones Analysis Tool\\Daten_automatisches_Fahren\\Situation_1_Auswahl\\Situation_1_nah.JPG");
            foreach (var directory in directories)
                foreach (var tag in directory.Tags)
                    list.AppendLine($"{directory.Name} - {tag.Name} = {tag.Description}");

            // Get the XMP meta data
            var xmpProps = directories.OfType<XmpDirectory>().FirstOrDefault().GetXmpProperties();
            foreach (var pair in xmpProps)
                list.AppendLine($"{pair.Key} - {pair.Value}");

            //File.WriteAllText("E:\\WPF Projects\\Automotive Drones Analysis Tool\\Daten_automatisches_Fahren\\Situation_1_Auswahl\\Situation_1_nah_metadata.txt", list.ToString());
        }
    }
}
