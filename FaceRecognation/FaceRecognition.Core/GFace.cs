﻿using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaceRecognition.Core
{
	public class GFace // Generalizing not G )0)) (G - govno)
	{
		public Image Img { get; set; }
		public AddPersistedFaceResult FaceIdOnAdding { get; set; }
	}

	public class GPerson
	{
		public int PersonLocalId { get; set; }
		public string PersonGroupId { get; set; }
		public CreatePersonResult PersonCreationId { get; set; }
		public List<GFace> Faces { get; set; } = new List<GFace>();
	}
}