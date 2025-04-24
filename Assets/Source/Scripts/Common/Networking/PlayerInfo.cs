using System;
using Fusion;

namespace TojGamesTask.Common.Networking
{
    public readonly struct PlayerInfo : IEquatable<PlayerInfo>
    {
        public PlayerRef Id { get; }
        public string Name { get; }

        public PlayerInfo(PlayerRef id, string name)
        {
            Id = id;
            Name = name;
        }
        public bool Equals(PlayerInfo other)
        {
            return Id.Equals(other.Id) && Name == other.Name;
        }
        
        public override bool Equals(object obj)
        {
            return obj is PlayerInfo other && Equals(other);
        }
        
        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Name);
        }
    }
}