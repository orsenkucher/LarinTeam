﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ProjectOxford.Face.Contract;
using Microsoft.ProjectOxford.Face;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;

namespace FaceRecognation._1._0
{
	public class MSAPIManager
	{
		//Singleton
		private static MSAPIManager _mSAPIManagerInstance;

		public static MSAPIManager MSAPIManagerInstance
		{
			get
			{
				if (_mSAPIManagerInstance == null)
				{
					_mSAPIManagerInstance = new MSAPIManager();
				}
				return _mSAPIManagerInstance;
			}
		}

		private MSAPIManager()
		{
			_faceServiceClient = new FaceServiceClient(KeyManager.Instance.MsPhotoKey);
		}

		private readonly IFaceServiceClient _faceServiceClient;

		public async void CreateFaceList(string faceListId, string faceListName)
		{
			try
			{
				await _faceServiceClient.CreateFaceListAsync(faceListId, faceListName);
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
			}
		}

		public async Task<AddPersistedFaceResult> AddFaceToFaceList(string faceListId, Stream imageAsStream)
		{
			try
			{
				var faceResult = await _faceServiceClient.AddFaceToFaceListAsync(faceListId, imageAsStream);
				if (faceResult == null) throw new Exception("AddPersistedFaceResult is null");
				return faceResult;
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
				return new AddPersistedFaceResult();
			}
		}

		private async Task<Microsoft.ProjectOxford.Face.Contract.Face[]> DetectFace(Stream imageAsStream)
		{
			try
			{
				var faces = await _faceServiceClient.DetectAsync(imageAsStream, true);
				if (faces.Length == 0) throw new Exception("FaceList is empty");
				return faces;
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
				return new Microsoft.ProjectOxford.Face.Contract.Face[0];
			}
		}

		public class FaceIdAndRect
		{
			public Guid FaceId { get; set; }
			public FaceRectangle FaceRect { get; set; }
			private FaceIdAndRect() { }
			public FaceIdAndRect(Guid faceId, FaceRectangle faceRectangle)
			{
				FaceId = faceId;
				FaceRect = faceRectangle;
			}
		}

		public async Task<FaceIdAndRect[]> GetFaceRectangle(Stream imageAsStream)
		{
			var faces = await DetectFace(imageAsStream);
			var faceIdAndRectList = new List<FaceIdAndRect>();
			foreach (var face in faces)
				faceIdAndRectList.Add(new FaceIdAndRect(face.FaceId, face.FaceRectangle));
			return faceIdAndRectList.ToArray();
		}

		public async Task<SimilarPersistedFace[]> CheckForSimilarity(FaceIdAndRect faceIdAndRect, string faceListId)
		{
			try
			{
				var similarFaces = await _faceServiceClient.FindSimilarAsync(faceIdAndRect.FaceId, faceListId);
				if (similarFaces.Length == 0) throw new Exception("There is no similar faces");
				return similarFaces;
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
				return new SimilarPersistedFace[0];
			}
		}
	}
}
