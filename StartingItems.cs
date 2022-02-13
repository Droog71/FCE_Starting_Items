using System.IO;
using UnityEngine;
using Lidgren.Network;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

public class StartingItems : FortressCraftMod
{
    private Coroutine serverCoroutine;
    private static List<string> savedPlayers;
    private static readonly string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    private static readonly string playersFilePath = Path.Combine(assemblyFolder, "players.txt");

    // Registers the mod.
    public override ModRegistrationData Register()
    {
        ModRegistrationData modRegistrationData = new ModRegistrationData();
        modRegistrationData.RegisterServerComms("Maverick.StartingItems", ServerWrite, ClientRead);
        return modRegistrationData;
    }

    // Called by unity engine on start up to initialize variables.
    public IEnumerator Start()
    {
        savedPlayers = new List<string>();
        if (WorldScript.mbIsServer || NetworkManager.mbHostingServer)
        {
            LoadPlayers();
        }
        yield return null;
    }

    // Loads player information from disk.
    private void LoadPlayers()
    {
        if (!File.Exists(playersFilePath))
        {
            File.Create(playersFilePath);
        }
        string fileContents = File.ReadAllText(playersFilePath);

        string[] allPlayers = fileContents.Split('}');
        foreach (string player in allPlayers)
        {
            savedPlayers.Add(player);
        }
    }

    // Saves player information to disk.
    private static void SavePlayer(string playerName)
    {
        string playerData = "";
        foreach (string player in savedPlayers)
        {
            playerData += player + "}";
        }
        savedPlayers.Add(playerName);
        playerData += playerName + "}";

        if (!File.Exists(playersFilePath))
        {
            File.Create(playersFilePath);
        }
        File.WriteAllText(playersFilePath, playerData);
    }

    // Called once per frame by unity engine.
    public void Update()
    {
        if (WorldScript.mbIsServer || NetworkManager.mbHostingServer)
        {
            serverCoroutine = StartCoroutine(CheckAreas());
        }
    }

    // Holds information for server to client comms.
    private struct ServerMessage
    {
        public int newPlayer;
    }

    // Networking.
    private static void ClientRead(NetIncomingMessage netIncomingMessage)
    {
        int readInt1 = netIncomingMessage.ReadInt32();
        int playerID = (int)NetworkManager.instance.mClientThread.mPlayer.mUserID;

        if (readInt1 == playerID)
        {
            int readInt2 = netIncomingMessage.ReadInt32();

            if (readInt2 == 1)
            {
                CollectStartingItems();
            }
        }
    }

    // Networking.
    private static void ClientWrite(BinaryWriter writer, object data)
    {
        writer.Write((int)data);
    }

    // Networking.
    private static void ServerWrite(BinaryWriter writer, Player player, object data)
    {
        ServerMessage message = (ServerMessage)data;
        writer.Write((int)player.mUserID);
        writer.Write(message.newPlayer);
    }

    // Sends chat messages from the server.
    private static void Announce(string msg)
    {
        ChatLine chatLine = new ChatLine();
        chatLine.mPlayer = -1;
        chatLine.mPlayerName = "[SERVER]";
        chatLine.mText = msg;
        chatLine.mType = ChatLine.Type.Normal;
        NetworkManager.instance.QueueChatMessage(chatLine);
    }

    // Checks player positions relative to protected areas and sets permissions.
    private IEnumerator CheckAreas()
    {
        if (NetworkManager.instance != null)
        {
            if (NetworkManager.instance.mServerThread != null)
            {
                List<NetworkServerConnection> connections = NetworkManager.instance.mServerThread.connections;
                for (int i = 0; i < connections.Count; i++)
                {
                    if (connections[i] != null)
                    {
                        if (connections[i].mState == eNetworkConnectionState.Playing)
                        {
                            Player player = connections[i].mPlayer;
                            if (player != null)
                            {
                                ServerMessage message = new ServerMessage();

                                if (savedPlayers != null)
                                {
                                    if (!savedPlayers.Contains(player.mUserID + player.mUserName))
                                    {
                                        Announce("Giving starting items to " + player.mUserName);
                                        SavePlayer(player.mUserID + player.mUserName);
                                        message.newPlayer = 1;
                                    }
                                    else
                                    {
                                        message.newPlayer = 0;
                                    }
                                }

                                ModManager.ModSendServerCommToClient("Maverick.StartingItems", player, message);
                                yield return new WaitForSeconds(0.25f);
                            }
                        }
                    }
                }
            }
        }
    }

    // Gives starting items to a player.
    private static void CollectStartingItems()
    {
        NetworkManager.instance.mClientThread.mPlayer.mInventory.CollectValue(507, 99, 7);
        NetworkManager.instance.mClientThread.mPlayer.mInventory.CollectValue(eCubeTypes.Torch, 0, 5);
        NetworkManager.instance.mClientThread.mPlayer.mInventory.CollectValue(eCubeTypes.OreSmelter, 1, 4);
        NetworkManager.instance.mClientThread.mPlayer.mInventory.CollectValue(eCubeTypes.Conveyor, 11, 160);
        NetworkManager.instance.mClientThread.mPlayer.mInventory.CollectValue(eCubeTypes.Conveyor, 13, 16);
        NetworkManager.instance.mClientThread.mPlayer.mInventory.CollectValue(eCubeTypes.Conveyor, 14, 16);
        NetworkManager.instance.mClientThread.mPlayer.mInventory.CollectValue(eCubeTypes.OreExtractor, 0, 3);
        NetworkManager.instance.mClientThread.mPlayer.mInventory.CollectValue(eCubeTypes.StorageHopper, 0, 5);
        NetworkManager.instance.mClientThread.mPlayer.mInventory.CollectValue(eCubeTypes.StorageHopper, 2, 5);
        NetworkManager.instance.mClientThread.mPlayer.mInventory.CollectValue(eCubeTypes.PowerStorageBlock, 0, 6);
    }
}
