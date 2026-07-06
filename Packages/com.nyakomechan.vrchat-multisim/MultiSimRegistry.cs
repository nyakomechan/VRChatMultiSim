#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using VRC.SDK3.ClientSim;
using VRC.SDKBase;

namespace MultiSim
{
    /// <summary>
    /// Tracks every networked scene object by its VRChat network id.
    /// ClientSim already assigns deterministic network ids (ordered by transform path) via
    /// ClientSimNetworkingView, and ships per-component encoders behind ClientSimNetworkIdHolder,
    /// but never attaches the holder to scene objects because it has no real transport.
    /// This registry attaches the holders so their Encode/Decode pipeline can be driven remotely.
    /// </summary>
    internal class MultiSimRegistry
    {
        private readonly Dictionary<int, ClientSimNetworkIdHolder> _holders =
            new Dictionary<int, ClientSimNetworkIdHolder>();

        // Every networked GameObject by id, including ones without syncable data
        // (e.g. a station whose GameObject has no synced variables).
        private readonly Dictionary<int, GameObject> _objects = new Dictionary<int, GameObject>();

        public IReadOnlyDictionary<int, ClientSimNetworkIdHolder> Holders => _holders;

        public void BuildFromScene()
        {
            _holders.Clear();
            _objects.Clear();

            VRC_SceneDescriptor descriptor = VRC_SceneDescriptor.Instance;
            if (descriptor == null)
            {
                MultiSimLog.Error("No scene descriptor; cannot register networked objects.");
                return;
            }

            // ClientSim runs this during its BeforeSceneLoad startup, which is too early to see
            // scene objects in editor play mode, so no ClientSimNetworkingViews get attached.
            // The call is idempotent (ids come from the descriptor's NetworkIDCollection).
            ClientSimNetworkingUtilities.ConfigureNetworkOnScene(descriptor);

            // Note: the views carry DontSave hide flags, which FindObjectsByType skips,
            // so enumerate through the descriptor's id collection instead.
            foreach (VRC.SDKBase.Network.NetworkIDPair pair in descriptor.NetworkIDCollection)
            {
                if (pair.gameObject == null || !pair.gameObject.TryGetComponent(out ClientSimNetworkingView view))
                {
                    continue;
                }
                int networkId = view.GetNetworkId();
                if (networkId <= 0)
                {
                    continue;
                }

                _objects[networkId] = view.gameObject;

                if (!view.TryGetComponent(out ClientSimNetworkIdHolder holder))
                {
                    holder = view.gameObject.AddComponent<ClientSimNetworkIdHolder>();
                    holder.SetNetworkView(view);
                    holder.SetNetworkComponents();
                    view.AddNetworkedObject(holder);
                }

                if (holder.GetNetworkComponentCount() == 0)
                {
                    // Nothing syncable on this object (e.g. all UdonBehaviours are SyncType.None).
                    continue;
                }

                if (_holders.ContainsKey(networkId))
                {
                    MultiSimLog.Warn($"Duplicate network id {networkId} on '{view.gameObject.name}'. Skipping.");
                    continue;
                }

                _holders[networkId] = holder;
            }

            MultiSimLog.Verbose($"Registered {_holders.Count} networked objects.");
        }

        public bool TryGetHolder(int networkId, out ClientSimNetworkIdHolder holder)
        {
            if (_holders.TryGetValue(networkId, out holder) && holder != null)
            {
                return true;
            }
            holder = null;
            return false;
        }

        public bool TryGetGameObject(int networkId, out GameObject obj)
        {
            if (_objects.TryGetValue(networkId, out obj) && obj != null)
            {
                return true;
            }
            obj = null;
            return false;
        }

        public bool TryGetNetworkId(GameObject obj, out int networkId)
        {
            networkId = 0;
            if (obj == null || !obj.TryGetComponent(out ClientSimNetworkingView view))
            {
                return false;
            }

            networkId = view.GetNetworkId();
            return networkId > 0;
        }

        public static int GetOwnerId(GameObject obj)
        {
            VRCPlayerApi owner = Networking.GetOwner(obj);
            return owner?.playerId ?? -1;
        }
    }
}
#endif
