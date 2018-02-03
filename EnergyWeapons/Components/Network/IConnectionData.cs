namespace Equinox.EnergyWeapons.Components.Network
{
    public interface IConnectionData
    {
        bool CanDissolve { get; }
        bool Bidirectional { get; }
    }
}
