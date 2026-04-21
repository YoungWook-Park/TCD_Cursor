using System;
using System.Collections.Generic;

namespace Tcd.Materials
{
    public interface IMaterialTracker
    {
        bool IsOccupied(MaterialLocation location);
        Material Get(MaterialLocation location);
        IReadOnlyDictionary<MaterialLocation, Material> Snapshot();

        void Place(Material material, MaterialLocation location);
        Material Remove(MaterialLocation location);
        void Move(MaterialLocation from, MaterialLocation to);
        void Clear();
    }

    public sealed class InMemoryMaterialTracker : IMaterialTracker
    {
        private readonly object _gate = new object();
        private readonly Dictionary<MaterialLocation, Material> _byLocation = new Dictionary<MaterialLocation, Material>();

        public bool IsOccupied(MaterialLocation location)
        {
            lock (_gate) return _byLocation.ContainsKey(location);
        }

        public Material Get(MaterialLocation location)
        {
            lock (_gate)
            {
                Material m;
                return _byLocation.TryGetValue(location, out m) ? m : null;
            }
        }

        public IReadOnlyDictionary<MaterialLocation, Material> Snapshot()
        {
            lock (_gate) return new Dictionary<MaterialLocation, Material>(_byLocation);
        }

        public void Place(Material material, MaterialLocation location)
        {
            if (material == null) throw new ArgumentNullException(nameof(material));
            if (location == MaterialLocation.None) throw new ArgumentException("Invalid location.", nameof(location));

            lock (_gate)
            {
                if (_byLocation.ContainsKey(location))
                    throw new InvalidOperationException($"Location already occupied: {location}");
                _byLocation[location] = material.With(location: location);
            }
        }

        public Material Remove(MaterialLocation location)
        {
            lock (_gate)
            {
                Material m;
                if (!_byLocation.TryGetValue(location, out m)) return null;
                _byLocation.Remove(location);
                return m.With(location: MaterialLocation.None);
            }
        }

        public void Move(MaterialLocation from, MaterialLocation to)
        {
            if (from == MaterialLocation.None || to == MaterialLocation.None)
                throw new ArgumentException("Invalid move location.");

            lock (_gate)
            {
                Material m;
                if (!_byLocation.TryGetValue(from, out m))
                    throw new InvalidOperationException($"No material at {from}");
                if (_byLocation.ContainsKey(to))
                    throw new InvalidOperationException($"Location already occupied: {to}");
                _byLocation.Remove(from);
                _byLocation[to] = m.With(location: to);
            }
        }

        public void Clear()
        {
            lock (_gate) _byLocation.Clear();
        }
    }
}
