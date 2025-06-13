using System;
using HarmonyLib;
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

        // Camera position validation
        private static Vector3 lastKnownGoodPosition = Vector3.zero;
        private static bool hasValidPosition = false;

        /// <summary>
        /// Determines if this is the instance for the local player
        /// </summary>
        private static bool IsLocalPlayerInstance()
        {
            // Only initialize once
            if (!isLocalPlayerInstance && localPlayer != null)
            {
                PlayerManager playerManager = UnityEngine.Object.FindFirstObjectByType<PlayerManager>();
                if (playerManager != null)
                {
                    Player currentPlayer = playerManager.GetLocalPlayer();
                    isLocalPlayerInstance = (currentPlayer != null && currentPlayer == localPlayer);

                    if (isLocalPlayerInstance)
                    {
                        Plugin.Log($"[Instance {instanceId}] This is the local player instance");
                    }
                }
            }

            return isLocalPlayerInstance;
        }

        /// <summary>
        /// Initialize player reference if not already set
        /// </summary>
        private static void EnsurePlayerReference()
        {
            if (localPlayer == null)
            {
                PlayerManager playerManager = UnityEngine.Object.FindFirstObjectByType<PlayerManager>();
                if (playerManager != null)
                {
                    localPlayer = playerManager.GetLocalPlayer();
                    Plugin.Log($"[Instance {instanceId}] Local player reference established: {(localPlayer != null ? "success" : "failed")}");

                    // Subscribe to team changed event if available
                    if (localPlayer != null)
                    {
                        GameEvents.SubscribeToTeamChanged(localPlayer, HandleTeamChanged);
                    }
                }
            }
        }

        /// <summary>
        /// Validates if a camera position is reasonable (not at origin or invalid)
        /// </summary>
        private static bool IsValidCameraPosition(Vector3 position)
        {
            // Check if position is at origin (likely invalid)
            if (position == Vector3.zero)
            {
                return false;
            }

            // Check if position has reasonable values
            if (float.IsNaN(position.x) || float.IsNaN(position.y) || float.IsNaN(position.z))
            {
                return false;
            }

            // Check if position is within reasonable bounds for a hockey rink
            if (Mathf.Abs(position.x) > 100f || Mathf.Abs(position.z) > 100f || position.y < 0f || position.y > 50f)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets a safe camera position, falling back to known good positions if needed
        /// </summary>
        private static Vector3 GetSafeCameraPosition(PlayerTeam team)
        {
            // Calculate ideal position
            float camDistance = team == PlayerTeam.Blue ? -15f : 15f;
            Vector3 idealPosition = new Vector3(0, CAM_HEIGHT, camDistance);

            // If ideal position is valid, use it
            if (IsValidCameraPosition(idealPosition))
            {
                lastKnownGoodPosition = idealPosition;
                hasValidPosition = true;
                return idealPosition;
            }

            // If we have a last known good position, use that
            if (hasValidPosition && IsValidCameraPosition(lastKnownGoodPosition))
            {
                Plugin.LogError($"[Instance {instanceId}] Using fallback position: {lastKnownGoodPosition}");
                return lastKnownGoodPosition;
            }

            // Ultimate fallback - center ice, high up
            Vector3 fallbackPosition = new Vector3(0, 20f, 0);
            Plugin.LogError($"[Instance {instanceId}] Using emergency fallback position: {fallbackPosition}");
            lastKnownGoodPosition = fallbackPosition;
            hasValidPosition = true;
            return fallbackPosition;
        }

        /// <summary>
        /// Initializes the custom camera for goalie view with enhanced validation
        /// </summary>
        private static void InitializeLeftOutCamera(PlayerTeam team)
        {
            try
            {
                if (!IsLocalPlayerInstance()) return;

                Plugin.Log($"[Instance {instanceId}] Initializing left out camera for team: " + team.ToString());

                // Ensure camera exists first
                EnsureLeftOutCameraExists();

                if (leftOutCamera == null)
                {
                    Plugin.LogError($"[Instance {instanceId}] Failed to create left out camera");
                    return;
                }

                // Get safe position
                Vector3 safePosition = GetSafeCameraPosition(team);
                float camRotation = team == PlayerTeam.Blue ? -180f : 0f;

                // Set position and validate
                leftOutCamera.transform.position = safePosition;
                leftOutCamera.transform.rotation = Quaternion.Euler(CAM_ANGLE, camRotation, 0);
                leftOutCamera.fieldOfView = CAM_FOV;

                // Validate the position was actually set
                Vector3 actualPosition = leftOutCamera.transform.position;
                if (!IsValidCameraPosition(actualPosition))
                {
                    Plugin.LogError($"[Instance {instanceId}] Camera position validation failed! Expected: {safePosition}, Actual: {actualPosition}");

                    // Force set position again
                    leftOutCamera.transform.position = safePosition;

                    // If still failing, delay initialization
                    if (!IsValidCameraPosition(leftOutCamera.transform.position))
                    {
                        Plugin.LogError($"[Instance {instanceId}] Camera position still invalid, marking uninitialized");
                        initialized = false;
                        return;
                    }
                }

                Plugin.Log($"[Instance {instanceId}] Left out camera initialized successfully at position: {actualPosition}");
                initialized = true;
            }
            catch (Exception e)
            {
                Plugin.LogError($"[Instance {instanceId}] Failed to initialize left out camera: " + e.Message);
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
                Plugin.Log($"[Instance {instanceId}] Creating new left out camera");

                // Clean up any existing broken camera
                if (leftOutCameraGameObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(leftOutCameraGameObject);
                }

                // Create new camera
                leftOutCameraGameObject = new GameObject("LeftOutCamera");

                // Ensure the GameObject is not destroyed when loading scenes
                UnityEngine.Object.DontDestroyOnLoad(leftOutCameraGameObject);

                leftOutCamera = leftOutCameraGameObject.AddComponent<Camera>();
                leftOutCamera.enabled = false; // Start disabled

                // Copy some settings from the main camera if available
                if (playerCam != null)
                {
                    leftOutCamera.clearFlags = playerCam.clearFlags;
                    leftOutCamera.backgroundColor = playerCam.backgroundColor;
                    leftOutCamera.cullingMask = playerCam.cullingMask;
                    leftOutCamera.depth = playerCam.depth + 1; // Render on top
                }

                Plugin.Log($"[Instance {instanceId}] Left out camera created successfully");
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
                Plugin.Log($"[Instance {instanceId}] Getting camera from player object");

                if (localPlayer.PlayerCamera != null && localPlayer.PlayerCamera.CameraComponent != null)
                {
                    playerCam = localPlayer.PlayerCamera.CameraComponent;
                    Plugin.Log($"[Instance {instanceId}] Player camera reference established");
                }
                else
                {
                    Plugin.LogError($"[Instance {instanceId}] Player camera component is null");
                }
            }
        }

        /// <summary>
        /// Handles input for camera toggling with cooldown and validation
        /// </summary>
        private static void HandleCameraToggleInput()
        {
            if (!IsLocalPlayerInstance()) return;

            // Check if the key is currently pressed
            bool isKeyPressed = Keyboard.current != null && Keyboard.current[toggleCameraKey].isPressed;

            // Toggle happens on the rising edge (when key transitions from not pressed to pressed)
            if (isKeyPressed && !keyWasPressed)
            {
                // Validate that we can actually toggle
                if (leftOutCamera == null || playerCam == null)
                {
                    Plugin.LogError($"[Instance {instanceId}] Cannot toggle - cameras not ready (leftOut: {leftOutCamera != null}, player: {playerCam != null})");
                    keyWasPressed = isKeyPressed;
                    return;
                }

                // Validate camera position before toggling
                if (leftOutCamToggle == false) // About to enable left out camera
                {
                    Vector3 currentPos = leftOutCamera.transform.position;
                    if (!IsValidCameraPosition(currentPos))
                    {
                        Plugin.LogError($"[Instance {instanceId}] Camera position invalid before toggle: {currentPos}, reinitializing...");

                        // Try to reinitialize
                        if (localPlayer != null && localPlayer.Team != null)
                        {
                            InitializeLeftOutCamera(localPlayer.Team.Value);
                        }

                        // Check again
                        if (!IsValidCameraPosition(leftOutCamera.transform.position))
                        {
                            Plugin.LogError($"[Instance {instanceId}] Camera reinitialization failed, aborting toggle");
                            keyWasPressed = isKeyPressed;
                            return;
                        }
                    }
                }

                // Perform the toggle
                leftOutCamToggle = !leftOutCamToggle;

                string cameraState = leftOutCamToggle ? "ENABLED" : "DISABLED";
                Plugin.Log($"[Instance {instanceId}] Camera toggled: {cameraState}");

                if (leftOutCamToggle)
                {
                    Plugin.Log($"[Instance {instanceId}] Left out camera position: {leftOutCamera.transform.position}");
                }
            }

            // Update key state for next frame
            keyWasPressed = isKeyPressed;
        }

        /// <summary>
        /// Updates camera enabled states based on toggle with validation
        /// </summary>
        private static void UpdateCameraStates()
        {
            if (!IsLocalPlayerInstance()) return;

            if (leftOutCamera != null && playerCam != null)
            {
                // Only update if the state actually changes
                if (leftOutCamera.enabled != leftOutCamToggle)
                {
                    // If enabling left out camera, validate position one more time
                    if (leftOutCamToggle)
                    {
                        Vector3 currentPos = leftOutCamera.transform.position;
                        if (!IsValidCameraPosition(currentPos))
                        {
                            Plugin.LogError($"[Instance {instanceId}] Invalid position detected during camera enable: {currentPos}");

                            // Force re-position
                            if (localPlayer != null && localPlayer.Team != null)
                            {
                                Vector3 safePos = GetSafeCameraPosition(localPlayer.Team.Value);
                                leftOutCamera.transform.position = safePos;
                                Plugin.Log($"[Instance {instanceId}] Forced camera to safe position: {safePos}");
                            }
                        }
                    }

                    leftOutCamera.enabled = leftOutCamToggle;
                    playerCam.enabled = !leftOutCamToggle;

                    Plugin.Log($"[Instance {instanceId}] Camera states updated: LeftOut={leftOutCamToggle}, Player={!leftOutCamToggle}");

                    if (leftOutCamToggle)
                    {
                        Plugin.Log($"[Instance {instanceId}] Active camera position: {leftOutCamera.transform.position}");
                    }
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
            hasValidPosition = false;
            lastKnownGoodPosition = Vector3.zero;

            Plugin.Log($"[Instance {instanceId}] Camera system has been reset");
        }

        /// <summary>
        /// Handles events when a player changes teams
        /// </summary>
        private static void HandleTeamChanged(Player player, PlayerTeam oldTeam, PlayerTeam newTeam)
        {
            // Check if this is our local player
            if (localPlayer != null && player == localPlayer)
            {
                Plugin.Log($"[Instance {instanceId}] Local player team changed from {oldTeam} to {newTeam}");
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
                    // Skip if this is a dedicated server
                    if (Plugin.IsDedicatedServer()) return;

                    // Ensure we have player and manager references
                    EnsurePlayerReference();

                    // Skip processing if we're not handling the local player
                    if (localPlayer == null) return;

                    // Check if player exists and is a goalie
                    if (localPlayer.Role?.Value == PlayerRole.Goalie)
                    {
                        // Additional check to make sure we're only working with local player's camera
                        if (__instance != localPlayer.PlayerCamera) return;

                        // Ensure cameras exist
                        EnsurePlayerCameraExists();
                        EnsureLeftOutCameraExists();

                        // Check for input to toggle camera
                        HandleCameraToggleInput();

                        // Initialize camera if not already done
                        if (!initialized && localPlayer.Team?.Value != null)
                        {
                            InitializeLeftOutCamera(localPlayer.Team.Value);
                        }

                        // Update camera enabled states
                        UpdateCameraStates();
                    }
                }
                catch (Exception e)
                {
                    Plugin.LogError($"[Instance {instanceId}] Error in camera tick: " + e.Message);
                    Plugin.LogError($"[Instance {instanceId}] Stack trace: " + e.StackTrace);
                    ResetCameraSystem();
                }
            }
        }
    }
}