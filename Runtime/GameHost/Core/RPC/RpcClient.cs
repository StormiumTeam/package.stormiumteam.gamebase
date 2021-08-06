using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using GameHost.Core.RPC.Interfaces;
using LiteNetLib;
using LiteNetLib.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace GameHost.Core.RPC
{
	public abstract class RpcClient : IDisposable
	{
		public abstract void SendNotification<T>(T packet)
			where T : IGameHostRpcPacket;

		public abstract Task<TResponse> SendRequest<T, TResponse>(T packet)
			where T : IGameHostRpcWithResponsePacket<TResponse>
			where TResponse : IGameHostRpcResponsePacket;

		public abstract void Poll();

		public abstract void Dispose();

		private Dictionary<string, List<Action<JToken>>>              notificationEventMap = new Dictionary<string, List<Action<JToken>>>();
		private Dictionary<string, List<Func<JToken, Task<JObject>>>> callbackEventMap     = new Dictionary<string, List<Func<JToken, Task<JObject>>>>();

		public void SubscribeNotification<T>(Action<T> action)
			where T : IGameHostRpcPacket, new()
		{
			var n = new T();
			if (!notificationEventMap.ContainsKey(n.MethodName))
				notificationEventMap[n.MethodName] = new List<Action<JToken>>();

			notificationEventMap[n.MethodName].Add(json => action(json.ToObject<T>()));
		}

		public void SubscribeRequest<T>(Func<T, Task<JObject>> action)
			where T : IGameHostRpcPacket, new()
		{
			var n = new T();
			if (!callbackEventMap.ContainsKey(n.MethodName))
				callbackEventMap[n.MethodName] = new List<Func<JToken, Task<JObject>>>();

			callbackEventMap[n.MethodName].Add(json => action(json.ToObject<T>()));
		}

		protected void AddNotificationEvent(string methodName, JToken json)
		{
			if (!notificationEventMap.TryGetValue(methodName, out var eventList))
				return;

			foreach (var ac in eventList)
				ac(json);
		}

		protected Task<JObject> AddRequestEvent(string methodName, JToken json)
		{
			if (!callbackEventMap.TryGetValue(methodName, out var eventList))
				throw new InvalidOperationException($"no callback subscription for {methodName}");

			Task<JObject> result = null;
			foreach (var ac in eventList)
			{
				result = ac(json);
				if (result != null)
					break;
			}

			return result;
		}
	}

	public class LiteNetLibRpcClient : RpcClient
	{
		private NetManager  netManager;
		private RpcListener listener;

		private uint                                           callHandles;
		private Dictionary<uint, TaskCompletionSource<string>> outboundCalls;

		public LiteNetLibRpcClient()
		{
			netManager    = new NetManager(listener = new RpcListener(this));
			outboundCalls = new Dictionary<uint, TaskCompletionSource<string>>();
		}

		public void ConnectTo(IPEndPoint endPoint)
		{
			Console.WriteLine("connecting to " + endPoint.ToString());
			
			netManager.Start();
			netManager.Connect(endPoint, "GAMEHOST.CLIENT.V2");
		}

		public override void SendNotification<T>(T packet)
		{
			var jObject = new JObject(
				new JProperty("jsonrpc", "2.0"),
				new JProperty("method", packet.MethodName),
				new JProperty("params", JObject.FromObject(packet))
			);

			var netPacket = NetDataWriter.FromString(jObject.ToString());
			netManager.SendToAll(netPacket, DeliveryMethod.ReliableOrdered);
		}

		public override async Task<TResponse> SendRequest<T, TResponse>(T packet)
		{
			var id = ++callHandles;

			var jObject = new JObject(
				new JProperty("jsonrpc", "2.0"),
				new JProperty("method", packet.MethodName),
				new JProperty("id", id),
				new JProperty("params", JObject.FromObject(packet))
			);
			
			Debug.LogError(jObject.ToString(Formatting.Indented));

			var netPacket = NetDataWriter.FromString(jObject.ToString());
			netManager.SendToAll(netPacket, DeliveryMethod.ReliableOrdered);

			var resultTcs = new TaskCompletionSource<string>();
			outboundCalls[id] = resultTcs;

			var jsonResult = await resultTcs.Task;
			outboundCalls.Remove(id);
			return JsonConvert.DeserializeObject<TResponse>(jsonResult);
		}

		public override void Poll()
		{
			if (netManager.IsRunning)
				netManager.PollEvents();
		}

		public override void Dispose()
		{
			netManager.Stop();
			netManager = null;
			listener   = null;
		}

		internal void AddResponse(JToken methodProperty, JToken resultProperty, JToken idProperty)
		{
			if (!outboundCalls.TryGetValue(idProperty.ToObject<uint>(), out var tcs))
			{
				Debug.LogWarning($"jsonrpc - no requests made with id {idProperty.ToObject<uint>()} on method {methodProperty.ToObject<string>()}");
				return;
			}

			tcs.SetResult(resultProperty.ToString());
		}

		internal void AddRequestOrNotification(JToken methodProperty, JToken paramsProperty, JToken idProperty)
		{
			// Notification
			if (idProperty == null)
			{
				AddNotificationEvent(methodProperty.ToObject<string>(), paramsProperty);
				return;
			}

			AddRequestEvent(methodProperty.ToObject<string>(), paramsProperty).ContinueWith(json =>
			{
				var jObject = new JObject(
					new JProperty("jsonrpc", "2.0"),
					new JProperty("method", methodProperty.ToObject<string>()),
					new JProperty("id", idProperty.ToObject<int>()),
					new JProperty("result", json.Result)
				);

				var netPacket = NetDataWriter.FromString(jObject.ToString());
				netManager.SendToAll(netPacket, DeliveryMethod.ReliableOrdered);
			});
		}

		internal void AddError(JToken errorProperty, JToken idProperty)
		{
			if (outboundCalls.TryGetValue(idProperty.ToObject<uint>(), out var tcs))
				tcs.SetException(new JsonRpcException(errorProperty["code"].ToObject<int>(), errorProperty["message"].ToObject<string>()));
			else
			{
				Debug.LogWarning($"jsonrpc - error found with id {idProperty.ToObject<uint>()} but no request has been made with it.");
			}
		}

		public void SubscribeConnected(Action onConnected)
		{
			listener.Connected += onConnected;
		}
	}

	public class JsonRpcException : Exception
	{
		public int    Code;
		public string Message;

		public JsonRpcException(int code, string message) : base($"{code} -> {message}")
		{
			Code    = code;
			Message = message;
		}
	}
}