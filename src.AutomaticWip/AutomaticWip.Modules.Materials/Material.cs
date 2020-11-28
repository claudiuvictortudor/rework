namespace AutomaticWip.Modules.Materials
{
    public sealed class Material
    {
        /// <summary>
        /// The identifier of the material
        /// </summary>
        public string PART_NUMBER { get; set; }

        /// <summary>
        /// The description of the material
        /// </summary>
        public string PART_NAME { get; set; } = "N/A";

        /// <summary>
        /// The group of the material
        /// </summary>
        public string PART_GROUP { get; set; } = "N/A";
    }
}
