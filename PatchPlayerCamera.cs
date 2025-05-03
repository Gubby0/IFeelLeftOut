using System;
using HarmonyLib;
using Il2CppInterop.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

namespace IFeelLeftOut
{
    internal class PatchPlayerCamera
    {
        // Camera settings
        private const float CAM_HEIGHT = 12f;
        private const float CAM_ANGLE = 60f;
        private const float CAM_FOV = 90f;

        // State variables
        private static bool initialized = false;
        private static bool leftOutCamToggle = false;

        // Components
        private static Player localPlayer;
        private static Camera playerCam;
        private static Camera leftOutCamera;
        private static GameObject leftOutCameraGameObject;

        // Input key for toggling camera
        private static Key toggleCameraKey = Key.F1;

        // Input handling state
        private static bool keyWasPressed = false;

        // Instance management
        private static bool isLocalPlayerInstance = false;
        private static int instanceId = UnityEngine.Random.Range(10000, 99999);

        /// <summary>
        /// Determines if this is the instance for the local player
        /// </summary>
        private static bool IsLocalPlayerInstance()
        {
            // Only initialize once
            if (!isLocalPlayerInstance && localPlayer != null)
            {
                Player currentPlayer = Plugin.playerManager?.GetLocalPlayer();
                isLocalPlayerInstance = (currentPlayer != null && currentPlayer == localPlayer);

                if (isLocalPlayerInstance)
                {
                    Plugin.Log.LogInfo($"[Instance {instanceId}] This is the local player instance");
                }
            }

            return isLocalPlayerInstance;
        }

        /// <summary>
        /// Initialize player reference if not already set
        /// </summary>
        private static void EnsurePlayerReference()
        {
            if (Plugin.playerManager == null)
            {
                Plugin.playerManager = NetworkBehaviourSingleton<PlayerManager>.instance;
            }

            if (localPlayer == null && Plugin.playerManager != null)
            {
                localPlayer = Plugin.playerManager.GetLocalPlayer();
                Plugin.Log.LogInfo($"[Instance {instanceId}] Local player reference established: {(localPlayer != null ? "success" : "failed")}");

                // Subscribe to team changed event if available
                if (localPlayer != null)
                {
                    GameEvents.SubscribeToTeamChanged(localPlayer, HandleTeamChanged);
                }
            }
        }

        /// <summary>
        /// Initializes the custom camera for goalie view
        /// </summary>
        private static void InitializeLeftOutCamera(PlayerTeam team)
        {
            try
            {
                if (!IsLocalPlayerInstance()) return;

                Plugin.Log.LogInfo($"[Instance {instanceId}] Initializing left out camera for team: " + team.ToString());

                // Calculate camera position based on team
                float camDistance = team == PlayerTeam.Blue ? -15f : 15f;
                float camRotation = team == PlayerTeam.Blue ? -180f : 0f;

                // Create camera if it doesn't exist
                EnsureLeftOutCameraExists();

                // Position and configure the camera
                leftOutCamera.transform.position = new Vector3(0, CAM_HEIGHT, camDistance);
                leftOutCamera.transform.rotation = Quaternion.Euler(CAM_ANGLE, camRotation, 0);
                leftOutCamera.fieldOfView = CAM_FOV;

                Plugin.Log.LogInfo($"[Instance {instanceId}] Left out camera initialized successfully");
                initialized = true;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"[Instance {instanceId}] Failed to initialize left out camera: " + e.Message);
                ResetCameraSystem();
            }
        }

        /// <summary>
        /// Ensures the left out camera exists, creating it if necessary
        /// </summary>
        private static void EnsureLeftOutCameraExists()
        {
            if (!IsLocalPlayerInstance()) return;

            if (leftOutCamera == null || leftOutCameraGameObject == null)
            {
                Plugin.Log.LogInfo($"[Instance {instanceId}] Creating new left out camera");
                leftOutCameraGameObject = new GameObject("LeftOutCamera");
                leftOutCamera = leftOutCameraGameObject.AddComponent<Camera>();
                leftOutCamera.enabled = leftOutCamToggle;
            }
        }

