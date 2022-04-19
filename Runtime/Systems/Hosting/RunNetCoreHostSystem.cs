using System;
using System.IO;
using System.Runtime.InteropServices;
using CoreLink.src;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace StormiumTeam.GameBase.Systems.Hosting
{
    public partial class RunNetCoreHostSystem : ComponentSystem
    {
        private EntityQuery _query;

        protected override void OnCreate()
        {
            base.OnCreate();

            _query = GetEntityQuery(typeof(NetCoreHost));
        }

        private CoreHost _coreHost;

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            
            InitCallbacks();

            var entity = _query.GetSingletonEntity();
            var hostData = EntityManager.GetComponentData<NetCoreHost>(entity);

            var path = hostData.OverrideHostPath ?? Application.persistentDataPath + "/dotnet/host/fxr/6.0.0/";
            Directory.CreateDirectory(path);
            
            var files = Directory.GetFiles(path);
            if (files.Length == 0)
            {
                Debug.LogError($"<HostFxr> No files found at '{path}'");
                EntityManager.AddComponentData(entity, new NetCoreHost.FilesNotFoundError());
            }

            _coreHost = CoreHost.Load(
                action =>
                {
                    using var list = new NativeList<IntPtr>(Allocator.Temp);

                    void Add(object del)
                    {
                        list.Add(Marshal.GetFunctionPointerForDelegate(del));
                        Debug.Log(Marshal.GetFunctionPointerForDelegate(del));
                    }

                    Add(_print);
                    Add(_receiveExchange);
                    Add(_sendExchange);
                },
                files[0],
                "",
                ""
            );
        }

        protected override void OnUpdate()
        {

        }
    }

    public class NetCoreHost : IComponentData
    {
        [CanBeNull] public string OverrideHostPath;
        [CanBeNull] public string OverrideProgramPath;

        public struct FilesNotFoundError : IComponentData
        {
        }
    }
}