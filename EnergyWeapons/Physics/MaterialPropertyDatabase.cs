using System.Collections.Generic;
using System.Linq;
using Sandbox.Definitions;
using VRage.Game;
using VRage.ObjectBuilders;

namespace Equinox.EnergyWeapons.Physics
{
    public class MaterialPropertyDatabase
    {
        public static readonly MaterialProperties IronMaterial = MakeMaterialAtomic(typeof(MyObjectBuilder_Ingot),
            "Iron", density: 7874, thermalConductivity: 79e-3, meltPoint: 1811, boilPoint: 3134, s: 449e-3,
            molMass: 55.845, hFus: 13.8, hVap: 347);

        public static readonly MaterialProperties StoneMaterial = MakeMaterial(typeof(MyObjectBuilder_Ore), "Stone",
            density: 3000, thermalConductivity: 0.6e-3, meltPoint: 1473, boilPoint: 12e3, s: 0.84, hFus: 57,
            hVap: 2000);

        public static readonly MaterialProperties IceMaterial = MakeMaterial(typeof(MyObjectBuilder_Ore), "Ice",
            density: 917, thermalConductivity: 1.6e-3, meltPoint: 273, boilPoint: 373, s: 4.184, hFus: 334, hVap: 2260);

        private readonly Dictionary<MyDefinitionId, MaterialProperties> _props =
            new Dictionary<MyDefinitionId, MaterialProperties>(MyDefinitionId.Comparer);

        public MaterialPropertyDatabase()
        {
            AddDefaultMaterials();
            while (AddDerivedRecipes())
            {
            }

            AddBackup(MyDefinitionManager.Static.GetPhysicalItemDefinitions()
                .Where(x => x.Id.TypeId == typeof(MyObjectBuilder_Ore)).Select(x => x.Id));
            AddBackup(MyDefinitionManager.Static.GetPhysicalItemDefinitions()
                .Where(x => x.Id.TypeId == typeof(MyObjectBuilder_Ingot)).Select(x => x.Id));
            AddBackup(MyDefinitionManager.Static.GetPhysicalItemDefinitions()
                .Where(x => x.Id.TypeId == typeof(MyObjectBuilder_Component)).Select(x => x.Id));
            AddVoxelMaterials();
        }

        private MaterialProperties AddMaterial(MyObjectBuilderType type, string subtype, double density,
            double thermalConductivity, double meltPoint, double boilPoint, double s,
            double hFus, double hVap)
        {
            var prop = MakeMaterial(type, subtype, density, thermalConductivity, meltPoint, boilPoint, s, hFus, hVap);
            _props.Add(prop.Material, prop);
            return prop;
        }

        private static MaterialProperties MakeMaterial(MyObjectBuilderType type, string subtype,
            double density, double thermalConductivity,
            double meltPoint, double boilPoint, double s, double hFus, double hVap)
        {
            return new MaterialProperties(new MyDefinitionId(type, subtype), (float) density,
                (float) thermalConductivity, (float) meltPoint,
                (float) boilPoint, (float) s, (float) hFus, (float) hVap);
        }

        private static MaterialProperties MakeMaterialAtomic(MyObjectBuilderType type, string subtype, double density,
            double thermalConductivity, double meltPoint,
            double boilPoint, double s, double molMass, double hFus, double hVap)
        {
            var mass = molMass * 1e-3; // kg/mol
            return MakeMaterial(type, subtype, density, thermalConductivity, meltPoint, boilPoint, s, hFus / mass,
                hVap / mass);
        }

        private MaterialProperties AddMaterialAtomic(MyObjectBuilderType type, string subtype,
            double density, double thermalConductivity,
            double meltPoint, double boilPoint, double s, double molMass, double hFus, double hVap)
        {
            var mtl = MakeMaterialAtomic(type, subtype, density, thermalConductivity, meltPoint, boilPoint, s, molMass,
                hFus, hVap);
            _props.Add(mtl.Material, mtl);
            return mtl;
        }

        private bool AddDerivedRecipes()
        {
            var changed = false;
            foreach (var kv in MyDefinitionManager.Static.GetBlueprintDefinitions())
            {
                // Try 1:  Map inputs onto outputs
                if (kv.Prerequisites.All(x => _props.ContainsKey(x.Id))
                    && kv.Results.All(x => !_props.ContainsKey(x.Id)))
                {
                    float totalInputMass = kv.Prerequisites.Sum(x =>
                        (float) x.Amount * MyDefinitionManager.Static.GetPhysicalItemDefinition(x.Id).Mass);
                    MaterialProperties combinedProps =
                        new MaterialProperties(default(MyDefinitionId), 0, 0, 0, 0, 0, 0, 0);
                    foreach (var p in kv.Prerequisites)
                    {
                        var mass = (float) p.Amount * MyDefinitionManager.Static.GetPhysicalItemDefinition(p.Id).Mass;
                        var prop = _props[p.Id];
                        combinedProps =
                            MaterialProperties.LinearCombination(default(MyDefinitionId), combinedProps, 1, prop, mass);
                    }

                    combinedProps = combinedProps.Clone(default(MyDefinitionId), 1 / totalInputMass);

                    foreach (var r in kv.Results)
                        _props[r.Id] = combinedProps.Clone(r.Id);
                    changed = true;
                }

                // Try 2:  Outputs onto inputs
                if (kv.Results.All(x => _props.ContainsKey(x.Id))
                    && kv.Prerequisites.All(x => !_props.ContainsKey(x.Id)))
                {
                    float totalOutputMass = kv.Results.Sum(x =>
                        (float) x.Amount * MyDefinitionManager.Static.GetPhysicalItemDefinition(x.Id).Mass);
                    MaterialProperties combinedProps =
                        new MaterialProperties(default(MyDefinitionId), 0, 0, 0, 0, 0, 0, 0);
                    foreach (var p in kv.Results)
                    {
                        var mass = (float) p.Amount * MyDefinitionManager.Static.GetPhysicalItemDefinition(p.Id).Mass;
                        var prop = _props[p.Id];
                        combinedProps =
                            MaterialProperties.LinearCombination(default(MyDefinitionId), combinedProps, 1, prop, mass);
                    }

                    combinedProps = combinedProps.Clone(default(MyDefinitionId), 1 / totalOutputMass);

                    foreach (var r in kv.Prerequisites)
                        _props[r.Id] = combinedProps.Clone(r.Id);
                    changed = true;
                }
            }

            return changed;
        }

