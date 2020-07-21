using System;
using GameHost.Core.IO;
using GameHost.InputBackendFeature.Components;
using GameHost.InputBackendFeature.Layouts;
using GameHost.Transports.enet;
using GameHost.Transports.Transports.ENet;
using RevolutionSnapshot.Core.Buffers;
using Unity.Entities;
using UnityEngine;

namespace GameHost.InputBackendFeature
{
	public class CreateGameHostInputBackendSystem : SystemBase
	{
		private InputBackendSystem   inputBackendSystem;
		private RegisterLayoutSystem layoutSystem;
		private ENetTransportDriver  driver;

		protected override void OnCreate()
		{
			base.OnCreate();
			inputBackendSystem = World.GetExistingSystem<InputBackendSystem>();
			layoutSystem       = World.GetExistingSystem<RegisterLayoutSystem>();
			driver             = new ENetTransportDriver(32);
		}

		protected override void OnUpdate()
		{
			driver.Update();

			while (driver.Accept().IsCreated)
			{
			}

			TransportEvent ev;
			while ((ev = driver.PopEvent()).Type != TransportEvent.EType.None)
			{
				switch (ev.Type)
				{
					case TransportEvent.EType.None:
						break;
					case TransportEvent.EType.RequestConnection:
						break;
					case TransportEvent.EType.Connect:
						break;
					case TransportEvent.EType.Disconnect:
						break;
					case TransportEvent.EType.Data:
						var reader = new DataBufferReader(ev.Data);
						var type   = (EMessageType) reader.ReadValue<int>();
						Console.WriteLine($"received {type}");
						switch (type)
						{
							case EMessageType.InputData:
							{
								var inputDataReader = new DataBufferReader(reader, reader.CurrReadIndex, reader.Length);
								var subType         = (EMessageInputType) inputDataReader.ReadValue<int>();
								switch (subType)
								{
									case EMessageInputType.Register:
									{
										OnRegisterLayoutActions(ev.Connection, ref inputDataReader);
										break;
									}
									case EMessageInputType.ReceiveRegister:
										throw new InvalidOperationException("shouldnt be on unity");
									case EMessageInputType.ReceiveInputs:
										throw new InvalidOperationException("shouldnt be on unity");
								}

								break;
							}
							default:
								throw new ArgumentOutOfRangeException();
						}

						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			driver.Dispose();
		}

		public bool Create(ushort port)
		{
			var addr = new Address {Port = port};
			driver.Bind(addr);
			return driver.Listen() >= 0;
		}

		private void OnRegisterLayoutActions(in TransportConnection connection, ref DataBufferReader reader)
		{
			inputBackendSystem.ClearCurrentActions();
			
			var length = reader.ReadValue<int>();
			for (var ac = 0; ac < length; ac++)
			{
				var actionId    = reader.ReadValue<int>();
				var layoutCount = reader.ReadValue<int>();

				var actionEntity = inputBackendSystem.RegisterAction(connection, new InputAction {Id = actionId});
				var layouts      = inputBackendSystem.GetLayoutsOf(actionEntity);
				for (var lyt = 0; lyt < layoutCount; lyt++)
				{
					var layoutType = reader.ReadString();
					var layoutId   = reader.ReadString();
					var skip       = reader.ReadValue<int>();

					var layout = layoutSystem.TryCreateLayout(layoutType, layoutId);
					if (layout == null)
					{
						Debug.LogWarning($"No type defined for layout '{layoutType}'");
						
						reader.CurrReadIndex += skip;
						continue;
					}

					Console.WriteLine($"Layout (Id={layoutId}, Type={layoutType})");
					layout.Deserialize(ref reader);
					layouts.Add(layout);
				}
			}
		}
	}
}