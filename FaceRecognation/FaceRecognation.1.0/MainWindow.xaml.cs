﻿using System;
using System.Collections.Generic;
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
using Newtonsoft.Json;
using Microsoft.ProjectOxford.Common;
using Microsoft.ProjectOxford.Face;
using System.IO;
using System.Diagnostics;
using System.Drawing;
using System.Threading;

namespace FaceRecognation._1._0
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			XTests test = new XTests();
			test.Run();
		}

		private MSAPIManager _msapiManager = MSAPIManager.MSAPIManagerInstance;
		private ImageProcessing _imgProcessing = ImageProcessing.ImageProcessingInstance;
		private List<System.Drawing.Image> _faces = new List<System.Drawing.Image>();
		private string _videoPath;
		private void cmdTakePhoto_Click(object sender, RoutedEventArgs e)
		{
			var openDlg = new Microsoft.Win32.OpenFileDialog();

			openDlg.Filter = "JPEG Image(*.jpg)|*.jpg|PNG Image(*.png)|*.png|MP4 Video(*.mp4)|*.mp4";
			bool? result = openDlg.ShowDialog(this);

			if (!(bool)result)
			{
				return;
			}

			string filePath = openDlg.FileName;

			// photo
			//var photo = System.Drawing.Image.FromFile(filePath);
			//_faces.Add(photo);
			//spTakenPhotos.Children.Add(GenerateImg(photo));

			// video
			_videoPath = filePath;
		}

		private System.Windows.Controls.Image GenerateImg(System.Drawing.Image photo)
		{
			return new System.Windows.Controls.Image
			{
				Source = _imgProcessing.ConvertImageToBitmapImage(photo),
				Height = 120,
				Width = 120,
				Stretch = Stretch.Uniform,
				Margin = new Thickness(7)
			};
		}

		private List<Guid> _persistedIds = new List<Guid>();
		private async void cmdAddFace_Click(object sender, RoutedEventArgs e)
		{
			(sender as Button).Content = "Adding...";
			foreach (var face in _faces)
			{
				var pres = await _msapiManager.AddFaceToFaceList(_facelistId, _imgProcessing.ImageToStream(face));
				_persistedIds.Add(pres.PersistedFaceId);
			}
			(sender as Button).Content = "Added successfuly.";
			await Task.Delay(TimeSpan.FromSeconds(1));
			(sender as Button).Content = "Add Taken faces tL";
		}

		private List<MSAPIManager.FaceIdAndRect> _faceIdAndRectList = new List<MSAPIManager.FaceIdAndRect>();
		private class FacesSelectedCounter
		{
			public event EventHandler OnAllWindowsClosed;
			private int _closedCounter;
			private int _maxWindows;
			private FacesSelectedCounter() { }
			public FacesSelectedCounter(int maxWindows)
			{
				_maxWindows = maxWindows;
			}
			public void IncCounter()
			{
				_closedCounter++;
				if (_closedCounter == _maxWindows)
					OnAllWindowsClosed?.Invoke(this, new EventArgs());
			}
		}

		private async void cmdDetectFace_Click(object sender, RoutedEventArgs e)
		{
			(sender as Button).Content = "Detecting...";

			// Detecting for Photos -------------------------------------------------
			//var res = new List<System.Drawing.Image>();
			//foreach (var photo in _faces)
			//{
			//	var faces = await _msapiManager.GetFaceRectangle(_imgProcessing.ImageToStream(photo));
			//	if (faces.Length == 0) continue;
			//	foreach (var face in faces)
			//	{
			//		var croppedFace = _imgProcessing.CropImage(photo, face.FaceRect);
			//		_faceIdAndRectList.Add(face);
			//		res.Add(croppedFace);
			//	}
			//}
			//_faces = res;
			//spTakenPhotos.Children.Clear();
			//foreach (var face in _faces)
			//	spTakenPhotos.Children.Add(GenerateImg(face));
			// ----------------------------------------------------------------------

			// Detecting for Videos -------------------------------------------------
			var faces4eachPerson = await VideoManager.getFacesFromVideo(_videoPath);
			var faceCounter = new FacesSelectedCounter(faces4eachPerson.Count);
			faceCounter.OnAllWindowsClosed += (so, a) =>
			{
				spTakenPhotos.Children.Clear();
				foreach (var face in _faces)
					spTakenPhotos.Children.Add(GenerateImg(face));
			};
			foreach (var person in faces4eachPerson)
			{
				var winSf = new windowSelectFace(person.Value);
				winSf.OnFaceSelected += (s, args) =>
				{
					_faces.Add(args.Face);
					faceCounter.IncCounter();
				};
				winSf.Show();
			}
			// ----------------------------------------------------------------------

			(sender as Button).Content = "Detected successfuly.";
			await Task.Delay(TimeSpan.FromSeconds(3));
			(sender as Button).Content = "Detect Faces";
		}

		private void cmdClearCache_Click(object sender, RoutedEventArgs e)
		{
			_imgProcessing.ClearCache();
			_faces = new List<System.Drawing.Image>();
			spTakenPhotos.Children.Clear();
			lbCompResults.Items.Clear();
		}

		private async void cmdFindSimilar_Click(object sender, RoutedEventArgs e)
		{
			(sender as Button).Content = "Comparing...";
			if (_faces.Count < 2) return;
			var compResult = await _msapiManager.CheckForSimilarity(_faceIdAndRectList.First(), _facelistId);
			foreach (var r in compResult)
				lbCompResults.Items.Add($"ID: {r.PersistedFaceId}; Conf: {r.Confidence:f}");
			(sender as Button).Content = "Compared successfuly.";
			await Task.Delay(TimeSpan.FromSeconds(1));
			(sender as Button).Content = "Compare faces";
		}

		private string _facelistId = "facelist0";
		private async void cmdCreateFaceList_Click(object sender, RoutedEventArgs e)
		{
			(sender as Button).Content = "Deleting old list...";
			_msapiManager.ATL_ACIDHOUZE(_facelistId);
			(sender as Button).Content = "Creating...";
			_msapiManager.CreateFaceList(_facelistId, "Lace list 0");
			(sender as Button).Content = "Created.";
			await Task.Delay(TimeSpan.FromSeconds(1));
			(sender as Button).Content = "Create FaceList";
		}
	}

	public class XTests
	{

		public void Run()
		{
			//Debug.WriteLine("KEK");
			//VideoManager.getFacesFromVideo("1.mp4");
		}
	}
}
