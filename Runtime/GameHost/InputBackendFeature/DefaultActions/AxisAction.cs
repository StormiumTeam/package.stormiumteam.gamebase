﻿using System;
 using System.Collections.Generic;
 using System.Collections.ObjectModel;
 using System.Linq;
 using GameHost.InputBackendFeature.BaseSystems;
 using GameHost.InputBackendFeature.Interfaces;
 using GameHost.InputBackendFeature.Layouts;
 using RevolutionSnapshot.Core.Buffers;
 using Unity.Collections;
 using Unity.Entities;
 using Unity.Mathematics;
 using UnityEngine.InputSystem.Controls;

 namespace GameHost.Inputs.DefaultActions
 {
     public struct AxisAction : IInputAction
     {
         public class Layout : InputLayoutBase
         {
             public CInput[] Negative;
             public CInput[] Positive;
             
             // empty for Activator.CreateInstance<>();
             public Layout(string id) : base(id)
             {
                 Inputs = new ReadOnlyCollection<CInput>(Array.Empty<CInput>());
             }

             public Layout(string id, IEnumerable<CInput> negative, IEnumerable<CInput> positive) : base(id)
             {
                 Negative = negative.ToArray();
                 Positive = positive.ToArray();
                 Inputs   = new ReadOnlyCollection<CInput>(Negative.Concat(Positive).ToArray());
             }

             public override void Serialize(ref DataBufferWriter buffer)
             {
                 void write(CInput[] array, ref DataBufferWriter writer)
                 {
                     writer.WriteInt(array.Length);
                     foreach (var input in array)
                         writer.WriteStaticString(input.Target);
                 }

                 write(Negative, ref buffer);
                 write(Positive, ref buffer);
             }

             public override void Deserialize(ref DataBufferReader buffer)
             {
                 void read(ref CInput[] array, ref DataBufferReader reader)
                 {
                     var count = reader.ReadValue<int>();
                     array = new CInput[count];
                     for (var i = 0; i != count; i++)
                         array[i] = new CInput(reader.ReadString());
                 }

                 read(ref Negative, ref buffer);
                 read(ref Positive, ref buffer);

                 Inputs = new ReadOnlyCollection<CInput>(Negative.Concat(Positive).ToArray());
             }
         }

         public float Value;

         public class InputActionSystem : InputActionSystemBase<AxisAction, Layout>
         {
             protected override void OnUpdate()
             {
                 foreach (var entity in InputQuery.ToEntityArray(Allocator.Temp))
                 {
                     var currentLayout = EntityManager.GetComponentData<InputCurrentLayout>(GetSingletonEntity<InputCurrentLayout>());

                     var layouts = GetLayouts(entity);
                     if (!layouts.TryGetOrDefault(currentLayout.Id, out var layout))
                         return;

                     var action = EntityManager.GetComponentData<AxisAction>(entity);
                     var value  = 0f;
                     foreach (var input in layout.Inputs)
                     {
                         if (Backend.GetInputControl(input.Target) is AxisControl buttonControl)
                         {
                             value += buttonControl.ReadValue();
                         }
                     }

                     action.Value = math.clamp(value, 0, 1);
                     EntityManager.SetComponentData(entity, action);
                 }
             }
         }

         public void Serialize(ref DataBufferWriter buffer)
         {
             buffer.WriteValue(Value);
         }

         public void Deserialize(ref DataBufferReader buffer)
         {
             Value = buffer.ReadValue<float>();
         }
     }
 }