﻿using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Game;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace Equinox.Utils
{
    public class PlayerCollection : IDisposable
    {
        private class PlayerAuxData
        {
            public readonly ulong AssociatedSteamId;

            public PlayerAuxData(ulong steam)
            {
                AssociatedSteamId = steam;
            }

            public IMyPlayer RealPlayer;
            public readonly HashSet<IMyPlayer> Bots = new HashSet<IMyPlayer>();
        }

        private readonly List<long> _keysToRemove = new List<long>();
        private readonly Dictionary<long, PlayerAuxData> _playerById = new Dictionary<long, PlayerAuxData>();
        private readonly Dictionary<ulong, PlayerAuxData> _playerBySteamId = new Dictionary<ulong, PlayerAuxData>();

        public delegate void PlayerSingleKeyEvent(IMyPlayer player);

        public event PlayerSingleKeyEvent PlayerJoined;
        public event PlayerSingleKeyEvent PlayerLeft;

        public PlayerCollection()
        {
            MyVisualScriptLogicProvider.PlayerConnected += PlayerConnected;
            MyVisualScriptLogicProvider.PlayerDisconnected += PlayerDisconnected;
            Refresh();
            foreach (var p in _playerBySteamId.Values)
                if (p.RealPlayer != null)
                    PlayerJoined?.Invoke(p.RealPlayer);
        }

        private void PlayerDisconnected(long playerId)
        {
            MyAPIGateway.Utilities.InvokeOnGameThread(() =>
            {
                PlayerAuxData aux;
                if (_playerById.TryGetValue(playerId, out aux) && aux.RealPlayer != null)
                    PlayerLeft?.Invoke(aux.RealPlayer);
                Refresh();
            });
        }

        private void PlayerConnected(long playerId)
        {
            MyAPIGateway.Utilities.InvokeOnGameThread(() =>
            {
                Refresh();
                PlayerAuxData aux;
                if (_playerById.TryGetValue(playerId, out aux) && aux.RealPlayer != null)
                    PlayerJoined?.Invoke(aux.RealPlayer);
            });
        }

        private void Refresh()
        {
            foreach (var v in _playerById.Values)
            {
                v.RealPlayer = null;
                v.Bots.Clear();
            }
            MyAPIGateway.Players.GetPlayers(null, (x) =>
            {
                PlayerAuxData aux;
                if (!_playerById.TryGetValue(x.IdentityId, out aux))
                {
                    aux = new PlayerAuxData(x.SteamUserId);
                    _playerById.Add(x.IdentityId, aux);
                    _playerBySteamId.Add(x.SteamUserId, aux);
                }
                if (x.IsBot)
                    aux.Bots.Add(x);
                else
                    aux.RealPlayer = x;
                return false;
            });

            _keysToRemove.Clear();
            _keysToRemove.AddRange(_playerById.Where(x => x.Value.RealPlayer == null && x.Value.Bots.Count == 0)
                .Select(x => x.Key));
            foreach (var l in _keysToRemove)
            {
                _playerBySteamId.Remove(_playerById[l].AssociatedSteamId);
                _playerById.Remove(l);
            }
            _keysToRemove.Clear();
        }

        public IMyPlayer TryGetPlayerByIdentity(long identity)
        {
            return _playerById.GetValueOrDefault(identity)?.RealPlayer;
        }

        public IMyPlayer TryGetPlayerBySteamId(ulong steam)
        {
            return _playerBySteamId.GetValueOrDefault(steam)?.RealPlayer;
        }

        public void Dispose()
        {
            // ReSharper disable DelegateSubtraction
            MyVisualScriptLogicProvider.PlayerConnected -= PlayerConnected;
            MyVisualScriptLogicProvider.PlayerDisconnected -= PlayerDisconnected;
            // ReSharper restore DelegateSubtraction
        }
    }
}