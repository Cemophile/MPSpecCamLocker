using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.Screens;

namespace MPSpecCamLocker
{
    [DefaultView]
    public class CameraLockerView : MissionView
    {
        private bool _isPerspectiveLocked;
        private MissionPeer _cachedPeer;
        private Team _cachedTeam;
        private object _originalCameraModeLogic;

        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();
            MissionScreen.SetCustomAgentListToSpectateGatherer(
                new MissionScreen.GatherCustomAgentListToSpectateDelegate(SpectatableAgents));
        }

        public override void OnMissionScreenTick(float dt)
        {
            if (Mission.Current == null) return;

            if (_cachedPeer == null)
                _cachedPeer = GetMyMissionPeer();

            if (_cachedPeer != null && _cachedTeam != _cachedPeer.Team)
                _cachedTeam = _cachedPeer.Team;

            if (_isPerspectiveLocked)
            {
                MissionScreen.SetFieldValue("_missionCameraModeLogic", null);
                MissionScreen.SetCameraLockState(true);
            }

            if (!Input.IsKeyReleased(InputKey.F10))
                return;

            if (!IsSpectator())
                return;

            _isPerspectiveLocked = !_isPerspectiveLocked;

            bool isFreeCam = MissionScreen.LastFollowedAgent == null;

            if (_isPerspectiveLocked)
            {
                var field = typeof(MissionScreen).GetField("_missionCameraModeLogic",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                _originalCameraModeLogic = field?.GetValue(MissionScreen);
                MissionScreen.SetFieldValue("_missionCameraModeLogic", null);

                if (!isFreeCam)
                    MissionScreen.SetCameraLockState(true);
            }
            else
            {
                MissionScreen.SetFieldValue("_missionCameraModeLogic", _originalCameraModeLogic);
                _originalCameraModeLogic = null;

                if (!isFreeCam)
                    MissionScreen.SetCameraLockState(false);
            }

            InformationManager.DisplayMessage(
                new InformationMessage("PerspectiveLock: " + (_isPerspectiveLocked ? "ON" : "OFF"),
                    Color.ConvertStringToColor("#FFDDDDFF")));
        }

        private MissionPeer GetMyMissionPeer()
        {
            foreach (NetworkCommunicator peer in GameNetwork.NetworkPeers)
            {
                if (peer.IsMine)
                    return peer.GetComponent<MissionPeer>();
            }
            return null;
        }

        private bool IsSpectator()
        {
            if (_cachedPeer == null) return true;
            return _cachedTeam == null ||
                   (!_cachedTeam.IsDefender && !_cachedTeam.IsAttacker);
        }

        private List<Agent> SpectatableAgents(Agent forcedAgentToInclude)
        {
            try
            {
                if (Mission.Current == null || Mission.Current.Agents == null)
                    return new List<Agent>();

                Agent[] snapshot;
                try { snapshot = Mission.Current.Agents.ToArray(); }
                catch { return new List<Agent>(); }

                List<Agent> agents = new List<Agent>();

                if (IsSpectator())
                {
                    foreach (var x in snapshot)
                        if (x != null && x.IsActive() && x.IsHuman)
                            agents.Add(x);
                }
                else
                {
                    Team myTeam = _cachedTeam ?? Agent.Main?.Team;

                    foreach (var x in snapshot)
                    {
                        if (x == null || !x.IsActive() || !x.IsHuman) continue;
                        if (myTeam == null || x.Team == myTeam)
                            agents.Add(x);
                    }
                }

                if (forcedAgentToInclude != null && !agents.Contains(forcedAgentToInclude))
                    agents.Add(forcedAgentToInclude);

                return agents;
            }
            catch
            {
                return new List<Agent>();
            }
        }
    }

    public static class MissionScreenExtensions
    {
        public static void SetFieldValue(this MissionScreen screen, string fieldName, object value)
        {
            var field = typeof(MissionScreen).GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            field?.SetValue(screen, value);
        }
    }
}