using Hex.Auxiliary;
using Hex.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hex.Models
{
    /// <summary> A collection of <see cref="Hexagon"/> instances mapped by automatically oriented <see cref="Cube"/>. </summary>
    public class HexagonMap
    {
        #region Constructors

        public HexagonMap(IEnumerable<Hexagon> hexagons, int numberOfOrientations, Func<int> orientationGetter)
        {
            var selectors = Generate.Range(numberOfOrientations)
                .Select(index => new Func<Hexagon, (Cube Cube, int Orientation, Hexagon Hexagon)>(hex => (hex.Cubes[index], index, hex)))
                .ToArray();
            this.HexagonByOrientedCubeMap = hexagons
                .SelectMulti(selectors)
                .ToDictionary(x => (x.Cube, x.Orientation), x => x.Hexagon);
            this.NumberOfOrientations = numberOfOrientations;
            this.OrientationGetter = orientationGetter;
        }

        #endregion

        #region Members

        /// <summary> Gets the hexagon with the specfied cube in the current orientation, or <see langword="default"/> if one is not found. </summary>
        public Hexagon this[Cube cube] =>
            this.HexagonByOrientedCubeMap.TryGetValue((cube, this.OrientationGetter()), out var hexagon) ? hexagon : default;

        /// <summary> Gets a collection containing the hexagons in the map. </summary>
        public ICollection<Hexagon> Values => this.HexagonByOrientedCubeMap.Values;

        /// <summary> Gets the number of hexagons in the map. </summary>
        public int Count => this.HexagonByOrientedCubeMap.Count / this.NumberOfOrientations;

        protected IDictionary<(Cube, int), Hexagon> HexagonByOrientedCubeMap { get; }

        protected int NumberOfOrientations { get; }

        protected Func<int> OrientationGetter { get; }

        #endregion
    }
}