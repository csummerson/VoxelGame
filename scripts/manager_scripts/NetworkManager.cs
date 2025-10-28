using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;

public partial class NetworkManager : Node
{
	public static NetworkManager instance { get; private set; }
	public const int PORT = 9000;
	ENetMultiplayerPeer peer;

	public bool isServer = false;

	public override void _EnterTree()
	{
		if (instance == null)
		{
			instance = this;
			SetProcess(false);
		}
		else
		{
			QueueFree();
		}
	}

	public void StartServer()
	{
		GD.Print("Starting server...");
		peer = new ENetMultiplayerPeer();
		peer.CreateServer(PORT, 32);
		Multiplayer.MultiplayerPeer = peer;

		GD.Print($"Server listening on port {PORT}");

		Multiplayer.PeerConnected += OnPeerConnected;
		Multiplayer.PeerDisconnected += OnPeerDisconnected;
	}

	// Server handlers
	private void OnPeerConnected(long id)
	{
		GD.Print($"Client connected: {id}");
	}

	private void OnPeerDisconnected(long id)
	{
		GD.Print($"Client disconnected: {id}");
	}

	// Client handler
	public bool StartClient(string address)
	{
		peer = new ENetMultiplayerPeer();
		Error error = peer.CreateClient(address, PORT);
		if (error != Error.Ok)
		{
			return false;
		}

		Multiplayer.MultiplayerPeer = peer;

		GD.Print($"Connected to server at {address}");

		return true;
	}

	public void CloseConnection()
	{
		peer.Close();
	}
}
