namespace MagicPot.Backend.Models
{
    public class NewPotWithCoverModel : NewPotModel
    {
        /// <summary>
        /// Cover image as Data URL (data:image/jpeg;base64,...).
        /// </summary>
        public string? CoverImage { get; set; }
    }
}
