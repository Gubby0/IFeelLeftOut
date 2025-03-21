using System;
using System.Collections.Generic;
using HarmonyLib;

namespace IFeelLeftOut
{
    /// <summary>
    /// Centralized event system for game events that can be observed by other components
    /// </summary>
    public static class GameEvents
    {
        #region Team Changed Event

        /// <summary>
        /// Delegate for team changed events
        /// </summary>
        /// <param name="player">The player whose team changed</param>
        /// <param name="oldTeam">The team the player was on</param>
        /// <param name="newTeam">The team the player is now on</param>
        public delegate void TeamChangedEventHandler(Player player, PlayerTeam oldTeam, PlayerTeam newTeam);

        /// <summary>
        /// Event that fires when a player's team changes
        /// </summary>
        public static event TeamChangedEventHandler OnTeamChanged;

        /// <summary>
        /// Dictionary to track which players have had event handlers attached
        /// </summary>
        private static readonly Dictionary<Player, bool> teamChangedSubscribed = new Dictionary<Player, bool>();

        /// <summary>
        /// Subscribe to team changed events for a specific player
        /// </summary>
        /// <param name="player">The player to monitor</param>
        /// <param name="handler">The callback to invoke when team changes</param>
        public static void SubscribeToTeamChanged(Player player, TeamChangedEventHandler handler)
        {
            if (player == null)
            {
                Plugin.Log.LogWarning("Attempted to subscribe to team changed events for null player");
                return;
            }

            // Add the handler to our event
            OnTeamChanged += handler;

            // Track that we've subscribed to this player
            if (!teamChangedSubscribed.ContainsKey(player))
            {
                teamChangedSubscribed[player] = true;
                Plugin.Log.LogInfo($"Subscribed to team changed events for player {player.GetInstanceID()}");
            }
        }

        /// <summary>
        /// Unsubscribe from team changed events for a specific player
        /// </summary>
        /// <param name="player">The player to stop monitoring</param>
        /// <param name="handler">The callback to remove</param>
        public static void UnsubscribeFromTeamChanged(Player player, TeamChangedEventHandler handler)
        {
            // Remove the handler
            OnTeamChanged -= handler;

            Plugin.Log.LogInfo($"Unsubscribed from team changed events for player {(player != null ? player.GetInstanceID().ToString() : "null")}");
        }

        /// <summary>
        /// Harmony patch to intercept team changed events
        /// </summary>
        [HarmonyPatch(typeof(Player), nameof(Player.OnPlayerTeamChanged))]
        private class PlayerTeamChangedPatch
        {
            [HarmonyPostfix]
            static void Postfix(Player __instance, PlayerTeam oldTeam, PlayerTeam newTeam)
            {
                try
                {
                    // Invoke the event with the player instance and team values
                    OnTeamChanged?.Invoke(__instance, oldTeam, newTeam);

                    Plugin.Log.LogInfo($"Player {__instance.GetInstanceID()} team changed from {oldTeam} to {newTeam}");
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"Error in team changed event handler: {e.Message}\n{e.StackTrace}");
                }
            }
        }
        #endregion

        #region Role Changed Event

        /// <summary>
        /// Delegate for role changed events
        /// </summary>
        /// <param name="player">The player whose role changed</param>
        /// <param name="oldRole">The role the player was in</param>
        /// <param name="newRole">The role the player is now in</param>
        public delegate void RoleChangedEventHandler(Player player, PlayerRole oldRole, PlayerRole newRole);

        /// <summary>
        /// Event that fires when a player's role changes
        /// </summary>
        public static event RoleChangedEventHandler OnRoleChanged;

        /// <summary>
        /// Harmony patch to intercept role changed events
        /// </summary>
        [HarmonyPatch(typeof(Player), nameof(Player.OnPlayerRoleChanged))]
        private class PlayerRoleChangedPatch
        {
            [HarmonyPostfix]
            static void Postfix(Player __instance, PlayerRole oldRole, PlayerRole newRole)
            {
                try
                {
                    // Invoke the event with the player instance and role values
                    OnRoleChanged?.Invoke(__instance, oldRole, newRole);

                    Plugin.Log.LogInfo($"Player {__instance.GetInstanceID()} role changed from {oldRole} to {newRole}");
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"Error in role changed event handler: {e.Message}\n{e.StackTrace}");
                }
            }
        }
        #endregion

        #region Template for Adding New Event Types
        /*
        // Step 1: Define the delegate for the event
        public delegate void NewEventTypeHandler(Player player, OtherParam param1, AnotherParam param2);
        
        // Step 2: Define the event itself
        public static event NewEventTypeHandler OnNewEventType;
        
        // Step 3: Create subscription methods (if needed)
        public static void SubscribeToNewEventType(Player player, NewEventTypeHandler handler)
        {
            OnNewEventType += handler;
        }
        
        public static void UnsubscribeFromNewEventType(Player player, NewEventTypeHandler handler)
        {
            OnNewEventType -= handler;
        }
        
        // Step 4: Create the Harmony patch to capture the event
        [HarmonyPatch(typeof(TargetClass), nameof(TargetClass.MethodToIntercept))]
        private class NewEventTypePatch
        {
            [HarmonyPostfix] // or [HarmonyPrefix] depending on needs
            static void Postfix(TargetClass __instance, OtherParam param1, AnotherParam param2)
            {
                try
                {
                    // Invoke the event with appropriate parameters
                    OnNewEventType?.Invoke(__instance as Player, param1, param2);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"Error in new event type handler: {e.Message}");
                }
            }
        }
        */
        #endregion
    }
}