using System;

namespace AutomaticWip.Modules.Subsets.Models
{
    /// <summary>
    /// Wip subset model
    /// </summary>
    public sealed class Subset
    {
        /// <summary>
        /// Unit id
        /// </summary>
        public string UNIT_ID { get; set; }

        /// <summary>
        /// Unit id type
        /// </summary>
        public string UNIT_ID_TYPE { get; set; }

        /// <summary>
        /// Current job
        /// </summary>
        public string JOB { get; set; }

        /// <summary>
        /// Current process step
        /// </summary>
        public string PROCESS_STEP { get; set; }

        /// <summary>
        /// Current equipment/workcenter
        /// </summary>
        public string WORKCENTER { get; set; }

        /// <summary>
        /// Pass:Fail
        /// </summary>
        public string U_STATE { get; set; }

        /// <summary>
        /// Unit quantity
        /// </summary>
        public double QUANTITY { get; set; }

        /// <summary>
        /// Diff in days from today
        /// </summary>
        public int DIFF_DAYS { get; set; }

        /// <summary>
        /// Diff in hours on last day
        /// </summary>
        public int DIFF_HOURS { get; set; }

        /// <summary>
        /// Diff in minutes on last hour
        /// </summary>
        public int DIFF_MINUTES { get; set; }

        /// <summary>
        /// When this subset was created at current operation
        /// </summary>
        public DateTime CREATED { get; set; }

        public string OPERATOR { get; set; }

    }
}
