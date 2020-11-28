namespace AutomaticWip.Core.Models
{
    /// <summary>
    /// Data model for materials
    /// </summary>
    public sealed class MATERIAL_DATA
    {
        /// <summary>
        /// Name of the material
        /// </summary>
        public string MATERIAL { get; set; } = Settings.NOT_AVAIBLE;

        /// <summary>
        /// Project/Group Name of the material
        /// </summary>
        public string PROJECT { get; set; } = Settings.NOT_AVAIBLE;

        /// <summary>
        /// Description of the material
        /// </summary>
        public string DESCRIPTION { get; set; } = Settings.NOT_AVAIBLE;

        /// <summary>
        /// Actual quantity of the material
        /// </summary>
        public int QUANTITY { get; set; } = 0;
    }
}
