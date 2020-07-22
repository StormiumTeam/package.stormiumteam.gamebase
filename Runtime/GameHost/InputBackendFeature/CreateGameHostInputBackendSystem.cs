using System;
using GameHost.Core.IO;
using GameHost.InputBackendFeature.Components;
using GameHost.InputBackendFeature.Layouts;
using GameHost.Transports.enet;
using GameHost.Transports.Transports.ENet;
using PataNext.Export.Desktop;
using RevolutionSnapshot.Core.Buffers;
using Unity.Entities;
using UnityEngine;

namespace GameHost.InputBackendFeature
{
	public class CreateGameHostInputBackendSystem : SystemBase
	{
		public TransportDriver Driver => driver;

		private ENetTransportDriver   enetDriver;
		private HeaderTransportDriver driver;

		private InputBackendSystem        inputBackendSystem;
		private RegisterInputLayoutSystem inputLayoutSystem;

		public CreateGameHostInputBackendSystem()
		{
			enetDriver = new ENetTransportDriver(32);
			driver     = new HeaderTransportDriver(enetDriver);

			var header = driver.WriteHeader();
			header.WriteInt((int) EMessageType.InputData);
		}
		
		protected override void OnCreate()
		{
			base.OnCreate();
			inputBackendSystem = World.GetExistingSystem<InputBackendSystem>();
			inputLayoutSystem  = World.GetExistingSystem<RegisterInputLayoutSystem>();
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
			enetDriver.Bind(addr);
			return enetDriver.Listen() >= 0;
		}

		private void OnRegisterLayoutActions(in TransportConnection connection, ref DataBufferReader reader)
		{
			inputBackendSystem.ClearCurrentActions();

			var length = reader.ReadValue<int>();
			for (var ac = 0; ac < length; ac++)
			{
				var actionId   = reader.ReadValue<int>();
				var actionType = reader.ReadString();
				var skipAction = reader.ReadValue<int>();

				var actionEntity = inputBackendSystem.RegisterAction(connection, actionType, new InputAction {Id = actionId});
				if (actionEntity == default)
				{
					Debug.LogWarning($"No type defined for action '{actionType}'");

					reader.CurrReadIndex += skipAction;
					continue;
				}

				var layoutCount = reader.ReadValue<int>();

				var layouts = inputBackendSystem.GetLayoutsOf(actionEntity);
				for (var lyt = 0; lyt < layoutCount; lyt++)
				{
					var layoutId   = reader.ReadString();
					var layoutType = reader.ReadString();
					var skipLayout = reader.ReadValue<int>();

					var layout = inputLayoutSystem.TryCreateLayout(layoutType, layoutId);
					if (layout == null)
					{
						Debug.LogWarning($"No type defined for layout '{layoutType}'");

						reader.CurrReadIndex += skipLayout;
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