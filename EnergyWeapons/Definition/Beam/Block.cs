using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using Equinox.Utils.Logging;
using VRage.Game;

namespace Equinox.EnergyWeapons.Definition.Beam
{
    public class Block : IEnumerable<Component>
    {
        [XmlElement("Emitter", typeof(Emitter))]
        [XmlElement("Weapon", typeof(Weapon))]
        [XmlElement("Path", typeof(Path))]
        [XmlElement("Optics", typeof(Optics))]
        public List<Component> Components { get; set; }

        public MyDefinitionId Id { get; set; }

        public IEnumerator<Component> GetEnumerator()
        {
            return Components.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(Component comp)
        {
            if (Components == null)
                Components = new List<Component>();
            Components.Add(comp);
        }


        #region Integrity Check

        private struct DummyInfo
        {
            public readonly HashSet<Component> InputFor, OutputFor, InternalFor;

            private DummyInfo(int k)
            {
                InputFor = new HashSet<Component>();
                OutputFor = new HashSet<Component>();
                InternalFor = new HashSet<Component>();
            }

            public static DummyInfo Create()
            {
                return new DummyInfo(0);
            }
        }

        private List<string> _detectorsIn, _detectorsOut, _detectorsBidir;

        /// <summary>
        /// Detectors that can accept an input signal
        /// </summary>
        public IReadOnlyList<string> InputDetectors
        {
            get
            {
                if (_detectorsIn == null || _detectorsOut == null || _detectorsBidir == null)
                    DetermineDetectors();
                return _detectorsIn;
            }
        }

        /// <summary>
        /// Detectors that output a signal.
        /// </summary>
        public IReadOnlyList<string> OutputDetectors
        {
            get
            {
                if (_detectorsIn == null || _detectorsOut == null || _detectorsBidir==null)
                    DetermineDetectors();
                return _detectorsOut;
            }
        }

        /// <summary>
        /// Detectors that output a signal or accept an input.
        /// </summary>
        public IReadOnlyList<string> BidirectionalDetectors
        {
            get
            {
                if (_detectorsIn == null || _detectorsOut == null || _detectorsBidir == null)
                    DetermineDetectors();
                return _detectorsBidir;
            }
        }

        private void DetermineDetectors()
        {
            Dictionary<string, DummyInfo> data = new Dictionary<string, DummyInfo>();
            foreach (var c in Components)
            {
                foreach (var k in c.Inputs)
                {
                    DummyInfo tmp;
                    if (!data.TryGetValue(k, out tmp))
                        data.Add(k, tmp = DummyInfo.Create());
                    tmp.InputFor.Add(c);
                }

                foreach (var k in c.Outputs)
                {
                    DummyInfo tmp;
                    if (!data.TryGetValue(k, out tmp))
                        data.Add(k, tmp = DummyInfo.Create());
                    tmp.OutputFor.Add(c);
                }

                foreach (var k in c.Internal)
                {
                    DummyInfo tmp;
                    if (!data.TryGetValue(k, out tmp))
                        data.Add(k, tmp = DummyInfo.Create());
                    tmp.InternalFor.Add(c);
                }
            }

            _detectorsIn = new List<string>();
            _detectorsOut = new List<string>();
            _detectorsBidir = new List<string>();
            foreach (var kv in data)
            {
                var inCount = kv.Value.InputFor.Except(kv.Value.OutputFor).Count();
                var outCount = kv.Value.OutputFor.Except(kv.Value.InputFor).Count();

                if (kv.Value.InternalFor.Count > 0)
                {
                    if (kv.Value.InternalFor.Count > 1)
                        EnergyWeaponsCore.LoggerStatic?.Warning(
                            $"For {Id}, dummy {kv.Key}: Internal on more than 1 thing: {string.Join(", ", kv.Value.InternalFor)}");
                    if (kv.Value.OutputFor.Count > 0)
                        EnergyWeaponsCore.LoggerStatic?.Warning(
                            $"For {Id}, dummy {kv.Key}: Internal used as output: {string.Join(", ", kv.Value.OutputFor)}");
                    if (kv.Value.InputFor.Count > 0)
                        EnergyWeaponsCore.LoggerStatic?.Warning(
                            $"For {Id}, dummy {kv.Key}: Internal used as input: {string.Join(", ", kv.Value.InputFor)}");
                }

                if (inCount  > 1)
                {
                    EnergyWeaponsCore.LoggerStatic?.Warning(
                        $"For {Id}, dummy {kv.Key}: used as multiple inputs: {string.Join(", ", kv.Value.InputFor)}");
                }

                if (outCount > 1)
                {
                    EnergyWeaponsCore.LoggerStatic?.Warning(
                        $"For {Id}, dummy {kv.Key}: used as multiple outputs: {string.Join(", ", kv.Value.OutputFor)}");
                }

                if (outCount > 0 && inCount == 0)
                    _detectorsOut.Add(kv.Key);
                if (outCount==0 && inCount > 0)
                    _detectorsIn.Add(kv.Key);
                if (outCount == 0 && inCount == 0 && kv.Value.InputFor.Intersect(kv.Value.OutputFor).Count() > 0)
                    _detectorsBidir.Add(kv.Key);
            }
        }

        #endregion
    }
}