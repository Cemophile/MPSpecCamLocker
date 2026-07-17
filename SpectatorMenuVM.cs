using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace MPSpecCamLocker
{
    public class SpectatorPlayerItemVM : ViewModel
    {
        private Agent _agent;
        private Action<Agent> _onSpectate;
        private HashSet<MissionPeer> _highlightedPeers;
        private string _name;

        private bool _isHighlighted = false;
        private string _rowColor = "#00000000";

        public Agent Agent => _agent;

        public SpectatorPlayerItemVM(Agent agent, Action<Agent> onSpectate, HashSet<MissionPeer> highlightedPeers)
        {
            _agent = agent;
            _onSpectate = onSpectate;
            _highlightedPeers = highlightedPeers;

            Name = (_agent.MissionPeer != null) ? _agent.MissionPeer.DisplayedName : _agent.Name;

            if (_agent.MissionPeer != null && _highlightedPeers.Contains(_agent.MissionPeer))
            {
                _isHighlighted = true;
                _rowColor = "#F1C40F88";
            }
        }

        [DataSourceProperty]
        public string Name
        {
            get => _name;
            set
            {
                if (value != _name)
                {
                    _name = value;
                    OnPropertyChangedWithValue(value, "Name");
                }
            }
        }

        [DataSourceProperty]
        public string RowColor
        {
            get => _rowColor;
            set
            {
                if (value != _rowColor)
                {
                    _rowColor = value;
                    OnPropertyChangedWithValue(value, "RowColor");
                }
            }
        }

        public bool IsValid()
        {
            return _agent != null && _agent.IsActive();
        }

        public void ExecuteSpectate()
        {
            if (IsValid() && _onSpectate != null)
            {
                _onSpectate(_agent);
            }
        }

        public void ExecuteToggleHighlight()
        {
            _isHighlighted = !_isHighlighted;
            RowColor = _isHighlighted ? "#F1C40F88" : "#00000000";

            if (_agent != null && _agent.MissionPeer != null)
            {
                if (_isHighlighted)
                {
                    _highlightedPeers.Add(_agent.MissionPeer);
                }
                else
                {
                    _highlightedPeers.Remove(_agent.MissionPeer);
                }
            }
        }
    }

    public class SpectatorMenuVM : ViewModel
    {
        private Action<Agent> _onSpectateAction;
        private MBBindingList<SpectatorPlayerItemVM> _team1Players;
        private MBBindingList<SpectatorPlayerItemVM> _team2Players;

        private HashSet<MissionPeer> _highlightedPeers;

        [DataSourceProperty]
        public MBBindingList<SpectatorPlayerItemVM> Team1Players
        {
            get => _team1Players;
            set
            {
                if (value != _team1Players)
                {
                    _team1Players = value;
                    OnPropertyChangedWithValue(value, "Team1Players");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<SpectatorPlayerItemVM> Team2Players
        {
            get => _team2Players;
            set
            {
                if (value != _team2Players)
                {
                    _team2Players = value;
                    OnPropertyChangedWithValue(value, "Team2Players");
                }
            }
        }

        public SpectatorMenuVM(Action<Agent> onSpectateAction)
        {
            _onSpectateAction = onSpectateAction;
            _highlightedPeers = new HashSet<MissionPeer>(); // Memory start
            Team1Players = new MBBindingList<SpectatorPlayerItemVM>();
            Team2Players = new MBBindingList<SpectatorPlayerItemVM>();
            RefreshPlayers();
        }

        public void RefreshPlayers()
        {
            if (Mission.Current == null) return;

            List<Agent> activeAgents = new List<Agent>();

            foreach (var peer in GameNetwork.NetworkPeers)
            {
                MissionPeer missionPeer = peer.GetComponent<MissionPeer>();
                if (missionPeer != null && missionPeer.Team != null &&
                    missionPeer.ControlledAgent != null && missionPeer.ControlledAgent.IsActive())
                {
                    activeAgents.Add(missionPeer.ControlledAgent);
                }
            }

            SyncTeam(Team1Players, activeAgents, team => team != null && team.IsAttacker);
            SyncTeam(Team2Players, activeAgents, team => team != null && team.IsDefender);
        }

        private void SyncTeam(MBBindingList<SpectatorPlayerItemVM> list, List<Agent> activeAgents, Func<Team, bool> teamCondition)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (!list[i].IsValid() || !activeAgents.Contains(list[i].Agent))
                {
                    list.RemoveAt(i);
                }
            }

            foreach (var agent in activeAgents)
            {
                if (teamCondition(agent.Team))
                {
                    bool exists = false;
                    foreach (var vm in list)
                    {
                        if (vm.Agent == agent)
                        {
                            exists = true;
                            break;
                        }
                    }

                    if (!exists)
                    {
                        list.Add(new SpectatorPlayerItemVM(agent, _onSpectateAction, _highlightedPeers));
                    }
                }
            }
        }
    }
}