#if UNITY_EDITOR
using System;
using UnityEngine;
using VRC.SDK3.ClientSim.Interfaces;
using VRC.SDK3.Data;
using VRC.Udon;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;

namespace MultiSim
{
    /// <summary>
    /// Replacement for ClientSim's UdonBehaviour encoder/decoder.
    ///
    /// ClientSim's ClientSimUdonEncodeDecode resolves variable types via
    /// UdonBehaviour.GetProgramVariableType, which reflects the *current boxed value* on the
    /// Udon heap. Its object-typed SetProgramVariable writes then retype the heap slot, after
    /// which the type lookup returns System.Object, the decoder's type switch falls through to
    /// null, and every subsequent sync erases the synced variables (breaking UdonSharp reads).
    ///
    /// This codec instead uses the symbol table's *declared* type and writes through
    /// IUdonHeap.SetHeapVariable(address, value, declaredType), keeping the slot strongly typed.
    /// Values travel as MultiSimJson typed tokens so the exact .NET type survives JSON.
    /// </summary>
    internal class MultiSimUdonCodec : IClientSimEncodeDecoder
    {
        public void PreEncode(MonoBehaviour component)
        {
            ((UdonBehaviour)component).OnPreSerialization();
        }

        public DataDictionary Encode(MonoBehaviour component)
        {
            UdonBehaviour udonBehaviour = (UdonBehaviour)component;
            DataDictionary data = new DataDictionary();

            foreach (IUdonSyncMetadata metadata in GetSyncMetadata(udonBehaviour))
            {
                object value = udonBehaviour.GetProgramVariable(metadata.Name);
                data[metadata.Name] = MultiSimJson.ObjectToTypedToken(value);
            }

            return data;
        }

        public void PostEncode(MonoBehaviour component, DataDictionary data)
        {
            ((UdonBehaviour)component).OnPostSerialization(new SerializationResult(true, data.Count * 4));
        }

        public bool IsManualSynced(MonoBehaviour component)
        {
            return ((UdonBehaviour)component).SyncMethod == VRC.SDKBase.Networking.SyncType.Manual;
        }

        public bool IsDirty(MonoBehaviour component, DataDictionary data)
        {
            UdonBehaviour udonBehaviour = (UdonBehaviour)component;

            foreach (IUdonSyncMetadata metadata in GetSyncMetadata(udonBehaviour))
            {
                if (!data.TryGetValue(metadata.Name, out DataToken previous))
                {
                    return true;
                }

                DataToken current = MultiSimJson.ObjectToTypedToken(udonBehaviour.GetProgramVariable(metadata.Name));
                if (!TokensEqual(previous, current))
                {
                    return true;
                }
            }

            return false;
        }

        public void Decode(MonoBehaviour component, DataDictionary data)
        {
            UdonBehaviour udonBehaviour = (UdonBehaviour)component;
            IUdonProgram program = MultiSimReflect.GetUdonProgram(udonBehaviour);
            if (program == null)
            {
                return;
            }

            IUdonSymbolTable symbolTable = program.SymbolTable;
            IUdonHeap heap = program.Heap;

            foreach (IUdonSyncMetadata metadata in GetSyncMetadata(udonBehaviour))
            {
                if (!data.TryGetValue(metadata.Name, out DataToken token))
                {
                    continue;
                }

                if (!symbolTable.TryGetAddressFromSymbol(metadata.Name, out uint address))
                {
                    continue;
                }

                Type declaredType = symbolTable.GetSymbolType(metadata.Name);
                object value = MultiSimJson.TypedTokenToObject(token);

                if (value == null)
                {
                    if (declaredType.IsValueType)
                    {
                        // Never null out a value-type slot; that corrupts the UdonSharp heap.
                        continue;
                    }
                }
                else if (!declaredType.IsInstanceOfType(value))
                {
                    try
                    {
                        value = Convert.ChangeType(value, declaredType);
                    }
                    catch (Exception)
                    {
                        MultiSimLog.Warn($"Cannot convert synced value of '{metadata.Name}' " +
                                         $"({value.GetType().Name} -> {declaredType.Name}) on '{udonBehaviour.name}'.");
                        continue;
                    }
                }

                heap.SetHeapVariable(address, value, declaredType);
            }

            if (!udonBehaviour.HasDoneStart)
            {
                MultiSimLog.Warn($"Skipping OnDeserialization for '{udonBehaviour.gameObject.name}' " +
                                 $"(HasDoneStart=false). Data was written to the heap but the event will " +
                                 $"not fire until the next sync.");
                return;
            }

            udonBehaviour.OnDeserialization(new DeserializationResult(0, 0, true));
        }

        private static System.Collections.Generic.IEnumerable<IUdonSyncMetadata> GetSyncMetadata(
            UdonBehaviour udonBehaviour)
        {
            IUdonSyncMetadataTable table = udonBehaviour.SyncMetadataTable;
            return table != null
                ? table.GetAllSyncMetadata()
                : Array.Empty<IUdonSyncMetadata>();
        }

        private static bool TokensEqual(DataToken a, DataToken b)
        {
            if (a.TokenType != b.TokenType)
            {
                return false;
            }

            switch (a.TokenType)
            {
                case TokenType.DataList:
                {
                    DataList listA = a.DataList;
                    DataList listB = b.DataList;
                    if (listA.Count != listB.Count)
                    {
                        return false;
                    }
                    for (int i = 0; i < listA.Count; i++)
                    {
                        if (!TokensEqual(listA[i], listB[i]))
                        {
                            return false;
                        }
                    }
                    return true;
                }
                case TokenType.DataDictionary:
                {
                    DataDictionary dictA = a.DataDictionary;
                    DataDictionary dictB = b.DataDictionary;
                    if (dictA.Count != dictB.Count)
                    {
                        return false;
                    }
                    foreach (System.Collections.Generic.KeyValuePair<DataToken, DataToken> pair in dictA)
                    {
                        if (!dictB.TryGetValue(pair.Key, out DataToken other) || !TokensEqual(pair.Value, other))
                        {
                            return false;
                        }
                    }
                    return true;
                }
                default:
                    return a.Equals(b);
            }
        }
    }
}
#endif
