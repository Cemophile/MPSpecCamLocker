using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Engine;
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

        private Camera _customCamera;
        private float _customDistance = 3.2f;

        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();
            MissionScreen.SetCustomAgentListToSpectateGatherer(
                new MissionScreen.GatherCustomAgentListToSpectateDelegate(SpectatableAgents));
        }

        private bool IsObserving()
        {
            if (_cachedPeer == null)
                return true;
            return _cachedPeer.ControlledAgent == null || !_cachedPeer.ControlledAgent.IsActive();
        }

        private void SwitchSpectatedAgent(int direction)
        {
            List<Agent> agents = SpectatableAgents(null);
            if (agents == null || agents.Count == 0) return;

            Agent current = MissionScreen.LastFollowedAgent;
            int currentIndex = current != null ? agents.IndexOf(current) : -1;

            if (direction > 0)
            {
                currentIndex = (currentIndex + 1) % agents.Count;
            }
            else
            {
                currentIndex--;
                if (currentIndex < 0)
                    currentIndex = agents.Count - 1;
            }

            Agent nextAgent = agents[currentIndex];

            if (nextAgent != current)
            {
                
                PropertyInfo prop = typeof(MissionScreen).GetProperty("LastFollowedAgent", BindingFlags.Public | BindingFlags.Instance);
                if (prop != null)
                {
                    MethodInfo setter = prop.GetSetMethod(true);
                    setter?.Invoke(MissionScreen, new object[] { nextAgent });
                }
            }
        }

        public override void OnMissionScreenTick(float dt)
        {
            if (Mission.Current == null) return;

            if (_cachedPeer == null)
                _cachedPeer = GetMyMissionPeer();

            if (_cachedPeer != null && _cachedTeam != _cachedPeer.Team)
                _cachedTeam = _cachedPeer.Team;

            bool isObserving = IsObserving();

            // F10
            if (Input.IsKeyReleased(InputKey.F8) && isObserving)
            {
                _isPerspectiveLocked = !_isPerspectiveLocked;
                InformationManager.DisplayMessage(
                    new InformationMessage("CamLock: " + (_isPerspectiveLocked ? "ON" : "OFF"),
                        Color.ConvertStringToColor("#FFDDDDFF")));
            }

            // Change the current player your spectating 
            if (_isPerspectiveLocked && isObserving)
            {
                if (Input.IsKeyReleased(InputKey.LeftMouseButton))
                {
                    SwitchSpectatedAgent(1); // next player
                }
                else if (Input.IsKeyReleased(InputKey.RightMouseButton))
                {
                    SwitchSpectatedAgent(-1); // previous
                }
            }

            Agent targetAgent = MissionScreen.LastFollowedAgent;

            bool isTargetValid = targetAgent != null && targetAgent.IsActive() && targetAgent.AgentVisuals != null;

            if (!_isPerspectiveLocked || !isObserving || !isTargetValid)
            {
                if (MissionScreen.CustomCamera != null && MissionScreen.CustomCamera == _customCamera)
                {
                    MissionScreen.CustomCamera = null;
                }

                if (!isObserving && _isPerspectiveLocked)
                {
                    _isPerspectiveLocked = false;
                }
                return;
            }

            // Mouse scroll for zooming
            float mouseScroll = Input.GetDeltaMouseScroll();
            if (mouseScroll != 0f)
            {
                _customDistance -= (mouseScroll / 120f) * 0.4f;
                _customDistance = MBMath.ClampFloat(_customDistance, 1.5f, 6.0f);
            }

            // Custom camera values. Do not edit.
            if (_customCamera == null)
            {
                _customCamera = Camera.CreateCamera();
            }

            MatrixFrame matrixFrame = MatrixFrame.Identity;
            matrixFrame.rotation.RotateAboutSide(1.5707964f);

            Vec3 lookDir = targetAgent.LookDirection;
            matrixFrame.rotation.RotateAboutForward(lookDir.AsVec2.RotationInRadians);

            float clampedZ = TaleWorlds.Library.MathF.Clamp(lookDir.z, -1f, 1f);
            matrixFrame.rotation.RotateAboutSide(TaleWorlds.Library.MathF.Asin(clampedZ));

            Vec3 eyePos = targetAgent.VisualPosition + new Vec3(0f, 0f, targetAgent.GetEyeGlobalHeight());

            float heightOffset = 0.35f + (_customDistance * 0.05f);
            Vec3 camPos = eyePos - (lookDir * _customDistance) + (Vec3.Up * heightOffset);

            matrixFrame.origin = camPos;
            _customCamera.Frame = matrixFrame;

            _customCamera.SetFovVertical(0.785398f, 1.7777f, 0.1f, 1000f);

            MissionScreen.CustomCamera = _customCamera;
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
}