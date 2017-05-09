﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaceRecognition.Core
{
	public class MessageManager
	{
		private static Lazy<MessageManager> _mmInstance = new Lazy<MessageManager>(() => new MessageManager());
		public static MessageManager MsgManagerInstance { get { return _mmInstance.Value; } }

		private MessageManager() { }

		public event EventHandler<string> OnMessageSended;
		public void WriteMessage(string msg)
		{
			OnMessageSended?.Invoke(this, msg);
		}
	}
}
