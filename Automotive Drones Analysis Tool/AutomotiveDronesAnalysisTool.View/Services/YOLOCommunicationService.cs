using Alturos.Yolo;
using Alturos.Yolo.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;

namespace AutomotiveDronesAnalysisTool.View.Services
{
    public class YOLOCommunicationService : ServiceBase
    {
        /// <summary>
        /// Sets the given widht and height to the YoloConfigs
        /// </summary>
        /// <param name="widthHeight"></param>
        public void SetWidthHeight(int widthHeight)
        {
            var configPath = ServiceContainer.GetService<GlobalEnviromentService>().YOLOConfigLocation;

            if (!File.Exists(configPath)) throw new FileNotFoundException($"Couldn't locate the YOLO configs at: {configPath}.");

            var oldLines = File.ReadAllLines(configPath);
            var newLines = new List<string>();
            foreach(var line in oldLines)
            {
                if (line.StartsWith("width"))
                {
                    newLines.Add($"width={widthHeight}");
                    continue;
                }
                if (line.StartsWith("height"))
                {
                    newLines.Add($"height={widthHeight}");
                    continue;
                }
                newLines.Add(line);
            }

            File.WriteAllLines(configPath, newLines);
        }

        /// <summary>
        /// Analyses a bitmap by detecting all the objects in it and returning them as a list.
        /// </summary>
        /// <param name="image"></param>
        public IEnumerable<YoloItem> DetectItemsInBitmap(Bitmap image)
        {
            var cfgPath = ServiceContainer.GetService<GlobalEnviromentService>().YOLOConfigLocation;
            var weightsPath = ServiceContainer.GetService<GlobalEnviromentService>().YOLOWeightsLocation;
            var namesPath = ServiceContainer.GetService<GlobalEnviromentService>().YOLONamesLocation;

            using (var yoloWrapper = new YoloWrapper(cfgPath, weightsPath, namesPath))
            {
                using (var memStream = new MemoryStream())
                {
                    image.Save(memStream, ImageFormat.Png);
                    var items = yoloWrapper.Detect(memStream.ToArray());
                    // We do NOT want sports ball as objects. 
                    // Later I can add a blacklist of all the objects that are not important to ignore them.
                    items = items.Where(i => i.Type != "sports ball").Select(i => i);

                    for (int i = 0; i < items.Count(); i++)
                        items.ElementAt(i).Type = $"object #{i}";

                    return items;
                }
            }
        }
    }
}
