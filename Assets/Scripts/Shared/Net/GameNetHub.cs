using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MLAPI.Serialization.Pooled;

namespace BossRoom
{
    public enum ConnectStatus
    {
        SUCCESS,           //client successfully connected. This may also be a successful reconnect. 
        ESERVERFULL,       //can't join, server is already at capacity. 
        EMATCHSTARTED,     //can't join, match is already in progress. 
        EUNKNOWN,          //can't join, reason unknown. 
    }

    /// <summary>
    /// The GameNetHub is a general-purpose relay for game network messages between the client and server. It is available
    /// as soon as the initial network connection has completed, and persists across all scenes. Its purpose is to move non-GameObject-specific
    /// methods between server and client. Generally these have to do with connection, and match end conditions. 
    /// </summary>
    /// 
    /// <remarks
    /// Why is there a C2S_ConnectFinished event here? How is that different from the "ApprovalCheck" logic that MLAPI optionally runs
    /// when establishing a new client connection? 
    /// MLAPI's ApprovalCheck logic doesn't offer a way to return rich data. We need to know certain things directly upon logging in, such as
    /// whether the game-layer even wants us to join (we could fail because the server is full, or some other non network related reason), and also
    /// what BossRoomState to transition to. We do this with a Custom Named Message, which fires on the server immediately after the approval check delegate
    /// has run. 
    /// 
    /// Why do we need to send a client GUID? What is it? Don't we already have a clientID? 
    /// ClientIDs are assigned on login. If you connect to a server, then your connection drops, and you reconnect, you get a new ClientID. This
    /// makes it awkward to get back your old character, which the server is going to hold onto for a fixed timeout. To properly reconnect and recover
    /// your character, you need a persistent identifier for your own client install. We solve that by generating a random GUID and storing it
    /// in player prefs, so it persists across sessions of the game. 
    /// </remarks>
    /// 
    public class GameNetHub : MonoBehaviour
    {
        public GameObject NetworkingManagerGO;

        private BossRoomClient.GNH_Client m_clientLogic;
        private BossRoomServer.GNH_Server m_serverLogic;

        public MLAPI.NetworkingManager NetManager { get; private set; }

        // Start is called before the first frame update
        void Start()
        {
            Object.DontDestroyOnLoad(this.gameObject);

            //FIXME_DMW: would like to remove this. I have added it because at the moment I can't stop MLAPI from destroying the 
            //GameHub object I created (that I intended to live for the entire lifetime of the game), and replacing it with its
            //own copy (which naturally doesn't have this editor-configured value set). I have tried setting "UsePrefabSync" to false,
            //but didn't stop this behavior. 
            if (!NetworkingManagerGO)
            {
                NetworkingManagerGO = GameObject.Find("NetworkingManager");
            }

            NetManager = NetworkingManagerGO.GetComponent<MLAPI.NetworkingManager>();

            //because we are not a true NetworkedBehavior, we don't get NetworkStart messages. But we still need to run at that point
            //where we know if we're a host or client. So we fake a "NetworkingManager.OnNetworkStarted" event out of the existing OnServerStarted
            //and OnClientConnectedCallback events. 
            NetManager.OnServerStarted += () =>
            {
                NetworkStart();
            };
            NetManager.OnClientConnectedCallback += (clientId) =>
            {
                if (clientId == NetManager.LocalClientId)
                {
                    NetworkStart();
                }
            };
        }

        private void RegisterClientMessageHandlers()
        {
            MLAPI.Messaging.CustomMessagingManager.RegisterNamedMessageHandler("S2C_ConnectResult", (senderClientId, stream) =>
            {
                using (PooledBitReader reader = PooledBitReader.Get(stream))
                {
                    ConnectStatus status = (ConnectStatus)reader.ReadInt32();
                    BossRoomState state = (BossRoomState)reader.ReadInt32();
                    m_clientLogic.RecvConnectFinished(status, state);
                }
            });
        }

        private void RegisterServerMessageHandlers()
        {
            //TODO: plug in any C->S message handlers here. 
        }


        /// <summary>
        /// This method runs when NetworkingManager has started up (following a succesful connect on the client, or directly after StartHost is invoked
        /// on the host). It is named to match NetworkedBehaviour.NetworkStart, and serves the same role, even though GameNetHub itself isn't a NetworkedBehaviour.
        /// </summary>
        public void NetworkStart()
        {
            if (NetManager.IsClient)
            {
                m_clientLogic = new BossRoomClient.GNH_Client(this);
                RegisterClientMessageHandlers();
            }
            if ( NetManager.IsServer )
            {
                m_serverLogic = new BossRoomServer.GNH_Server(this);
                RegisterServerMessageHandlers();
            }
            if( NetManager.IsHost )
            {
                //special host code. This is what kicks off the flow that happens on a regular client
                //when it has finished connecting successfully. A dedicated server would remove this. 
                m_clientLogic.RecvConnectFinished(ConnectStatus.SUCCESS, BossRoomState.CHARSELECT);
            }
        }

        /// <summary>
        /// Wraps the invocation of NetworkingManager.StartClient, including our GUID as the payload. 
        /// </summary>
        /// <param name="ipaddress">the IP address of the host to connect to. (IPV4 only)</param>
        /// <param name="port">The port of the host to connect to. </param>
        public void StartClient(string ipaddress, int port)
        {
            BossRoomClient.GNH_Client.StartClient(this, ipaddress, port);
        }

        /// <summary>
        /// Wraps the invocation of NetworkingManager.StartHost. 
        /// </summary>
        /// <param name="ipaddress">The IP address of the network interface we should listen to connections on. (IPV4 only)</param>
        /// <param name="port">The port we should listen on. </param>
        public void StartHost(string ipaddress, int port )
        {
            BossRoomServer.GNH_Server.StartHost(this, ipaddress, port);
        }

        //Server->Client RPCs

        public void S2C_ConnectResult( ulong netId, ConnectStatus status, BossRoomState targetState )
        {
            using (PooledBitStream stream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                {
                    writer.WriteInt32((int)status);
                    writer.WriteInt32((int)targetState);
                    MLAPI.Messaging.CustomMessagingManager.SendNamedMessage("S2C_ConnectResult", netId, stream, "MLAPI_INTERNAL");
                }
            }
        }
    }
}
