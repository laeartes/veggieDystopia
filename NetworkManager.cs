using Godot;
using System;

public partial class NetworkManager : Node3D
{
	[Export] public PackedScene PlayerScene;
	[Export] public int Port = 8910;
	[Export] public string Address = "127.0.0.1";

	private ENetMultiplayerPeer _peer;

	public override void _Ready()
	{
		_peer = new ENetMultiplayerPeer();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		// Press TAB to Host
		if (@event.IsActionPressed("ui_focus_next")) 
		{
			HostGame();
		}
		// Press ENTER to Join
		else if (@event.IsActionPressed("ui_accept")) 
		{
			JoinGame();
		}
	}

	public void HostGame()
	{
		_peer.CreateServer(Port);
		Multiplayer.MultiplayerPeer = _peer;
		Multiplayer.PeerConnected += OnPeerConnected;
		
		// Host gets Peer ID 1
		SpawnPlayer(1);
		GD.Print("Server hosted on port ", Port);
	}

	public void JoinGame()
	{
		_peer.CreateClient(Address, Port);
		Multiplayer.MultiplayerPeer = _peer;
		GD.Print("Client connecting to host at ", Address);
	}

	private void OnPeerConnected(long id)
	{
		// Host spawns incoming players
		SpawnPlayer((int)id);
	}

	private void SpawnPlayer(int peerId)
	{
		Player playerInstance = PlayerScene.Instantiate<Player>();
		playerInstance.Name = peerId.ToString();
		
		// Spawn players with slight horizontal separation
		playerInstance.Position = new Vector3(peerId * 2f, 2f, 0f); 
		AddChild(playerInstance);
	}
}