        private void AddVoxelMaterials()
        {
            foreach (var vox in MyDefinitionManager.Static.GetVoxelMaterialDefinitions())
            {
                if (_props.ContainsKey(vox.Id))
                    continue;
                if (string.IsNullOrWhiteSpace(vox.MinedOre))
                    continue;
                var minedMtl = new MyDefinitionId(typeof(MyObjectBuilder_Ore), vox.MinedOre);
                MyPhysicalItemDefinition minedDef;
                if (!MyDefinitionManager.Static.TryGetPhysicalItemDefinition(minedMtl, out minedDef))
                    continue;
                MaterialProperties prop;
                if (!_props.TryGetValue(minedDef.Id, out prop))
                    continue;
                _props.Add(vox.Id, prop.Clone(vox.Id, 1, vox.MinedOreRatio));
            }
        }

        private void AddDefaultMaterials()
        {
            // mostly guesses.  big guesses.
            _props.Add(StoneMaterial.Material, StoneMaterial);
            _props.Add(IceMaterial.Material, IceMaterial);

            // http://reference.wolfram.com/language/ref/ElementData.html
            _props.Add(IronMaterial.Material, IronMaterial);
            AddMaterialAtomic(typeof(MyObjectBuilder_Ingot), "Nickel", density: 8908, thermalConductivity: 91e-3,
                meltPoint: 1728, boilPoint: 3186, s: 445e-3, molMass: 158.6934, hFus: 7.2, hVap: 378);
            AddMaterialAtomic(typeof(MyObjectBuilder_Ingot), "Cobalt", density: 8900, thermalConductivity: 100e-3,
                meltPoint: 1768, boilPoint: 3200, s: 421e-3, molMass: 58.933, hFus: 16.2, hVap: 375);
            AddMaterialAtomic(typeof(MyObjectBuilder_Ingot), "Magnesium", density: 1738, thermalConductivity: 160e-3,
                meltPoint: 923, boilPoint: 1363, s: 1020e-3, molMass: 24.305, hFus: 8.7, hVap: 128);
            AddMaterialAtomic(typeof(MyObjectBuilder_Ingot), "Silicon", density: 2330, thermalConductivity: 150e-3,
                meltPoint: 1687, boilPoint: 3173, s: 710e-3, molMass: 28.085, hFus: 50.2, hVap: 359);
            AddMaterialAtomic(typeof(MyObjectBuilder_Ingot), "Silver", density: 10.49e3, thermalConductivity: 430e-3,
                meltPoint: 1234, boilPoint: 2435, s: 235e-3, molMass: 107.86, hFus: 11.3, hVap: 255);
            AddMaterialAtomic(typeof(MyObjectBuilder_Ingot), "Gold", density: 19.3e3, thermalConductivity: 320e-3,
                meltPoint: 1337, boilPoint: 3129, s: 129.1e-3, molMass: 197.97, hFus: 12.5, hVap: 330);
            AddMaterialAtomic(typeof(MyObjectBuilder_Ingot), "Platinum", density: 21.45e3, thermalConductivity: 71e-3,
                meltPoint: 2041, boilPoint: 4098, s: 133e-3, molMass: 195.1, hFus: 20, hVap: 490);
            AddMaterialAtomic(typeof(MyObjectBuilder_Ingot), "Uranium", density: 19.05e3, thermalConductivity: 27e-3,
                meltPoint: 1408, boilPoint: 4200, s: 116e-3, molMass: 238.03, hFus: 14, hVap: 420);

            {
                var scrapId = new MyDefinitionId(typeof(MyObjectBuilder_Ore), "Scrap");
                _props.Add(scrapId, IronMaterial.Clone(scrapId));
            }
        }

        private void AddBackup(IEnumerable<MyDefinitionId> ids)
        {
            var changed = false;
            foreach (var id in ids)
            {
                if (_props.ContainsKey(id))
                    continue;
                changed = true;
                _props.Add(id, IronMaterial.Clone(id));
            }

            if (changed)
                while (AddDerivedRecipes())
                {
                }
        }

        public MaterialProperties PropertiesOf(MyDefinitionId id, MaterialProperties defaultTemplate = null)
        {
            MaterialProperties res;
            if (!_props.TryGetValue(id, out res))
                _props[id] = res = (defaultTemplate ?? IronMaterial).Clone(id);
            return res;
        }
    }
}