namespace Equinox.EnergyWeapons.Misc
{
    public interface ICoreRefComponent
    {
        void OnAddedToCore(EnergyWeaponsCore core);
        void OnBeforeRemovedFromCore();
    }
}
