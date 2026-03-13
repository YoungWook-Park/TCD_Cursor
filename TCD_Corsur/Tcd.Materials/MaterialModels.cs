using System;

namespace Tcd.Materials
{
    public enum MaterialKind
    {
        UpperFilm = 0,
        LowerFilm = 1,
        BondedProduct = 2,
    }

    public enum MaterialState
    {
        None = 0,
        Loaded = 1,
        InProcess = 2,
        Completed = 3,
        Scrapped = 4,
    }

    public enum MaterialLocation
    {
        None = 0,
        Stage1 = 1,
        Stage2 = 2,
        Robot = 3,
        UpperChamber = 4,
        LowerChamber = 5,
    }

    public sealed class Material
    {
        public Material(Guid id, MaterialKind kind, MaterialState state, MaterialLocation location)
        {
            Id = id;
            Kind = kind;
            State = state;
            Location = location;
        }

        public Guid Id { get; }
        public MaterialKind Kind { get; }
        public MaterialState State { get; }
        public MaterialLocation Location { get; }

        public Material With(MaterialState? state = null, MaterialLocation? location = null)
            => new Material(Id, Kind, state ?? State, location ?? Location);

        public override string ToString() => $"{Kind} {Id:N} [{State}] @ {Location}";
    }
}

