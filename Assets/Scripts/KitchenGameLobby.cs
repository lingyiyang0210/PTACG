using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class KitchenGameLobby : MonoBehaviour
{
    public static KitchenGameLobby Instance { get; private set; }

    private const string KEY_RELAY_JOIN_CODE = "RelayJoinCode";

    public event EventHandler OnCreateLobbyStarted;
    public event EventHandler OnCreateLobbyFailed;
    public event EventHandler OnJoinStarted;
    public event EventHandler OnQuickJoinFailed;
    public event EventHandler OnJoinFailed;
    public event EventHandler<OnLobbyListChangedEventArgs> OnLobbyListChanged;

    public class OnLobbyListChangedEventArgs : EventArgs
    {
        public List<Lobby> lobbyList;
    }

    private Lobby joinedLobby;

    private float heartbeatTimer;
    private float listLobbiesTimer;

    private void Awake()
    {
        Instance = this;

        DontDestroyOnLoad(gameObject);

        InitializeUnityAuthentication();
    }

    private async void InitializeUnityAuthentication()
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                InitializationOptions initializationOptions = new InitializationOptions();

                await UnityServices.InitializeAsync(initializationOptions);
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            Debug.Log(
                "Unity Services initialized. Player ID: " +
                AuthenticationService.Instance.PlayerId
            );
        }
        catch (Exception e)
        {
            Debug.LogError(
                "Unity Services initialization failed: " + e
            );
        }
    }

    private void Update()
    {
        HandleHeartbeat();
        HandlePeriodicListLobbies();
    }

    private void HandlePeriodicListLobbies()
    {
        if (
            joinedLobby == null &&
            UnityServices.State == ServicesInitializationState.Initialized &&
            AuthenticationService.Instance.IsSignedIn &&
            SceneManager.GetActiveScene().name ==
            Loader.Scene.LobbyScene.ToString()
        )
        {
            listLobbiesTimer -= Time.deltaTime;

            if (listLobbiesTimer <= 0f)
            {
                float listLobbiesTimerMax = 3f;

                listLobbiesTimer = listLobbiesTimerMax;

                ListLobbies();
            }
        }
    }

    private void HandleHeartbeat()
    {
        if (IsLobbyHost())
        {
            heartbeatTimer -= Time.deltaTime;

            if (heartbeatTimer <= 0f)
            {
                float heartbeatTimerMax = 15f;

                heartbeatTimer = heartbeatTimerMax;

                LobbyService.Instance.SendHeartbeatPingAsync(
                    joinedLobby.Id
                );
            }
        }
    }

    private bool IsLobbyHost()
    {
        return
            joinedLobby != null &&
            joinedLobby.HostId ==
            AuthenticationService.Instance.PlayerId;
    }

    private async void ListLobbies()
    {
        try
        {
            QueryLobbiesOptions queryLobbiesOptions =
                new QueryLobbiesOptions
                {
                    Filters = new List<QueryFilter>
                    {
                        new QueryFilter(
                            QueryFilter.FieldOptions.AvailableSlots,
                            "0",
                            QueryFilter.OpOptions.GT
                        )
                    }
                };

            QueryResponse queryResponse =
                await LobbyService.Instance.QueryLobbiesAsync(
                    queryLobbiesOptions
                );

            OnLobbyListChanged?.Invoke(
                this,
                new OnLobbyListChangedEventArgs
                {
                    lobbyList = queryResponse.Results
                }
            );
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    public async void CreateLobby(
        string lobbyName,
        bool isPrivate
    )
    {
        OnCreateLobbyStarted?.Invoke(
            this,
            EventArgs.Empty
        );

        try
        {
            Debug.Log("Creating Relay allocation...");

            Allocation allocation =
                await RelayService.Instance.CreateAllocationAsync(
                    KitchenGameMultiplayer.MAX_PLAYER_AMOUNT - 1
                );

            string relayJoinCode =
                await RelayService.Instance.GetJoinCodeAsync(
                    allocation.AllocationId
                );

            Debug.Log(
                "Relay Join Code: " +
                relayJoinCode
            );

            UnityTransport unityTransport =
                NetworkManager.Singleton
                .GetComponent<UnityTransport>();

            unityTransport.SetRelayServerData(
                new RelayServerData(
                    allocation,
                    "dtls"
                )
            );

            joinedLobby =
                await LobbyService.Instance.CreateLobbyAsync(
                    lobbyName,
                    KitchenGameMultiplayer.MAX_PLAYER_AMOUNT,
                    new CreateLobbyOptions
                    {
                        IsPrivate = isPrivate,

                        Data =
                            new Dictionary<string, DataObject>
                            {
                                {
                                    KEY_RELAY_JOIN_CODE,
                                    new DataObject(
                                        DataObject.VisibilityOptions.Member,
                                        relayJoinCode
                                    )
                                }
                            }
                    }
                );

            Debug.Log(
                "Lobby created: " +
                joinedLobby.Name
            );

            KitchenGameMultiplayer.Instance.StartHost();

            Loader.LoadNetwork(
                Loader.Scene.CharacterSelectScene
            );
        }
        catch (Exception e)
        {
            Debug.LogError(
                "Create Lobby / Relay failed: " +
                e
            );

            OnCreateLobbyFailed?.Invoke(
                this,
                EventArgs.Empty
            );
        }
    }

    public async void QuickJoin()
    {
        OnJoinStarted?.Invoke(
            this,
            EventArgs.Empty
        );

        try
        {
            joinedLobby =
                await LobbyService.Instance
                .QuickJoinLobbyAsync();

            await JoinRelayFromLobby();

            KitchenGameMultiplayer.Instance.StartClient();
        }
        catch (Exception e)
        {
            Debug.LogError(
                "Quick Join failed: " +
                e
            );

            OnQuickJoinFailed?.Invoke(
                this,
                EventArgs.Empty
            );
        }
    }

    public async void JoinWithId(
        string lobbyId
    )
    {
        OnJoinStarted?.Invoke(
            this,
            EventArgs.Empty
        );

        try
        {
            joinedLobby =
                await LobbyService.Instance
                .JoinLobbyByIdAsync(
                    lobbyId
                );

            await JoinRelayFromLobby();

            KitchenGameMultiplayer.Instance.StartClient();
        }
        catch (Exception e)
        {
            Debug.LogError(
                "Join With ID failed: " +
                e
            );

            OnJoinFailed?.Invoke(
                this,
                EventArgs.Empty
            );
        }
    }

    public async void JoinWithCode(
        string lobbyCode
    )
    {
        OnJoinStarted?.Invoke(
            this,
            EventArgs.Empty
        );

        try
        {
            joinedLobby =
                await LobbyService.Instance
                .JoinLobbyByCodeAsync(
                    lobbyCode
                );

            await JoinRelayFromLobby();

            KitchenGameMultiplayer.Instance.StartClient();
        }
        catch (Exception e)
        {
            Debug.LogError(
                "Join With Code failed: " +
                e
            );

            OnJoinFailed?.Invoke(
                this,
                EventArgs.Empty
            );
        }
    }

    private async System.Threading.Tasks.Task JoinRelayFromLobby()
    {
        if (joinedLobby == null)
        {
            throw new Exception(
                "Joined Lobby is null."
            );
        }

        if (
            joinedLobby.Data == null ||
            !joinedLobby.Data.ContainsKey(
                KEY_RELAY_JOIN_CODE
            )
        )
        {
            throw new Exception(
                "Relay Join Code was not found in Lobby Data."
            );
        }

        string relayJoinCode =
            joinedLobby.Data[
                KEY_RELAY_JOIN_CODE
            ].Value;

        Debug.Log(
            "Joining Relay with code: " +
            relayJoinCode
        );

        JoinAllocation joinAllocation =
            await RelayService.Instance
                .JoinAllocationAsync(
                    relayJoinCode
                );

        UnityTransport unityTransport =
            NetworkManager.Singleton
                .GetComponent<UnityTransport>();

        unityTransport.SetRelayServerData(
            new RelayServerData(
                joinAllocation,
                "dtls"
            )
        );

        Debug.Log(
            "Relay connection configured successfully."
        );
    }

    public async void DeleteLobby()
    {
        if (joinedLobby != null)
        {
            try
            {
                await LobbyService.Instance
                    .DeleteLobbyAsync(
                        joinedLobby.Id
                    );

                joinedLobby = null;
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError(e);
            }
        }
    }

    public async void LeaveLobby()
    {
        if (joinedLobby != null)
        {
            try
            {
                await LobbyService.Instance
                    .RemovePlayerAsync(
                        joinedLobby.Id,
                        AuthenticationService.Instance.PlayerId
                    );

                joinedLobby = null;
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError(e);
            }
        }
    }

    public async void KickPlayer(
        string playerId
    )
    {
        if (IsLobbyHost())
        {
            try
            {
                await LobbyService.Instance
                    .RemovePlayerAsync(
                        joinedLobby.Id,
                        playerId
                    );
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError(e);
            }
        }
    }

    public Lobby GetLobby()
    {
        return joinedLobby;
    }
}