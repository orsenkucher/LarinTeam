﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ProjectOxford.Face.Contract;
using Microsoft.ProjectOxford.Video;
using Microsoft.ProjectOxford.Video.Contract;
using System.IO;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Drawing;
using MediaToolkit;
using MediaToolkit.Options;
using MediaToolkit.Model;

namespace FaceRecognation._1._0
{
    public static class VideoManager
    {
        private static VideoServiceClient videoServiceClient = new VideoServiceClient(KeyManager.Instance.MsVideoKey);
        private static double TimeScale;
        private static int VideoWidth;
        private static int VideoHeight;
        private static string CurrentVideoPath;


        private static async Task<FaceDetectionResult> getFaceDetectionAsync(string filePath)
        {
            Operation videoOperation;
            using (var fs = new FileStream(filePath, FileMode.Open))
            {
                videoOperation = await videoServiceClient.CreateOperationAsync(fs, new FaceDetectionOperationSettings());
            }
            OperationResult operationResult;
            while (true)
            {
                operationResult = await videoServiceClient.GetOperationResultAsync(videoOperation);
                if (operationResult.Status == OperationStatus.Succeeded || operationResult.Status == OperationStatus.Failed)
                {
                    break;
                }

                Task.Delay(30000).Wait();
            }
            var faceDetectionTrackingResultJsonString = operationResult.ProcessingResult;
            var faceDetecionTracking = JsonConvert.DeserializeObject<FaceDetectionResult>(faceDetectionTrackingResultJsonString);
            TimeScale = faceDetecionTracking.Timescale;
            return faceDetecionTracking;
        }

        private static Dictionary<int, CoolEvent> getCoolEvents(FaceDetectionResult faceDetectionTracking)
        {
            //List<int> IDs = faceDetectionTracking.FacesDetected.Select(x => x.FaceId).ToList();

            var Fragments = faceDetectionTracking.Fragments.Where(x => x.Events != null).ToArray();

            var idDict = getDictionary(Fragments);
            return idDict;
        }

        private static List<Image> getFacesFromCoords(Dictionary<int, Fragment<FaceEvent>> faceCoordinates)
        {
            List<Image> CroppedFaces = new List<Image>();

           
            return CroppedFaces;
        }

        private static void getFrame(string path, double startTime, int id)
        {
            if (!Directory.Exists("TempData"))
                Directory.CreateDirectory("TempData");

            var inputFile = new MediaFile() { Filename = path };
            var outputFile = new MediaFile() { Filename = $@"TempData/{id}.png" };
            
            using (var engine = new Engine())
            {
                engine.GetMetadata(inputFile);
                var options = new ConversionOptions() { Seek = TimeSpan.FromMilliseconds(startTime) };
                engine.GetThumbnail(inputFile, outputFile, options);
            }
        }

        class CoolEvent
        {
            public FaceRectangle rec = new FaceRectangle();
            public long startTime;
            public int Id;
        }

        private static Dictionary<int, CoolEvent> getDictionary(Fragment<FaceEvent>[] fragments)
        {
            Dictionary<int, List<CoolEvent>> dic = new Dictionary<int, List<CoolEvent>>();

            foreach(var fragment in fragments)
            {
                var startTime = fragment.Start;
                var interval = fragment.Interval;
                
                for (int momentId = 0; momentId < fragment.Events.Length; momentId++)
                {
                    long time = startTime + momentId * (long)interval;
                    foreach(var face in fragment.Events[momentId])
                    {
                        CoolEvent faceEvent = new CoolEvent
                        {
                            Id = face.Id,
                            startTime = time
                        };

                        faceEvent.rec.Height = Convert.ToInt32(VideoHeight * face.Height);
                        faceEvent.rec.Width = Convert.ToInt32(VideoWidth * face.Width);
                        faceEvent.rec.Left = Convert.ToInt32(VideoWidth * face.X);
                        faceEvent.rec.Top = Convert.ToInt32(VideoHeight * face.Y);

                        if (faceEvent.rec.Height + faceEvent.rec.Top > VideoHeight)
                            faceEvent.rec.Height = VideoHeight - faceEvent.rec.Top;
                        if (faceEvent.rec.Width + faceEvent.rec.Left > VideoWidth)
                            faceEvent.rec.Width = VideoWidth - faceEvent.rec.Left;

                        if (!dic.Keys.Contains(faceEvent.Id))
                            dic[faceEvent.Id] = new List<CoolEvent>();
                        dic[faceEvent.Id].Add(faceEvent);
                    }
                }
            }
            Dictionary<int, CoolEvent> coolDic = new Dictionary<int, CoolEvent>();
            foreach(var key in dic.Keys)
            {
                coolDic[key] = dic[key][dic[key].Count / 3];
            }
            return coolDic;
        }

        private static void setVideoResol()
        {
            MediaFile inputFile = new MediaFile() { Filename = CurrentVideoPath };
            MediaFile testFile = new MediaFile() { Filename = "DeleteIt.png" };
            Engine eng = new Engine();
            eng.GetMetadata(inputFile);

            var FrameSize = inputFile.Metadata.VideoData.FrameSize;
            if (FrameSize == null) FrameSize = "1920x1080";

            var options = new ConversionOptions() { Seek = TimeSpan.FromSeconds(0) };
            eng.GetThumbnail(inputFile, testFile, options);
            Image testImage = ImageProcessing.ImageProcessingInstance.LoadImageFromFile("DeleteIt.png");
            VideoWidth = testImage.Width;
            VideoHeight = testImage.Height;
        }

        public static async void getFacesFromVideo(string path)
        {
            CurrentVideoPath = path;
            setVideoResol();

            videoServiceClient.Timeout = TimeSpan.FromMinutes(5);
            FaceDetectionResult faceDetectionResult = await getFaceDetectionAsync(path);

            Debug.WriteLine("Got FDR!!!!)))");
            Dictionary<int, CoolEvent> FaceIds = getCoolEvents(faceDetectionResult);

            foreach (int id in FaceIds.Keys)
            {
                var curEvent = FaceIds[id];
                var startTimeMili = curEvent.startTime / TimeScale * 1000;
                getFrame(path, startTimeMili, id);
                
                var img = ImageProcessing.ImageProcessingInstance.LoadImageFromFile($@"TempData/{id}.png");
                img = ImageProcessing.ImageProcessingInstance.CropImage(img, FaceIds[id].rec);
                ImageProcessing.ImageProcessingInstance.SaveImageToFile($@"TempData/{id}Face.png", img, System.Drawing.Imaging.ImageFormat.Png);
                //File.Delete($@"TempData/{id}.png");
            }
        }
    }
}
