using System;
using static AutomaticWip.Contracts.DataTableExtensions;

namespace AutomaticWip.Modules.Stocks.Models
{
    /// <summary>
    /// Stock data from mes
    /// </summary>
    [DataModel]
    public sealed class Container
    {
        /// <summary>
        /// The material number of this stock
        /// </summary>
        public string MATERIAL { get; set; } = "N/A";

        /// <summary>
        /// Identifier of this container
        /// </summary>
        [MapTo(Value = "PART_ID")]
        public string UNIT_ID { get; set; } = "N/A";

        /// <summary>
        /// Identifier's type
        /// </summary>
        [MapTo(Value = "PART_ID_TYPE")]
        public string UNIT_ID_TYPE { get; set; } = "N/A";

        /// <summary>
        /// The quantity of this container
        /// </summary>
        public double QUANTITY { get; set; } = 0;

        /// <summary>
        /// Difference in days since created
        /// </summary>
        [MapTo(Value = "AGE_DAYS")]
        public int DIFF_DAYS { get; set; } = 0;

        /// <summary>
        /// Difference in hours from day
        /// </summary>
        [MapTo(Value = "AGE_HOURS")]
        public int DIFF_HOURS { get; set; } = 0;

        /// <summary>
        /// Difference in minutes from hour
        /// </summary>
        [MapTo(Value = "AGE_MINUTES")]
        public int DIFF_MINUTES { get; set; } = 0;

        /// <summary>
        /// When this container was created,
        /// </summary>
        public DateTime CREATED { get; set; }

        /// <summary>
        /// When this was sync from source server
        /// </summary>
        public DateTime SYNCHRONISATION_TIME { get; set; }
    }
}