        /// <summary>
        /// Ensures the player camera reference is set
        /// </summary>
        private static void EnsurePlayerCameraExists()
        {
            if (!IsLocalPlayerInstance()) return;

            if (playerCam == null && localPlayer != null)
            {
                Plugin.Log.LogInfo($"[Instance {instanceId}] Getting camera from player object");
                playerCam = localPlayer.PlayerCamera.CameraComponent;
            }
        }

        /// <summary>
        /// Handles input for camera toggling with improved detection
        /// </summary>
        private static void HandleCameraToggleInput()
        {
            if (!IsLocalPlayerInstance()) return;

            // Check if the key is currently pressed
            bool isKeyPressed = Keyboard.current[toggleCameraKey].isPressed;

            // Toggle happens on the rising edge (when key transitions from not pressed to pressed)
            if (isKeyPressed && !keyWasPressed)
            {
                // Toggle the camera state (true -> false, false -> true)
                leftOutCamToggle = !leftOutCamToggle;

                string cameraState = leftOutCamToggle ? "ENABLED" : "DISABLED";
                Plugin.Log.LogInfo($"[Instance {instanceId}] Camera toggled: {cameraState}");
            }

            // Update key state for next frame
            keyWasPressed = isKeyPressed;
        }

        /// <summary>
        /// Updates camera enabled states based on toggle
        /// </summary>
        private static void UpdateCameraStates()
        {
            if (!IsLocalPlayerInstance()) return;

            if (leftOutCamera != null && playerCam != null)
            {
                // Only update if the state actually changes
                if (leftOutCamera.enabled != leftOutCamToggle)
                {
                    leftOutCamera.enabled = leftOutCamToggle;
                    playerCam.enabled = !leftOutCamToggle;
                    Plugin.Log.LogInfo($"[Instance {instanceId}] Camera states updated: LeftOut={leftOutCamToggle}, Player={!leftOutCamToggle}");
                }
            }
        }

        /// <summary>
        /// Resets the entire camera system
        /// </summary>
        private static void ResetCameraSystem()
        {
            if (!IsLocalPlayerInstance()) return;

            playerCam = null;

            if (leftOutCameraGameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(leftOutCameraGameObject);
            }

            leftOutCamera = null;
            leftOutCameraGameObject = null;
            leftOutCamToggle = false;
            initialized = false;
            keyWasPressed = false;

            Plugin.Log.LogInfo($"[Instance {instanceId}] Camera system has been reset");
        }

        /// <summary>
        /// Handles events when a player changes teams
        /// </summary>
        private static void HandleTeamChanged(Player player, PlayerTeam oldTeam, PlayerTeam newTeam)
        {
            // Check if this is our local player
            if (localPlayer != null && player == localPlayer)
            {
                Plugin.Log.LogInfo($"[Instance {instanceId}] Local player team changed from {oldTeam} to {newTeam}");
                ResetCameraSystem();
            }
        }

        [HarmonyPatch(typeof(PlayerCamera), nameof(PlayerCamera.OnTick))]
        private class PatchPlayerCameraOnTick
        {
            private static void Postfix(PlayerCamera __instance)
            {
                try
                {
                    // Ensure we have player and manager references
                    EnsurePlayerReference();

                    // Skip processing if we're not handling the local player
                    if (localPlayer == null) return;

                    // Check if player exists and is a goalie
                    if (localPlayer.Role.Value == PlayerRole.Goalie)
                    {
                        // Additional check to make sure we're only working with local player's camera
                        if (__instance != localPlayer.PlayerCamera) return;

                        // Ensure cameras exist
                        EnsurePlayerCameraExists();
                        EnsureLeftOutCameraExists();

                        // Check for input to toggle camera
                        HandleCameraToggleInput();

                        // Initialize camera if not already done
                        if (!initialized)
                        {
                            InitializeLeftOutCamera(localPlayer.Team.Value);
                        }

                        // Update camera enabled states
                        UpdateCameraStates();
                    }
                }
                catch (Exception e)
                {
                    Plugin.Log.LogWarning($"[Instance {instanceId}] Error in camera tick: " + e.Message);
                    ResetCameraSystem();
                }
            }
        }
    }
}